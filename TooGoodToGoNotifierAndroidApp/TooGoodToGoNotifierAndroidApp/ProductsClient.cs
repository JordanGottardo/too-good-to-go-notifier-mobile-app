using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Android.Util;
using TooGoodToGoNotifierAndroidApp.Dtos;
using Xamarin.Essentials;

namespace TooGoodToGoNotifierAndroidApp
{

    public class ProductsClient
    {

        private const string ServerUrlKey = "serverUrl";
        private const string UsernameKey = "username";
        private readonly HttpClient _client;
        
        public ProductsClient()
        {
            _client = new HttpClient();
        }

        public async Task SubscribeUser()
        {
            try
            {
                var uri = await GetUrl("tokens/update");

                var response = await _client.PostAsync(uri, null);

                if (!response.IsSuccessStatusCode)
                {
                    Log.Error(Constants.AppName, $"An error occurred while subscribing user StatusCode: {response.StatusCode} ReasonPhrase: {response.ReasonPhrase}");

                    return;
                }
            
                var content = await response.Content.ReadAsStringAsync();
                var products = JsonSerializer.Deserialize<List<ProductDto>>(content);

            }
            catch (Exception e)
            {
                Log.Error(Constants.AppName, $"An error occurred while retrieving products {e}");
            }
        }

        public async Task<bool> IsUserSubscribed()
        {
            try
            {
                var uri = await GetUrl("tokens");

                var response = await _client.GetAsync(uri);

                if (!response.IsSuccessStatusCode)
                {
                    Log.Error(Constants.AppName, $"An error occurred retrieving tokens from user: {response.StatusCode} ReasonPhrase: {response.ReasonPhrase}");

                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                Log.Error(Constants.AppName, $"An error occurred while retrieving products {e}");
                return false;
            }
        }

        public async Task<IEnumerable<ProductDto>> GetAvailableProducts()
        {
            try
            {
                var uri = await GetUrl("products");

                var response = await _client.GetAsync(uri);

                if (!response.IsSuccessStatusCode)
                {
                    Log.Error(Constants.AppName, $"An error occurred while retrieving available products StatusCode: {response.StatusCode} ReasonPhrase: {response.ReasonPhrase}");

                    return Enumerable.Empty<ProductDto>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var products = JsonSerializer.Deserialize<List<ProductDto>>(content);

                return products;
            }
            catch (Exception e)
            {
                Log.Error(Constants.AppName, $"An error occurred while retrieving products {e}");
                return Enumerable.Empty<ProductDto>();
            }
        }

        private static async Task<Uri> GetUrl(string path)
        {
            var serverUrl = await SecureStorage.GetAsync(ServerUrlKey);
            var username = await SecureStorage.GetAsync(UsernameKey);

            var uri = new UriBuilder(serverUrl)
            {
                Path = path,
                Query = $"userEmail={username}"
            }.Uri;

            Log.Info(Constants.AppName, $"uri={uri}");

            return uri;
        }
    }
    

}