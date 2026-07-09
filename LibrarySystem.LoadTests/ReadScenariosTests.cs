using System.Text;
using System.Text.Json;
using NBomber.CSharp;
using NBomber.Http.FSharp;
using Xunit;
using Shouldly;

namespace LibrarySystem.LoadTests;

public class ReadScenariosTests
{
    // base address must match whatever port `dotnet run` printed
    private readonly HttpClient _httpClient = new() { BaseAddress = new Uri("http://localhost:5154") };

    [Fact]
    public async Task list_all_books()
    {
        var listAllBooksScenario = Scenario.Create("get_all_books", async context =>
        {
            var request = Http.createRequest("GET", "/api/books");
            var response = await Http.send(_httpClient, request);

            // response.StatusCode from NBomber.Http.FSharp is a string ("OK", "NotFound", etc.),
            // not the System.Net.HttpStatusCode enum — so we compare against the string form.
            if (response.StatusCode is "OK")
            {
                return Response.Ok(statusCode: response.StatusCode);
            }

            // Log the real status code so failures are diagnosable instead of a bare count.
            context.Logger.Error("get_all_books failed: {StatusCode} {Body}",
                response.StatusCode, response.Message ?? "");
            return Response.Fail(statusCode: response.StatusCode);
        })
        // Simulation.Inject(rate, interval, during) injects
        //      `rate` virtual users every `interval`.
        //      interval = 1s gives a steady ~100 requests/sec for 30 seconds
            .WithWarmUpDuration(TimeSpan.FromSeconds(5))
            .WithLoadSimulations(
                Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
            );

        var getSingleBookScenario = Scenario.Create("get_single_book", async context =>
        {
            var randomId = Random.Shared.Next(1, 51);
            var request = Http.createRequest("GET", $"/api/books/{randomId}");
            var response = await Http.send(_httpClient, request);

            if (response.StatusCode is "OK")
            {
                return Response.Ok(statusCode: response.StatusCode);
            }

            context.Logger.Error("get_single_book failed: {StatusCode} {Body}",
                response.StatusCode, response.Message ?? "");
            return Response.Fail(statusCode: response.StatusCode);
        })
            .WithLoadSimulations(
                Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
            );

        // Both scenarios run together in a single NBomber session
        //      load is applied concurrently against the same running app instance.
        var nodeStats = NBomberRunner
            .RegisterScenarios(listAllBooksScenario, getSingleBookScenario)
            .Run();

        var listAllStats = nodeStats.ScenarioStats.First(x => x.ScenarioName is "get_all_books");
        var getSingleStats = nodeStats.ScenarioStats.First(x => x.ScenarioName is "get_single_book");

        // NBomber doesn't have an "Assertions.Define" API
        // (checked the official docs — it doesn't exist in the C# API).
        // The documented pattern is exactly this:
        //      pull final stats after Run() completes
        //      and assert against them with Shouldly
        listAllStats.Ok.Latency.Percent99.ShouldBeLessThan(300); // p99 < 300ms
        getSingleStats.Ok.Latency.Percent99.ShouldBeLessThan(300);

        listAllStats.Fail.Request.Count.ShouldBe(0);
        getSingleStats.Fail.Request.Count.ShouldBe(0);

        // After running, check the console output or bin/Debug/.../reports folder
        // for the generated HTML report (NBomber prints the exact path at the end of the run).
        // It contains p50/p95/p99 latency and RPS per scenario.
    }

    [Fact]
    public async Task borrow_book()
    {
        // Unlike the read scenarios, this one has side effects and can
        //      fail business rules (e.g. book has 0 copies left, member has a fine, etc.)
        //      A 422 here is an EXPECTED outcome, not a bug —
        //      only a 500 means the server actually broke.
        var borrowBookScenario = Scenario.Create("borrow_book", async context =>
        {
            var randomMemberId = Random.Shared.Next(1, 201);
            var randomBookId = Random.Shared.Next(1, 51);

            // Matches BorrowRequest { MemberId, BookId } — JSON binding is case-insensitive
            // by default in ASP.NET Core, so lowercase field names here are fine.
            var payload = JsonSerializer.Serialize(new
            {
                memberId = randomMemberId,
                bookId = randomBookId
            });

            var request = Http.createRequest("POST", "/api/borrow");
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await Http.send(_httpClient, request);

            // 201 Created = loan succeeded.
            // 422 UnprocessableEntity = expected business rule violation.
            //      Note: the endpoint's own catch-all (catch (Exception ex) => UnprocessableEntity)
            //      means most failures, even unexpected ones, surface as 422 rather than 500 —
            //      so a 500 here would mean something broke outside that try/catch (e.g. model binding, infra-level crash).
            if (response.StatusCode is "Created" || response.StatusCode is "UnprocessableEntity")
            {
                return Response.Ok(statusCode: response.StatusCode);
            }

            context.Logger.Error("borrow_book failed: {StatusCode} {Body}",
                response.StatusCode, response.Message ?? "");
            return Response.Fail(statusCode: response.StatusCode);
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 20, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(60))
        );

        var nodeStats = NBomberRunner
            .RegisterScenarios(borrowBookScenario)
            .Run();

        var borrowStats = nodeStats.ScenarioStats.First(x => x.ScenarioName is "borrow_book");

        // The only hard requirement: zero real failures (not 201/422).
        borrowStats.Fail.Request.Count.ShouldBe(0);

        /*
         * How to find and read your results after every run (do this each time, not just once):

            Run the test.
            Open Powershell
                cd LibrarySystem.LoadTests\bin\Debug\net10.0\reports\
                dir
            Find the newest folder (sort by "Date modified" — it's named with a timestamp).
                Get-Content .\nbomber_report_*.txt
            Look for two sections:

            scenario: borrow_book near the top → gives you ok count, fail count, and latency (p50/p75/p95/p99).
            status codes for scenario: borrow_book further down → gives you the exact breakdown, e.g. Created | 502 and UnprocessableEntity | 698.
         */

        /*
           What happens if two virtual users try to borrow the LAST copy of the same book at the same time?
              
            BorrowService.BorrowBookAsync must check + decrement AvailableCopies atomically
                for one of them to succeed (201)
                the other gets 422 ("Failed to borrow book. Check business rules.").
         */
    }
}
