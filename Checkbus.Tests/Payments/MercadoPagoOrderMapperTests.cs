using Checkbus.ApiService.Application.Interfaces.Payments;
using Checkbus.ApiService.Domain.ValueObjects;
using Checkbus.ApiService.Infrastructure.Implementations.Payments;
using MercadoPago.Http;
using MercadoPago.Resource.Order;

namespace Checkbus.Tests.Payments;

/// <summary>
/// Unit tests for <see cref="MercadoPagoOrderMapper"/> — the pure request/response mapping
/// between our <see cref="IPaymentService"/> DTOs and the mercadopago-sdk 2.12.1 Orders API
/// client types (odd/tasks/pagos-mercadopago.md Fase B). Deliberately the ONLY thing this Fase
/// unit-tests for the Mercado Pago adapter: the actual Orders API HTTP round-trip
/// (<see cref="MercadoPagoPaymentService"/>'s own calls into <c>OrderClient</c>) needs live
/// sandbox credentials that don't exist yet (G1, a later Fase) — these tests exercise only the
/// mapping logic, with plain constructed SDK objects, no HTTP involved.
/// </summary>
public class MercadoPagoOrderMapperTests
{
    [Theory]
    [InlineData(1500, "1500.00")]
    [InlineData(1500.5, "1500.50")]
    [InlineData(0.1, "0.10")]
    public void FormatAmount_UsesInvariantTwoDecimalString(decimal amount, string expected)
    {
        Assert.Equal(expected, MercadoPagoOrderMapper.FormatAmount(amount));
    }

    [Fact]
    public void BuildCreateRequest_MapsTypeOnline_TotalAmount_ExternalReference_AndOnePaymentTransaction()
    {
        var request = new CreateOrderRequest(new Money(4500m, "ARS"), "payment-external-ref-123");

        var sdkRequest = MercadoPagoOrderMapper.BuildCreateRequest(request);

        Assert.Equal("online", sdkRequest.Type);
        Assert.Equal("4500.00", sdkRequest.TotalAmount);
        Assert.Equal("payment-external-ref-123", sdkRequest.ExternalReference);
        Assert.NotNull(sdkRequest.Transactions);
        Assert.Single(sdkRequest.Transactions!.Payments);
        Assert.Equal("4500.00", sdkRequest.Transactions.Payments[0].Amount);
        Assert.Null(sdkRequest.Payer);
    }

    [Fact]
    public void BuildCreateRequest_WithPayerEmail_SetsPayer()
    {
        var request = new CreateOrderRequest(new Money(1500m), "ref", PayerEmail: "maria@checkbus.test");

        var sdkRequest = MercadoPagoOrderMapper.BuildCreateRequest(request);

        Assert.NotNull(sdkRequest.Payer);
        Assert.Equal("maria@checkbus.test", sdkRequest.Payer!.Email);
    }

    [Fact]
    public void ToCreateOrderResult_MapsOrderId()
    {
        var order = new Order { Id = "mp-order-123" };

        var result = MercadoPagoOrderMapper.ToCreateOrderResult(order);

        Assert.Equal("mp-order-123", result.OrderId);
    }

    [Fact]
    public void ExtractCheckoutUrl_PrefersRawJsonResponseBody_WhenPresent()
    {
        var order = new Order
        {
            Id = "mp-order-1",
            ApiResponse = new MercadoPagoResponse(200, new Dictionary<string, string>(), "{\"checkout_url\":\"https://mp.test/checkout/abc\"}")
        };

        var checkoutUrl = MercadoPagoOrderMapper.ExtractCheckoutUrl(order);

        Assert.Equal("https://mp.test/checkout/abc", checkoutUrl);
    }

    [Fact]
    public void ExtractCheckoutUrl_FallsBackToPaymentMethodRedirectUrl_WhenNoRawCheckoutUrl()
    {
        var order = new Order
        {
            Id = "mp-order-2",
            ApiResponse = new MercadoPagoResponse(200, new Dictionary<string, string>(), "{\"id\":\"mp-order-2\"}"),
            Transactions = new OrderTransaction
            {
                Payments = new List<OrderPayment>
                {
                    new()
                    {
                        PaymentMethod = new OrderPaymentMethod { RedirectUrl = "https://mp.test/redirect/xyz" }
                    }
                }
            }
        };

        var checkoutUrl = MercadoPagoOrderMapper.ExtractCheckoutUrl(order);

        Assert.Equal("https://mp.test/redirect/xyz", checkoutUrl);
    }

