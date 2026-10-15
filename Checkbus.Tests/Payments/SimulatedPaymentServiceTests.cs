using Checkbus.ApiService.Application.Interfaces.Payments;
using Checkbus.ApiService.Domain.ValueObjects;
using Checkbus.ApiService.Infrastructure.Implementations.Payments;

namespace Checkbus.Tests.Payments;

/// <summary>
/// Proves <see cref="SimulatedPaymentService"/> (odd/tasks/pagos-mercadopago.md Fase J1) — the
/// in-memory fake <see cref="IPaymentService"/> adapter built to work around I10 (Mercado Pago
/// account KYC blocker) for this academic project. No HTTP, no DI container — plain construction
/// against a known <see cref="SimulatedPaymentOptions"/>, same posture as
/// <c>MercadoPagoOrderMapperTests</c>.
/// </summary>
public class SimulatedPaymentServiceTests
{
    private static SimulatedPaymentService CreateService(string apiBaseUrl = "https://checkbus-demo.test") =>
        new(new SimulatedPaymentOptions { ApiBaseUrl = apiBaseUrl });

    [Fact]
    public async Task CreateOrderAsync_ReturnsCheckoutUrl_ContainingGeneratedOrderIdAndConfiguredApiBaseUrl()
    {
        var service = CreateService();
        var request = new CreateOrderRequest(new Money(1500m), "payment-ref-1");

        var result = await service.CreateOrderAsync(request, CancellationToken.None);

        Assert.StartsWith("SIM-", result.OrderId);
        Assert.NotNull(result.CheckoutUrl);
        Assert.StartsWith("https://checkbus-demo.test/api/public/payments/simulate/", result.CheckoutUrl);
        Assert.EndsWith(result.OrderId, result.CheckoutUrl);
    }

    [Fact]
    public async Task CreateOrderAsync_BlankApiBaseUrl_StillReturnsAUsableRelativeCheckoutUrl()
    {
        var service = CreateService(apiBaseUrl: string.Empty);
        var request = new CreateOrderRequest(new Money(1500m), "payment-ref-1");

        var result = await service.CreateOrderAsync(request, CancellationToken.None);

        Assert.Equal($"/api/public/payments/simulate/{result.OrderId}", result.CheckoutUrl);
    }

    [Fact]
    public async Task CreateOrderAsync_TwoCalls_GenerateDifferentOrderIds()
    {
        var service = CreateService();
        var request = new CreateOrderRequest(new Money(1500m), "payment-ref-1");

        var first = await service.CreateOrderAsync(request, CancellationToken.None);
        var second = await service.CreateOrderAsync(request, CancellationToken.None);

        Assert.NotEqual(first.OrderId, second.OrderId);
    }

    [Fact]
    public async Task GetOrderAsync_UnknownOrderId_ReturnsUnknown_RatherThanThrowing()
    {
        var service = CreateService();

        var result = await service.GetOrderAsync("SIM-never-created", CancellationToken.None);

        Assert.Equal(MercadoPagoOrderStatus.Unknown, result.Status);
    }

    [Fact]
    public async Task GetOrderAsync_FreshlyCreatedOrder_StartsAsPending()
    {
        var service = CreateService();
        var request = new CreateOrderRequest(new Money(1500m), "payment-ref-1");
        var created = await service.CreateOrderAsync(request, CancellationToken.None);

        var result = await service.GetOrderAsync(created.OrderId, CancellationToken.None);

        Assert.Equal(MercadoPagoOrderStatus.Pending, result.Status);
    }

    [Fact]
    public async Task GetOrderAsync_ReflectsStatusSetViaTheNarrowerStoreInterface()
    {
        ISimulatedOrderStore store = CreateService();
        var service = (SimulatedPaymentService)store;
        var request = new CreateOrderRequest(new Money(1500m), "payment-ref-1");
        var created = await service.CreateOrderAsync(request, CancellationToken.None);

        store.SetStatus(created.OrderId, MercadoPagoOrderStatus.Approved);

        var result = await service.GetOrderAsync(created.OrderId, CancellationToken.None);
        Assert.Equal(MercadoPagoOrderStatus.Approved, result.Status);
    }

    [Fact]
    public void ISimulatedOrderStore_SetStatus_OnUnknownOrderId_IsANoOp_RatherThanThrowing()
    {
        ISimulatedOrderStore store = CreateService();

        store.SetStatus("SIM-never-created", MercadoPagoOrderStatus.Approved);

        Assert.Null(store.Get("SIM-never-created"));
    }

    [Fact]
    public async Task ISimulatedOrderStore_Get_ReturnsTheStoredRecordWithOriginalRequestFields()
    {
        ISimulatedOrderStore store = CreateService();
        var service = (SimulatedPaymentService)store;
        var request = new CreateOrderRequest(
            new Money(2500m, "ARS"), "payment-ref-2",
            SuccessUrl: "https://checkbus.test/ok", FailureUrl: "https://checkbus.test/fail", PendingUrl: "https://checkbus.test/pending");
        var created = await service.CreateOrderAsync(request, CancellationToken.None);

        var record = store.Get(created.OrderId);

        Assert.NotNull(record);
        Assert.Equal("payment-ref-2", record!.ExternalReference);
        Assert.Equal(2500m, record.TotalAmount.Amount);
        Assert.Equal("https://checkbus.test/ok", record.SuccessUrl);
        Assert.Equal("https://checkbus.test/fail", record.FailureUrl);
        Assert.Equal("https://checkbus.test/pending", record.PendingUrl);
    }

    [Fact]
    public async Task RefundAsync_SetsStoredStatusToRefunded()
    {
        var service = CreateService();
        var request = new CreateOrderRequest(new Money(1500m), "payment-ref-1");
        var created = await service.CreateOrderAsync(request, CancellationToken.None);

        await service.RefundAsync(created.OrderId, amount: null, CancellationToken.None);

        var result = await service.GetOrderAsync(created.OrderId, CancellationToken.None);
        Assert.Equal(MercadoPagoOrderStatus.Refunded, result.Status);
    }

    [Fact]
    public async Task RefundAsync_PartialAmount_StillSetsStatusToRefunded_NoRealMoneyMotionModeled()
    {
        var service = CreateService();
        var request = new CreateOrderRequest(new Money(1500m), "payment-ref-1");
        var created = await service.CreateOrderAsync(request, CancellationToken.None);

        await service.RefundAsync(created.OrderId, new Money(500m), CancellationToken.None);

        var result = await service.GetOrderAsync(created.OrderId, CancellationToken.None);
        Assert.Equal(MercadoPagoOrderStatus.Refunded, result.Status);
    }
}
