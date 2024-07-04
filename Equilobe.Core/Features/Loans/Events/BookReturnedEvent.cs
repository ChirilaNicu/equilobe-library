using Equilobe.Core.Features.Books;
using Equilobe.Core.Features.Loans;
using Equilobe.Core.Shared.Models;
using Equilobe.Core.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Equilobe.Core.DomainEvents
{
    public class BookReturnedEvent : INotification
    {
        public Guid BookId { get; }
        public DateTime ReturnDate { get; }
        public BookQualityState QualityState { get; }

        public BookReturnedEvent(Guid bookId, BookQualityState qualityState, DateTime returnDate)
        {
            BookId = bookId;
            QualityState = qualityState;
            ReturnDate = returnDate;
        }
    }

    public class BookReturnedEventHandler : INotificationHandler<BookReturnedEvent>
    {
        private readonly ILibraryDbContext _dbContext;
        private readonly IPenaltyCalculator _penaltyCalculator;

        public BookReturnedEventHandler(ILibraryDbContext dbContext, IPenaltyCalculator penaltyCalculator)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _penaltyCalculator = penaltyCalculator ?? throw new ArgumentNullException(nameof(penaltyCalculator));
        }

        public async Task Handle(BookReturnedEvent notification, CancellationToken cancellationToken)
        {
            var book = await _dbContext.Books
                .FirstOrDefaultAsync(b => b.Id == notification.BookId, cancellationToken)
                ?? throw new KeyNotFoundException(nameof(Book));

            var loan = await _dbContext.Loans
                .FirstOrDefaultAsync(l => l.BookId == notification.BookId, cancellationToken)
                ?? throw new KeyNotFoundException(nameof(Loan));

            var differenceQualityState = notification.QualityState - book.QualityState;
            var penalty = _penaltyCalculator.CalculatePenalty(book.RentPrice, differenceQualityState, loan.DueDate, notification.ReturnDate);

            loan.ReturnBook(notification.QualityState, notification.ReturnDate, penalty);

            book.ReturnBook(notification.QualityState);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
