using LibrarySystem.Services.Services;
using Xunit;
using Moq;
using LibrarySystem.Data.Models;
using FluentAssertions;
using LibrarySystem.Data.Repositories;
using LibrarySystem.Data.Context;
using Shouldly;

namespace LibrarySystem.FluentAssertionsTests;

public class CalculateFineTests
{
    // Write one test for LoanService.CalculateFine() using FluentAssertions syntax: result.Should().Be(expectedFine).
    [Fact]
    public void CalculateFine_BorrowService_ShouldBeExpectedFine_FluentAssertions()
    {
        // Arrange
        var borrowService = new BorrowService(
            Mock.Of<IBookRepository>(),
            Mock.Of<IMemberRepository>(),
            Mock.Of<IBorrowRepository>(),
            Mock.Of<LibraryAppDbContext>());
        var borrow = new Borrow
        {
            DueDate = DateTime.UtcNow.AddDays(-5) // 5 days overdue
        };
        var expectedFine = 5 * 0.50m;

        // Act
        var result = borrowService.CalculateFine(borrow);

        // Assert
        result.Should().Be(expectedFine);
    }

    // Write the identical test next to it using Shouldly: result.ShouldBe(expectedFine)
    [Fact]
    public void CalculateFine_BorrowService_ShouldBeExpectedFine_Shouldly()
    {
        // Arrange
        var borrowService = new BorrowService(
            Mock.Of<IBookRepository>(),
            Mock.Of<IMemberRepository>(),
            Mock.Of<IBorrowRepository>(),
            Mock.Of<LibraryAppDbContext>());
        var borrow = new Borrow
        {
            DueDate = DateTime.UtcNow.AddDays(-5) // 5 days overdue
        };
        var expectedFine = 5 * 0.50m;

        // Act
        var result = borrowService.CalculateFine(borrow);

        // Assert
        result.ShouldBe(expectedFine);
    }
}
