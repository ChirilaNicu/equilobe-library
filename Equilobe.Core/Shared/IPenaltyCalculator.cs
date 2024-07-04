using Equilobe.Core.Shared.Models;
using Equilobe.Core.Features.Loans;


namespace Equilobe.Core.Shared
{
    public interface IPenaltyCalculator
    {
        decimal CalculatePenalty(Money rentPrice, int differenceQualityState, DateTime dueDate, DateTime returnDate);
    }
}
