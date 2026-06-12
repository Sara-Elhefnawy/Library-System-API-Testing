using LibrarySystem.Data.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace LibrarySystem.UnitTests;

public class MemberRepositoryConstraintsTests : SqliteTestBase
{
    // attempting to insert two members with the same email address
    // throws a DbUpdateException (unique constraint).
    // Assert with ShouldThrow<DbUpdateException>.
    [Fact]
    public async Task IfInsert2MemberSameEmail_ThrowDbUpdateException()
    {
        var member1 = new Member
        {
            Id = 1,
            FullName = "Member 1 Name",
            Email = "test@email.com",
            MembershipExpiryDate = DateTime.UtcNow.AddMonths(4),
            OutstandingFine = 0
        };
        var member2 = new Member
        {
            Id = 2,
            FullName = "Member 2 Name",
            Email = "test@email.com",
            MembershipExpiryDate = DateTime.UtcNow.AddMonths(2),
            OutstandingFine = 0
        };

        await DbContext.Members.AddRangeAsync(member1, member2);

        var exception = await Should.ThrowAsync<DbUpdateException>
            (async () => await DbContext.SaveChangesAsync());

        exception.InnerException.Message.ShouldContain("UNIQUE constraint failed");
        exception.InnerException.ShouldBeOfType<SqliteException>();
    }

    // attempting to insert a book with the same ISBN as an existing book throws DbUpdateException
    [Fact]
    public async Task IfInsert2BooksSameISBN_ThrowDbUpdateException()
    {
        var book1 = new Book
        {
            Id = 1,
            Author = "Author 1",
            ISBN = "123456",
            AvailableCopies = 0,
            Title = "Title",
            TotalCopies = 0,
        };
        var book2 = new Book
        {
            Id = 2,
            Author = "Author 1",
            ISBN = "123456",
            AvailableCopies = 0,
            Title = "Title",
            TotalCopies = 0,
        };

        await DbContext.Books.AddAsync(book1);
        await DbContext.SaveChangesAsync();

        await DbContext.Books.AddAsync(book2);
        var exception = await Should.ThrowAsync<DbUpdateException>
            (async () => await DbContext.SaveChangesAsync());

        exception.InnerException.Message.ShouldContain("UNIQUE constraint failed");
        exception.InnerException.ShouldBeOfType<SqliteException>();
    }

    // deleting a member who has active loans
    // throws a referential integrity exception (configure cascade behavior in EF and test it)
    [Fact]
    public async Task IfDeletingMemberWhoHasActiveBorrows_ThrowsRefrentialIntegrityException()
    {
        // Arrange
        var member = new Member
        {
            Id = 4,
            FullName = "Active Borrower",
            Email = "active@example.com",
            MembershipExpiryDate = DateTime.UtcNow.AddYears(1),
            OutstandingFine = 0
        };

        var book = new Book
        {
            Id= 2,
            Title = "Test Book",
            Author = "Test Author",
            ISBN = "9876543210123",
            TotalCopies = 2,
            AvailableCopies = 2
        };

        await DbContext.Members.AddAsync(member);
        await DbContext.Books.AddAsync(book);
        await DbContext.SaveChangesAsync();

        var loan = new Borrow
        {
            Id = 1,
            BookId = book.Id,
            MemberId = member.Id,
            BorrowedAt = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(14),
            ReturnedAt = null,
            FineAmount = 0
        };

        await DbContext.Borrows.AddAsync(loan);
        await DbContext.SaveChangesAsync();

        // Detach all tracked entities to avoid EF Core's change tracking interference
        DbContext.ChangeTracker.Clear();

        // Re-attach just the member we want to delete
        var memberToDelete = await DbContext.Members.FindAsync(member.Id);

        // Act & Assert - Try to delete member with active loan
        DbContext.Members.Remove(memberToDelete);

        var exception = await Should.ThrowAsync<DbUpdateException>(async () =>
            await DbContext.SaveChangesAsync());

        // SQLite throws a foreign key constraint violation
        exception.InnerException.ShouldBeOfType<SqliteException>();
        exception.InnerException.Message.ShouldContain("FOREIGN KEY constraint failed");
    }
}
