using LibrarySystem.Data.Context;
using LibrarySystem.Data.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.MsSql;
using Xunit;

namespace LibrarySystem.IntegrationTests;

public class LibraryWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private MsSqlContainer _dbContainer = null!;
    private string _databaseName = $"LibrarySystemTest_{Guid.NewGuid():N}";

    // In InitializeAsync():
    //      use MsSqlBuilder to build and start an MsSqlContainer.
    //      Store the container reference
    public async Task InitializeAsync()
    {
        _dbContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("Your_password123!")
            .Build();

        await _dbContainer.StartAsync();
    }

    // Override ConfigureWebHost():
    //      remove the existing LibraryDbContext registration
    //      and re-register it using the Testcontainer's connection string
    //      from container.GetConnectionString()
    // In ConfigureWebHost():
    //      after registering the new DbContext, resolve it from app.ApplicationServices,
    //      call database.EnsureDeleted() then database.Migrate()
    //      to start each test run with a clean, migrated database
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove the real database connection that your app registered in Program.cs.
            services.RemoveAll(typeof(DbContextOptions<LibraryAppDbContext>));

            // Create a dedicated database for testing
            var connectionString = _dbContainer.GetConnectionString();
            var testConnectionString = connectionString.Replace("Database=master", $"Database={_databaseName}");

            // Add a new database connection pointing to the Docker container.
            // GetConnectionString() returns something like:
            // "Server=localhost,12345;Database=master;User Id=sa;Password=Your_password123!;"
            // The port number is dynamic — Testcontainers assigns it automatically.
            services.AddDbContext<LibraryAppDbContext>(options =>
            {
                options.UseSqlServer(testConnectionString);
            });

            // Build a temporary service provider to run database migration & seeding
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LibraryAppDbContext>();

            // Ensure the schema is ready
            dbContext.Database.EnsureCreated();

            SeedData(dbContext);
        });
    }

    // Create a SeedData() method that inserts a known set of books and members
    //      using the DbContext directly.
    //      Call it at the end of InitializeAsync().
    public void SeedData(LibraryAppDbContext dbContext)
    {
        if (!dbContext.Books.Any())
        {
            var book1 = new Book
            {
                Author = "Author1",
                ISBN = "1234567890123",
                Title = "Title1",
                TotalCopies = 40,
                AvailableCopies = 5,
            };
            var book2 = new Book
            {
                Author = "Author2",
                ISBN = "1234567890124",
                Title = "Title2",
                TotalCopies = 40,
                AvailableCopies = 3,
            };
            var book3 = new Book
            {
                Author = "Author3",
                ISBN = "1234567890125",
                Title = "Title3",
                TotalCopies = 40,
                AvailableCopies = 0,
            };

            dbContext.Books.AddRange(book1, book2, book3);
        }

        if (!dbContext.Members.Any())
        {
            var member1 = new Member
            {
                FullName = "Yara",
                MembershipExpiryDate = DateTime.UtcNow.AddMonths(14),
                Email = "yara@gmail.com",
                OutstandingFine = 0
            };
            var member2 = new Member
            {
                FullName = "Yasser",
                MembershipExpiryDate = DateTime.UtcNow.AddMonths(14),
                Email = "yasser@gmail.com",
                OutstandingFine = 0
            };
            var member3 = new Member
            {
                FullName = "Nada",
                MembershipExpiryDate = DateTime.UtcNow.AddMonths(14),
                Email = "nada@gmail.com",
                OutstandingFine = 0
            };

            dbContext.Members.AddRange(member1, member2, member3);
        }

        dbContext.SaveChanges();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (_dbContainer != null)
        {
            await _dbContainer.StopAsync();
            await _dbContainer.DisposeAsync();
        }
    }
}
