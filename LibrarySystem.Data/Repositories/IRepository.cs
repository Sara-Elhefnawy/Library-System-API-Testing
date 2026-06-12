using LibrarySystem.Data.Models;

namespace LibrarySystem.Data.Repositories;

public interface IRepository<IEntity> where IEntity : BaseEntity
{
    Task<IEntity?> GetByIdAsync(int id);

    Task<IEnumerable<IEntity>> GetAllAsync();

    Task AddAsync(IEntity book);

    void Update(IEntity entity);

    Task<int> CommitChanges();
}
