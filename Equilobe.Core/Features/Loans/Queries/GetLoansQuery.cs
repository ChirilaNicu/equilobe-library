using Equilobe.Core.Features.Loans.DTO;
using Equilobe.Core.Shared.Pagination;
using Microsoft.EntityFrameworkCore;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Equilobe.Core.Shared;

namespace Equilobe.Core.Features.Loans.Queries
{

    public class GetLoansQuery : IRequest<PaginatedList<LoanDTO>>
    {
        public Guid? UserId { get; set; }
        public DateTime? LoanDate { get; set; }
        public bool? IsReturned { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string SortBy { get; set; } = "id";
        public string SortDirection { get; set; } = "asc";
        public bool IncludeBookDetails { get; set; } = false;
    }


    public class GetLoansQueryHandler : IRequestHandler<GetLoansQuery, PaginatedList<LoanDTO>>
    {
        private readonly ILibraryDbContext _context;

        public GetLoansQueryHandler(ILibraryDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedList<LoanDTO>> Handle(GetLoansQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Loans.AsQueryable();

            if (request.UserId.HasValue)
            {
                query = query.Where(l => l.UserId == request.UserId.Value);
            }

            if (request.LoanDate.HasValue)
            {
                query = query.Where(l => l.LoanDate.Date == request.LoanDate.Value.Date);
            }

            if (request.IsReturned.HasValue)
            {
                query = query.Where(l => l.ReturnDate.HasValue == request.IsReturned.Value);
            }

            switch (request.SortBy.ToLower())
            {
                case "loandate":
                    query = request.SortDirection == "desc" ? query.OrderByDescending(l => l.LoanDate) : query.OrderBy(l => l.LoanDate);
                    break;
                case "returndate":
                    query = request.SortDirection == "desc" ? query.OrderByDescending(l => l.ReturnDate) : query.OrderBy(l => l.ReturnDate);
                    break;
                default:
                    query = request.SortDirection == "desc" ? query.OrderByDescending(l => l.Id) : query.OrderBy(l => l.Id);
                    break;
            }

            var totalItems = await query.CountAsync(cancellationToken);
            var loans = await query.Skip((request.PageNumber - 1) * request.PageSize)
                                   .Take(request.PageSize)
                                   .ToListAsync(cancellationToken);

            var loanDTOs = new List<LoanDTO>();

            if (request.IncludeBookDetails)
            {
                var bookIds = loans.Select(l => l.BookId).ToList();
                var books = await _context.Books.Where(b => bookIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, cancellationToken);

                foreach (var loan in loans)
                {
                    books.TryGetValue(loan.BookId, out var book);
                    var bookTitle = book?.Metadata.Title;
                    loanDTOs.Add(new LoanDTO(loan, true, bookTitle));
                }
            }
            else
            {
                loanDTOs = loans.Select(l => new LoanDTO(l, false)).ToList();
            }

            return new PaginatedList<LoanDTO>(loanDTOs, totalItems, request.PageNumber, request.PageSize);
        }
    }
}

