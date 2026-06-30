using LibrarySystem.Data.Models;
using LibrarySystem.Data.Repositories.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.API.EndPoint;

public static class MapBookItem
{
    public static void MapBookItemEndPoint(this WebApplication app)
    {
        // Return all books.
        // Support optional query param ?available=true to filter only books with AvailableCopies > 0
        app.MapGet("/api/books", async (bool ? available, IBookRepository repo) =>
        {
            IEnumerable<Book> books;

            if (available == true)
            {
                // Only show books with available copies
                books = await repo.GetAllAvailableBooksAsync();
            }
            else
            {
                // Show all books
                books = await repo.GetAllAsync();
            }

            return Results.Ok(books);
        });

        // Return a single book.
        // Return 404 if not found
        app.MapGet("/api/books/{id}", async ([FromRoute] int id, IBookRepository repo) =>
        {
            var book = await repo.GetByIdAsync(id);
            return book is not null ? Results.Ok(book) : Results.NotFound();
        });

        // Create a book.
        // Validate ISBN uniqueness.
        // Return 201 with the created resource
        app.MapPost("/api/books", async ([FromBody] Book book, IBookRepository repo) =>
        {
            if (string.IsNullOrEmpty(book.ISBN) || book.ISBN.Length != 13)
                return Results.BadRequest("ISBN must be exactly 13 digits");

            if (await repo.GetByISBNAsync(book.ISBN) is not null)
                return Results.Conflict($"A book with ISBN {book.ISBN} already exists.");

            await repo.AddAsync(book);
            await repo.CommitChanges();
            return Results.Created($"/api/books/{book.Id}", book);
        });
    }
}
