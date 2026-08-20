namespace PriceCollector.PriceHandlers
{
    public interface InterfacePriceAgregator
    {
        public decimal AverageAgregator(List<decimal> pricesList);
    }
}
