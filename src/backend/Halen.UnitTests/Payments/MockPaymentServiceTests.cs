using FluentAssertions;
using Halen.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Halen.UnitTests.Payments;

[TestClass]
public class MockPaymentServiceTests
{
    private MockPaymentService _sut = null!;

    [TestInitialize]
    public void Initialize()
    {
        _sut = new MockPaymentService(Mock.Of<ILogger<MockPaymentService>>());
    }

    [TestMethod]
    public async Task CreateIntentAsync_ReturnsSuccessWithNonEmptyPaymentIntentId()
    {
        var result = await _sut.CreateIntentAsync(
            Guid.NewGuid(), 150m, "USD", "idempotency_key_123");

        result.Success.Should().BeTrue();
        result.PaymentIntentId.Should().NotBeNullOrEmpty();
        result.ErrorMessage.Should().BeNull();
    }

    [TestMethod]
    public async Task CaptureIntentAsync_ReturnsSuccess()
    {
        var result = await _sut.CaptureIntentAsync("mock_intent_abc");

        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [TestMethod]
    public async Task RefundIntentAsync_ReturnsSuccess()
    {
        var result = await _sut.RefundIntentAsync("mock_intent_abc");

        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }
}
