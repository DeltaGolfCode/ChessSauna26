using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;

using NSubstitute;

using PaymentGateway.Logic.ExternalResources;
using PaymentGateway.Logic.ExternalResources.Models.Response;
using PaymentGateway.Logic.Models.Request;

namespace PaymentGateway.Logic.Tests.ExternalResources;

[ExcludeFromCodeCoverage]
public class BankGatewayTests
{
    private sealed class FakeHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(response);
        }
    }

    private static PaymentRequest CreateValidPaymentRequest()
    {
        return new PaymentRequest
        {
            CardNumber = "4532123456789012",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "GBP",
            Amount = 100,
            Cvv = "123"
        };
    }

    private static (BankGateway Gateway, FakeHttpMessageHandler Handler) CreateGateway(HttpResponseMessage response)
    {
        var handler = new FakeHttpMessageHandler(response);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://bank.test") };

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient("BankGateway").Returns(httpClient);

        return (new BankGateway(httpClientFactory), handler);
    }

    [Fact]
    public async Task SendPaymentRequestAsync_AuthorizedResponse_ReturnsDeserializedBankResponse()
    {
        // Arrange
        var responseBody = new BankPaymentResponse { Authorized = true, AuthorizationCode = "ABC123" };
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(responseBody)
        };
        var (gateway, _) = CreateGateway(httpResponse);

        // Act
        var result = await gateway.SendPaymentRequestAsync(CreateValidPaymentRequest());

        // Assert
        Assert.True(result.Authorized);
        Assert.Equal("ABC123", result.AuthorizationCode);
    }

    [Fact]
    public async Task SendPaymentRequestAsync_ValidRequest_SendsRequestToPaymentsEndpoint()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new BankPaymentResponse { Authorized = true })
        };
        var (gateway, handler) = CreateGateway(httpResponse);

        // Act
        await gateway.SendPaymentRequestAsync(CreateValidPaymentRequest());

        // Assert
        Assert.Equal("/payments", handler.LastRequest?.RequestUri?.AbsolutePath);
        Assert.Equal(HttpMethod.Post, handler.LastRequest?.Method);
    }

    [Fact]
    public async Task SendPaymentRequestAsync_ValidRequest_UsesNamedBankGatewayClient()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new BankPaymentResponse { Authorized = true })
        };
        var handler = new FakeHttpMessageHandler(httpResponse);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://bank.test") };

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient("BankGateway").Returns(httpClient);
        var gateway = new BankGateway(httpClientFactory);

        // Act
        await gateway.SendPaymentRequestAsync(CreateValidPaymentRequest());

        // Assert
        httpClientFactory.Received(1).CreateClient("BankGateway");
    }

    [Fact]
    public async Task SendPaymentRequestAsync_ErrorStatusCode_ThrowsHttpRequestException()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        var (gateway, _) = CreateGateway(httpResponse);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => gateway.SendPaymentRequestAsync(CreateValidPaymentRequest()));
    }

    [Fact]
    public async Task SendPaymentRequestAsync_EmptyResponseBody_ThrowsInvalidOperationException()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json")
        };
        var (gateway, _) = CreateGateway(httpResponse);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => gateway.SendPaymentRequestAsync(CreateValidPaymentRequest()));
        Assert.Equal("Empty response from bank.", exception.Message);
    }
}
