using System;
using System.Threading.Tasks;
using Android.Content;
using Android.Runtime;
using Android.Util;
using AndroidX.Core.App;
using AndroidX.Work;

namespace TooGoodToGoNotifierAndroidApp
{
    internal class ProductMonitorWorker: Worker
    {
        private readonly Context _context;
        private readonly GrpcProductsMonitor _productsMonitor;

        public ProductMonitorWorker(Context context, WorkerParameters workerParams) : base(context, workerParams)
        {
            Log.Debug(Constants.AppName, "ProductMonitorWorker Constructor");

            _context = context;
            _productsMonitor = new GrpcProductsMonitor();

            CreateTestNotification();
        }

        public override Result DoWork()
        {
            Log.Debug(Constants.AppName, "ProductMonitorWorker DoWork");

            try
            {
                Task.Run(async () =>
                {
                    _productsMonitor.NewProductAvailable += ProductsMonitorOnNewProductAvailable;
                    _productsMonitor.StartMonitoring();
                    Log.Debug(Constants.AppName, "ProductMonitorWorker Monitoring started. Waiting 9 mins");

                    await Task.Delay(TimeSpan.FromMinutes(9));

                    Log.Debug(Constants.AppName, "ProductMonitorWorker Monitoring started. Waited 9 mins");
                    _productsMonitor.StopMonitoring();

                    Log.Debug(Constants.AppName, "ProductMonitorWorker Monitoring stopped");
                }).Wait();
            }
            catch (Exception e)
            {
                Log.Error(Constants.AppName, $"{nameof(ProductService)} An error occurred while starting monitoring {e}");
                return Result.InvokeFailure();
            }
            finally
            {
                _productsMonitor.NewProductAvailable -= ProductsMonitorOnNewProductAvailable;
            }

            //CreateTestNotification();

            return Result.InvokeSuccess();
        }

        #region Utility Methods

        private void CreateTestNotification()
        {
            var notificationBuilder = new NotificationCompat.Builder(_context, Constants.NewProductNotificationChannelId)
                .SetSmallIcon(Resource.Drawable.notification_bg)
                .SetContentTitle("From ProductMonitorWorker2")
                .SetContentText("From ProductMonitorWorker2");

            var notificationManager = NotificationManagerCompat.From(_context);
            notificationManager.Notify(6665, notificationBuilder.Build());
        }

        private void ProductsMonitorOnNewProductAvailable(object sender, ProductResponseEventArgs e)
        {
            Log.Debug(Constants.AppName, "ProductService: received new product available");

            var notificationBuilder = new NotificationCompat.Builder(_context, Constants.NewProductNotificationChannelId)
                .SetSmallIcon(Resource.Drawable.notification_bg)
                .SetContentTitle("New product available")
                .SetContentText($"Item {e.Id} available at € {e.Price} in {e.StoreName} store");

            var notificationManager = NotificationManagerCompat.From(_context);
            notificationManager.Notify(int.Parse(e.Id), notificationBuilder.Build());
        }

        #endregion
    }
}