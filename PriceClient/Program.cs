using PriceClient;


Console.WriteLine("Hello, World!");

Console.WriteLine("Started!");

ProtoClient _client = new ProtoClient();

// Send request to PriceCollector service
// Timestamp require
Console.Write("Timestamp: ");
long tmStmp = Convert.ToInt64(Console.ReadLine());

_client.GetPrice(tmStmp);




Console.ReadLine();
