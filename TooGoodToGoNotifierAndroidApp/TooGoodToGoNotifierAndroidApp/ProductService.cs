using System;
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
            Log.Debug(Constants.AppName, "OnStartCommands");

            _productsMonitor.NewProductAvailable += ProductsMonitorOnNewProductAvailable;
            _productsMonitor.StartMonitoring();

            return StartCommandResult.Sticky;
        }

        #region Utility Methods

        private void ProductsMonitorOnNewProductAvailable(object sender, ProductResponseEventArgs e)
        {
            Log.Debug(Constants.AppName, "ProductService: received new product available");

            var notificationBuilder = new NotificationCompat.Builder(this, Constants.ChannelId)
                .SetSmallIcon(Resource.Drawable.notification_bg)
                //.SetContentTitle(Resources.GetString("Resource.String.notification_content_title"))
                .SetContentTitle("New product available")
                .SetContentText($"Item {e.Id} available at € {e.Price} in {e.StoreName} store");

            var notificationManager = NotificationManagerCompat.From(this);
            notificationManager.Notify(int.Parse(e.Id), notificationBuilder.Build());
        }

        private void GetPrice(ProductResponseEventArgs args)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}