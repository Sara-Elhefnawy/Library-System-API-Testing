using LibrarySystem.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Data.Context;

public class LibraryAppDbContext : DbContext
{
    public DbSet<Book> Books { get; set; } = default!;
    public DbSet<Member> Members { get; set; } = default!;
    public DbSet<Loan> Loans { get; set; } = default!;

    protected LibraryAppDbContext()
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LibraryAppDbContext).Assembly);
    }
}
