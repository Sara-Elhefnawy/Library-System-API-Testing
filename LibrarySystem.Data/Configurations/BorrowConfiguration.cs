using LibrarySystem.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibrarySystem.Data.Configurations;

public class BorrowConfiguration : IEntityTypeConfiguration<Borrow>
{
    public void Configure(EntityTypeBuilder<Borrow> builder)
    {
        builder.HasIndex(b => new { b.BookId, b.MemberId, b.BorrowedAt })
            .IsUnique();

        builder.Property(b => b.FineAmount)
            .HasPrecision(18,2);

        builder.HasOne(b => b.Member)
            .WithMany(m => m.Borrows)
            .HasForeignKey(b => b.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Book)
            .WithMany(b => b.Borrows)
            .HasForeignKey(b => b.BookId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
