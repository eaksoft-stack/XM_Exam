using PriceClient;


Console.WriteLine("Started!");

Console.WriteLine("Please fill http communication port /You will see it in PriceCollector program mindow/):");
string _port = Console.ReadLine();
string _endPoint = "http://localhost:" + _port;


ProtoClient _client = new ProtoClient();

// Send request to PriceCollector service
// Timestamp require
Console.Write("Timestamp: ");
long tmStmp = Convert.ToInt64(Console.ReadLine());

_client.GetPrice(_endPoint,tmStmp);




Console.ReadLine();
