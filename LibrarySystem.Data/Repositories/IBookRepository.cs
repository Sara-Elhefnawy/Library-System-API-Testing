using LibrarySystem.Data.Models;

namespace LibrarySystem.Data.Repositories;

public interface IBookRepository : IRepository<Book>
{
    Task<IEnumerable<Book>> GetAllAvailableBooksAsync();

    Task<bool> ISBNExistsAsync(string iSBN);
}
