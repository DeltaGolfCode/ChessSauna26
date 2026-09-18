using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PaymentGateway.Logic.Tests;

[ExcludeFromCodeCoverage]
public class RegisterExternalServicesTests
{
    [Fact]
    public void RegisterServices_ValidBaseUrl_ConfiguresClientWithBaseAddress()
    {
        // Arrange
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Services:BankGateway:BaseUrl"] = "https://bank.example.com"
        });
        var httpClientFactory = BuildHttpClientFactory(configuration);

        // Act
        var client = httpClientFactory.CreateClient("BankGateway");

        // Assert
        Assert.Equal(new Uri("https://bank.example.com"), client.BaseAddress);
    }

    [Fact]
    public void RegisterServices_NoTimeoutConfigured_DefaultsToTenSeconds()
    {
        // Arrange
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Services:BankGateway:BaseUrl"] = "https://bank.example.com"
        });
        var httpClientFactory = BuildHttpClientFactory(configuration);

        // Act
        var client = httpClientFactory.CreateClient("BankGateway");

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(10), client.Timeout);
    }

    [Fact]
    public void RegisterServices_TimeoutConfigured_UsesConfiguredValue()
    {
        // Arrange
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Services:BankGateway:BaseUrl"] = "https://bank.example.com",
            ["Services:BankGateway:TimeoutSeconds"] = "30"
        });
        var httpClientFactory = BuildHttpClientFactory(configuration);

        // Act
        var client = httpClientFactory.CreateClient("BankGateway");

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(30), client.Timeout);
    }

    [Fact]
    public void RegisterServices_MissingBaseUrl_ThrowsInvalidOperationExceptionOnClientCreation()
    {
        // Arrange
        var configuration = CreateConfiguration(new Dictionary<string, string?>());
        var httpClientFactory = BuildHttpClientFactory(configuration);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => httpClientFactory.CreateClient("BankGateway"));
        Assert.Equal("BankGateway BaseUrl is not configured in appsettings.json", exception.Message);
    }

    [Fact]
    public void RegisterServices_InvalidBaseUrlFormat_ThrowsInvalidOperationExceptionOnClientCreation()
    {
        // Arrange
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Services:BankGateway:BaseUrl"] = "not a valid url"
        });
        var httpClientFactory = BuildHttpClientFactory(configuration);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => httpClientFactory.CreateClient("BankGateway"));
        Assert.Equal("Invalid BankGateway BaseUrl format: not a valid url", exception.Message);
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string?> settings)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    private static IHttpClientFactory BuildHttpClientFactory(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.RegisterServices(configuration);
        return services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();
    }
}