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
        private bool newRequestFlg = false;
        private long reqTimestampSeconds=0;
        private string reqSymbol = "";
        private DateTimeOffset passedHour;


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
                        if (long.TryParse(parsedMessage[1], out reqTimestampSeconds))
                        {
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
        //------------------------
        private async void PricesProcessing(CancellationToken stoppingToken)
        {
            if (CheckTime())                                                    // If it is time
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
                

       
                priceProcessor.Processing(reqTime.ToUnixTimeSeconds());
                priceProcessor.SavePrice();
            }


            if (newRequestFlg)                                                  // If have external request
            {
                DateTimeOffset reqHour = DateTimeOffset.FromUnixTimeSeconds(reqTimestampSeconds); 
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
            }

            newRequestFlg = false;
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
