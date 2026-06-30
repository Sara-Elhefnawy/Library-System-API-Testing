using System.Net;
using System.Net.Http.Json;
using LibrarySystem.API.EndPoint;
using LibrarySystem.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace LibrarySystem.IntegrationTests;

// (a) reset before each test class:
//      Performance is simple
//      Isolation is poor cuz tests can interfere with each other
//      Database set up once per test class, torn down after all tests complete
//      Best for read-only tests that don't modify data
// (b) reset before each test method:
//      Performance is simple
//      Isolation is good cuz each test runs with clean state
//      Database cleaned/re-seeded before each individual test
//      Best for most integration tests that modify data
// (c) wrap each test in a rolled-back transaction:
//      Performance is complex
//      Isolation is excellent
//      Each test wrapped in a transaction that's rolled back at the end
//      Best for tests needing speed with perfect isolation
public class BorrowIsolationTests(LibraryWebAppFactory factory) : IClassFixture<LibraryWebAppFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Borrow_FirstTest_LeavesNoAvailableLoanForMember()
    {
        var response = await _client.PostAsJsonAsync("/api/borrow",
            new BorrowRequest { BookId = factory.SeededAvailableBookId, MemberId = factory.SeededMemberId });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Borrow_SecondTest_SameMemberSameBook()
    {
        var response = await _client.PostAsJsonAsync("/api/borrow",
            new BorrowRequest { BookId = factory.SeededAvailableBookId, MemberId = factory.SeededMemberId });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var book = await (await _client.GetAsync($"/api/books/{factory.SeededAvailableBookId}"))
            .Content.ReadFromJsonAsync<Data.Models.Book>();

        // Should be down exactly 1 from the original seed value (3), not 2,
        // proving the previous test's loan didn't leak through.
        book!.AvailableCopies.ShouldBe(2);
    }

    // Why we only truncate Borrows, and not Books/Members?
    //      Cuz Books and Members are seeded once when the factory starts
    //      and neither test in this class modifies them
    //          only Borrows rows get created.
    //      Re-deleting and re-inserting Books/Members before every single test would mean
    //          extra round-trips to SQL Server for data that hasn't actually changed,
    //          slowing the suite down for no benefit
    //      reset Books/Members only between tests if a test actually change them
    //      in a way that could leak into the next test

    // Runs after EVERY test method in this class (xunit creates a new
    // class instance per test, so this fires once per [Fact]).
    public async Task DisposeAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LibraryAppDbContext>();
        await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE Borrows");
    }
}
