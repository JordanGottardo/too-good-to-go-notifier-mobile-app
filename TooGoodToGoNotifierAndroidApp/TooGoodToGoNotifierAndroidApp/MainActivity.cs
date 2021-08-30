using System;
using System.Diagnostics.CodeAnalysis;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;
using AndroidX.Core.View;
using AndroidX.DrawerLayout.Widget;
using AndroidX.Work;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Navigation;
using Google.Android.Material.Snackbar;
using TooGoodToGoNotifierAndroidApp.Fragments;
using Xamarin.Essentials;
using Fragment = AndroidX.Fragment.App.Fragment;
using FragmentTransaction = AndroidX.Fragment.App.FragmentTransaction;

namespace TooGoodToGoNotifierAndroidApp
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    [SuppressMessage("ReSharper", "UnusedType.Global")]
    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class MainActivity : AppCompatActivity, NavigationView.IOnNavigationItemSelectedListener
    {
        #region Private Fields

        private ProductsFragment _productFragment;
        private SettingsFragment _settingsFragment;
        
        #endregion

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Log.Debug(Constants.AppName, "MainActivity OnCreate");

            Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);
            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            InitFragments();
            CreateNotificationChannels();
            InitProductsMonitoring();

            var fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += FabOnClick;

            var drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            var toggle = new ActionBarDrawerToggle(this, drawer, toolbar, Resource.String.navigation_drawer_open, Resource.String.navigation_drawer_close);
            drawer.AddDrawerListener(toggle);
            toggle.SyncState();

            var navigationView = FindViewById<NavigationView>(Resource.Id.nav_view);
            navigationView.SetNavigationItemSelectedListener(this);
        }

        public override void OnBackPressed()
        {
            var drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            if(drawer.IsDrawerOpen(GravityCompat.Start))
            {
                drawer.CloseDrawer(GravityCompat.Start);
            }
            else
            {
                base.OnBackPressed();
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            var id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        public bool OnNavigationItemSelected(IMenuItem item)
        {
            var id = item.ItemId;

            switch (id)
            {
                case Resource.Id.nav_gallery:
                    Log.Debug("TooGoodToGoNotifierApp", "Clicked on nav gallery");
                    ReplaceFragment(_settingsFragment);
                    break;
                case Resource.Id.nav_slideshow:
                    Log.Debug("TooGoodToGoNotifierApp", "Clicked on slideshow");
                    ReplaceFragment(_productFragment);
                    break;
                case Resource.Id.nav_send:
                    break;
            }

            var drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            drawer.CloseDrawer(GravityCompat.Start);
            return true;
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected override void OnDestroy()
        {
            Log.Debug(Constants.AppName, "MainActivity OnDestroy");

            base.OnDestroy();
        }

        #region Utility Methods

        private void ReplaceFragment(Fragment fragment)
        {
            if (fragment.IsVisible)
            {
                return;
            }

            InATransaction(transaction =>
            {
                transaction.Replace(Resource.Id.fragment_container, fragment);
                transaction.AddToBackStack(null);
            });
        }

        private void InitFragments()
        {
            _productFragment = new ProductsFragment();
            _settingsFragment = new SettingsFragment();
        }

        private void InATransaction(Action<FragmentTransaction> action)
        {
            var transaction = SupportFragmentManager.BeginTransaction();

            action(transaction);

            transaction.Commit();
        }

        private static void FabOnClick(object sender, EventArgs eventArgs)
        {
            var view = (View)sender;
            Snackbar.Make(view, "Replace with your own action", Snackbar.LengthLong)
                .SetAction("Action", (View.IOnClickListener)null).Show();
        }

        private void InitProductsMonitoring()
        {
            Log.Debug(Constants.AppName, "MainActivity InitProductsMonitoring");
            
            var productMonitorRequest = PeriodicWorkRequest.Builder.From<ProductMonitorWorker>(TimeSpan.FromMinutes(20))
                .Build();

            WorkManager
                .GetInstance(this)
                .EnqueueUniquePeriodicWork("monitorProducts", ExistingPeriodicWorkPolicy.Replace, productMonitorRequest);
        }

        private void CreateNotificationChannels()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                // Notification channels are new in API 26 (and not a part of the
                // support library). There is no need to create a notification
                // channel on older versions of Android.
                return;
            }

            CreateNotificationChannel(
                Resource.String.product_notification_channel_name,
                Resource.String.product_notification_channel_description, 
                Constants.NewProductNotificationChannelId);

            CreateNotificationChannel(
                Resource.String.foreground_channel_name,
                Resource.String.foreground_channel_description,
                Constants.ForegroundServiceNotificationChannelId);
        }

        private void CreateNotificationChannel(int channelNameId, int channelDescriptionId, string notificationChannelId)
        {
            var name = GetString(channelNameId);
            var description = GetString(channelDescriptionId);
            var channel = new NotificationChannel(notificationChannelId, name, NotificationImportance.Default)
            {
                Description = description
            };

            var notificationManager = (NotificationManager) GetSystemService(NotificationService);
            notificationManager.CreateNotificationChannel(channel);
        }

        #endregion
    }
}

