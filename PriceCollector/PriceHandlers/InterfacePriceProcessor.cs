using PriceCollector.Services;

namespace PriceCollector.PriceHandlers
{
    public interface InterfacePriceProcessor
    {   
        public void ProcessorInit(Dictionary<string, string> priceFeeders, string connStrDb);

        public void Processing(long reqTime);
        public void SavePrice();

    }
}
