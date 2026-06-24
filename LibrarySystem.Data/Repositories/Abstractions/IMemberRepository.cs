using LibrarySystem.Data.Models;

namespace LibrarySystem.Data.Repositories.Abstractions;

public interface IMemberRepository : IRepository<Member>
{
    Task<bool> EmailExistsAsync(string email);
    Task<decimal> GetOutstandingFineAsync(int id);
}