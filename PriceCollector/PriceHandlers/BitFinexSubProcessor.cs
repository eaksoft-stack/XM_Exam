using System.Text.Json;

namespace PriceCollector.PriceHandlers
{
    public class BitFinexSubProcessor:InterfaceSubProcessor
    {        
        const string bitfinexBaseUrl = "https://api-pub.bitfinex.com/v2/candles/trade:1h:t";        // End point
        const int httpClientTimeOutsec = 10;                                                        // 10 sec timeout for Http client
        

        private int _processorId;
        private string _feedType;
        private string _feedURL;
        private decimal resultPrice;
        private bool stateFlg;
        private HttpClient client;


        public BitFinexSubProcessor(string feedType, string feedURL, int idProc)
        {
            _feedType = feedType;
            _feedURL = feedURL;
            _processorId = idProc;
            stateFlg = false;

            client = new();
            client.Timeout= TimeSpan.FromSeconds(httpClientTimeOutsec);
        }

        public  void Processing(string symbol, long timestamp)
        {
            stateFlg = false;
            resultPrice = 0.0m;

            long endTimestamp = timestamp + 3600;
            long begTimeStampMsec = timestamp * 1000;
            long endTimeStampMsec = endTimestamp * 1000;
            string endPointUrl = bitfinexBaseUrl + symbol.ToUpper() + "/hist?start=" + begTimeStampMsec + "&end=" + endTimeStampMsec + "&limit=1";
            var request = new HttpRequestMessage(HttpMethod.Get, endPointUrl);  //"https://api-pub.bitfinex.com/v2/candles/trade:1h:tBTCUSD/hist?start=1672531200000&end=1672534800000&limit=1");            

            using HttpResponseMessage response = client.Send(request);

            if (response.IsSuccessStatusCode)
            {                
                using var stream = response.Content.ReadAsStream();
                using var reader = new System.IO.StreamReader(stream);
                string jsonString = reader.ReadToEnd();
                var candle = JsonSerializer.Deserialize<List<decimal[]>>(jsonString);
                if ((candle!=null) && (candle.Count>0))
                resultPrice = candle[0][4];
            }
                          

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
