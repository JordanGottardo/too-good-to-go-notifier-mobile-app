using System;
using System.Threading;
using System.Threading.Tasks;
using Android.Util;
using Grpc.Core;

namespace TooGoodToGoNotifierAndroidApp
{
    internal class GrpcProductsMonitor
    {
        #region Private fields

        private readonly CancellationToken _cancellationToken;
        private readonly CancellationTokenSource _cancellationTokenSource;

        #endregion

        #region Initialization

        public GrpcProductsMonitor()
        {
            Log.Debug(Constants.AppName, $"{nameof(GrpcProductsMonitor)} constructor");

            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
        }

        #endregion

        #region Events

        public event EventHandler<ProductResponseEventArgs> NewProductAvailable;

        #endregion

        public void StartMonitoring()
        {
            Log.Debug(Constants.AppName, $"{nameof(GrpcProductsMonitor)} running StartMonitoring");

            Task.Run(async () =>
            {
                try
                {
                    var channel = new Channel("too-good-to-go-cloud-notifier.jordangottardo.com", 50051, new SslCredentials());
                    var client = new ProductsManager.ProductsManagerClient(channel);
                    var request = new ProductRequest
                    {
                        User = "User1"
                    };
                    using var call = client.GetProducts(request);

                    while (await call.ResponseStream.MoveNext(_cancellationToken))
                    {
                        var serverMessage = call.ResponseStream.Current;
                        if (serverMessage.MessageCase == ProductServerMessage.MessageOneofCase.KeepAlive)
                        {
                            Log.Debug(Constants.AppName, $"{nameof(GrpcProductsMonitor)} StartMonitoring keep alive received");
                        }
                        else
                        {
                            var productResponse = serverMessage.ProductResponse;
                            Log.Debug(Constants.AppName, $"{nameof(GrpcProductsMonitor)} StartMonitoring " +
                                                         $"Product ID = {productResponse.Id}" +
                                                         $"Price = {productResponse.Price}");

                            OnNewProductAvailable(ToProductResponseEventArgs(productResponse));
                        }
                    }
                }
                catch (RpcException e)
                {
                    Log.Error(Constants.AppName, $"{nameof(GrpcProductsMonitor)} RpcError while reading channel {e}");
                }
                catch (Exception e)
                {
                    Log.Error(Constants.AppName, $"{nameof(GrpcProductsMonitor)} Unknown error while reading channel {e}");
                    throw;
                }
            });
        }

        public void StopMonitoring()
        {
            Log.Debug(Constants.AppName, $"{nameof(GrpcProductsMonitor)} StopMonitoring");

            _cancellationTokenSource.Cancel();
        }

        #region Utility Methods

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