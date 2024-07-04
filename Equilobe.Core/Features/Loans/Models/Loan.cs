using Equilobe.Core.Shared.Models;
using Equilobe.Core.Shared.SeedWork;
using Equilobe.Core.DomainEvents;
using Equilobe.Core.Features.Users;
using Equilobe.Core.Features.Books;
using Microsoft.VisualBasic;
using Equilobe.Core.Shared;

namespace Equilobe.Core.Features.Loans;

public class Loan : Entity, IAggregateRoot
{
    public Guid BookId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime LoanDate { get; private set; }
    public DateTime DueDate { get; private set; }
    public DateTime? ReturnDate { get; private set; }
    public Money PaidAmount { get; private set; } = null!;
    public User User { get; private set; } = null!;
    public Book Book { get; private set; } = null!;

    private Loan() { }

    public Loan(Guid bookId, Guid userId, DateTime? dueDate = null) : base()
    {
        BookId = bookId;
        UserId = userId;
        LoanDate = DateTime.UtcNow;
        DueDate = dueDate ?? LoanDate.AddDays(14);
        ReturnDate = null;
        PaidAmount = new Money(0, Currency.RON);
        AddDomainEvent(new BookLoanedEvent(bookId));
    }

    public void ReturnBook(BookQualityState qualityState, DateTime returnDate, decimal penalty)
    {
        ReturnDate = returnDate;
        PaidAmount = new Money(penalty, Currency.RON);
        AddDomainEvent(new BookReturnedEvent(BookId, qualityState, returnDate));
    }
}

public class DefaultPenaltyCalculator : IPenaltyCalculator
{
    public decimal CalculatePenalty(Money rentPrice, int differenceQualityState, DateTime dueDate, DateTime returnDate)
    {
        decimal totalAmount = 0;
        if (differenceQualityState > 2)
        {
            totalAmount = differenceQualityState * rentPrice.Amount * 0.2m; // 20% of rent price per difference quality state
        }

        var overdueDays = (returnDate - dueDate).Days;
        if (overdueDays > 0)
        {
            totalAmount += overdueDays * (rentPrice.Amount * 0.01m); // 1% of rent price per day
        }
        return totalAmount;
    }
}
public class SimplePenaltyCalculator : IPenaltyCalculator
{
    public decimal CalculatePenalty(Money rentPrice, int differenceQualityState, DateTime dueDate, DateTime returnDate)
    {
        var overdueDays = (returnDate - dueDate).Days;
        if (overdueDays <= 0) return 0;

        return overdueDays * (rentPrice.Amount * 0.01m); // 1% of rent price per day
    }
}