using Equilobe.Core.Features.Books;
using Equilobe.Core.Features.Books.Queries;
using Equilobe.Core.Features.Loans;
using Equilobe.Core.Features.Loans.DTO;
using Equilobe.Core.Features.Loans.Queries;
using Equilobe.Core.Shared;
using Equilobe.Core.Shared.Models;

using MockQueryable.Moq;
using Moq;


namespace Equilobe.Tests.UnitTests
{
    public class QueryHandlerTests
    {
        private readonly Mock<ILibraryDbContext> _libraryDbContextMock;

        private readonly GetBooksQueryHandler _getAllBooksQueryHandler;
        private readonly GetAvailableBooksQueryHandler _getNumberAvailableBookQueryHandler;
        private readonly GetLoansQueryHandler _getLoansQueryHandler;

        public QueryHandlerTests()
        {
            _libraryDbContextMock = new Mock<ILibraryDbContext>();

            _getAllBooksQueryHandler = new GetBooksQueryHandler(_libraryDbContextMock.Object);
            _getNumberAvailableBookQueryHandler = new GetAvailableBooksQueryHandler(_libraryDbContextMock.Object);
            _getLoansQueryHandler = new GetLoansQueryHandler(_libraryDbContextMock.Object);
        }

        [Fact]
        public async Task Should_Get_All_Books()
        {
            var books = new List<Book>
            {
                new Book(new Money(10m, Currency.USD), new BookMetadata("Title1", new Author("Author", "1"), "9781234567890")),
                new Book(new Money(15m, Currency.EUR), new BookMetadata("Title2", new Author("Author", "2"), "9781234567891")),
            };

            var booksDbSetMock = books.AsQueryable().BuildMockDbSet();
            _libraryDbContextMock.Setup(db => db.Books).Returns(booksDbSetMock.Object);

            var result = await _getAllBooksQueryHandler.Handle(new GetBooksQuery(), CancellationToken.None);

            Assert.Equal(books.Count, result.Items.Count); // Check the number of books returned

            for (int i = 0; i < books.Count; i++)
            {
                Assert.Equal(books[i].Metadata.Title, result.Items[i].Metadata.Title);
                Assert.Equal(books[i].Metadata.Author, result.Items[i].Metadata.Author);
            }

            _libraryDbContextMock.Verify(db => db.Books, Times.Once);
        }

        [Fact]
        public async Task Should_Get_Number_Available_Books()
        {
            var isbn1 = "9781234567890";
            var bookMetadata = new BookMetadata("Title1", new Author("Author", "1"), isbn1);
            var book1 = new Book(new Money(10m, Currency.USD), bookMetadata);
            var book2 = new Book(new Money(9m, Currency.EUR), bookMetadata);

            var booksDbSetMock = new List<Book>() { book1, book2 }
                .AsQueryable()
                .BuildMockDbSet();
            _libraryDbContextMock.Setup(db => db.Books).Returns(booksDbSetMock.Object);

            var query = new GetAvailableBooksQuery(isbn1);
            var result = await _getNumberAvailableBookQueryHandler.Handle(query, CancellationToken.None);

            Assert.Equal(2, result);
        }

        [Fact]
        public async Task Handle_ReturnsFilteredAndPaginatedBooks_HappyPath()
        {
            // Arrange
            var books = new List<Book>
            {
                new Book(new Money(10m, Currency.USD), new BookMetadata("Title1", new Author("Author", "1"), "9781234567890")),
                new Book(new Money(15m, Currency.EUR), new BookMetadata("Title2", new Author("Author", "2"), "9781234567891")),
                new Book(new Money(20m, Currency.USD), new BookMetadata("Title3", new Author("Author", "3"), "9781234567892")),
            };

            var booksDbSetMock = books.AsQueryable().BuildMockDbSet();
            _libraryDbContextMock.Setup(x => x.Books).Returns(booksDbSetMock.Object);

            var handler = new GetBooksQueryHandler(_libraryDbContextMock.Object);
            var query = new GetBooksQuery
            {
                Title = "Title",
                QualityState = null,
                IsAvailable = true,
                PageNumber = 1,
                PageSize = 2,
                SortBy = "title",
                SortDirection = "asc"
            };

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(2, result.Items.Count); 
            Assert.Equal("Title1", result.Items.First().Metadata.Title);
            Assert.Equal("Title2", result.Items.Last().Metadata.Title);
        }
        [Fact]
        public async Task Handle_ReturnsEmptyList_WhenNoBooksMatchFilters()
        {
            var books = new List<Book>
            {
                new Book(new Money(10m, Currency.USD), new BookMetadata("Title1", new Author("Author", "1"), "9781234567890")),
                new Book(new Money(15m, Currency.EUR), new BookMetadata("Title2", new Author("Author", "2"), "9781234567891")),
            };

            var booksDbSetMock = books.AsQueryable().BuildMockDbSet();
            _libraryDbContextMock.Setup(x => x.Books).Returns(booksDbSetMock.Object);

            var handler = new GetBooksQueryHandler(_libraryDbContextMock.Object);
            var query = new GetBooksQuery
            {
                Title = "NonexistentTitle",
                QualityState = null,
                IsAvailable = true,
                PageNumber = 1,
                PageSize = 10,
                SortBy = "title",
                SortDirection = "asc"
            };

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Empty(result.Items);  
        }
        [Fact]
        public async Task Handle_ReturnsPaginatedLoans_Correctly()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var bookId = Guid.NewGuid();
            var loans = new List<Loan>
            {
                new Loan(bookId, userId, DateTime.UtcNow.AddDays(-5)),
                new Loan(bookId, userId, DateTime.UtcNow.AddDays(-4)),
                new Loan(bookId, userId, DateTime.UtcNow.AddDays(-3)),
                new Loan(bookId, userId, DateTime.UtcNow.AddDays(-2)),
                new Loan(bookId, userId, DateTime.UtcNow.AddDays(-1))
            };

            var loansDbSetMock = loans.AsQueryable().BuildMockDbSet();
            _libraryDbContextMock.Setup(x => x.Loans).Returns(loansDbSetMock.Object);

            var handler = new GetLoansQueryHandler(_libraryDbContextMock.Object);
            var query = new GetLoansQuery
            {
                PageNumber = 1,
                PageSize = 3,
                SortBy = "loandate",
                SortDirection = "asc",
                IncludeBookDetails = false
            };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Items.Count); 
            Assert.Equal(loans[0].LoanDate, result.Items.First().LoanDate);
            Assert.Equal(loans[2].LoanDate, result.Items.Last().LoanDate);
            Assert.Equal(5, result.TotalItems); 
        }
    }
}
