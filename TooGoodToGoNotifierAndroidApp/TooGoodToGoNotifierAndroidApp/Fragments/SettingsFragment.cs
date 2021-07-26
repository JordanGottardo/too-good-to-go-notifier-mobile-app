using System;
using System.Threading.Tasks;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
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

        #endregion

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.fragment_settings, container, false);

            _channelUrlEditText = view.FindViewById<EditText>(Resource.Id.channelUrlEditText);
            _usernameEditText = view.FindViewById<EditText>(Resource.Id.usernameEditText);
            _passwordEditText = view.FindViewById<EditText>(Resource.Id.passwordEditText);

            FillEditTextWithDataIfAvailable();
            InitSaveCredentialsButton(view);

            return view;
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

        private void InitSaveCredentialsButton(View view)
        {
            var saveCredentialsButton = view.FindViewById<Button>(Resource.Id.saveCredentialsButton);
            saveCredentialsButton.Click += SaveCredentialsButtonOnClickAsync;
        }

        private async void SaveCredentialsButtonOnClickAsync(object sender, EventArgs e)
        {
            Log.Debug(Constants.AppName, "SaveCredentialsButtonOnClickAsync");

            var channelUrl = _channelUrlEditText.Text;
            var username = _usernameEditText.Text;
            var password = _passwordEditText.Text;

            try
            {
                await SecureStorage.SetAsync("channelUrl", channelUrl);
                await SecureStorage.SetAsync("username", username);
                await SecureStorage.SetAsync("password", password);
            }
            catch (Exception ex)
            {
                Log.Error(Constants.AppName, $"An error occurred while saving credentials to secure storage {ex}");
            }
        }

        #endregion
    }
}