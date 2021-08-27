using System;
using System.Threading.Tasks;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using Google.Android.Material.Snackbar;
using Xamarin.Essentials;
using Fragment = AndroidX.Fragment.App.Fragment;

namespace TooGoodToGoNotifierAndroidApp.Fragments
{
    public class SettingsFragment : Fragment
    {
        #region Private fields

        private EditText _channelUrlEditText;
        private EditText _usernameEditText;
        private EditText _passwordEditText;
        private View _view;

        #endregion

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            _view = inflater.Inflate(Resource.Layout.fragment_settings, container, false);

            _channelUrlEditText = _view.FindViewById<EditText>(Resource.Id.channelUrlEditText);
            _usernameEditText = _view.FindViewById<EditText>(Resource.Id.usernameEditText);
            _passwordEditText = _view.FindViewById<EditText>(Resource.Id.passwordEditText);

            FillEditTextWithDataIfAvailable();
            InitStartMonitoringButton(_view);
            InitStopMonitoringButton(_view);

            return _view;
        }



        private void FillEditTextWithDataIfAvailable()
        {
            var channelUrl = SecureStorage.GetAsync("channelUrl").Result;
            var username = SecureStorage.GetAsync("username").Result;
            var password = SecureStorage.GetAsync("password").Result;

            if (channelUrl != null)
            {
                _channelUrlEditText.Text = channelUrl;
            }

            if (username != null)
            {
                _usernameEditText.Text = username;
            }

            if (password != null)
            {
                _passwordEditText.Text = password;
            }
        }


        #region Utility Methods

        private void InitStartMonitoringButton(View view)
        {
            var startMonitoringButton = view.FindViewById<Button>(Resource.Id.startMonitoringButton);
            startMonitoringButton.Click += StartMonitoringButtonOnClickAsync;
        }

        private void InitStopMonitoringButton(View view)
        {
            var stopMonitoringButton = view.FindViewById<Button>(Resource.Id.stopMonitoringButton);
            stopMonitoringButton.Click += StopMonitoringButtonOnClickAsync;
        }

        private void StopMonitoringButtonOnClickAsync(object sender, EventArgs e)
        {
            Log.Debug(Constants.AppName, "StopMonitoringButtonOnClickAsync");

            try
            {
                var channelUrlAndPort = _channelUrlEditText.Text;
                var username = _usernameEditText.Text;
                var password = _passwordEditText.Text;

                StopMonitoring(channelUrlAndPort, username, password);
            }
            catch (Exception ex)
            {
                Log.Error(Constants.AppName, $"An error occurred while saving credentials to secure storage {ex}");
            }
        }

        private async void StartMonitoringButtonOnClickAsync(object sender, EventArgs e)
        {
            Log.Debug(Constants.AppName, "StartMonitoringButtonOnClickAsync");

            var channelUrlAndPort = _channelUrlEditText.Text;
            var username = _usernameEditText.Text;
            var password = _passwordEditText.Text;

            try
            {
                await SecureStorage.SetAsync("channelUrl", channelUrlAndPort);
                await SecureStorage.SetAsync("username", username);
                await SecureStorage.SetAsync("password", password);

                StartMonitoring(channelUrlAndPort, username, password);
            }
            catch (Exception ex)
            {
                Log.Error(Constants.AppName, $"An error occurred while saving credentials to secure storage {ex}");
            }
        }

        private void StartMonitoring(string channelUrl, string username, string password)
        {
            var productsClientFactory = new ProductsClientFactory();
            var productsClient = productsClientFactory.Create(channelUrl);

            var productMonitoringRequest = CreateProductMonitoringRequest(username, password);

            try
            {
                productsClient.StartMonitoring(productMonitoringRequest);
                Log.Debug(Constants.AppName, "Start product monitoring successful");
            }
            catch (Exception e)
            {
                Log.Debug(Constants.AppName, $"An error occurred while starting monitoring {e}");
                var snackBar = Snackbar.Make(_view, Resource.String.start_monitoring_failure, Snackbar.LengthShort);
                snackBar.Show();
            }
        }

        private void StopMonitoring(string channelUrl, string username, string password)
        {
            var productsClientFactory = new ProductsClientFactory();
            var productsClient = productsClientFactory.Create(channelUrl);

            var productStopMonitoringRequest = CreateProductStopMonitoringRequest(username);

            try
            {
                productsClient.StopMonitoring(productStopMonitoringRequest);
                Log.Debug(Constants.AppName, "Stop product monitoring successful");
            }
            catch (Exception e)
            {
                Log.Debug(Constants.AppName, $"An error occurred while stopping monitoring {e}");
                var snackBar = Snackbar.Make(_view, Resource.String.stop_monitoring_failure, Snackbar.LengthShort);
                snackBar.Show();
            }
        }

        private static ProductMonitoringRequest CreateProductMonitoringRequest(string username, string password)
        {
            return new ProductMonitoringRequest
            {
                Username = username,
                Password = password
            };
        }

        private static ProductStopMonitoringRequest CreateProductStopMonitoringRequest(string username)
        {
            return new ProductStopMonitoringRequest
            {
                Username = username,
            };
        }

        #endregion
    }
}