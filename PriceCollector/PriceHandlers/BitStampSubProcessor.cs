using Grpc.Core;
using System.Globalization;
using System.Text.Json;


namespace PriceCollector.PriceHandlers
{
    public class BitStampSubProcessor: InterfaceSubProcessor
    {
        const string bitStampBaseUrl = "https://www.bitstamp.net/api/v2/ohlc/";                     // End point
        const int httpClientTimeOutsec = 10;                                                        // 10 sec timeout for Http client

        private int _processorId;
        private string _feedType;
        private string _feedURL;
        private decimal resultPrice;
        private bool stateFlg;
        private HttpClient client;


        public BitStampSubProcessor(string feedType, string feedURL, int idProc)
        {
            _feedType = feedType;
            _feedURL = feedURL;
            _processorId = idProc;
            stateFlg = false;

            client = new();
            client.Timeout = TimeSpan.FromSeconds(httpClientTimeOutsec);
        }

        public void Processing(string symbol, long timestamp)
        {
            resultPrice= 0.0000m;
            stateFlg = false;

            string endPointUrl = bitStampBaseUrl + symbol.ToLower() + "/?step=3600&limit=1&start=" + timestamp;
            var request = new HttpRequestMessage(HttpMethod.Get, endPointUrl);

            using HttpResponseMessage response = client.Send(request);

            if (response.IsSuccessStatusCode)
            {
                using var stream = response.Content.ReadAsStream();
                using var reader = new System.IO.StreamReader(stream);
                string jsonString = reader.ReadToEnd();               
                ClosePriceBitstampType data = JsonSerializer.Deserialize<ClosePriceBitstampType>(jsonString)?? new ClosePriceBitstampType();

                if ((data.Data != null) &&(data.Data.Ohlc!=null) && (data.Data.Ohlc.Count() > 0))
                {
                    string closePrice = data.Data.Ohlc[0].Close ?? "0.0000";
                    resultPrice = decimal.Parse(closePrice, CultureInfo.InvariantCulture);
                }
            }         
            

            // ClosePriceBitstampType data = client.GetFromJsonAsync<ClosePriceBitstampType>(endPointUrl);//"https://www.bitstamp.net/api/v2/ohlc/btcusd/?step=3600&limit=1&start=1672531200");

            stateFlg = true;
        }

        //--------------------------------------
        // Get Subprocessor Id
        //--------------------
        public int GetId()
        {
            return _processorId;
        }

        //--------------------------------------
        // Check processing completed
        //---------------------
        public bool IsCompleted()
        {
            return stateFlg;
        }

        //--------------------------------------
        //
        //-----------------------
        public decimal GetResultPrice()
        {
            return resultPrice;
        }

    }
}
