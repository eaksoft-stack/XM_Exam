namespace PriceCollector.PriceHandlers
{
    public class AveragePriceAgregator: InterfacePriceAgregator
    {
        public decimal AverageAgregator(List<decimal> pricesList)
        {
            decimal resultPrice = 0.0m;
        
            if (pricesList.Count > 0)
            {
                resultPrice=pricesList.Average();
            }

            return resultPrice;

        }

    }
}
