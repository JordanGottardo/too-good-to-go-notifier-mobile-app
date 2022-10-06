using System.Diagnostics.CodeAnalysis;
using Android.Content;
using Android.Util;
using AndroidX.Work;

namespace TooGoodToGoNotifierAndroidApp
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    internal class ProductMonitorWorker: Worker
    {
        private readonly Context _context;

        public ProductMonitorWorker(Context context, WorkerParameters workerParams) : base(context, workerParams)
        {
            Log.Debug(Constants.AppName, "ProductMonitorWorker Constructor");
            _context = context;
        }

        public override Result DoWork()
        {
            // Log.Debug(Constants.AppName, "ProductMonitorWorker DoWork");
            //
            // try
            // {
            //     Task.Run(async () =>
            //     {
            //
            //         Log.Debug(Constants.AppName, "ProductMonitorWorker Monitoring started. Waiting 1 mins");
            //
            //
            //         Log.Debug(Constants.AppName, "ProductMonitorWorker Monitoring started. Waited 1 mins");
            //
            //         Log.Debug(Constants.AppName, "ProductMonitorWorker Monitoring stopped");
            //     }).Wait();
            // }
            // catch (Exception e)
            // {
            //     Log.Error(Constants.AppName, $"{nameof(ProductMonitorWorker)} An error occurred while starting monitoring {e}");
            //     return Result.InvokeFailure();
            // }
            // finally
            // {
            //     _productsMonitor.NewProductAvailable -= ProductsMonitorOnNewProductAvailable;
            // }

            return Result.InvokeSuccess();
        }

        // #region Utility Methods
        //
        // private void ProductsMonitorOnNewProductAvailable(object sender, ProductResponseEventArgs e)
        // {
        //     Log.Debug(Constants.AppName, "ProductService: received new product available");
        //
        //     var notificationBuilder = new NotificationCompat.Builder(_context, Constants.NewProductNotificationChannelId)
        //         .SetSmallIcon(Resource.Drawable.notification_icon)
        //         .SetContentTitle(_context.GetString(Resource.String.new_product_available_title))
        //         .SetContentText(_context.GetString(Resource.String.new_product_available_text, e.Price.ToString(CultureInfo.InvariantCulture), e.StoreName));
        //
        //     var notificationManager = NotificationManagerCompat.From(_context);
        //     notificationManager.Notify(int.Parse(e.Id), notificationBuilder.Build());
        // }
        //
        // #endregion
    }
}