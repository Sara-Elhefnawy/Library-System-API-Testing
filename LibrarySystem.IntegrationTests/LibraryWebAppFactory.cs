using LibrarySystem.Data.Context;
using LibrarySystem.Data.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Xunit;

namespace LibrarySystem.IntegrationTests;

// Shared test infrastructure: every integration test class plugs into this
// one factory via IClassFixture<LibraryWebAppFactory>, so the container
// only spins up ONCE per test class, not once per test method.
public class LibraryWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Spins up a real SQL Server instance inside a Docker container.
    // This gives production-identical behavior (real T-SQL, real constraints)
    //      instead of an in-memory fake database.
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithPassword("Strong_password_123!")
        .Build();

    // Known IDs that every test method can rely on
    //      without re-querying the database to "find" a book/member first.
    public int SeededAvailableBookId { get; private set; }
    public int SeededUnavailableBookId { get; private set; }
    public int SeededMemberId { get; private set; }

    // xunit calls this once before any test in the class runs.
    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    // WebApplicationFactory calls this while building the test host.
    // It swaps the app's real SQL Server connection for the Testcontainer's connection,
    //      then prepare the schema + seed data.
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove the DbContext registration that Program.cs set up
            //      so we can replace it with the Testcontainer's.
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<LibraryAppDbContext>));

            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<LibraryAppDbContext>(options =>
            {
                // Testcontainers' connection string targets the "master" system database by default.
                // We can't EnsureDeleted()/Migrate() against master
                //      so we explicitly point at a named database instead
                //          SQL Server creates it on first use.
                var connectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
                {
                    InitialCatalog = "LibraryTestDb"
                }.ConnectionString;

                options.UseSqlServer(connectionString);
            });

            // Build a temporary service provider just to run migrations
            //      and seed data before the real host starts handling requests.
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LibraryAppDbContext>();

            // Start every test run from a clean, freshly migrated schema.
            db.Database.EnsureDeleted();
            db.Database.Migrate();

            SeedData(db);
        });
    }

    private void SeedData(LibraryAppDbContext db)
    {
        var availableBook = new Book
        {
            Title = "Clean Architecture",
            Author = "Robert C. Martin",
            ISBN = "9780134494166",
            TotalCopies = 3,
            AvailableCopies = 3
        };

        var unavailableBook = new Book
        {
            Title = "Domain-Driven Design",
            Author = "Eric Evans",
            ISBN = "9780321125217",
            TotalCopies = 1,
            AvailableCopies = 0 // to confirm the "book not available" business rule
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
        db.SaveChanges();

        SeededAvailableBookId = availableBook.Id;
        SeededUnavailableBookId = unavailableBook.Id;
        SeededMemberId = member.Id;
    }

    // xunit calls this once after all tests in the class have finished.
    public new async Task DisposeAsync()
    {
        await _container.StopAsync();
        await _container.DisposeAsync();
    }
}
