using Google.Protobuf.WellKnownTypes;
using PriceCollector.DB;
using PriceCollector.Services;
using System.Diagnostics;
using System.Linq.Expressions;

namespace PriceCollector.PriceHandlers
{
    public class ClosePriceProcessor:InterfacePriceProcessor
    {
        private const string symbol="btcusd";
        private string _connectionStringDB="";
        private Dictionary<string, string> _priceFeeders= new Dictionary<string, string>();
        private ILogger<KernelService> _logger;
        List<InterfaceSubProcessor> emProcessorPool = new List<InterfaceSubProcessor>();                 // List of SubProcessors which get prices from external sources


        private decimal closedPriceAgregated=0.0m;
        private long priceTimestamp;

        public ClosePriceProcessor(ILogger<KernelService> logger)
        {
            _logger = logger;  
        }    

        //------------------------------------------
        // Initializing the processor
        //    - create sub processors pool
        //    - SQLite
        //--------------------------------
        public void ProcessorInit(Dictionary<string, string> priceFeeders, string connStrDb)
        {
            _connectionStringDB = connStrDb;
            _priceFeeders =priceFeeders;

            // Init SubProcessor Pool
            int procIdCount = 0;

            while (procIdCount < _priceFeeders.Count())
            {
                KeyValuePair<string, string> pair = _priceFeeders.ElementAt(procIdCount);

                InterfaceSubProcessor? currSubProcessor = null;
                switch (pair.Key /*feed type*/)                                               
                {
                    case "bitstamp":
                        currSubProcessor = new BitStampSubProcessor(pair.Key, pair.Value, procIdCount);
                        
                        break;
                    case "bitfinex":
                        currSubProcessor = new BitFinexSubProcessor(pair.Key, pair.Value, procIdCount);
                      
                        break;
                }
                if (currSubProcessor != null) emProcessorPool.Add(currSubProcessor);

                procIdCount = procIdCount + 1;
            }

        }

        //----------------------------------------
        // Processing Prices
        //------------------------------ 
        public void Processing(long reqTime)
        {            

            //Open Price processing
            // ...
            //Max Price processing
            // ...
            //Min price processing
            // ...
            //Close Price processing

            closedPriceAgregated = ClosePriceProcessing(reqTime);
            priceTimestamp = reqTime;
        }

        //----------------------------------------
        // Save agregated price in SQLite
        //-----------------------------
        public void SavePrice()
        {
            InterfaceDBHandler sqLite = new SQLiteHandler();                                            // Create SQLite DB and prices table
            sqLite.DeletePrice(_connectionStringDB, priceTimestamp);                                    // Delete record if already exists
            sqLite.InsertPrice(_connectionStringDB, symbol, priceTimestamp, closedPriceAgregated);      // Insert the record 
        }



        //--------------------------------------------
        // this method contains algorithm which handle closed price
        //---------------------
        private decimal ClosePriceProcessing(long reqTime)
        {
            decimal resultPrice = 0.0m;
            List<decimal> pricesList = new List<decimal>();                                             // Prices list        
         

            if (emProcessorPool.Count > 0)
            {

                List<Task> processingTasks = new List<Task>();
                foreach (InterfaceSubProcessor processor in emProcessorPool)                           // Run all Sub processors
                {
                    Task currTask = Task.Run(() => processor.Processing(symbol,reqTime));
                    processingTasks.Add(currTask);
                }
                


                foreach (Task currT in processingTasks)                                                // wait all task completed
                {
                    
                     currT.Wait();
                }

                
                foreach (InterfaceSubProcessor processor in emProcessorPool)                           // Read results from all processors
                {
                    if (processor.IsCompleted())
                    {
                        pricesList.Add(processor.GetResultPrice());                                   // get price by feed/ subProcessor
                    }
                }


                // Price agregate
                AveragePriceAgregator agregator = new AveragePriceAgregator();                        // agregate prices
                resultPrice = agregator.AverageAgregator(pricesList);


            }



            return resultPrice;
        }



    }
}
