using LibrarySystem.Data.Context;
using LibrarySystem.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Data.Repositories;

public class BookRepository(LibraryAppDbContext dbContext) : Repository<Book>(dbContext), IBookRepository
{
    public async Task<IEnumerable<Book>> GetAllAvailableBooksAsync()
        => await dbContext.Books.Where(b => b.AvailableCopies > 0).ToListAsync();

    public async Task<Book?> GetByISBNAsync(string iSBN)
        => await dbContext.Books.FirstOrDefaultAsync(b => b.ISBN == iSBN);
}
