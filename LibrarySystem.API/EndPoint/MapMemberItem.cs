using LibrarySystem.Data.Models;
using LibrarySystem.Data.Repositories.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.API.EndPoint;

public static class MapMemberItem
{
    public static void MapMemberItemEndPoint(this WebApplication app)
    {
        // Return member details including active loan count and outstanding fine.
        app.MapGet("/api/members/{id}", async ([FromRoute] int id, IMemberRepository memberRepo, IBorrowRepository borrowRepo) =>
        {
            var member = await memberRepo.GetByIdAsync(id);
            if (member is null)
                return Results.NotFound();
            var activeLoansCount = await borrowRepo.GetActiveBorrowsByMemberIdAsync(id);
            var outstandingFine = await memberRepo.GetOutstandingFineAsync(id);
            var result = new
            {
                member.Id,
                member.FullName,
                ActiveLoansCount = activeLoansCount,
                OutstandingFine = outstandingFine
            };
            return Results.Ok(result);
        });

        // Register a new member.
        // Email must be unique. Return 201
        app.MapPost("/api/members", async ([FromBody] Member member, IMemberRepository repo) =>
        {
            if (await repo.EmailExistsAsync(member.Email))
                return Results.BadRequest($"A member with email {member.Email} already exists.");
            await repo.AddAsync(member);
            await repo.CommitChanges();
            return Results.Created($"/api/members/{member.Id}", member);
        });
    }
}
