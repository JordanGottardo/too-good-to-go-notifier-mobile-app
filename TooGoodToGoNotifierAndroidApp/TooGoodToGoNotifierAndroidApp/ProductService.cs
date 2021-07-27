using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using AndroidX.Core.App;

namespace TooGoodToGoNotifierAndroidApp
{
    [Service]
    internal class ProductService: Service
    {
        #region Private fields

        private GrpcProductsMonitor _productsMonitor;
        private const int ForegroundNotificationId = 123;

        #endregion

        public override IBinder OnBind(Intent intent)
        {
            Log.Debug(Constants.AppName, "OnBind");
            return null;
        }

        public override void OnCreate()
        {
            base.OnCreate();

            Log.Debug(Constants.AppName, "OnCreate");

            _productsMonitor = new GrpcProductsMonitor();
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            Log.Debug(Constants.AppName, "OnStartCommand");

            CreateNotificationAndStartForegroundService();

            _productsMonitor.NewProductAvailable += ProductsMonitorOnNewProductAvailable;
            try
            {
                _productsMonitor.StartMonitoring();
            }
            catch (Exception e)
            {
                Log.Error(Constants.AppName, $"{nameof(ProductService)} An error occurred while starting monitoring {e}");
                throw;
            }

            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            Log.Debug(Constants.AppName, "OnDestroy");

            _productsMonitor.NewProductAvailable -= ProductsMonitorOnNewProductAvailable;
            _productsMonitor.StopMonitoring();
            StopForeground(true);

            base.OnDestroy();
        }

        #region Utility Methods

        private void ProductsMonitorOnNewProductAvailable(object sender, ProductResponseEventArgs e)
        {
            Log.Debug(Constants.AppName, "ProductService: received new product available");

            var notificationBuilder = new NotificationCompat.Builder(this, Constants.NewProductNotificationChannelId)
                .SetSmallIcon(Resource.Drawable.notification_bg)
                .SetContentTitle("New product available")
                .SetContentText($"Item {e.Id} available at € {e.Price} in {e.StoreName} store");

            var notificationManager = NotificationManagerCompat.From(this);
            notificationManager.Notify(int.Parse(e.Id), notificationBuilder.Build());
        }

        private void CreateNotificationAndStartForegroundService()
        {
            var notification = new NotificationCompat.Builder(this, Constants.ForegroundServiceNotificationChannelId)
                .SetSmallIcon(Resource.Drawable.notification_bg)
                .SetContentTitle("Foreground service")
                .SetContentText($"Foreground service")
                .SetOngoing(true)
                .Build();

            StartForeground(ForegroundNotificationId, notification);
        }

        #endregion
    }
}