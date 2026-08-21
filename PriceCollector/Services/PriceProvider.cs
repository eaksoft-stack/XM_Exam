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

            // Format timestamp
            reqTimestamp = FormatTimestamp(reqTimestamp);

            // Try to get the price from DB            

            db = new SQLiteHandler();
            price = db.SelectPrice(GlobalConstants.connectionStringDB, reqTimestamp, reqSymbol);

            if (price == 0.00m)                                                                                 // If no valid price then send signal to Kernel service to take price from external sources
            {
                // GetPriceFromLiveSources( reqSymbol, reqTimestamp, price);
                // Send signal to Kernel Service

                await GlobalConstants.channel.Writer.WriteAsync(reqSymbol + ";" + reqTimestamp);
                GlobalConstants.channel.Writer.Complete();

                await GlobalConstants.signal.Task;

                price = db.SelectPrice(GlobalConstants.connectionStringDB, reqTimestamp, reqSymbol);             // Try to get price from DB again
            }


            // Return responce

            return await Task.FromResult(new ClosePriceReply
            {
                Price = price.ToString(),
                Symbol = reqSymbol,
                TickTimestamp = reqTimestamp
            });

        }

        
        //-----------------------------------------------
        // Formatting input Timestamp
        //-------------------------
        long FormatTimestamp(long tmStmp)
        {
            DateTimeOffset reqHour = DateTimeOffset.FromUnixTimeSeconds(tmStmp);
            DateTimeOffset reqTime = new DateTimeOffset(
                reqHour.Year,
                reqHour.Month,
                reqHour.Day,
                reqHour.Hour,
                0,
                0,
                reqHour.Offset
            );

            return reqTime.ToUnixTimeSeconds();
        }

    }

}



