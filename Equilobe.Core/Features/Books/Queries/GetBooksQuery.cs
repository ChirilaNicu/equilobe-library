using Equilobe.Core.Features.Books.DTO;
using Equilobe.Core.Shared.Pagination;
using Microsoft.EntityFrameworkCore;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Equilobe.Core.Shared.Models;
using Equilobe.Core.Shared;

namespace Equilobe.Core.Features.Books.Queries
{
    public class GetBooksQuery : IRequest<PaginatedList<BookDTO>>
    {
        public string? Title { get; set; }
        public string? QualityState { get; set; }
        public bool? IsAvailable { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string SortBy { get; set; } = "id";
        public string SortDirection { get; set; } = "asc";
    }

    public class GetBooksQueryHandler : IRequestHandler<GetBooksQuery, PaginatedList<BookDTO>>
    {
        private readonly ILibraryDbContext _context;

        public GetBooksQueryHandler(ILibraryDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedList<BookDTO>> Handle(GetBooksQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Books.AsQueryable();

            if (!string.IsNullOrEmpty(request.Title))
            {
                query = query.Where(b => b.Metadata.Title.Contains(request.Title));
            }

            if (!string.IsNullOrEmpty(request.QualityState))
            {
                if (Enum.TryParse<BookQualityState>(request.QualityState, out var qualityState))
                {
                    query = query.Where(b => b.QualityState == qualityState);
                }
                else
                {
                    throw new ArgumentException("Invalid QualityState value");
                }
            }

            if (request.IsAvailable.HasValue)
            {
                query = query.Where(b => b.IsAvailable == request.IsAvailable.Value);
            }

            switch (request.SortBy.ToLower())
            {
                case "title":
                    query = request.SortDirection == "desc" ? query.OrderByDescending(b => b.Metadata.Title) : query.OrderBy(b => b.Metadata.Title);
                    break;
                case "creationdate":
                    query = request.SortDirection == "desc" ? query.OrderByDescending(b => b.CreatedAt) : query.OrderBy(b => b.CreatedAt);
                    break;
                default:
                    query = request.SortDirection == "desc" ? query.OrderByDescending(b => b.Id) : query.OrderBy(b => b.Id);
                    break;
            }

            var totalItems = await query.CountAsync(cancellationToken);
            var books = await query.Skip((request.PageNumber - 1) * request.PageSize)
                                   .Take(request.PageSize)
                                   .ToListAsync(cancellationToken);

            var bookDTOs = books.Select(b => new BookDTO(b)).ToList();

            return new PaginatedList<BookDTO>(bookDTOs, totalItems, request.PageNumber, request.PageSize);
        }
    }
}
