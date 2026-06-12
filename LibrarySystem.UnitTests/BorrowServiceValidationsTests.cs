using LibrarySystem.Data.Context;
using LibrarySystem.Data.Models;
using LibrarySystem.Data.Repositories;
using LibrarySystem.Services.Exceptions;
using LibrarySystem.Services.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shouldly;
using Xunit;

namespace LibrarySystem.UnitTests;

public class BorrowServiceValidationsTests : IDisposable
{
    private readonly Mock<IBookRepository> _bookRepositoryMock;
    private readonly Mock<IMemberRepository> _memberRepositoryMock;
    private readonly Mock<IBorrowRepository> _borrowRepositoryMock;
    private readonly LibraryAppDbContext _dbContext;
    private readonly BorrowService _borrowService;

    public BorrowServiceValidationsTests()
    {
        var options = new DbContextOptionsBuilder<LibraryAppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new LibraryAppDbContext(options);

        _bookRepositoryMock = new Mock<IBookRepository>();
        _memberRepositoryMock = new Mock<IMemberRepository>();
        _borrowRepositoryMock = new Mock<IBorrowRepository>();

        _borrowService = new BorrowService(
                _bookRepositoryMock.Object,
                _memberRepositoryMock.Object,
                _borrowRepositoryMock.Object,
                _dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    // when the member has 3 active loans, BorrowBookAsync throws a custom LoanLimitExceededException.
    // Use ShouldThrowAsync<LoanLimitExceededException>
    [Fact]
    public async Task IfMemberHas3ActiveBorrows_BorrowBookAsync_ThrowsBorrowLimitExceededException()
    {
        // Arrange
        var memberId = 1;
        var bookId = 1;

        var member = new Member
        {
            Id = memberId,
            MembershipExpiryDate = DateTime.UtcNow.AddDays(30),
            OutstandingFine = 0
        };

        var book = new Book
        {
            Id = bookId,
            AvailableCopies = 1
        };

        var activeLoans = new List<Borrow>
        {
            new() { Id = 1, MemberId = memberId, ReturnedAt = null },
            new() { Id = 2, MemberId = memberId, ReturnedAt = null },
            new() { Id = 3, MemberId = memberId, ReturnedAt = null }
        };

        // Setup mocks
        _memberRepositoryMock.Setup(m => m.GetByIdAsync(memberId))
            .ReturnsAsync(member);

        _bookRepositoryMock.Setup(b => b.GetByIdAsync(bookId))
            .ReturnsAsync(book);

        _borrowRepositoryMock.Setup(b => b.GetActiveBorrowsByMemberIdAsync(memberId))
            .ReturnsAsync(activeLoans);

        // Act & Assert
        var exception = await Should.ThrowAsync<LoanLimitExceededException>(() =>
            _borrowService.BorrowBookAsync(bookId, memberId));

        exception.Message.ShouldBe("Member cannot borrow more than 3 books at the same time");
    }

    // when AvailableCopies == 0, calling BorrowBookAsync throws BookNotAvailableException
   [Fact]
    public async Task IfAvailableCopiesIsZero_BorrowBookAsync_ThrowsBookNotAvailableException()
    {
        // Arrange
        var memberId = 1;
        var bookId = 1;

        var member = new Member
        {
            Id = memberId,
            MembershipExpiryDate = DateTime.UtcNow.AddDays(30),
            OutstandingFine = 0
        };

        var book = new Book
        {
            Id = bookId,
            AvailableCopies = 0
        };

        var availableBooks = new List<Borrow>();

        // Setup mocks
        _memberRepositoryMock.Setup(m => m.GetByIdAsync(memberId))
            .ReturnsAsync(member);

        _bookRepositoryMock.Setup(b => b.GetByIdAsync(bookId))
            .ReturnsAsync(book);

        _borrowRepositoryMock.Setup(b => b.GetActiveBorrowsByMemberIdAsync(memberId))
            .ReturnsAsync(availableBooks);

        // Act & Assert
        var exception = await Should.ThrowAsync<BookNotAvailableException>(() =>
            _borrowService.BorrowBookAsync(bookId, memberId));

        exception.Message.ShouldBe("No copies of this book are available for borrowing");
    }

    // when MembershipExpiryDate is yesterday, calling BorrowBookAsync throws MembershipExpiredException.
    [Fact]
    public async Task IfMembershipExpiryDateIsYesterday_BorrowBookAsync_ThrowsMembershipExpiredException()
    {
        // Arrange
        var memberId = 1;
        var bookId = 1;

        var member = new Member
        {
            Id = memberId,
            MembershipExpiryDate = DateTime.Today.AddDays(-1)
        };

        var book = new Book
        {
            Id = bookId,
            Title = "Title",
        };

        _memberRepositoryMock.Setup(m => m.GetByIdAsync(memberId)).ReturnsAsync(member);
        _bookRepositoryMock.Setup(b => b.GetByIdAsync(bookId)).ReturnsAsync(book);

        // Act & Assert
        var exception = await Should.ThrowAsync<MembershipExpiredException>(() =>
            _borrowService.BorrowBookAsync(bookId, memberId));

        exception.Message.ShouldBe("Membership has expired");
    }

    // when the member has OutstandingFine > 0, calling BorrowBookAsync throws OutstandingFineException.
    [Fact]
    public async Task IfMemberHasOutstandingFine_BorrowBookAsync_ThrowsOutstandingFineException()
    {
        // Arrange
        var memberId = 1;
        var bookId = 1;
        var fine = 30m;

        var member = new Member
        {
            Id = memberId,
            OutstandingFine = fine,
            MembershipExpiryDate = DateTime.UtcNow.AddDays(30)
        };

        var book = new Book
        {
            Id = bookId,
            Title = "Title",
            AvailableCopies = 1
        };

        var activeLoans = new List<Borrow>();

        _memberRepositoryMock.Setup(m => m.GetByIdAsync(memberId)).ReturnsAsync(member);
        _bookRepositoryMock.Setup(b => b.GetByIdAsync(bookId)).ReturnsAsync(book);
        _borrowRepositoryMock.Setup(b => b.GetActiveBorrowsByMemberIdAsync(memberId)).ReturnsAsync(activeLoans);

        // Act & Assert
        var exception = await Should.ThrowAsync<OutstandingFineException>(() =>
            _borrowService.BorrowBookAsync(bookId, memberId));

        exception.Message.ShouldBe($"Member has outstanding fine of £{fine}");
    }

    // a successful borrow calls ILoanRepository.AddAsync() exactly once
    // verify this with mock.Verify()
    [Fact]
    public async Task SuccessfullBorrowCalls_IBorrowRepository_AddAsync_OneTimeOnly()
    {
        // Arrange
        var bookId = 1;
        var memberId = 1;

        var book = new Book
        {
            Id = bookId,
            Title = "Test Book",
            AvailableCopies = 5,
            TotalCopies = 5,
            ISBN = "1234567890123",
            Author = "Test Author"
        };

        var member = new Member
        {
            Id = memberId,
            FullName = "John Doe",
            Email = "john@example.com",
            MembershipExpiryDate = DateTime.UtcNow.AddMonths(6),
            OutstandingFine = 0
        };

        var activeBorrows = new List<Borrow>();

        _memberRepositoryMock.Setup(m => m.GetByIdAsync(memberId)).ReturnsAsync(member);
        _bookRepositoryMock.Setup(b => b.GetByIdAsync(bookId)).ReturnsAsync(book);
        _borrowRepositoryMock.Setup(x => x.GetActiveBorrowsByMemberIdAsync(memberId)).ReturnsAsync(activeBorrows);
        _borrowRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Borrow>())).Returns(Task.CompletedTask);

        // Act
        var result = await _borrowService.BorrowBookAsync(bookId, memberId);

        // Assert
        _borrowRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Borrow>()),
            Times.Once);

        book.AvailableCopies.ShouldBe(4);
    }
}
