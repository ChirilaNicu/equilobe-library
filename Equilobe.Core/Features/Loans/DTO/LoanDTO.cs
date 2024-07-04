using Equilobe.Core.Features.Books;

namespace Equilobe.Core.Features.Loans.DTO
{
    public class LoanDTO
    {
        public Guid Id { get; set; }
        public Guid? BookId { get; set; }
        public Guid UserId { get; set; }
        public DateTime LoanDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public decimal PaidAmount { get; set; }
        public string? BookTitle { get; set; }

        public LoanDTO(Loan loan, bool includeBookDetails = false, string? bookTitle = null)
        {
            Id = loan.Id;
            BookId = includeBookDetails ? (Guid?)loan.BookId : null;
            UserId = loan.UserId;
            LoanDate = loan.LoanDate;
            DueDate = loan.DueDate;
            ReturnDate = loan.ReturnDate;
            PaidAmount = loan.PaidAmount.Amount;
            BookTitle = bookTitle;
        }
    }
}
