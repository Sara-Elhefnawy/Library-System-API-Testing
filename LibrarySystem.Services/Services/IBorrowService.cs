using LibrarySystem.Data.Models;

namespace LibrarySystem.Services.Services;

public interface IBorrowService
{
    Task<Borrow> BorrowBookAsync(int bookId, int memberId);

    Task<Borrow?> ReturnBookAsync(int borrowId);

    decimal CalculateFine(Borrow borrow);
}
