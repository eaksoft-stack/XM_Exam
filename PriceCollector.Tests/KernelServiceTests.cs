using System;
using System.Reflection;
using System.Runtime.Serialization;
using Xunit;

namespace PriceCollector.Tests
{
    public class KernelServiceTests
    {
        [Fact]
        public void CheckTime_WhenPassedHourIsDifferent_ReturnsTrue_And_UpdatesPassedHour()
        {
            // Arrange
            var ksType = Type.GetType("PriceCollector.Services.KernelService, PriceCollector");
            Assert.NotNull(ksType);

            // Create instance without invoking ctor to avoid side effects
            var instance = FormatterServices.GetUninitializedObject(ksType);

            // Compute current hour rounded to the hour (as CheckTime does)
            DateTimeOffset now = DateTimeOffset.Now;
            var prevHour = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, now.Offset);

            // Set the private field 'passedHour' to a different value (one hour earlier)
            var passedHourField = ksType.GetField("passedHour", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(passedHourField);
            passedHourField.SetValue(instance, prevHour.AddHours(-1));

            // Act
            var method = ksType.GetMethod("CheckTime", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            bool result = (bool)method.Invoke(instance, Array.Empty<object>());

            // Assert
            Assert.True(result, "CheckTime should return true when passedHour differs from current hour.");

            var updatedPassedHour = (DateTimeOffset)passedHourField.GetValue(instance);
            Assert.Equal(prevHour, updatedPassedHour);
        }

        [Fact]
        public void CheckTime_WhenPassedHourIsSame_ReturnsFalse()
        {
            // Arrange
            var ksType = Type.GetType("PriceCollector.Services.KernelService, PriceCollector");
            Assert.NotNull(ksType);

            var instance = FormatterServices.GetUninitializedObject(ksType);

            DateTimeOffset now = DateTimeOffset.Now;
            var prevHour = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, now.Offset);

            var passedHourField = ksType.GetField("passedHour", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(passedHourField);
            passedHourField.SetValue(instance, prevHour);

            var method = ksType.GetMethod("CheckTime", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            // Act
            bool result = (bool)method.Invoke(instance, Array.Empty<object>());

            // Assert
            Assert.False(result, "CheckTime should return false when passedHour equals current hour.");
        }
    }
}
