using System;
using System.Threading.Tasks;
using Android.Util;
using Grpc.Core;

namespace TooGoodToGoNotifierAndroidApp
{
    internal class GrpcProductsMonitor
    {
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
                    while (await call.ResponseStream.MoveNext())
                    {
                        Log.Debug(Constants.AppName, $"{nameof(GrpcProductsMonitor)} StartMonitoring " +
                                                     $"Product ID = {call.ResponseStream.Current.Id}" +
                                                     $"Price = {call.ResponseStream.Current.Price}");
                    }
                }
                catch (Exception e)
                {
                    Log.Error(Constants.AppName, $"{nameof(GrpcProductsMonitor)} StartMonitoring error while reading channel {e}");
                    throw;
                }
            });
        }
    }
}