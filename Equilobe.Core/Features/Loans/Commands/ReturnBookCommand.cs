using Equilobe.Core.Shared.Models;
using Equilobe.Core.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Equilobe.Core.Features.Books;

namespace Equilobe.Core.Features.Loans.Commands
{
    public class ReturnBookCommand : IRequest<Unit>
    {
        public required Guid BookId { get; init; }
        public DateTime? ReturnDate { get; init; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public required BookQualityState BookQuality { get; init; }
    }

    public class ReturnBookCommandHandler : IRequestHandler<ReturnBookCommand, Unit>
    {
        private readonly ILibraryDbContext _dbContext;
        private readonly IPenaltyCalculator _penaltyCalculator;

        public ReturnBookCommandHandler(ILibraryDbContext dbContext, IPenaltyCalculator penaltyCalculator)
        {
            _dbContext = dbContext;
            _penaltyCalculator = penaltyCalculator;
        }

        public async Task<Unit> Handle(ReturnBookCommand request, CancellationToken cancellationToken)
        {
            var loan = await _dbContext.Loans
                .FirstOrDefaultAsync(l => l.BookId == request.BookId, cancellationToken)
                ?? throw new KeyNotFoundException(nameof(Loan));

            var book = await _dbContext.Books
                .FirstOrDefaultAsync(b => b.Id == request.BookId, cancellationToken)
                ?? throw new KeyNotFoundException(nameof(Book));

            var penalty = _penaltyCalculator.CalculatePenalty(book.RentPrice, request.BookQuality - book.QualityState, loan.DueDate, request.ReturnDate ?? DateTime.UtcNow);

            loan.ReturnBook(request.BookQuality, request.ReturnDate ?? DateTime.UtcNow, penalty);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
