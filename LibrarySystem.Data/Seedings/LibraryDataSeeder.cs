using LibrarySystem.Data.Context;
using LibrarySystem.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Data.Seedings;

public static class LibraryDataSeeder
{
    public static async Task<(int availableBookId, int unavailableBookId, int memberId)> SeedIntegrationTestDataAsync(LibraryAppDbContext db)
    {
        var availableBook = new Book
        {
            Title = "Clean Architecture",
            Author = "Robert C. Martin",
            ISBN = "9780134494166",
            TotalCopies = 3,
            AvailableCopies = 3
        };

        // Deliberately has 0 available copies — used by tests that confirm "book not available" business rule
        var unavailableBook = new Book
        {
            Title = "Domain-Driven Design",
            Author = "Eric Evans",
            ISBN = "9780321125217",
            TotalCopies = 1,
            AvailableCopies = 0
        };

        var member = new Member
        {
            FullName = "Sara Test",
            Email = "sara.test@example.com",
            MembershipExpiryDate = DateTime.UtcNow.AddYears(1),
            OutstandingFine = 0
        };

        db.Books.AddRange(availableBook, unavailableBook);
        db.Members.Add(member);
        await db.SaveChangesAsync();

        return (availableBook.Id, unavailableBook.Id, member.Id);
    }

    public static async Task SeedLoadTestDataAsync(LibraryAppDbContext db)
    {
        if (await db.Books.CountAsync() >= 50 && await db.Members.CountAsync() >= 200)
            return;

        var books = Enumerable.Range(1, 50).Select(i => new Book
        {
            Title = $"Load Test Book {i}",
            Author = $"Author {i}",
            ISBN = $"LOADTEST{i:D5}",
            TotalCopies = 10,
            AvailableCopies = 10
        });

        var members = Enumerable.Range(1, 200).Select(i => new Member
        {
            FullName = $"Load Test Member {i}",
            Email = $"loadtest{i}@example.com",
            MembershipExpiryDate = DateTime.UtcNow.AddYears(1),
            OutstandingFine = 0
        });

        db.Books.AddRange(books);
        db.Members.AddRange(members);
        await db.SaveChangesAsync();
    }
}
