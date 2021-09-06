using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Android.Util;
using Grpc.Core;
using Xamarin.Essentials;
using Timer = System.Timers.Timer;

namespace TooGoodToGoNotifierAndroidApp
{
    [SuppressMessage("ReSharper", "MethodSupportsCancellation")]
    [SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Global")]
    internal class GrpcProductsMonitor
    {
        #region Private fields

        private static readonly TimeSpan ChannelRetryInterval = TimeSpan.FromSeconds(90);
        private CancellationToken _cancellationToken;
        private CancellationTokenSource _cancellationTokenSource;
        private ProductsManager.ProductsManagerClient _productsManagerClient;
        private readonly object _channelLock;
        private bool _monitoringStarted;
        private IClientStreamWriter<ProductClientMessage> _requestStream;
        private Timer _keepAliveTimer;
        private Channel _channel;

        #endregion

        #region Initialization

        public GrpcProductsMonitor()
        {
            Log.Debug(Constants.AppName, $"{nameof(GrpcProductsMonitor)} constructor");

            _channelLock = new object();
        }

        #endregion

        #region Events

        public event EventHandler<ProductResponseEventArgs> NewProductAvailable;

        #endregion

        public void StartMonitoring()
        {
            lock (_channelLock)
            {
                Log.Debug(Constants.AppName, $"{nameof(GrpcProductsMonitor)} running StartMonitoring");

                if (_monitoringStarted)
                {
                    Log.Debug(Constants.AppName, $"{nameof(GrpcProductsMonitor)} monitoring has already started. Returning");
                    return;
                }

                _monitoringStarted = true;
                _cancellationTokenSource = new CancellationTokenSource();
                _cancellationToken = _cancellationTokenSource.Token;

                Task.Run(async () =>
                {
                    try
                    {
                        if (await StopMonitoringHasBeenRequested())
                        {
                            Log.Error(Constants.AppName, $"{nameof(GrpcProductsMonitor)} Monitoring has not started. Press the StartMonitoring button in the settings page");
                            
                            return;
                        }

                        await CreateChannelIfNecessaryAsync();
                        await StartProductsMonitoringAsync();
                    }
                    catch (Exception e)
                    {
                        Log.Error(Constants.AppName, $"{nameof(GrpcProductsMonitor)} Error while reading channel {e}. Restarting monitoring");

                        _cancellationTokenSource.Cancel();
                        _keepAliveTimer?.Stop();

                        await Task.Delay(ChannelRetryInterval);
                        lock (_channelLock)
                        {
                            if (_monitoringStarted)
                            {
                                _monitoringStarted = false;
                                StartMonitoring();
                            }
                        }
                    }
                });
            }
        }

        private static async Task<bool> StopMonitoringHasBeenRequested()
        {
            var stopMonitoringValue = await GetFromSecureStorage("stopMonitoring");

            return stopMonitoringValue is null || bool.Parse(stopMonitoringValue);
        }

        public void StopMonitoring()
        {
            lock (_channelLock)
            {
                Log.Debug(Constants.AppName, $"{nameof(GrpcProductsMonitor)} StopMonitoring");

                _monitoringStarted = false;
                _cancellationTokenSource?.Cancel();
                _keepAliveTimer?.Stop();
            }
        }

        #region Utility Methods

        private async Task StartProductsMonitoringAsync()
        {
            await EnsureProductMonitoringIsStarted();

            using var duplexStream = _productsManagerClient.GetProducts();
            _requestStream = duplexStream.RequestStream;
            await SendGetProductsRequestAndKeepalivesAsync();

            Log.Debug(Constants.AppName, $"{nameof(GrpcProductsMonitor)} getting products");

            while (await duplexStream.ResponseStream.MoveNext(_cancellationToken))
            {
                var serverMessage = duplexStream.ResponseStream.Current;
                if (serverMessage.MessageCase == ProductServerMessage.MessageOneofCase.KeepAlive)
                {
                    Log.Debug(Constants.AppName, $"{nameof(GrpcProductsMonitor)} keep alive received");
                }
                else
                {
                    var productResponse = serverMessage.ProductResponse;
                    Log.Debug(Constants.AppName, $"{nameof(GrpcProductsMonitor)} " +
                                                 $"Product ID = {productResponse.Id}" +
                                                 $"Price = {productResponse.Price}");

                    OnNewProductAvailable(ToProductResponseEventArgs(productResponse));
                }
            }
        }

        private async Task EnsureProductMonitoringIsStarted()
        {
            Log.Debug(Constants.AppName, $"{nameof(GrpcProductsMonitor)} ensuring product monitoring is started");

            var username = await GetFromSecureStorage("username");
            var password = await GetFromSecureStorage("password");

            var productMonitorRequest = CreateProductMonitorRequest(username, password);

            try
            {
                _productsManagerClient.StartMonitoring(productMonitorRequest);
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.AlreadyExists)
            {
            }
        }

        private static ProductMonitoringRequest CreateProductMonitorRequest(string username, string password)
        {
            return new ProductMonitoringRequest
            {
                Username = username,
                Password = password
            };
        }

        private async Task CreateChannelIfNecessaryAsync()
        {
            if (_channel is null)
            {
                var channelUrlAndPort = await GetFromSecureStorage("channelUrl");

                if (IsNullOrWhitespace(channelUrlAndPort))
                {
                    throw new ArgumentException("Channel URL has not been set");
                }
                Log.Debug(Constants.AppName, $"{nameof(GrpcProductsMonitor)} channel url {channelUrlAndPort}");

                _channel = new Channel(channelUrlAndPort, new SslCredentials());
                _productsManagerClient = new ProductsManager.ProductsManagerClient(_channel);
            }
        }

        private async Task SendGetProductsRequestAndKeepalivesAsync()
        {
            var startGettingProductsRequest = await GetProductRequestOrFailAsync();
            await _requestStream.WriteAsync(startGettingProductsRequest);

            _keepAliveTimer = new Timer(TimeSpan.FromSeconds(20).TotalMilliseconds)
            {
                AutoReset = true
            };
            _keepAliveTimer.Elapsed += KeepAliveTimerOnElapsed;
            _keepAliveTimer.Start();
        }

        private void KeepAliveTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            Log.Debug(Constants.AppName, $"{nameof(GrpcProductsMonitor)} sending keep alive at {e.SignalTime}");

            try
            {
                _requestStream.WriteAsync(AKeepAliveMessage());
            }
            catch (Exception ex)
            {
                Log.Error(Constants.AppName, $"{nameof(GrpcProductsMonitor)} An error occurred while writing keep alive {ex}");
            }
        }

        private static ProductClientMessage AKeepAliveMessage()
        {
            var keepAlive = new KeepAlive();
            var clientMessage = new ProductClientMessage
            {
                KeepAlive = keepAlive
            };

            return clientMessage;
        }

        private static async Task<ProductClientMessage> GetProductRequestOrFailAsync()
        {
            var username = await GetFromSecureStorage("username");

            if (IsNullOrWhitespace(username))
            {
                throw new ArgumentException("Username or password has not been set");
            }

            var productRequest = AProductRequestForUser(username);

            return new ProductClientMessage
            {
                ProductRequest = productRequest
            };
        }

        private static ProductResponseEventArgs ToProductResponseEventArgs(ProductResponse productResponse)
        {
            return new ProductResponseEventArgs
            {
                Id = productResponse.Id,
                Price = GetPrice(productResponse),
                StoreName = productResponse.Store.Name
            };
        }

        private static decimal GetPrice(ProductResponse productResponse)
        {
            var price = productResponse.Price.ToString();
            return decimal.Parse(price.Insert(price.Length - productResponse.Decimals, "."));
        }

        protected virtual void OnNewProductAvailable(ProductResponseEventArgs e)
        {
            NewProductAvailable?.Invoke(this, e);
        }

        private static ProductRequest AProductRequestForUser(string username)
        {
            var request = new ProductRequest
            {
                Username = username,
            };
            return request;
        }

        private static bool IsNullOrWhitespace(string s)
        {
            return string.IsNullOrWhiteSpace(s);
        }

        private static async Task<string> GetFromSecureStorage(string key)
        {
            return await SecureStorage.GetAsync(key);
        }

        #endregion
    }

    #region Event Args

    internal class ProductResponseEventArgs : EventArgs
    {
        public string Id { get; set; }
        public decimal Price { get; set; }
        public string StoreName { get; set; }
    }

    #endregion
}