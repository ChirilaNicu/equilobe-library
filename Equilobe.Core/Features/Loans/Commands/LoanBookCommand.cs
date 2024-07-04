using Equilobe.Core.Features.Users;
using Equilobe.Core.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Equilobe.Core.Features.Loans.Commands;

public class LoanBookCommand : IRequest
{
    public required string ISBN { get; init; }
    public required string Username { get; init; }
    public DateTime? LoanDate { get; init; }
}

public class LoanBookCommandHandler : IRequestHandler<LoanBookCommand>
{
    private readonly ILibraryDbContext _dbContext;

    public LoanBookCommandHandler(ILibraryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(LoanBookCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
         .FirstOrDefaultAsync(u => u.Username == request.Username, cancellationToken)
         ?? throw new KeyNotFoundException(nameof(User));


        var books = _dbContext.Books.Where(b => b.Metadata.ISBN == request.ISBN);

        // We take by default the best book to rent,
        var bestQualityBookItem = await books
                          .Where(bi => bi.IsAvailable) // Only available books
                          .OrderByDescending(bi => bi.QualityState)
                          .FirstOrDefaultAsync(cancellationToken) ?? throw new ArgumentException("No available book found.");
        var loan = new Loan(bestQualityBookItem.Id, user.Id, request.LoanDate ?? DateTime.UtcNow);

        await _dbContext.Loans.AddAsync(loan, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
