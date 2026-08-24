PriceAgregator
PriceAgregator solution contains 3 projects:
PriceCollector 
PriceClient 
PriceCollector.test 
PriceCollector.test is unit testing program which contain PriceCollector tests.
PriceClient is simple gRPC client which send price requests to PriceCollector program. To run PriceClient follow next steps:
1. Compile the PriceClient project. 
2. Navigate to \PriceAgregator\PriceClient\bin\Release\net10.0 
3. run PriceClient application 
4. fill the comunication port /you can find it in PriceCollector programs window/. Please note that progam use http not https 
5. fill the timestamp in seconds format 
6. press Enter. 
PriceCollector program is service. For testing purpoce no need to install it , you can run it manually from folder:
..\PriceAgregator\PriceCollector\bin\Release\net10.0\PriceCollector.exe . The same folder contains configuration file: appsettings.json / appsettings.Development.json
PriceCollector using SQLite DB. The program create automatically local DB and table "ClosePrice". You will find the DB file in folder: ..\PriceAgregator\PriceCollector\PriceDatabase.db
To run PriceCollector follow next steps:
1. Compile the PriceCollector project. 
2. Navigate to C:\XM_Exam\PriceAgregator\PriceCollector\bin\Release\net10.0 
3. run PriceCollector.exe 
4. copy http: port number 
5. Run PriceClient program using port numeb which you copy 
ABout PriceCollector program
Price collector program is a gRPC service. It contains two sub services:
PriceProvider 
KernelService 
PriceProvider handle all gRPC requests. KernelService implement the program bussines logic. PriceProvider can only read from SQLite DB. KerneleService can only write to SQLite DB. Both of services exchange messages and signals.
PriceProvider send price request message to KernelService. KernelService response using .NET signal. When PriceProvider receive price request, it try to take price from SQLite DB if price exsists , result send to client. If price missing then PriceProvider send message to KerneleService and wait to be signaled from it. When signal is avaliable the PriceProvider try to re-read the price from SQLite DB.
KernelService periodically /1 per hour/ send requests to external price feeders. The close prices are agregated using average algorithm. Result saved in SQLite DB. KerneleService make extra price request if receive price request message from PriceProvider service.



