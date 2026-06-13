using Shouldly;
using System.Net;
using Xunit;

namespace LibrarySystem.IntegrationTests;

public class SmokeTest : IClassFixture<LibraryWebAppFactory>
{
    private readonly LibraryWebAppFactory _factory;
    private readonly HttpClient _client;

    public SmokeTest(LibraryWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Debug_CheckAllEndpoints()
    {
        // Wait for database to be ready
        await Task.Delay(1000);

        // Test different possible endpoint paths
        var endpoints = new[]
        {
            "/api/books",
            "/books",
            "api/books",
            "/api/Books",
            "/"
        };

        foreach (var endpoint in endpoints)
        {
            var response = await _client.GetAsync(endpoint);
            Console.WriteLine($"Endpoint {endpoint}: {(int)response.StatusCode} - {response.StatusCode}");
        }

        // This should help us see which endpoints are actually registered
        Assert.True(true);
    }

    [Fact]
    public void TestRunnerWorks()
    {
        Assert.True(true, "Test runner is working!");
    }

    [Fact]
    public async Task Debug_GetAllEndpoints()
    {
        // Try to get the OpenAPI document to see what endpoints are registered
        var response = await _client.GetAsync("/openapi/v1.json");

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine("OpenAPI document:");
            Console.WriteLine(content);
        }
        else
        {
            Console.WriteLine($"OpenAPI endpoint returned: {response.StatusCode}");
        }

        // Try common paths
        var paths = new[] { "/api/books", "/api/Books", "/books", "/" };
        foreach (var path in paths)
        {
            var res = await _client.GetAsync(path);
            Console.WriteLine($"{path}: {(int)res.StatusCode} - {res.StatusCode}");
        }
    }

    // Verify the factory works by writing a single "smoke test" that calls
    // GET /api/books
    // and asserts the response is 200 OK
    [Fact]
    public async Task VerifyTheFactoryWorks_AssertResponseIsOK()
    {
        await Task.Delay(1000);

        // Act
        var response = await _client.GetAsync("/api/books");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Optional: Verify we actually got data
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldNotBeEmpty();

        // Additional verification
        Console.WriteLine($"Response content: {content}");
    }
}
