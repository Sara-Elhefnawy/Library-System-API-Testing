namespace LibrarySystem.Data.Models;

public class Book : BaseEntity
{
    public string Title { get; set; } = default!;

    public string Author { get; set; } = default!;

    public string ISBN { get; set; } = default!;

    public int TotalCopies { get; set; }

    public int AvailableCopies { get; set; }

    public ICollection<Borrow> Borrows { get; set; } = [];
}
