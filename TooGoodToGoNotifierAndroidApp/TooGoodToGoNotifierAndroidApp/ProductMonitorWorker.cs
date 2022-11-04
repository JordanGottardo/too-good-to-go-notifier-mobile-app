using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Android.Content;
using Android.Util;
using AndroidX.Core.App;
using AndroidX.Work;
using TooGoodToGoNotifierAndroidApp.Dtos;
using Xamarin.Essentials;

namespace TooGoodToGoNotifierAndroidApp
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    internal class ProductMonitorWorker: Worker
    {
        private const string StopMonitoringKey = "stopMonitoring";
        private readonly Context _context;
        private readonly ProductsClient _productsClient;

        public ProductMonitorWorker(Context context, WorkerParameters workerParams) : base(context, workerParams)
        {
            Log.Debug(Constants.AppName, "ProductMonitorWorker Constructor");
            _context = context;
            _productsClient = new ProductsClient();
        }

        public override Result DoWork()
        {
            try
            {
                Log.Debug(Constants.AppName, "ProductMonitorWorker DoWork");

                var stopMonitoringValue = SecureStorage.GetAsync(StopMonitoringKey).Result;

                var stopMonitoring = stopMonitoringValue is null ? false : bool.Parse(stopMonitoringValue);

                if (stopMonitoring)
                {
                    Log.Debug(Constants.AppName, "ProductMonitorWorker: monitoring is stopped. Not retrieving products");

                    return Result.InvokeSuccess();
                }

                Log.Debug(Constants.AppName, "ProductMonitorWorker: monitoring is started. Retrieving products");

                var availableProducts = _productsClient.GetAvailableProducts().Result.ToList();

                Log.Debug(Constants.AppName, $"ProductMonitorWorker: retrieved {availableProducts.Count} products");

                foreach (var availableProduct in availableProducts)
                {
                    DisplayAvailableProduct(availableProduct);
                }

                return Result.InvokeSuccess();
            }
            catch (Exception e)
            {
                Log.Error(Constants.AppName, $"ProductMonitorWorker: an error occurred while executing DoWork due to {e}");

                return Result.InvokeFailure();
            }

        }

        #region Utility Methods
        
        private void DisplayAvailableProduct(ProductDto product)
        {
            Log.Debug(Constants.AppName, "ProductMonitorWorker: displaying new product");
        
            var notificationBuilder = new NotificationCompat.Builder(_context, Constants.NewProductNotificationChannelId)
                .SetSmallIcon(Resource.Drawable.notification_icon)
                .SetContentTitle(_context.GetString(Resource.String.new_product_available_title))
                .SetContentText(_context.GetString(Resource.String.new_product_available_text, product.Price.ToString(CultureInfo.InvariantCulture), product.StoreName));
        
            var notificationManager = NotificationManagerCompat.From(_context);
            notificationManager.Notify(int.Parse(product.ProductId), notificationBuilder.Build());
        }
        
        #endregion
    }
}