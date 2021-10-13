using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Util;
using AndroidX.Core.App;
using AndroidX.Work;

namespace TooGoodToGoNotifierAndroidApp
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    internal class ProductMonitorWorker: Worker
    {
        private readonly Context _context;
        private readonly GrpcProductsMonitor _productsMonitor;

        public ProductMonitorWorker(Context context, WorkerParameters workerParams) : base(context, workerParams)
        {
            Log.Debug(Constants.AppName, "ProductMonitorWorker Constructor");
            _context = context;
            _productsMonitor = new GrpcProductsMonitor();
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
                    Log.Debug(Constants.AppName, "ProductMonitorWorker Monitoring started. Waiting 1 mins");

                    await Task.Delay(TimeSpan.FromMinutes(1));

                    Log.Debug(Constants.AppName, "ProductMonitorWorker Monitoring started. Waited 1 mins");
                    _productsMonitor.StopMonitoring();

                    Log.Debug(Constants.AppName, "ProductMonitorWorker Monitoring stopped");
                }).Wait();
            }
            catch (Exception e)
            {
                Log.Error(Constants.AppName, $"{nameof(ProductMonitorWorker)} An error occurred while starting monitoring {e}");
                return Result.InvokeFailure();
            }
            finally
            {
                _productsMonitor.NewProductAvailable -= ProductsMonitorOnNewProductAvailable;
            }

            return Result.InvokeSuccess();
        }

        #region Utility Methods

        private void ProductsMonitorOnNewProductAvailable(object sender, ProductResponseEventArgs e)
        {
            Log.Debug(Constants.AppName, "ProductService: received new product available");

            var notificationBuilder = new NotificationCompat.Builder(_context, Constants.NewProductNotificationChannelId)
                .SetSmallIcon(Resource.Drawable.notification_bg)
                .SetContentTitle(_context.GetString(Resource.String.new_product_available_title))
                .SetContentText(_context.GetString(Resource.String.new_product_available_text, e.Price.ToString(CultureInfo.InvariantCulture), e.StoreName));

            var notificationManager = NotificationManagerCompat.From(_context);
            notificationManager.Notify(int.Parse(e.Id), notificationBuilder.Build());
        }

        #endregion
    }
}