using System.Net;
using System.Net.Http.Json;
using LibrarySystem.API.EndPoint;
using LibrarySystem.Data.Context;
using LibrarySystem.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace LibrarySystem.IntegrationTests;

// All borrow/return tests share ONE database via IClassFixture, so without cleanup,
//      loans created by one test would still exist when the next test runs
//          (and corrupt assertions about AvailableCopies, loan counts, etc).
// IAsyncLifetime.DisposeAsync runs after every individual [Fact],
//      resetting just the rows these tests actually change.
public class BorrowLifecycleTests(LibraryWebAppFactory factory) : IClassFixture<LibraryWebAppFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Borrow_Returns201WithDueDateAndNullReturnedAt()
    {
        var request = new BorrowRequest 
        { 
            BookId = factory.SeededAvailableBookId, 
            MemberId = factory.SeededMemberId 
        };

        var response = await _client.PostAsJsonAsync("/api/borrow", request);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var borrow = await response.Content.ReadFromJsonAsync<Borrow>();
        borrow.ShouldNotBeNull();
        borrow.ReturnedAt.ShouldBeNull();
        borrow.DueDate.Date.ShouldBe(DateTime.UtcNow.AddDays(14).Date);
    }

    [Fact]
    public async Task Borrow_DecreasesAvailableCopies()
    {
        // confirm the DB was updated, not just the response
        var before = await (await _client.GetAsync($"/api/books/{factory.SeededAvailableBookId}"))
            .Content.ReadFromJsonAsync<Book>();

        await _client.PostAsJsonAsync("/api/borrow",
            new BorrowRequest 
            { 
                BookId = factory.SeededAvailableBookId, 
                MemberId = factory.SeededMemberId 
            });

        var after = await (await _client.GetAsync($"/api/books/{factory.SeededAvailableBookId}"))
            .Content.ReadFromJsonAsync<Book>();

        after.AvailableCopies.ShouldBe(before.AvailableCopies - 1);
    }

    [Fact]
    public async Task Borrow_NoAvailableCopies_Returns422WithMessage()
    {
        var request = new BorrowRequest 
        { 
            BookId = factory.SeededUnavailableBookId, 
            MemberId = factory.SeededMemberId 
        };

        var response = await _client.PostAsJsonAsync("/api/borrow", request);
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);

        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("no copies of this book are available", Case.Insensitive);
    }

    [Fact]
    public async Task Borrow_MemberAtLoanLimit_Returns422WithMessage()
    {
        // Member starts with 0 active loans
        // borrow 3 distinct available books hits the limit.
        for (int i = 0; i < 3; i++)
        {
            // Create a NEW book (so we don't affect other tests)
            var newBook = new Book
            {
                Title = $"Loan-Limit Book {i}",
                Author = "Test Author",
                ISBN = $"111111111111{i}",
                TotalCopies = 1,
                AvailableCopies = 1
            };

            // Create the book via POST /api/books
            var createResp = await _client.PostAsJsonAsync("/api/books", newBook);
            var created = await createResp.Content.ReadFromJsonAsync<Book>();

            // Borrow the book via POST /api/borrow
            var borrowResp = await _client.PostAsJsonAsync("/api/borrow",
                new BorrowRequest 
                { 
                    BookId = created!.Id, 
                    MemberId = factory.SeededMemberId 
                });

            borrowResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        }

        // Try to Borrow a 4th Book (Should Fail)
        var fourthBook = new Book
        {
            Title = "One Too Many",
            Author = "Test Author",
            ISBN = "9999999999999",
            TotalCopies = 1,
            AvailableCopies = 1
        };
        var fourthCreate = await _client.PostAsJsonAsync("/api/books", fourthBook);
        var fourthBookEntity = await fourthCreate.Content.ReadFromJsonAsync<Book>();

        var response = await _client.PostAsJsonAsync("/api/borrow",
            new BorrowRequest 
            { 
                BookId = fourthBookEntity!.Id, 
                MemberId = factory.SeededMemberId 
            });

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();

        body.ShouldContain("cannot borrow more than 3 books", Case.Insensitive);
    }

    [Fact]
    public async Task Return_ValidActiveLoan_Returns200WithZeroFine()
    {
        var borrowResp = await _client.PostAsJsonAsync("/api/borrow",
            new BorrowRequest 
            { 
                BookId = factory.SeededAvailableBookId, 
                MemberId = factory.SeededMemberId 
            });
        var borrow = await borrowResp.Content.ReadFromJsonAsync<Borrow>();

        var returnResp = await _client.PutAsync($"/api/borrow/{borrow!.Id}/return", null);
        returnResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await returnResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        json.GetProperty("fineAmount").GetDecimal().ShouldBe(0m);
    }

    [Fact]
    public async Task Return_IncreasesAvailableCopies()
    {
        var before = await (await _client.GetAsync($"/api/books/{factory.SeededAvailableBookId}"))
            .Content.ReadFromJsonAsync<Book>();

        var borrowResp = await _client.PostAsJsonAsync("/api/borrow",
            new BorrowRequest 
            { 
                BookId = factory.SeededAvailableBookId, 
                MemberId = factory.SeededMemberId 
            });
        var borrow = await borrowResp.Content.ReadFromJsonAsync<Borrow>();

        var afterBorrow = await (await _client.GetAsync($"/api/books/{factory.SeededAvailableBookId}"))
            .Content.ReadFromJsonAsync<Book>();

        afterBorrow.AvailableCopies.ShouldBe(before.AvailableCopies - 1);

        // Return the book
        await _client.PutAsync($"/api/borrow/{borrow.Id}/return", null);

        var afterReturn = await (await _client.GetAsync($"/api/books/{factory.SeededAvailableBookId}"))
            .Content.ReadFromJsonAsync<Book>();

        afterReturn.AvailableCopies.ShouldBe(before.AvailableCopies);
    }

    [Fact]
    public async Task Return_AlreadyReturnedLoan_Returns422()
    {
        var borrowResp = await _client.PostAsJsonAsync("/api/borrow",
            new BorrowRequest 
            { 
                BookId = factory.SeededAvailableBookId, 
                MemberId = factory.SeededMemberId 
            });
        var borrow = await borrowResp.Content.ReadFromJsonAsync<Borrow>();

        var firstReturn = await _client.PutAsync($"/api/borrow/{borrow!.Id}/return", null);
        firstReturn.StatusCode.ShouldBe(HttpStatusCode.OK);

        var secondReturn = await _client.PutAsync($"/api/borrow/{borrow.Id}/return", null);
        secondReturn.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task FullCycle_BorrowReturnBorrowAgain_KeepsAvailableCopiesConsistent()
    {
        // Book's starting AvailableCopies                            => 2
        var first = await (await _client.GetAsync($"/api/books/{factory.SeededAvailableBookId}"))
            .Content.ReadFromJsonAsync<Book>();

        // Decrement AvailableCopies by 1                             => 2-1 = 1
        var firstBorrow = await _client.PostAsJsonAsync("/api/borrow",
            new BorrowRequest
            {
                BookId = factory.SeededAvailableBookId,
                MemberId = factory.SeededMemberId
            });
        var firstLoan = await firstBorrow.Content.ReadFromJsonAsync<Borrow>();

        // Increment AvailableCopies back by 1, undoing step 1        => 1+1 = 2
        await _client.PutAsync($"/api/borrow/{firstLoan!.Id}/return", null);

        // Decrement AvailableCopies by 1 again, same as step 1 did   => 2-1 = 1
        var secondBorrow = await _client.PostAsJsonAsync("/api/borrow",
            new BorrowRequest
            {
                BookId = factory.SeededAvailableBookId,
                MemberId = factory.SeededMemberId
            });

        secondBorrow.StatusCode.ShouldBe(HttpStatusCode.Created);

        // What AvailableCopies after: borrow -> return -> borrow
        var afterSecondBorrow = await
            (await _client.GetAsync($"/api/books/{factory.SeededAvailableBookId}"))
            .Content.ReadFromJsonAsync<Book>();

        // AvailableCopies should be exactly 1 less than where we started
        afterSecondBorrow!.AvailableCopies.ShouldBe(first!.AvailableCopies - 1);
    }

    // Runs after every [Fact] in this class.
    // Resets exactly the rows the tests above change:
    //      active loans, AvailableCopies, and OutstandingFine.
    public async Task DisposeAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LibraryAppDbContext>();

        // Every test in this class calls POST /api/borrow, which inserts a row into the Borrows table.
        // If we didn't delete those rows between tests,
        //      by the time test #5 runs, DB would still contain every loan created by tests #1-#4.
        // That leftover data could make a member look like they already have 3 active loans (loan-limit bug)
        //      even though THIS test never borrowed anything itself.
        await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE Borrows");

        // Borrowing a book creates a row and decreases the Book's AvailableCopies number 
        // Deleting the Borrows rows above does NOT undo that decrease
        // So after every test, we manually reset every book's AvailableCopies
        //      back to its original TotalCopies value
        // EXCEPT one specific book
        //      the one we deliberately seeded with AvailableCopies = 0 in LibraryWebAppFactory.SeedData
        // If we reset that book's copies too, it would no longer be "unavailable" and that test would start failing.
        await db.Database.ExecuteSqlRawAsync(
            $"UPDATE Books SET AvailableCopies = TotalCopies WHERE Id != {factory.SeededUnavailableBookId}");

        // Re-applies AvailableCopies = 0
        await db.Database.ExecuteSqlRawAsync(
                $"UPDATE Books SET AvailableCopies = 0 WHERE Id = {factory.SeededUnavailableBookId}");
    }
}
