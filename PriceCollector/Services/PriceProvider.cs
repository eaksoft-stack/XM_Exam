using Grpc.Core;
using System.Threading.Channels;
using PriceCollector.DB;
using PriceCollector.Services;

namespace PriceCollector.Services
{

    public class PriceProvider(ILogger<PriceProvider> logger) : ClosePriceProvider.ClosePriceProviderBase
    {
        decimal price;
        InterfaceDBHandler db;
        public override async Task<ClosePriceReply> GetClosePrice(GetClosePriceRequest request, ServerCallContext context)
        {
            string reqSymbol = request.Symbol;
            long reqTimestamp = request.TickTimestamp;
            logger.LogInformation("The request is received for symbol: {Symbol}", reqSymbol);

            // Try to get the price from DB            

            db = new SQLiteHandler();
            price = db.SelectPrice(GlobalConstants.connectionStringDB, reqTimestamp, reqSymbol);

            if (price == 0.00m)                                                                                 // If no valid price then send signal to Kernel service to take price from external sources
            {
                await GetPriceFromLiveSources( reqSymbol, reqTimestamp, price);
            }

            // Return responce

            return await Task.FromResult(new ClosePriceReply
            {
                Price = price.ToString(),
                Symbol = reqSymbol,
                TickTimestamp = reqTimestamp
            });

        }


        //----------------------------------------------
        // Get requested price from Live price feeders
        //----------------------------
        private async Task GetPriceFromLiveSources(string reqSymbol, long reqTimestamp, decimal price)
        {
            // Send signal to Kernel Service
            
            await GlobalConstants.channel.Writer.WriteAsync(reqSymbol + ";" + reqTimestamp);
            GlobalConstants.channel.Writer.Complete();

            await GlobalConstants.signal.Task;
            


            price = db.SelectPrice(GlobalConstants.connectionStringDB, reqTimestamp, reqSymbol);             // Try to get price from DB again

        }
    }

}



