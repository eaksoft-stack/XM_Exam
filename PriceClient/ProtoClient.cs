using Grpc.Net.Client;
using GrpcService1;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;



namespace PriceClient
{
    internal class ProtoClient
    {
      
        public void GetPrice (long timestamp)
        {
            // 
            GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:5219");

            // 
            var client = new ClosePriceProvider.ClosePriceProviderClient(channel);

            // Request
            var request = new GetClosePriceRequest { 
                Symbol = "BTCUSD",
                TickTimestamp= timestamp
            };

            try
            {
               
                var response = client.GetClosePriceAsync(request);

                Console.WriteLine($"Responce: Symbol:{response.ResponseAsync.Result.Symbol}  Price:{response.ResponseAsync.Result.Price} Timestamp:{response.ResponseAsync.Result.TickTimestamp} ");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Communication error: {ex.Message}");
            }
        }




    }
}
