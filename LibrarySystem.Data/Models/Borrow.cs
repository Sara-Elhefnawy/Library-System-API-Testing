namespace LibrarySystem.Data.Models;

public class Borrow : BaseEntity
{
    public int BookId { get; set; }
    public Book Book { get; set; } = default!;

    public int MemberId { get; set; }
    public Member Member { get; set; } = default!;

    public DateTime BorrowedAt { get; set; }

    public DateTime DueDate { get; set; }

    public DateTime? ReturnedAt { get; set; }

    public decimal FineAmount { get; set; }
}
