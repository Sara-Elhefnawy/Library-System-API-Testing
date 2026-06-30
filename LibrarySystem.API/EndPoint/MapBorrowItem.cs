using LibrarySystem.Data.Repositories.Abstractions;
using LibrarySystem.Services.Exceptions;
using LibrarySystem.Services.Services;

namespace LibrarySystem.API.EndPoint;

public static class MapBorrowItem
{
    public static void MapBorrowItemEndPoint(this WebApplication app)
    {
        // Borrow a book. Body: { memberId, bookId }.
        // Apply all 8 business rules.
        // Return 201 with loan details, or 422 with a clear error message if a rule is violated
        app.MapPost("/api/borrow", async (BorrowRequest request, IBorrowService borrowService) =>
        {
            try
            {
                var borrow = await borrowService.BorrowBookAsync(request.BookId, request.MemberId);

                if (borrow is null)
                    return Results.UnprocessableEntity("Failed to borrow book. Check business rules.");

                return Results.Created($"/api/loans/{borrow.Id}", borrow);
            }
            catch (Exception ex)
            {
                return Results.UnprocessableEntity(ex.Message);
            }
        });

        // Return a borrowed book.
        // Calculate fine if overdue.
        // Return updated loan with fine amount
        app.MapPut("/api/borrow/{id}/return", async (int id, IBorrowService borrowService) =>
        {
            try
            {
                var borrow = await borrowService.ReturnBookAsync(id);

                if (borrow is null)
                    return Results.NotFound($"Borrow record {id} not found");

                return Results.Ok(new
                {
                    borrow.Id,
                    borrow.FineAmount,
                    Message = borrow.FineAmount > 0 ? $"Late return! Fine: £{borrow.FineAmount}" : "Book returned on time"
                });
            }
            // ReturnBookAsync throws (rather than returning null)
            //      when the loan was already returned.
            // Catch it here so the client gets a clean 422
            //      instead of an unhandled-exception 500.
            catch (AlreadyReturnedException ex)
            {
                return Results.UnprocessableEntity(ex.Message);
            }
        });

        // Return all loans for a member, ordered by BorrowedAt descending.
        // Include book title in the response
        app.MapGet("/api/borrow", async (int memberId, IBorrowRepository borrowRepo) =>
        {
            if (memberId <= 0)
                return Results.BadRequest("MemberId is required");

            var loans = await borrowRepo.GetAllMemberBorrowsAsync(memberId);

            var result = loans?.Select(l => new
            {
                l.Id,
                BookTitle = l.Book.Title,
                l.BorrowedAt,
                l.DueDate,
                l.ReturnedAt,
                l.FineAmount
            });

            return Results.Ok(result ?? []);
        });

    }
}
public class BorrowRequest
{
    public int MemberId { get; set; }
    public int BookId { get; set; }
}
