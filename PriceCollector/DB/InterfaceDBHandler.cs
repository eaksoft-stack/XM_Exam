namespace PriceCollector.DB
{
    public interface InterfaceDBHandler
    {

        //----------------------------------------------
        //
        //---------------------------
        public void CreateSQLiteDB(string connectionString);



        //----------------------------------------------
        //
        //---------------------------
        public void InsertPrice(string connectionString, string symbol, long timestamp, decimal price);
        

        //----------------------------------------------
        //
        //---------------------------
        public void DeletePrice(string connectionString, decimal timestamp);
        


        //----------------------------------------------
        //
        //---------------------------
        public decimal SelectPrice(string connectionString, long timestamp, string symbol);
        


    }
}
