using LibrarySystem.Data.Context;
using LibrarySystem.Data.Models;
using LibrarySystem.Data.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Data.Repositories;

public class BorrowRepository(LibraryAppDbContext dbContext) : Repository<Borrow>(dbContext), IBorrowRepository
{
    // Return all loans for a member, ordered by BorrowedAt descending.
    // Include book title in the response
    public async Task<IReadOnlyList<Borrow>?> GetActiveBorrowsByMemberIdAsync(int memberId)
        => await dbContext.Borrows
            .Include(b => b.Book)
            .Where(b => b.MemberId == memberId && b.ReturnedAt == null)
            .OrderByDescending(b => b.BorrowedAt)
            .ToListAsync();

    public async Task<int> GetActiveBorrowsCountAsync(int memberId)
        => await dbContext.Borrows
            .Where(b => b.MemberId == memberId && b.ReturnedAt == null)
            .CountAsync();

    public async Task<IReadOnlyList<Borrow>?> GetAllMemberBorrowsAsync(int memberId)
    => await dbContext.Borrows
        .Include(b => b.Book)
        .Where(b => b.MemberId == memberId)
        .OrderByDescending(b => b.BorrowedAt)
        .ToListAsync();
}
