using LibrarySystem.Data.Models;

namespace LibrarySystem.Data.Repositories;

public interface IBorrowRepository : IRepository<Borrow>
{
    Task<IReadOnlyList<Borrow>?> GetActiveBorrowsByMemberIdAsync(int memberId);

    Task<int> GetActiveBorrowsCountAsync(int memberId);

    Task<IReadOnlyList<Borrow>?> GetAllMemberBorrowsAsync(int memberId);
}