using LibrarySystem.Data.Context;
using LibrarySystem.Data.Models;
using LibrarySystem.Data.Repositories.Abstractions;
using LibrarySystem.Services.Exceptions;

namespace LibrarySystem.Services.Services;

public class BorrowService(
    IBookRepository bookRepository,
    IMemberRepository memberRepository,
    IBorrowRepository borrowRepository,
    LibraryAppDbContext dbContext) : IBorrowService
{
    // A member cannot borrow if their MembershipExpiryDate is in the past
    // A member cannot borrow more than 3 books at the same time (active loans)
    // A book cannot be borrowed if AvailableCopies == 0
    // A member cannot borrow if they have an OutstandingFine > 0
    // Borrowing decrements AvailableCopies by 1 atomically
    public async Task<Borrow> BorrowBookAsync(int bookId, int memberId)
    {
        var book = await bookRepository.GetByIdAsync(bookId);
        if (book is null)
            throw new Exception($"Book with ID {bookId} not found");

        var member = await memberRepository.GetByIdAsync(memberId);
        if (member is null)
            throw new Exception($"Member with ID {memberId} not found");

        if (member.MembershipExpiryDate < DateTime.UtcNow)
            throw new MembershipExpiredException();

        var activeBorrows = await borrowRepository.GetActiveBorrowsByMemberIdAsync(memberId);
        if(activeBorrows != null && activeBorrows.Count >= 3)
            throw new LoanLimitExceededException();

        if (book.AvailableCopies == 0)
            throw new BookNotAvailableException();

        if (member.OutstandingFine > 0) 
            throw new OutstandingFineException(member.OutstandingFine);

        var borrow = new Borrow
        {
            BookId = bookId,
            MemberId = memberId,
            BorrowedAt = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(14)
        };

        book.AvailableCopies--;
        bookRepository.Update(book);

        await borrowRepository.AddAsync(borrow);

        await dbContext.SaveChangesAsync();

        return borrow;
    }

    // Returning a book increments AvailableCopies by 1
    // On return, if ReturnedAt > DueDate, calculate fine as (ReturnedAt - DueDate).Days × £0.50 and add it to Member.OutstandingFine
     //A loan that is already returned (RetunedAt != null) cannot be returned again
    public async Task<Borrow?> ReturnBookAsync(int borrowId)
    {
        var borrow = await borrowRepository.GetByIdAsync(borrowId);
        if (borrow is null)
            return null;

        if (borrow.ReturnedAt != null)
            throw new AlreadyReturnedException();

        var book = await bookRepository.GetByIdAsync(borrow.BookId);
        if(book is null) 
            return null;

        var member = await memberRepository.GetByIdAsync(borrow.MemberId);
        if (member is null)
            return null;

        borrow.ReturnedAt = DateTime.UtcNow;

        if (borrow.ReturnedAt > borrow.DueDate)
        {
            var daysOverdue = (borrow.ReturnedAt.Value - borrow.DueDate).Days;
            borrow.FineAmount = daysOverdue * 0.50m;
            member.OutstandingFine += borrow.FineAmount;
        }

        memberRepository.Update(member);

        book.AvailableCopies++;
        bookRepository.Update(book);

        borrowRepository.Update(borrow);

        await dbContext.SaveChangesAsync();

        return borrow;
    }

    public decimal CalculateFine(Borrow borrow)
    {
        if (borrow.ReturnedAt == null || borrow.ReturnedAt <= borrow.DueDate)
            return 0;

        var daysOverdue = (borrow.ReturnedAt.Value - borrow.DueDate).Days;
        return daysOverdue * 0.50m;
    }
}
