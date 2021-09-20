using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using Google.Android.Material.Snackbar;
using Grpc.Core;
using Xamarin.Essentials;
using Fragment = AndroidX.Fragment.App.Fragment;

namespace TooGoodToGoNotifierAndroidApp.Fragments
{
    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
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

        private async void StopMonitoringButtonOnClickAsync(object sender, EventArgs e)
        {
            Log.Debug(Constants.AppName, "StopMonitoringButtonOnClickAsync");

            try
            {
                await SetInSecureStorage("stopMonitoring", bool.TrueString);

                var channelUrlAndPort = _channelUrlEditText.Text;
                var username = _usernameEditText.Text;
                

                await StopMonitoringAsync(channelUrlAndPort, username);
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
                await SetInSecureStorage("stopMonitoring", bool.FalseString);

                await SetInSecureStorage("channelUrl", channelUrlAndPort);
                await SetInSecureStorage("username", username);
                await SetInSecureStorage("password", password);

                await StartMonitoring(channelUrlAndPort, username, password);
            }
            catch (Exception ex)
            {
                Log.Error(Constants.AppName, $"An error occurred while saving credentials to secure storage {ex}");
            }
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

        private static async Task SetInSecureStorage(string key, string value)
        {
            await SecureStorage.SetAsync(key, value);
        }

        private async Task StartMonitoring(string channelUrl, string username, string password)
        {
            var productsClientFactory = new ProductsClientFactory();
            var productsClient = productsClientFactory.Create(channelUrl);

            var productMonitoringRequest = CreateProductMonitoringRequest(username, password);

            try
            {
                await productsClient.StartMonitoringAsync(productMonitoringRequest);
                Log.Debug(Constants.AppName, "Start product monitoring successful");
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.AlreadyExists)
            {
                Log.Debug(Constants.AppName, $"Monitoring has already started {e}");
                var snackBar = Snackbar.Make(_view, Resources.GetText(Resource.String.start_monitoring_failure_already_started), Snackbar.LengthShort);
                snackBar.Show();
            }
            catch (Exception e)
            {
                Log.Error(Constants.AppName, $"An error occurred while starting monitoring {e}");
                var snackBar = Snackbar.Make(_view, Resource.String.start_monitoring_failure, Snackbar.LengthShort);
                snackBar.Show();
            }
        }

        private async Task StopMonitoringAsync(string channelUrl, string username)
        {
            var productsClientFactory = new ProductsClientFactory();
            var productsClient = productsClientFactory.Create(channelUrl);

            var productStopMonitoringRequest = CreateProductStopMonitoringRequest(username);

            try
            {
                await productsClient.StopMonitoringAsync(productStopMonitoringRequest);
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