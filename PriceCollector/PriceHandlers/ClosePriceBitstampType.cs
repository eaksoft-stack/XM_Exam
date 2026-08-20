using System.Text.Json.Serialization;

namespace PriceCollector.PriceHandlers
{
    public class ClosePriceBitstampType
    {
        [JsonPropertyName("data")]
        public CryptoData? Data { get; set; }

    }


    public class CryptoData
    {
        [JsonPropertyName("pair")]
        public string? Pair { get; set; }

        [JsonPropertyName("ohlc")]
        public List<OhlcRecord>? Ohlc { get; set; }
    }



    public class OhlcRecord
    {
        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; set; }

        [JsonPropertyName("open")]
        public string? Open { get; set; }

        [JsonPropertyName("high")]
        public string? High { get; set; }

        [JsonPropertyName("low")]
        public string? Low { get; set; }

        [JsonPropertyName("close")]
        public string? Close { get; set; }

        [JsonPropertyName("volume")]
        public string? Volume { get; set; }


    }






}
