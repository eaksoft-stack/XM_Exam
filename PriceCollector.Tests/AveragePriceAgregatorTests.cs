using System.Collections.Generic;
using PriceCollector.PriceHandlers;
using Xunit;

namespace PriceCollector.Tests
{
    public class AveragePriceAgregatorTests
    {
        [Fact]
        public void AverageAgregator_ReturnsCorrectAverage_ForNonEmptyList()
        {
            // Arrange
            var agregator = new AveragePriceAgregator();
            var prices = new List<decimal> { 1.5m, 2.5m, 3.0m };

            // Act
            var result = agregator.AverageAgregator(prices);

            // Assert
            Assert.Equal(7.0m / 3.0m, result);
        }

        [Fact]
        public void AverageAgregator_ReturnsZero_ForEmptyList()
        {
            // Arrange
            var agregator = new AveragePriceAgregator();
            var prices = new List<decimal>();

            // Act
            var result = agregator.AverageAgregator(prices);

            // Assert
            Assert.Equal(0.0m, result);
        }
    }
}