    [Fact]
    public void ExtractCheckoutUrl_ReturnsNull_WhenNeitherSourceHasOne()
    {
        // No ApiResponse at all (e.g. a hand-built Order, same shape a unit test constructs) —
        // the JSON-parsing fallback must not throw on a null/empty raw body.
        var order = new Order { Id = "mp-order-3" };

        var checkoutUrl = MercadoPagoOrderMapper.ExtractCheckoutUrl(order);

        Assert.Null(checkoutUrl);
    }

    [Theory]
    [InlineData("approved")]
    [InlineData("accredited")]
    [InlineData("APPROVED")]
    public void MapStatus_PaymentApprovedVariants_MapToApproved(string rawStatus)
    {
        var order = OrderWithPaymentStatus(rawStatus);

        Assert.Equal(MercadoPagoOrderStatus.Approved, MercadoPagoOrderMapper.MapStatus(order));
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("in_process")]
    [InlineData("in_mediation")]
    public void MapStatus_PendingVariants_MapToPending(string rawStatus)
    {
        var order = OrderWithPaymentStatus(rawStatus);

        Assert.Equal(MercadoPagoOrderStatus.Pending, MercadoPagoOrderMapper.MapStatus(order));
    }

    [Theory]
    [InlineData("rejected")]
    [InlineData("cancelled")]
    [InlineData("canceled")]
    public void MapStatus_RejectedVariants_MapToRejected(string rawStatus)
    {
        var order = OrderWithPaymentStatus(rawStatus);

        Assert.Equal(MercadoPagoOrderStatus.Rejected, MercadoPagoOrderMapper.MapStatus(order));
    }

    [Theory]
    [InlineData("refunded")]
    [InlineData("charged_back")]
    public void MapStatus_RefundedVariants_MapToRefunded(string rawStatus)
    {
        var order = OrderWithPaymentStatus(rawStatus);

        Assert.Equal(MercadoPagoOrderStatus.Refunded, MercadoPagoOrderMapper.MapStatus(order));
    }

    [Fact]
    public void MapStatus_UnknownRawStatus_MapsToUnknown()
    {
        var order = OrderWithPaymentStatus("something_mercadopago_invented_tomorrow");

        Assert.Equal(MercadoPagoOrderStatus.Unknown, MercadoPagoOrderMapper.MapStatus(order));
    }

    [Fact]
    public void MapStatus_NoPaymentAttemptYet_FallsBackToOrderLevelStatus()
    {
        // A freshly created order with no payment attempt yet — Order.Status ("created") is the
        // only signal available, same convention already covered by the "Pending" theory above.
        var order = new Order { Id = "mp-order-4", Status = "created", Transactions = null };

        Assert.Equal(MercadoPagoOrderStatus.Pending, MercadoPagoOrderMapper.MapStatus(order));
    }

    [Fact]
    public void ToOrderStatusResult_MapsOrderIdAndStatus()
    {
        var order = OrderWithPaymentStatus("approved");

        var result = MercadoPagoOrderMapper.ToOrderStatusResult(order);

        Assert.Equal(order.Id, result.OrderId);
        Assert.Equal(MercadoPagoOrderStatus.Approved, result.Status);
    }

    [Fact]
    public void BuildFullRefundRequest_HasNoTransactions()
    {
        // Empty body = total refund, Mercado Pago's own convention.
        var request = MercadoPagoOrderMapper.BuildFullRefundRequest();

        Assert.Null(request.Transactions);
    }

    [Fact]
    public void BuildPartialRefundRequest_TargetsThePaymentId_WithFormattedAmount()
    {
        var request = MercadoPagoOrderMapper.BuildPartialRefundRequest("mp-payment-789", 1500m);

        Assert.NotNull(request.Transactions);
        Assert.Single(request.Transactions!);
        Assert.Equal("mp-payment-789", request.Transactions[0].Id);
        Assert.Equal("1500.00", request.Transactions[0].Amount);
    }

    private static Order OrderWithPaymentStatus(string rawStatus) => new()
    {
        Id = "mp-order-status-test",
        Status = "processed",
        Transactions = new OrderTransaction
        {
            Payments = new List<OrderPayment> { new() { Status = rawStatus } }
        }
    };
}
