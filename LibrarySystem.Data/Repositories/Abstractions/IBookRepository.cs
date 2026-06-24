using LibrarySystem.Data.Models;

namespace LibrarySystem.Data.Repositories.Abstractions;

public interface IBookRepository : IRepository<Book>
{
    Task<IEnumerable<Book>> GetAllAvailableBooksAsync();

    Task<Book?> GetByISBNAsync(string iSBN);
}
