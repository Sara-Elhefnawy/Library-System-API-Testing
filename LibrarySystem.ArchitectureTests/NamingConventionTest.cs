using LibrarySystem.Data.Context;
using LibrarySystem.Services.Services;
using NetArchTest.Rules;
using FluentAssertions;
using System.Reflection;
using Xunit;

namespace LibrarySystem.ArchitectureTests;

public class NamingAndStructureTests
{
    private static readonly Assembly _apiAssembly = typeof(Program).Assembly;
    private static readonly Assembly _servicesAssembly = typeof(IBorrowService).Assembly;
    private static readonly Assembly _dataAssembly = typeof(LibraryAppDbContext).Assembly;

    private static readonly Assembly[] _allAssemblies =
    [
        _apiAssembly,
        _servicesAssembly,
        _dataAssembly
    ];

    [Fact]
    public void All_Service_Classes_Must_Implement_Corresponding_Interface()
    {
        var serviceClasses = Types.InAssembly(_servicesAssembly)
            .That()
            .HaveNameEndingWith("Service")
            .And()
            .AreClasses()
            .GetTypes()
            .ToList();

        // Search for the interface in ALL assemblies (not just the Services assembly)
        // This is important because interfaces might be defined in a different project
        var serviceInterfaces = _allAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsInterface && t.Name.StartsWith("I") && t.Name.EndsWith("Service"))
            .ToList();

        var errors = new List<string>();

        foreach (var serviceClass in serviceClasses)
        {
            var expectedInterfaceName = $"I{serviceClass.Name}";

            var matchingInterface = serviceInterfaces
                .FirstOrDefault(t => t.Name == expectedInterfaceName);

            if (matchingInterface is null)
            {
                errors.Add($"Missing interface '{expectedInterfaceName}' for class '{serviceClass.Name}'");
            }
            else if (!matchingInterface.IsAssignableFrom(serviceClass))
            {
                errors.Add($"Class '{serviceClass.Name}' does not implement '{expectedInterfaceName}'");
            }
        }

        errors.Should().BeEmpty("All Service classes must implement I[Name]Service interface");
    }

    [Fact]
    public void All_Repository_Classes_Must_Implement_Corresponding_Interface()
    {
        var repositoryClasses = Types.InAssembly(_dataAssembly)
            .That()
            .HaveNameEndingWith("Repository")
            .And()
            .AreClasses()
            .GetTypes()
            .ToList();

        // Search for the interface in ALL assemblies (not just the Repository assembly)
        // This is important because interfaces might be defined in a different project
        var repositoryInterfaces = _allAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsInterface && t.Name.StartsWith("I") && t.Name.EndsWith("Repository"))
            .ToList();

        var errors = new List<string>();

        foreach (var repoClass in repositoryClasses)
        {
            var expectedInterfaceName = $"I{repoClass.Name}";

            // Try to find the interface in ALL assemblies
            var matchingInterface = repositoryInterfaces
                .FirstOrDefault(t => t.IsInterface && t.Name == expectedInterfaceName);

            if (matchingInterface is null)
            {
                errors.Add($"Missing interface '{expectedInterfaceName}' for class '{repoClass.Name}'");
            }
            else if (!matchingInterface.IsAssignableFrom(repoClass))
            {
                errors.Add($"Class '{repoClass.Name}' does not implement '{expectedInterfaceName}'");
            }
        }

        errors.Should().BeEmpty("All Repository classes must implement I[Name]Repository interface");
    }

    [Fact]
    public void All_Controllers_Must_Reside_In_LibrarySystem_API_Controllers_Namespace()
    {
        // .ResideInNamespace() checks that the type is in the specified namespace
        var result = Types.InAssembly(_apiAssembly)
            .That()
            .HaveNameEndingWith("Controller")
            .And()
            .AreClasses()
            .Should()
            .ResideInNamespace("LibrarySystem.API.Controllers")  // Must be in this namespace
            .GetResult();

        result.IsSuccessful.Should().BeTrue("All controllers must be in LibrarySystem.API.Controllers namespace");
    }

    [Fact]
    public void No_Class_Should_Have_Manager_In_Its_Name()
    {
        // .NotHaveNameMatching() uses a regex pattern to check names
        var result = Types.InAssembly(_apiAssembly)
            .That()
            .AreClasses()
            .Should()
            .NotHaveNameMatching(".*[Mm]anager.*")
            .GetResult();

        result.IsSuccessful.Should().BeTrue("No class should have 'Manager' in its name");
    }
}
