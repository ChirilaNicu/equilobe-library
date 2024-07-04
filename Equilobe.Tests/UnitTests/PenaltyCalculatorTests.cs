using Equilobe.Core.Shared.Models;
using Equilobe.Core.Features.Loans;
using Xunit;
using Equilobe.Core.Shared;

namespace Equilobe.Tests.UnitTests
{
    public class PenaltyCalculatorTests
    {
        private readonly IPenaltyCalculator _simplePenaltyCalculator;

        public PenaltyCalculatorTests()
        {
            _simplePenaltyCalculator = new SimplePenaltyCalculator();
        }

        [Fact]
        public void Should_Calculate_Penalty_Without_QualityState_Consideration()
        {
            // Arrange
            var rentPrice = new Money(100m, Currency.RON);
            var dueDate = DateTime.UtcNow.AddDays(-10);
            var returnDate = DateTime.UtcNow;

            // Act
            var penalty = _simplePenaltyCalculator.CalculatePenalty(rentPrice, 5, dueDate, returnDate);

            // Assert
            Assert.Equal(10m, penalty); // 10 days overdue, 1% per day of 100 = 10
        }
    }
}
