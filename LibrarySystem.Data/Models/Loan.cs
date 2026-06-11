namespace LibrarySystem.Data.Models;

public class Loan
{
    public int Id { get; set; }

    public int BookId { get; set; }
    public Book Book { get; set; } = default!;

    public int MemberId { get; set; }
    public Member Member { get; set; } = default!;

    public DateTime BorrowedAt { get; set; }

    public DateTime DueDate { get; set; }

    public DateTime? returnedAt { get; set; }

    public decimal FineAmount { get; set; }
}
