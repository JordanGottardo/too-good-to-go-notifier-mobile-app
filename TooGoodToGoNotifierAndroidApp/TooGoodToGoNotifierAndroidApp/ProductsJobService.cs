using System;
using System.Threading.Tasks;
using Android.App;
using Android.App.Job;
using Android.Util;
using AndroidX.Core.App;
using AndroidX.RecyclerView.Widget;

namespace TooGoodToGoNotifierAndroidApp
{
    [Service(Name = "com.companyname.toogoodtogonotifierandroidapp.productsJobService",
        Permission = "android.permission.BIND_JOB_SERVICE")]
    internal class ProductsJobService : JobService
    {
        #region Private fields

        private GrpcProductsMonitor _productsMonitor;

        #endregion

        public override bool OnStartJob(JobParameters @params)
        {
            Log.Debug(Constants.AppName, "ProductsJobService OnStartJob");

            Task.Run(() =>
            {
                _productsMonitor = new GrpcProductsMonitor();
                _productsMonitor.NewProductAvailable += ProductsMonitorOnNewProductAvailable;
                try
                {
                    _productsMonitor.StartMonitoring();
                    //job finished?
                }
                catch (Exception e)
                {
                    Log.Error(Constants.AppName, $"{nameof(ProductService)} An error occurred while starting monitoring {e}");
                    throw;
                }
            });

            return true;
        }
        
        public override bool OnStopJob(JobParameters @params)
        {
            Log.Debug(Constants.AppName, "ProductsJobService OnStopJob");

            _productsMonitor.NewProductAvailable -= ProductsMonitorOnNewProductAvailable;
            _productsMonitor.StopMonitoring();
            
            return true;
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

        #endregion
    }
}