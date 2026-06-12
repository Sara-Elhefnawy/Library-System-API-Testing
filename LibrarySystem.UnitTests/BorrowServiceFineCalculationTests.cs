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

public class BorrowServiceFineCalculationTests : IDisposable
{
    private readonly Mock<IBookRepository> _bookRepositoryMock;
    private readonly Mock<IMemberRepository> _memberRepositoryMock;
    private readonly Mock<IBorrowRepository> _borrowRepositoryMock;
    private readonly LibraryAppDbContext _dbContext;
    private readonly BorrowService _borrowService;

    public BorrowServiceFineCalculationTests()
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

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0.50)]
    [InlineData(3, 1.50)]
    [InlineData(14, 7)]
    [InlineData(-5, 0)]
    public void FineCalculationLogic(int daysOverdue, decimal expectedFine)
    {
        // Arrange
        var dueDate = new DateTime(2024, 1, 1);
        DateTime? returnDate;

        // Returned before or on due date
        if (daysOverdue <= 0)
        {
            // due: Jan 1, return; Dec 31
            returnDate = dueDate.AddDays(daysOverdue - 1);
        }
        else
        {
            returnDate = dueDate.AddDays(daysOverdue);
        }

        var borrow = new Borrow
        {
            Id = 1,
            BookId = 1,
            MemberId = 1,
            DueDate = dueDate,
            ReturnedAt = returnDate,
            BorrowedAt = dueDate.AddDays(-14),
            FineAmount = 0
        };

        // Act
        var result = _borrowService.CalculateFine(borrow);

        // Assert
        result.ShouldBe(expectedFine);
    }

    // when a book is returned late, Member.OutstandingFine increases by the correct amount.
    // Verify IMemberRepository.UpdateAsync() is called with a member whose fine matches your expected value
    [Fact]
    public async Task IfBookReturnedLate_OutstandingFineIncresases_VerifyIMemberRepositoryUpdateAsync_IsCalled()
    {
        var memberId = 1;
        var bookId = 1;
        var initialFine = 0m;
        var daysOverdue = 5;
        var expectedFine = daysOverdue * 0.50m;

        var member = new Member
        {
            Id = memberId,
            FullName = "John Doe",
            Email = "john@example.com",
            MembershipExpiryDate = DateTime.UtcNow.AddMonths(6),
            OutstandingFine = initialFine
        };

        var book = new Book
        {
            Id = bookId,
            Title = "Test Book",
            AvailableCopies = 3,
            TotalCopies = 5,
            ISBN = "1234567890123",
            Author = "Test Author"
        };

        var borrow = new Borrow
        {
            Id = 1,
            BookId = bookId,
            MemberId = memberId,
            BorrowedAt = DateTime.UtcNow.AddDays(-(daysOverdue + 14)),
            DueDate = DateTime.UtcNow.AddDays(-daysOverdue),
            ReturnedAt = null,
            FineAmount = 0
        };

        _borrowRepositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(borrow);
        _bookRepositoryMock.Setup(r => r.GetByIdAsync(bookId))
            .ReturnsAsync(book);
        _memberRepositoryMock.Setup(r => r.GetByIdAsync(memberId))
            .ReturnsAsync(member);
        _borrowRepositoryMock.Setup(r => r.Update(It.IsAny<Borrow>()));
        _bookRepositoryMock.Setup(r => r.Update(It.IsAny<Book>()));
        _memberRepositoryMock.Setup(r => r.Update(It.IsAny<Member>()));

        // Act
        var result = await _borrowService.ReturnBookAsync(1);

        // Assert
        result.FineAmount.ShouldBe(expectedFine);

        _memberRepositoryMock.Verify(
            x => x.Update(It.Is<Member>(m => m.OutstandingFine == expectedFine)),
            Times.Once);
    }

    // when ReturnedAt != null (already returned),
    // calling ReturnBookAsync throws AlreadyReturnedException

    [Fact]
    public async Task ReturnBook_WhenAlreadyReturned_ThrowsAlreadyReturnedException()
    {
        // Arrange
        var borrowId = 1;
        var borrow = new Borrow
        {
            Id = borrowId,
            BookId = 1,
            MemberId = 1,
            BorrowedAt = DateTime.UtcNow.AddDays(-10),
            DueDate = DateTime.UtcNow.AddDays(4),
            ReturnedAt = DateTime.UtcNow.AddDays(-2),
            FineAmount = 0
        };

        _borrowRepositoryMock.Setup(r => r.GetByIdAsync(borrowId))
            .ReturnsAsync(borrow);

        // Act & Assert
        var exception = await Should.ThrowAsync<AlreadyReturnedException>(() =>
            _borrowService.ReturnBookAsync(borrowId));

        exception.Message.ShouldBe("This book has already been returned");
    }

    // returning a book on time (before DueDate) results in FineAmount == 0
    [Fact]
    public async Task ReturnBook_OnTime_ResultsInZeroFine()
    {
        // Arrange
        var memberId = 1;
        var bookId = 1;
        var initialFine = 0m;

        var member = new Member
        {
            Id = memberId,
            FullName = "John Doe",
            Email = "john@example.com",
            MembershipExpiryDate = DateTime.UtcNow.AddMonths(6),
            OutstandingFine = initialFine
        };

        var book = new Book
        {
            Id = bookId,
            Title = "Test Book",
            AvailableCopies = 3,
            TotalCopies = 5,
            ISBN = "1234567890123",
            Author = "Test Author"
        };

        var borrow = new Borrow
        {
            Id = 1,
            BookId = bookId,
            MemberId = memberId,
            BorrowedAt = DateTime.UtcNow.AddDays(-10),
            DueDate = DateTime.UtcNow.AddDays(4),
            ReturnedAt = null,
            FineAmount = 0
        };

        _borrowRepositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(borrow);
        _bookRepositoryMock.Setup(r => r.GetByIdAsync(bookId))
            .ReturnsAsync(book);
        _memberRepositoryMock.Setup(r => r.GetByIdAsync(memberId))
            .ReturnsAsync(member);
        _borrowRepositoryMock.Setup(r => r.Update(It.IsAny<Borrow>()));
        _bookRepositoryMock.Setup(r => r.Update(It.IsAny<Book>()));

        // Act
        var result = await _borrowService.ReturnBookAsync(1);

        // Assert
        result.FineAmount.ShouldBe(0);
        result.ReturnedAt.ShouldNotBeNull();

        member.OutstandingFine.ShouldBe(0);

        // Verify Member.Update was NOT called with any fine (or called with 0 fine)
        _memberRepositoryMock.Verify(
            x => x.Update(It.Is<Member>(m => m.OutstandingFine == 0)),
            Times.Once);

        _bookRepositoryMock.Verify(x => x.Update(It.IsAny<Book>()), Times.Once);
        _borrowRepositoryMock.Verify(x => x.Update(It.IsAny<Borrow>()), Times.Once);
    }
}
