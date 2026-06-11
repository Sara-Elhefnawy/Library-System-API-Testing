using LibrarySystem.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibrarySystem.Data.Configurations;

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.Property(b => b.Title)
            .HasMaxLength(200);

        builder.Property(b => b.Author)
            .HasMaxLength(100);

        builder.Property(b => b.ISBN)
            .HasMaxLength(20);

        builder.HasIndex(b => b.ISBN)
            .IsUnique();
    }
}
