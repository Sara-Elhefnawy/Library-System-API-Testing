using LibrarySystem.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Data.Context;

public class LibraryAppDbContext(DbContextOptions<LibraryAppDbContext> options) : DbContext(options)
{
    public DbSet<Book> Books { get; set; } = default!;
    public DbSet<Member> Members { get; set; } = default!;
    public DbSet<Borrow> Borrows { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LibraryAppDbContext).Assembly);
    }
}
