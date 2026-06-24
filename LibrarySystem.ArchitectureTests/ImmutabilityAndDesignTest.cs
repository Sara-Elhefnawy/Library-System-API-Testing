using FluentAssertions;
using LibrarySystem.Data.Context;
using LibrarySystem.Data.Models;
using LibrarySystem.Services.Services;
using NetArchTest.Rules;
using Shouldly;
using System.Collections;
using System.Reflection;
using Xunit;

namespace LibrarySystem.ArchitectureTests;

public class ImmutabilityAndDesignTest
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
    public void AllCustomExceptionClasses_MustInheritFromException()
    {
        var result = Types.InAssembly(_servicesAssembly)
            .That()
            .ResideInNamespace("LibrarySystem.Services.Exceptions")
            .And()
            .AreClasses()
            .Should()
            .Inherit(typeof(Exception))
            .GetResult();

        result.IsSuccessful.ShouldBeTrue($"Exception classes not inheriting from Exception: {string.Join(", ", result.FailingTypes?.Select(t => t.Name) ?? [])}");
    }

    [Fact]
    public void ServiceClassesMustNotBeStatic()
    {
        var result = Types.InAssembly(_servicesAssembly)
            .That()
            .ResideInNamespace("LibrarySystem.Services.Services")
            .And()
            .AreClasses()
            .Should()
            .NotBeStatic()
            .GetResult();

        result.IsSuccessful.ShouldBeTrue($"Static service classes: {string.Join(", ", result.FailingTypes?.Select(t => t.Name) ?? [])}");
    }

    // Tests that all entity navigation properties (collections) have public setters.
    // This prevents NullReferenceException when accessing navigation properties that haven't been initialized.
    [Fact]
    public void EntityNavigationProperties_MustHavePublicSetters()
    {
        var entities = Types.InAssembly(_dataAssembly)
            .That()
            .ResideInNamespace("LibrarySystem.Data.Models")
            .And()
            .AreClasses()
            .And()
            .Inherit(typeof(BaseEntity))
            .GetTypes();

        var issues = new List<string>();

        foreach (var entity in entities)
        {
            // Get all public instance properties of this entity
            var properties = entity.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                // Filter to only collection properties (e.g., ICollection<Borrow>, List<Book>)
                // We exclude string because it's IEnumerable<char> but we don't treat it as a collection
                .Where(p => IsCollectionType(p.PropertyType));

            // Check each collection property
            foreach (var prop in properties)
            {
                // Check if the property has a public setter
                //      - SetMethod is null: property is read-only (getter only)
                //      - SetMethod.IsPrivate: property has a private setter
                // Both cases mean the property can't be set from outside the class
                if (prop.SetMethod == null || prop.SetMethod.IsPrivate)
                    issues.Add($"{entity.Name}.{prop.Name} - no public setter");
            }
        }

        issues.ShouldBeEmpty($"Entity navigation properties with issues found: {string.Join(", ", issues)}");
    }

    private bool IsCollectionType(Type type)
    {
        if (type == typeof(string)) return false;
        return typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string);
    }
}