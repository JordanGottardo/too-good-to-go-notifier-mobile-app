using System.Text.Json.Serialization;

namespace TooGoodToGoNotifierAndroidApp.Dtos
{

    public class ProductDto
    {
        [JsonPropertyName("productId")]
        public string ProductId { get; set; }
        
        [JsonPropertyName("price")]
        public int Price { get; set; }
        
        [JsonPropertyName("decimals")]
        public int Decimals { get; set; }
        
        [JsonPropertyName("isAvailable")]
        public bool IsAvailable { get; set; }
        
        [JsonPropertyName("storeName")]
        public string StoreName { get; set; }
    }

}