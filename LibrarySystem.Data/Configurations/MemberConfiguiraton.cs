using LibrarySystem.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibrarySystem.Data.Configurations;

public class MemberConfiguiraton : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.Property(m => m.FullName)
            .HasMaxLength(150);

        builder.Property(m => m.Email)
            .HasMaxLength(100);

        builder.HasIndex(m => m.Email)
            .IsUnique();

        builder.Property(m => m.OutstandingFine)
            .HasPrecision(18,2);
    }
}
