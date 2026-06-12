namespace LibrarySystem.Data.Models;

public class Member : BaseEntity
{
    public string FullName { get; set; } = default!;

    public string Email { get; set; } = default!;

    public DateTime MembershipExpiryDate { get; set; }

    public decimal OutstandingFine { get; set; }

    public ICollection<Borrow> Borrows { get; set; } = [];
}
