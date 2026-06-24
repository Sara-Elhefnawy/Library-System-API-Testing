using LibrarySystem.Data.Context;
using LibrarySystem.Data.Models;
using LibrarySystem.Data.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Data.Repositories;

public class MemberRepository(LibraryAppDbContext dbContext) : Repository<Member>(dbContext), IMemberRepository
{
    public Task<bool> EmailExistsAsync(string email)
        => dbContext.Members
            .AsNoTracking()
            .AnyAsync(m => m.Email == email);

    public async Task<decimal> GetOutstandingFineAsync(int id)
        => await dbContext.Members
            .AsNoTracking()
            .Where(m => m.Id == id)
            .Select(m => m.OutstandingFine)
            .FirstOrDefaultAsync();
}
