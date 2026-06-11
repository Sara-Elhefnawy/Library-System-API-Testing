namespace LibrarySystem.Data.Models;

public class Book
{
    public int Id { get; set; }

    public string Title { get; set; } = default!;

    public string Author { get; set; } = default!;

    public string ISBN { get; set; } = default!;

    public int TotalCopies { get; set; }

    public int AvailableCopies { get; set; }
}
