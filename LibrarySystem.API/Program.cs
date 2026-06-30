using LibrarySystem.API.EndPoint;
using LibrarySystem.Data.Context;
using LibrarySystem.Data.Repositories;
using LibrarySystem.Data.Repositories.Abstractions;
using LibrarySystem.Services.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Book <-> Borrow is a circular reference
//      (Book.Borrows contains Borrows, each Borrow.Book points back to the Book).
// Without this, serializing any Book or Borrow that includes navigation properties
// throws a JsonException ("possible object cycle detected").
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

builder.Services.AddDbContext<LibraryAppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddScoped<IBorrowService, BorrowService>();
builder.Services.AddScoped<IBorrowRepository, BorrowRepository>();
builder.Services.AddScoped<IMemberRepository, MemberRepository>();
builder.Services.AddScoped<IBookRepository, BookRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();


app.MapBookItemEndPoint();
app.MapBorrowItemEndPoint();
app.MapMemberItemEndPoint();


app.Run();
