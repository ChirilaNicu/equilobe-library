using Equilobe.Core.Features.Books;
using Equilobe.Core.Features.Books.Commands;
using Equilobe.Core.Features.Loans;
using Equilobe.Core.Features.Loans.Commands;
using Equilobe.Core.Features.Users;
using Equilobe.Core.Shared.Models;
using Equilobe.Core.Shared;
using MockQueryable.Moq;
using Moq;

namespace Equilobe.Tests.UnitTests
{
    public class CommandHandlerTests
    {
        private readonly Mock<ILibraryDbContext> _libraryDbContextMock;
        private readonly Mock<IPenaltyCalculator> _penaltyCalculatorMock;

        private readonly AddBookCommandHandler _addBookHandler;
        private readonly DeleteBookCommandHandler _deleteBookHandler;
        private readonly LoanBookCommandHandler _loanBookCommandHandler;
        private readonly ReturnBookCommandHandler _returnBookCommandHandler;

        public CommandHandlerTests()
        {
            // Arrange
            _libraryDbContextMock = new Mock<ILibraryDbContext>();
            _penaltyCalculatorMock = new Mock<IPenaltyCalculator>();

            _addBookHandler = new AddBookCommandHandler(_libraryDbContextMock.Object);
            _deleteBookHandler = new DeleteBookCommandHandler(_libraryDbContextMock.Object);
            _loanBookCommandHandler = new LoanBookCommandHandler(_libraryDbContextMock.Object);
            _returnBookCommandHandler = new ReturnBookCommandHandler(_libraryDbContextMock.Object, _penaltyCalculatorMock.Object);
        }

         [Fact]
        public async Task Should_Add_Book()
        {
            // Arrange
            var command = new AddBookCommand
            {
                AuthorFirstName = "Some",
                AuthorLastName = "Author",
                ISBN = "9781234567890",
                Title = "Some Title",
                RentPrice = 10,
                RentCurrency = Currency.EUR,
            };

            // Setup mock to simulate the behavior of the db
            var authorsMock = new List<Author>().AsQueryable().BuildMockDbSet();
            _libraryDbContextMock.Setup(db => db.Authors).Returns(authorsMock.Object);

            // Act
            await _addBookHandler.Handle(command, CancellationToken.None);

            // Assert
            _libraryDbContextMock.Verify(db => db.Authors.Add(It.IsAny<Author>()), Times.Once);
            _libraryDbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Should_Delete_Book()
        {
            // Arrange
            string title = "Some Title";
            Author author = new("Some", "Author");
            string isbn = "9781234567890";
            Money money = new(10.23m, Currency.RON);

            // Create a book object
            var bookMetadata = new BookMetadata(title, author, isbn);
            var book = new Book(money, bookMetadata);
            var booksMock = new List<Book>() { book  }
                .AsQueryable()
                .BuildMockDbSet();

            // Set up the repository to remove the book when RemoveBook is called
            _libraryDbContextMock.Setup(x => x.Books).Returns(booksMock.Object);
            _libraryDbContextMock.Setup(repo => repo.Books.Remove(book)).Verifiable();

            // Now prepare to delete the book
            var deleteCommand = new DeleteBookCommand { BookId = book.Id };

            // Act
            await _deleteBookHandler.Handle(deleteCommand, CancellationToken.None);

            // Assert
            _libraryDbContextMock.Verify(repo => repo.Books.Remove(book), Times.Once);
        }

        [Fact]
        public async Task Should_Loan_Book()
        {
            // Arrange
            var isbn = "1234567891011";
            var mockReader = new User("Cititor1", UserRole.Reader);
            var mockLibrarian = new User("Bibliotecar1", UserRole.Librarian);
            var mockBookMetadata = new BookMetadata("Sample Book", new Author("John", "Doe"), isbn);
            var mockBook = new Book(new Money(50m, Currency.RON), mockBookMetadata);

            var usersDbSetMock = new List<User>() { mockReader, mockLibrarian }
                .AsQueryable()
                .BuildMockDbSet();
            var booksDbSetMock = new List<Book>() { mockBook }
                .AsQueryable()
                .BuildMockDbSet();

            _libraryDbContextMock.Setup(x => x.Users).Returns(usersDbSetMock.Object);
            _libraryDbContextMock.Setup(x => x.Books).Returns(booksDbSetMock.Object);
            _libraryDbContextMock.Setup(x => x.Loans.AddAsync(It.Is<Loan>(l => l.UserId == mockReader.Id && l.BookId == mockBook.Id), It.IsAny<CancellationToken>()))
                .Verifiable();
            var command = new LoanBookCommand
            {
                ISBN = isbn,
                Username = mockReader.Username,
            };

            // Act
            await _loanBookCommandHandler.Handle(command, CancellationToken.None);

            // Assert
            _libraryDbContextMock.Verify(x => x.Loans.AddAsync(It.Is<Loan>(l => l.UserId == mockReader.Id && l.BookId == mockBook.Id), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Should_Return_Book()
        {
            // Arrange
            var mockUser = new User("Bibliotecar1", UserRole.Librarian);
            var bookItemId = Guid.NewGuid();
            var loan = new Loan(bookItemId, mockUser.Id);
            var loansMock = new List<Loan>() { loan }
                .AsQueryable()
                .BuildMockDbSet();

            _libraryDbContextMock.Setup(x => x.Loans).Returns(loansMock.Object);

            var returnDate = new DateTime(2023, 1, 1); // fixed date
            var command = new ReturnBookCommand
            {
                BookId = bookItemId,
                BookQuality = BookQualityState.Good,
                ReturnDate = returnDate,
            };

            // Act
            await _returnBookCommandHandler.Handle(command, default);

            // Assert
            Assert.Equal(returnDate, loan.ReturnDate);
        }
    }
}
