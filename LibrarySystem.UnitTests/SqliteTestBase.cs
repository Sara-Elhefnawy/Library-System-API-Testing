using LibrarySystem.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.UnitTests;

// opens a SqliteConnection,
// creates and migrates a LibraryDbContext,
// and closes the connection in Dispose()
public class SqliteTestBase : IDisposable
{
    protected LibraryAppDbContext DbContext { get; private set; }

    private bool _disposed = false;

    public SqliteTestBase()
    {
        DbContext = CreateSqLiteContext();
    }

    private LibraryAppDbContext CreateSqLiteContext()
    {
        var builder = new DbContextOptionsBuilder<LibraryAppDbContext>()
            .UseSqlite("DataSource=:memory:", x => { });

        var context = new LibraryAppDbContext(builder.Options);

        context.Database.OpenConnection();
        context.Database.EnsureCreated();

        return context;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            DbContext?.Dispose();
            _disposed = true;
        }
    }
}