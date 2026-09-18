using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;

using Microsoft.Extensions.Logging;

using NSubstitute;

using PaymentGateway.Logic.ExternalResources;
using PaymentGateway.Logic.ExternalResources.Models.Response;
using PaymentGateway.Logic.Models.Request;

namespace PaymentGateway.Logic.Tests.ExternalResources;

[ExcludeFromCodeCoverage]
public class BankGatewayTests
{
    [Fact]
    public async Task SendPaymentRequestAsync_AuthorizedResponse_ReturnsDeserializedBankResponse()
    {
        // Arrange
        var responseBody = new BankPaymentResponse { Authorized = true, AuthorizationCode = "ABC123" };
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(responseBody)
        };
        var (gateway, _, _) = CreateGateway(httpResponse);

        // Act
        var result = await gateway.SendPaymentRequestAsync(CreateValidPaymentRequest());

        // Assert
        Assert.NotNull(result);

        Assert.Multiple(
            () => Assert.True(result.Authorized),
            () => Assert.Equal("ABC123", result.AuthorizationCode));
    }

    [Fact]
    public async Task SendPaymentRequestAsync_ValidRequest_SendsRequestToPaymentsEndpoint()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new BankPaymentResponse { Authorized = true })
        };
        var (gateway, handler, _) = CreateGateway(httpResponse);

        // Act
        await gateway.SendPaymentRequestAsync(CreateValidPaymentRequest());

        // Assert
        Assert.Multiple(
            () => Assert.Equal("/payments", handler.LastRequest?.RequestUri?.AbsolutePath),
            () => Assert.Equal(HttpMethod.Post, handler.LastRequest?.Method));
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
        var gateway = new BankGateway(httpClientFactory, Substitute.For<ILogger<BankGateway>>());

        // Act
        await gateway.SendPaymentRequestAsync(CreateValidPaymentRequest());

        // Assert
        httpClientFactory.Received(1).CreateClient("BankGateway");
    }

    [Fact]
    public async Task SendPaymentRequestAsync_SuccessfulResponse_DoesNotLog()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new BankPaymentResponse { Authorized = true })
        };
        var (gateway, _, logger) = CreateGateway(httpResponse);

        // Act
        await gateway.SendPaymentRequestAsync(CreateValidPaymentRequest());

        // Assert
        Assert.DoesNotContain(logger.ReceivedCalls(), call => call.GetMethodInfo().Name == nameof(ILogger.Log));
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task SendPaymentRequestAsync_ErrorStatusCode_ReturnsNull(HttpStatusCode statusCode)
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(statusCode);
        var (gateway, _, _) = CreateGateway(httpResponse);

        // Act
        var result = await gateway.SendPaymentRequestAsync(CreateValidPaymentRequest());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SendPaymentRequestAsync_ErrorStatusCode_LogsHttpRequestExceptionAsError()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        var (gateway, _, logger) = CreateGateway(httpResponse);

        // Act
        await gateway.SendPaymentRequestAsync(CreateValidPaymentRequest());

        // Assert
        var logArguments = Assert.Single(logger.ReceivedCalls(), call => call.GetMethodInfo().Name == nameof(ILogger.Log))
            .GetArguments();

        Assert.Multiple(
            () => Assert.Equal(LogLevel.Error, logArguments[0]),
            () => Assert.IsType<HttpRequestException>(logArguments[3]));
    }

    [Fact]
    public async Task SendPaymentRequestAsync_EmptyResponseBody_ReturnsNull()
    {
        // Arrange
        var (gateway, _, _) = CreateGateway(CreateEmptyBodyResponse());

        // Act
        var result = await gateway.SendPaymentRequestAsync(CreateValidPaymentRequest());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SendPaymentRequestAsync_EmptyResponseBody_LogsInvalidOperationExceptionAsError()
    {
        // Arrange
        var (gateway, _, logger) = CreateGateway(CreateEmptyBodyResponse());

        // Act
        await gateway.SendPaymentRequestAsync(CreateValidPaymentRequest());

        // Assert
        var logArguments = Assert.Single(logger.ReceivedCalls(), call => call.GetMethodInfo().Name == nameof(ILogger.Log))
            .GetArguments();
        var exception = Assert.IsType<InvalidOperationException>(logArguments[3]);

        Assert.Multiple(
            () => Assert.Equal(LogLevel.Error, logArguments[0]),
            () => Assert.Equal("Empty response from bank.", exception.Message));
    }

    [Fact]
    public async Task SendPaymentRequestAsync_BankUnreachable_ReturnsNull()
    {
        // Arrange
        var handler = new ThrowingHttpMessageHandler(new HttpRequestException("Connection refused"));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://bank.test") };

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient("BankGateway").Returns(httpClient);
        var gateway = new BankGateway(httpClientFactory, Substitute.For<ILogger<BankGateway>>());

        // Act
        var result = await gateway.SendPaymentRequestAsync(CreateValidPaymentRequest());

        // Assert
        Assert.Null(result);
    }

    private sealed class FakeHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(response);
        }
    }

    private sealed class ThrowingHttpMessageHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromException<HttpResponseMessage>(exception);
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

    private static HttpResponseMessage CreateEmptyBodyResponse()
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json")
        };
    }

    private static (BankGateway Gateway, FakeHttpMessageHandler Handler, ILogger<BankGateway> Logger) CreateGateway(HttpResponseMessage response)
    {
        var handler = new FakeHttpMessageHandler(response);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://bank.test") };

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient("BankGateway").Returns(httpClient);

        var logger = Substitute.For<ILogger<BankGateway>>();

        return (new BankGateway(httpClientFactory, logger), handler, logger);
    }
}