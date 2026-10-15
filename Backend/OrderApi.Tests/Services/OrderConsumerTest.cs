using Azure.Messaging.ServiceBus;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using OrderApi.Data;
using OrderApi.Enum;
using OrderApi.Model;
using OrderApi.Services;
using Xunit;
using System;

namespace OrderApi.Tests.Services;

public class OrderConsumerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<ILogger<OrderConsumer>> _loggerMock;
    private readonly OrderConsumer _consumer;

    public OrderConsumerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);

        // Mock do IServiceScopeFactory → retorna o mesmo DbContext em memória
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(x => x.GetService(typeof(AppDbContext)))
            .Returns(_context);

        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);

        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);

        _loggerMock = new Mock<ILogger<OrderConsumer>>();

        // Configuração fake do Service Bus (não conecta de verdade)
        var configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(c => c["ServiceBus:ConnectionString"])
            .Returns("Endpoint=sb://fake;SharedAccessKeyName=fake;SharedAccessKey=fake;");
        configurationMock.Setup(c => c["ServiceBus:QueueName"])
            .Returns("orders");

        _consumer = new OrderConsumer(configurationMock.Object, _scopeFactoryMock.Object, _loggerMock.Object);
    }

    private static ServiceBusReceivedMessage CriarMensagem(int orderId, string eventType = "OrderCreated")
    {
        var properties = new Dictionary<string, object>
        {
            ["EventType"] = eventType
        };

        return ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString($"{{\"orderId\":{orderId}}}"),
            correlationId: orderId.ToString(),
            properties: properties
        );
    }

    [Fact]
    public async Task ProcessarMensagem_QuandoPedidoPendente_DeveFinalizar()
    {
        // Arrange
        var order = new Order
        {
            Cliente = "João",
            Produto = "Notebook",
            Valor = 3500m,
            Status = OrderStatus.Pendente
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var message = CriarMensagem(order.Id);

        // Act
        await _consumer.ProcessarMensagemAsync(message);

        // Assert
        var orderAtualizado = await _context.Orders.FindAsync(order.Id);
        orderAtualizado.Should().NotBeNull();
        orderAtualizado!.Status.Should().Be(OrderStatus.Finalizado);
    }

    [Fact]
    public async Task ProcessarMensagem_QuandoPedidoJaProcessando_DeveIgnorarPorIdempotencia()
    {
        // Arrange
        var order = new Order
        {
            Cliente = "Maria",
            Produto = "Mouse",
            Valor = 90m,
            Status = OrderStatus.Processando
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var message = CriarMensagem(order.Id);

        // Act
        await _consumer.ProcessarMensagemAsync(message);

        // Assert
        var orderAtualizado = await _context.Orders.FindAsync(order.Id);
        orderAtualizado!.Status.Should().Be(OrderStatus.Processando); // não mudou
    }

    [Fact]
    public async Task ProcessarMensagem_QuandoPedidoJaFinalizado_DeveIgnorarPorIdempotencia()
    {
        // Arrange
        var order = new Order
        {
            Cliente = "Carlos",
            Produto = "Teclado",
            Valor = 250m,
            Status = OrderStatus.Finalizado
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var message = CriarMensagem(order.Id);

        // Act
        await _consumer.ProcessarMensagemAsync(message);

        // Assert
        var orderAtualizado = await _context.Orders.FindAsync(order.Id);
        orderAtualizado!.Status.Should().Be(OrderStatus.Finalizado);
    }

    [Fact]
    public async Task ProcessarMensagem_QuandoPedidoNaoExiste_NaoDeveLancarExcecao()
    {
        // Arrange
        var message = CriarMensagem(999);

        // Act
        var act = async () => await _consumer.ProcessarMensagemAsync(message);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ProcessarMensagem_QuandoEventTypeInvalido_DeveIgnorar()
    {
        // Arrange
        var order = new Order
        {
            Cliente = "Ana",
            Produto = "Monitor",
            Valor = 1200m,
            Status = OrderStatus.Pendente
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var message = CriarMensagem(order.Id, eventType: "OutroEvento");

        // Act
        await _consumer.ProcessarMensagemAsync(message);

        // Assert
        var orderAtualizado = await _context.Orders.FindAsync(order.Id);
        orderAtualizado!.Status.Should().Be(OrderStatus.Pendente); // não processou
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}