﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Android.Util;
using Grpc.Core;
using Xamarin.Essentials;

namespace TooGoodToGoNotifierAndroidApp
{
    [SuppressMessage("ReSharper", "MethodSupportsCancellation")]
    [SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Global")]
    internal class GrpcProductsMonitor
    {
        #region Private fields

        private CancellationToken _cancellationToken;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly ProductsManager.ProductsManagerClient _productsManagerClient;
        private readonly object _channelLock;
        private bool _monitoringStarted;
        private static readonly TimeSpan ChannelRetryInterval = TimeSpan.FromSeconds(90);

        #endregion

        #region Initialization

        public GrpcProductsMonitor()
        {
            Log.Debug(Constants.AppName, $"{nameof(GrpcProductsMonitor)} constructor");

            var channel = new Channel("too-good-to-go-cloud-notifier.jordangottardo.com", 50051, new SslCredentials());
            _productsManagerClient = new ProductsManager.ProductsManagerClient(channel);
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
                        await StartProductsMonitoring();
                    }
                    catch (Exception e)
                    {
                        Log.Error(Constants.AppName, $"{nameof(GrpcProductsMonitor)} Error while reading channel {e}. Restarting monitoring");
                        
                        _cancellationTokenSource.Cancel();
                        
                        await Task.Delay((ChannelRetryInterval));
                        _monitoringStarted = false;
                        StartMonitoring();
                    }
                });
            }
        }

        public void StopMonitoring()
        {
            lock (_channelLock)
            {
                Log.Debug(Constants.AppName, $"{nameof(GrpcProductsMonitor)} StopMonitoring");

                _monitoringStarted = false;
                _cancellationTokenSource.Cancel();
            }
        }

        #region Utility Methods

        private async Task StartProductsMonitoring()
        {
            var request = await GetProductRequestOrFailAsync();
            using var call = _productsManagerClient.GetProducts(request);

            Log.Debug(Constants.AppName, $"{nameof(GrpcProductsMonitor)} getting products");

            while (await call.ResponseStream.MoveNext(_cancellationToken))
            {
                var serverMessage = call.ResponseStream.Current;
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

        private static async Task<ProductRequest> GetProductRequestOrFailAsync()
        {
            var username = await SecureStorage.GetAsync("username");
            var password = await SecureStorage.GetAsync("password");

            if (username is null || password is null)
            {
                throw new ArgumentException("Username or password has not been set");
            }

            var request = AProductRequestForUser(
                username,
                password);
            return request;
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

        private static ProductRequest AProductRequestForUser(string username, string password)
        {
            var request = new ProductRequest
            {
                Username = username,
                Password = password
            };
            return request;
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