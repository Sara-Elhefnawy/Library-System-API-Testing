using LibrarySystem.Data.Models;

namespace LibrarySystem.Data.Repositories;

public interface IMemberRepository : IRepository<Member>
{
    Task<bool> EmailExistsAsync(string email);
    Task<decimal> GetOutstandingFineAsync(int id);
}