using PriceCollector.Services;
using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);

//----------------------------------------------------------------------------------------------------------------
// The program contain two services:
//          - Grpc service which handle request for new price , and Ping request
//          - Kernel service which periodically get prices from price feeders and agregaet it. Result saved in DB 
//----------------------------------------------------------------------------------------------------------------

// Add GRPC services to the container.
builder.Services.AddGrpc();

// Add Kernel /Background service/
builder.Services.AddHostedService<KernelService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<PriceProvider>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
