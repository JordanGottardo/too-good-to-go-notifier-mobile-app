using Grpc.Core;

namespace TooGoodToGoNotifierAndroidApp
{
    internal class ProductsClientFactory
    {
        public ProductsManager.ProductsManagerClient Create(string channelUrlAndPort)
        {
            var channel = new Channel(channelUrlAndPort, new SslCredentials());
            return new ProductsManager.ProductsManagerClient(channel);
        }
    }
}