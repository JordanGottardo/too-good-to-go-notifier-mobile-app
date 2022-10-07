using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using Xamarin.Essentials;
using Fragment = AndroidX.Fragment.App.Fragment;

namespace TooGoodToGoNotifierAndroidApp.Fragments
{
    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    public class SettingsFragment : Fragment
    {
        #region Private fields
        
        private const string ServerUrlKey = "serverUrl";
        private const string UsernameKey = "username";
        private const string StopMonitoringKey = "stopMonitoring";
        private EditText _serverUrlEditText;
        private EditText _usernameEditText;
        private View _view;

        #endregion

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            _view = inflater.Inflate(Resource.Layout.fragment_settings, container, false);

            _serverUrlEditText = _view.FindViewById<EditText>(Resource.Id.serverUrlEditText);
            _usernameEditText = _view.FindViewById<EditText>(Resource.Id.usernameEditText);

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
                await SetInSecureStorage(StopMonitoringKey, bool.TrueString);

            }
            catch (Exception ex)
            {
                Log.Error(Constants.AppName, $"An error occurred while saving credentials to secure storage {ex}");
            }
        }

        private async void StartMonitoringButtonOnClickAsync(object sender, EventArgs e)
        {
            Log.Debug(Constants.AppName, "StartMonitoringButtonOnClickAsync");

            var serverUrl = _serverUrlEditText.Text;
            var username = _usernameEditText.Text;

            try
            {
                await SetInSecureStorage(StopMonitoringKey, bool.FalseString);

                await SetInSecureStorage(ServerUrlKey, serverUrl);
                await SetInSecureStorage(UsernameKey, username);

            }
            catch (Exception ex)
            {
                Log.Error(Constants.AppName, $"An error occurred while saving credentials to secure storage {ex}");
            }
        }

        private void FillEditTextWithDataIfAvailable()
        {
            var serverUrl = SecureStorage.GetAsync(ServerUrlKey).Result;
            var username = SecureStorage.GetAsync(UsernameKey).Result;

            if (serverUrl != null)
            {
                _serverUrlEditText.Text = serverUrl;
            }

            if (username != null)
            {
                _usernameEditText.Text = username;
            }
        }

        private static async Task SetInSecureStorage(string key, string value)
        {
            await SecureStorage.SetAsync(key, value);
        }

        #endregion
    }
}