using LibrarySystem.Data.Context;
using LibrarySystem.Services.Services;
using NetArchTest.Rules;
using FluentAssertions;
using System.Reflection;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using LibrarySystem.Data.Repositories.Abstractions;

namespace LibrarySystem.ArchitectureTests;

public class LayerDependencyTest
{
    private static readonly Assembly _apiAssembly = typeof(Program).Assembly;
    private static readonly Assembly _servicesAssembly = typeof(IBorrowService).Assembly;
    private static readonly Assembly _dataAssembly = typeof(LibraryAppDbContext).Assembly;

    [Fact]
    public void Services_ShouldNotDependOnAPI()
    {
        // Arrange & Act
        var result = Types.InAssembly(_servicesAssembly)
            .Should()
            .NotHaveDependencyOn("LibrarySystem.API")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue("LibrarySystem.Services should not reference LibrarySystem.API");
    }

    [Fact]
    public void Data_ShouldNotDependOnServicesOrAPI()
    {
        // Arrange & Act
        var result = Types.InAssembly(_dataAssembly)
            .Should()
            .NotHaveDependencyOn("LibrarySystem.Services")
            .And()
            .NotHaveDependencyOn("LibrarySystem.API")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue("LibrarySystem.Data should not reference LibrarySystem.Services or LibrarySystem.API");
    }

    [Fact]
    public void Controllers_ShouldNotDirectlyReferenceLibraryDbContext()
    {
        // Arrange
        var controllers = Types.InAssembly(_apiAssembly)
            .That()
            .Inherit(typeof(ControllerBase))
            .GetTypes();

        // Act
        var result = Types.InAssembly(_apiAssembly)
            .That()
            .Inherit(typeof(ControllerBase))
            .Should()
            .NotHaveDependencyOn("LibrarySystem.Data.Context")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue("Controllers should not directly reference LibraryDbContext. Use service layer instead.");
    }

    [Fact]
    public void Controllers_ShouldNotReferenceRepositoryInterfaces()
    {
        // Arrange
        var repositoryInterfaces = new[]
        {
            typeof(IBookRepository).FullName,
            typeof(IMemberRepository).FullName,
            typeof(IBorrowRepository).FullName
        };

        // Act
        var result = Types.InAssembly(_apiAssembly)
            .That()
            .Inherit(typeof(ControllerBase))
            .Should()
            .NotHaveDependencyOnAny(repositoryInterfaces)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue("Controllers should depend on service interfaces, not repository interfaces directly.");
    }
}
