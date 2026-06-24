using LibrarySystem.Data.Context;
using LibrarySystem.Data.Models;
using LibrarySystem.Data.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Data.Repositories;

public class Repository<IEntity>(LibraryAppDbContext dbContext) : IRepository<IEntity> where IEntity : BaseEntity
{
    public async Task<IEntity?> GetByIdAsync(int id)
        => await dbContext.Set<IEntity>().FindAsync(id);

    public async Task<IEnumerable<IEntity>> GetAllAsync()
        => await dbContext.Set<IEntity>().AsNoTracking().ToListAsync();

    public async Task AddAsync(IEntity entity)
        => await dbContext.Set<IEntity>().AddAsync(entity);

    public void Update(IEntity entity) 
        => dbContext.Set<IEntity>().Update(entity);

    public async Task<int> CommitChanges()
        => await dbContext.SaveChangesAsync();
}
