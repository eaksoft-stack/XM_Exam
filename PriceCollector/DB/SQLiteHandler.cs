using Microsoft.Data.Sqlite;


namespace PriceCollector.DB
{
    public class SQLiteHandler: InterfaceDBHandler
    {
        //----------------------------------------------
        //
        //---------------------------
        public void CreateSQLiteDB(string connectionString)
        {
            string createTableSql = @"
            CREATE TABLE IF NOT EXISTS Prices (                
                symbol TEXT NOT NULL,
                close_price REAL,
                timestamp INTEGER
            );";


            using (var connection = new SqliteConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    Console.WriteLine("DB created and open sucessfully");


                    using (var command = new SqliteCommand(createTableSql, connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("Table Price created");
                    }
                }
                catch (SqliteException ex)
                {
                    Console.WriteLine($"Error SQLite : {ex.Message}");
                }
            }

        }


        //----------------------------------------------
        //
        //---------------------------
        public void InsertPrice(string connectionString, string symbol, long timestamp, decimal price)
        {
            string sql = "INSERT INTO Prices (symbol, timestamp, close_price) VALUES (@sy, @ts, @pr)";

            // 2. Отваряне на връзката и изпълнение на командата
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                using (var command = new SqliteCommand(sql, connection))
                {

                    command.Parameters.AddWithValue("@sy", symbol);
                    command.Parameters.AddWithValue("@ts", timestamp);
                    command.Parameters.AddWithValue("@pr", price);


                    int rowsAffected = command.ExecuteNonQuery();
                }
            }


        }

        //----------------------------------------------
        //
        //---------------------------
        public void DeletePrice(string connectionString, decimal timestamp)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();


                string sql = "DELETE FROM Prices WHERE timestamp = @ts;";

                using (var command = new SqliteCommand(sql, connection))
                {

                    command.Parameters.AddWithValue("@ts", timestamp);

                    int rowsAffected = command.ExecuteNonQuery();

                }
            }
        }


        //----------------------------------------------
        //
        //---------------------------
        public decimal SelectPrice(string connectionString, long timestamp, string symbol)
        {
            decimal price = 0.00m;


            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                string sql = "SELECT close_price FROM Prices WHERE (timestamp = @ts) and (symbol=@sy)";
                using (var command = new SqliteCommand(sql, connection))
                {

                    command.Parameters.AddWithValue("@ts", timestamp);
                    command.Parameters.AddWithValue("@sy", symbol);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            price = reader.GetDecimal(0);

                        }
                    }
                }
            }




            return price;
        }



    }
}
