using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PriceCollector.DB;
using PriceCollector.PriceHandlers;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;


namespace PriceCollector.Services
{
    public class KernelService : BackgroundService
    {
        #region Kernel settings
        int delaySeconds = 1;                                                                   // 1 second delay
        int hbMessagePeriod = 60;                                                               // Period in seconds for Heart Beat log message. Each 1 min the program logs Heart Beat message     
        
        #endregion


        private readonly ILogger<KernelService> _logger;
        private readonly IConfiguration _configuration;
       
        private Dictionary<string,string> priceFeeders;
        private string listenIP;
        private InterfacePriceProcessor priceProcessor;
        private volatile bool newRequestFlg = false;
        private long reqTimestampSeconds =0;
        private volatile string reqSymbol = "";
        private DateTimeOffset passedHour;                                                                          // value of the hour which is last processed


        //------------------------------------------
        // Constructor
        //---------------------
        public KernelService(ILogger<KernelService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;            

            priceFeeders = _configuration.GetSection("AppSettings:priceSources").Get<Dictionary<string, string>>() ?? new Dictionary<string, string>();
                                                                                                                    // Get prices feeders from appsettings
            listenIP = _configuration.GetSection("AppSettings:listenIP").Get<string>() ??"";                        // Get listen IP from appsettings


            InterfaceDBHandler sqLite = new SQLiteHandler();                                                        // Create SQLite DB and prices table
            sqLite.CreateSQLiteDB(GlobalConstants.connectionStringDB);

            priceProcessor = new ClosePriceProcessor(_logger);                                                      // Create Processor which fetch and calculate average price
            priceProcessor.ProcessorInit(priceFeeders, GlobalConstants.connectionStringDB);                         // Processor can have only one instance , because contains HTTPClients
                                                                                                                    // Do not use in loop !!!!!
                                                                                                                    // 
            

        }

        //------------------------------------------
        // Consumer wait and handle messages send from PriceProvider service /grpc service/
        //--------------------------
        protected async Task Consumer(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)                                                          // Loop continues until the application is stopped
            {
                // Read until the channel is marked as complete
                await foreach (var message in GlobalConstants.channel.Reader.ReadAllAsync())
                {
                    string[] parsedMessage = message.Split(';');
                    if (parsedMessage.Length ==2)
                    {
                        reqSymbol = parsedMessage[0];
                        long currTimestamp;
                        if (long.TryParse(parsedMessage[1], out currTimestamp))
                        {
                            Volatile.Write(ref reqTimestampSeconds, currTimestamp);
                            newRequestFlg = true;
                        }
                    }

                    
                }
            }
        
            Console.WriteLine("Consumer: Channel closed. Stopping.");
        }





        //-------------------------------------------
        // Worker Task
        //----------------------------

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int maxLoops = Convert.ToInt32(hbMessagePeriod / delaySeconds);
            int logMsgCounter = 0;

            Consumer(stoppingToken);                                                                                // Init Task which receive messages from PriceProvider service

            _logger.LogInformation("Background Worker started at: {time}", DateTimeOffset.Now);                     // Initial log message                                                    

            while (!stoppingToken.IsCancellationRequested)                                                          // Loop continues until the application is stopped
            {
                if (logMsgCounter > maxLoops)
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);                        // Heart Beat log message
                    logMsgCounter = 0;
                }
                else { logMsgCounter = logMsgCounter + 1; };

                try
                {
                    if (priceFeeders.Count > 0)
                    {
                        PricesProcessing(stoppingToken);                                                           // Calling Price processing task
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred in the kernel service.");
                }

               
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);                                // Wait 1 second before running again
            }

            _logger.LogInformation("Background Kernel is stopping.");
        }




        //-------------------------------------------
        // Price Processing 
        // Price is processing if have external requiest for it or
        // if new 1 hour candel is avaliable
        //------------------------
        private async void PricesProcessing(CancellationToken stoppingToken)
        {
            if (newRequestFlg)                                                  // If have external request
            {
                ExternalPriceRequestProcessing();
            }

            if (CheckTime())                                                    // If it is time
            {
                HourlyPriceRequestProcessing();
            }

        }


        //-------------------------------------------
        // External request processing
        //--------------------------
        void ExternalPriceRequestProcessing()
        {

            long currTimestamp = Volatile.Read(ref reqTimestampSeconds);
            DateTimeOffset reqHour = DateTimeOffset.FromUnixTimeSeconds(currTimestamp);
            DateTimeOffset reqTime = new DateTimeOffset(
                reqHour.Year,
                reqHour.Month,
                reqHour.Day,
                reqHour.Hour,
                0,
                0,
                reqHour.Offset
            );

            priceProcessor.Processing(reqTime.ToUnixTimeSeconds());
            priceProcessor.SavePrice();

            // Send signal to PriceProvider service. Price is ready. PriceProvider service must take the new price from DB! 
            GlobalConstants.signal.SetResult(true);
            newRequestFlg = false;

        }



        //-------------------------------------------
        // Hourly request processing
        //-------------------------
        void HourlyPriceRequestProcessing()
        {

            DateTimeOffset currTime = DateTimeOffset.UtcNow;
            DateTimeOffset prevHour = currTime.AddHours(-1);

            DateTimeOffset reqTime = new DateTimeOffset(
                prevHour.Year,
                prevHour.Month,
                prevHour.Day,
                prevHour.Hour,
                0,
                0,
                prevHour.Offset
            );



            priceProcessor.Processing(reqTime.ToUnixTimeSeconds());         // Processing the price. Collect prices from feeders and calculate average price

            if (priceProcessor.SavePrice() != 0)                            // try to save the price
            {                                                               // if price not saved because price is incorrect / price == 0/ then ..
                passedHour = default;                                       // next second the porgram will send new request for the same hour
                                                                            // if all feeds return price =0 the same request will be repited maximum 60 times per one hour.
                                                                            // when hour expire and no positive responce , we will see gap in price database
            }


        }


    
        //-------------------------------------------
        // Check for Hour
        //--------------------------
        Boolean CheckTime()
        {          
            bool retStat = false;
            DateTimeOffset currHour=DateTimeOffset.Now;
            

            DateTimeOffset prevHour = new DateTimeOffset(
                    currHour.Year,
                    currHour.Month,
                    currHour.Day,
                    currHour.Hour,
                    0,
                    0,
                    currHour.Offset
                );


            if (passedHour!=prevHour)
            {
                retStat = true;
                passedHour = prevHour;

                Task.Delay(TimeSpan.FromSeconds(2));                                // delay 2sec
            }


            return retStat;
        }



    }
}
