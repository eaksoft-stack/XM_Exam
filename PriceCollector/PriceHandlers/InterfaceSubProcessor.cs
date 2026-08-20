namespace PriceCollector.PriceHandlers
{
    public interface InterfaceSubProcessor
    {
        //--------------------------------------
        //
        //--------------------
        public void Processing(string symbol, long timestamp);



        //--------------------------------------
        // Get Subprocessor Id
        //--------------------
        public int GetId();



        //--------------------------------------
        // Check processing completed
        //---------------------
        public bool IsCompleted();


        //--------------------------------------
        //
        //-----------------------
        public decimal GetResultPrice();



    }
}
