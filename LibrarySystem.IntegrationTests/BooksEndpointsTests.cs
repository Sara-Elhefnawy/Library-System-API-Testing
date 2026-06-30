using System.Net;
using System.Net.Http.Json;
using LibrarySystem.Data.Models;
using Shouldly;
using Xunit;

namespace LibrarySystem.IntegrationTests;

public class BooksEndpointsTests(LibraryWebAppFactory factory) : IClassFixture<LibraryWebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetBooks_ReturnsSeededBooks()
    {
        var response = await _client.GetAsync("/api/books");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var books = await response.Content.ReadFromJsonAsync<List<Book>>();
        books.ShouldNotBeNull();
        books.Count.ShouldBeGreaterThanOrEqualTo(2);
        books.ShouldAllBe(b => !string.IsNullOrEmpty(b.Title));
    }

    [Fact]
    public async Task GetBooks_AvailableTrue_ExcludesZeroCopyBooks()
    {
        var response = await _client.GetAsync("/api/books?available=true");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var books = await response.Content.ReadFromJsonAsync<List<Book>>();
        books.ShouldNotBeNull();
        books.ShouldAllBe(b => b.AvailableCopies > 0);
        books.ShouldNotContain(b => b.Id == factory.SeededUnavailableBookId);
    }

    [Fact]
    public async Task GetBookById_ReturnsBook()
    {
        var response = await _client.GetAsync($"/api/books/{factory.SeededAvailableBookId}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var book = await response.Content.ReadFromJsonAsync<Book>();
        book.ShouldNotBeNull();
        book.Id.ShouldBe(factory.SeededAvailableBookId);
    }

    [Fact]
    public async Task GetBookById_IdNotExist_Returns404()
    {
        var response = await _client.GetAsync("/api/books/99999");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostBook_Returns201()
    {
        var newBook = new Book
        {
            Title = "Refactoring",
            Author = "Martin Fowler",
            ISBN = "9780201485677",
            TotalCopies = 2,
            AvailableCopies = 2
        };

        var response = await _client.PostAsJsonAsync("/api/books", newBook);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();

        var created = await response.Content.ReadFromJsonAsync<Book>();
        created.ShouldNotBeNull();
        created.Title.ShouldBe("Refactoring");
        created.Id.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task PostBook_DuplicateIsbn_ReturnsConflict()
    {
        var duplicate = new Book
        {
            Title = "Clean Architecture (dup)",
            Author = "Robert C. Martin",
            ISBN = "9780134494166", // matches the seeded "availableBook"
            TotalCopies = 1,
            AvailableCopies = 1
        };

        var response = await _client.PostAsJsonAsync("/api/books", duplicate);
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PostBook_EmptyISBN_ReturnsBadRequest()
    {
        var invalidBook = new Book
        {
            Title = "Missing ISBN Book",
            Author = "Someone",
            ISBN = "",
            TotalCopies = 1,
            AvailableCopies = 1
        };

        var response = await _client.PostAsJsonAsync("/api/books", invalidBook);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
