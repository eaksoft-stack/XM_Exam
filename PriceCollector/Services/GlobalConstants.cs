using System.Threading.Channels;

namespace PriceCollector.Services
{
    public static class GlobalConstants
    {
        public const string connectionStringDB = "Data Source=PriceDatabase.db";                             // SQLite connection string



        // Create an unbounded channel
        public static Channel<string> channel = Channel.CreateUnbounded<string>();

        // Create signal object

        public static TaskCompletionSource<bool> signal = new TaskCompletionSource<bool>();
    }
}
