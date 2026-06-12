using LibrarySystem.Data.Context;
using LibrarySystem.Data.Models;
using LibrarySystem.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace LibrarySystem.UnitTests;

public class BookRepositoryQueryTests : IDisposable
{
    private LibraryAppDbContext _dbContext;
    private BookRepository _bookRepository;

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    // Create a helper method CreateInMemoryContext() that returns a fresh LibraryDbContext
    // with a unique database name per test (use Guid.NewGuid().ToString() as the DB name to isolate tests)
    private LibraryAppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<LibraryAppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new LibraryAppDbContext(options);
    }

    // GetAvailableBooksAsync() returns only books where AvailableCopies > 0.
    // Seed 3 books (2 available, 1 unavailable) and assert count is 2 with ShouldBe(2)
    [Fact]
    public async Task Verify_GetAvailableBooksAsync_CountShouldBe2()
    {
        // Arrange
        _dbContext = CreateInMemoryContext();
        _bookRepository = new BookRepository(_dbContext);

        // Seed 3 books (2 available, 1 unavailable)
        var book1 = new Book
        {
            Id = 1,
            ISBN = "Book1",
            Title = "Title",
            Author = "Author",
            AvailableCopies = 2,
            TotalCopies = 10,
        };
        var book2 = new Book
        {
            Id = 2,
            ISBN = "Book2",
            Title = "Title",
            Author = "Author",
            AvailableCopies = 5,
            TotalCopies = 41,
        };
        var book3 = new Book
        {
            Id = 3,
            ISBN = "Book3",
            Title = "Title",
            Author = "Author",
            AvailableCopies = 0,
            TotalCopies = 10,
        };

        await _dbContext.Books.AddRangeAsync(book1, book2, book3);
        await _dbContext.SaveChangesAsync();

        // Act
        var availableBooks = await _bookRepository.GetAllAvailableBooksAsync();

        // Assert
        availableBooks.Count().ShouldBe(2);
        availableBooks.ShouldAllBe(b => b.AvailableCopies > 0);
    }

    // GetByISBNAsync("9780123456789") returns the correct book when it exists
    [Fact]
    public async Task Verify_GetByISBNAsync_ReturnsCorrectly()
    {
        // Arragne
        var targetISBN = "9780123456789";
        var _dbContext = CreateInMemoryContext();
        var _bookRepository = new BookRepository(_dbContext);

        var book = new Book
        {
            Id = 1,
            Title = "Title",
            ISBN = targetISBN,
            Author = "Author",
            AvailableCopies = 5,
            TotalCopies = 52
        };

        await _dbContext.Books.AddAsync(book);
        await _dbContext.SaveChangesAsync();

        // Act
        var getISBN = await _bookRepository.GetByISBNAsync(targetISBN);

        // Assert
        getISBN.ShouldNotBeNull();
        getISBN.ISBN.ShouldBe(targetISBN);
        getISBN.Title.ShouldBe("Title");
    }

    // GetByISBNAsync returns null when no book matches.
    [Fact]
    public async Task Verify_GetByISBNAsync_ReturnsNullIfNoBooksMatches()
    {
        // Arragne
        var _dbContext = CreateInMemoryContext();
        var _bookRepository = new BookRepository(_dbContext);

        var book = new Book
        {
            Id = 1,
            Title = "Title",
            ISBN = "targetISBN",
            Author = "Author",
            AvailableCopies = 5,
            TotalCopies = 52
        };

        await _dbContext.Books.AddAsync(book);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _bookRepository.GetByISBNAsync("5784758898179");

        // Assert
        result.ShouldBeNull();
    }

    // GetActiveLoansForMemberAsync(memberId) returns only loans where ReturnedAt == null
    [Fact]
    public async Task Verify_GetActiveLoansForMemberAsync_ReturnsIfReturnedAtEqualsNull()
    {
        var _dbContext = CreateInMemoryContext();
        var _memberRepository = new MemberRepository(_dbContext);
        var _borrowRepository = new BorrowRepository(_dbContext);

        var memberId = 1;
        var book1 = new Book
        {
            Id = 1,
            ISBN = "Book1",
            Title = "Title",
            Author = "Author",
            AvailableCopies = 2,
            TotalCopies = 10,
        };
        var book2 = new Book
        {
            Id = 2,
            ISBN = "Book2",
            Title = "Title",
            Author = "Author",
            AvailableCopies = 5,
            TotalCopies = 41,
        };
        var book3 = new Book
        {
            Id = 3,
            ISBN = "Book3",
            Title = "Title",
            Author = "Author",
            AvailableCopies = 0,
            TotalCopies = 10,
        };

        await _dbContext.Books.AddRangeAsync(book1, book2, book3);
        await _dbContext.SaveChangesAsync();

        var activeLoan = new Borrow
        {
            BookId = 1,
            MemberId = memberId,
            BorrowedAt = DateTime.UtcNow.AddDays(-5),
            DueDate = DateTime.UtcNow.AddDays(9),
            ReturnedAt = null 
        };

        var anotherActiveLoan = new Borrow
        {
            BookId = 2,
            MemberId = memberId,
            BorrowedAt = DateTime.UtcNow.AddDays(-3),
            DueDate = DateTime.UtcNow.AddDays(11),
            ReturnedAt = null 
        };

        var returnedLoan = new Borrow
        {
            BookId = 3,
            MemberId = memberId,
            BorrowedAt = DateTime.UtcNow.AddDays(-10),
            DueDate = DateTime.UtcNow.AddDays(-3),
            ReturnedAt = DateTime.UtcNow.AddDays(-2), 
            FineAmount = 1.50m
        };

        await _dbContext.Borrows.AddRangeAsync(activeLoan, anotherActiveLoan, returnedLoan);
        await _dbContext.SaveChangesAsync();

        // Act
        var activeLoans = await _borrowRepository.GetActiveBorrowsByMemberIdAsync(memberId);

        // Assert
        activeLoans.ShouldNotBeNull();
        activeLoans.Count.ShouldBe(2);
        activeLoans.ShouldAllBe(l => l.ReturnedAt == null);
    }
}
