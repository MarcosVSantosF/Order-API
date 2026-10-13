using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using OrderApi.Controllers;
using OrderApi.Data;
using OrderApi.DTOs;
using OrderApi.Enum;
using OrderApi.Model;
using OrderApi.Services;
using Xunit;

namespace OrderApi.Tests.Controllers;

public class OrderControllerTests : IDisposable
{
    private readonly Mock<IServiceBusService> _serviceBusMock;
    private readonly AppDbContext _context;
    private readonly OrderController _controller;

    public OrderControllerTests()
    {
        // Configura o banco em memória (isolado por teste)
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _serviceBusMock = new Mock<IServiceBusService>();

        _controller = new OrderController(_context, _serviceBusMock.Object);
    }

    // -------------------------------------------------
    // POST /Order
    // -------------------------------------------------

    [Fact]
    public async Task Criar_DeveRetornarCreated_EPublicarEvento()
    {
        // Arrange
        var dto = new CreateOrderDto
        {
            Cliente = "João Silva",
            Produto = "Notebook",
            Valor = 3500.00m
        };

        // Act
        var result = await _controller.Criar(dto);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);

        var order = createdResult.Value.Should().BeOfType<Order>().Subject;
        order.Cliente.Should().Be("João Silva");
        order.Produto.Should().Be("Notebook");
        order.Valor.Should().Be(3500.00m);
        order.Status.Should().Be(OrderStatus.Pendente);
        order.Id.Should().BeGreaterThan(0);

        // Verifica se publicou no Service Bus
        _serviceBusMock.Verify(
            x => x.PublicarPedidoAsync(order.Id),
            Times.Once);
    }

    [Fact]
    public async Task Criar_DeveSalvarNoBanco()
    {
        // Arrange
        var dto = new CreateOrderDto
        {
            Cliente = "Maria",
            Produto = "Mouse",
            Valor = 89.90m
        };

        // Act
        await _controller.Criar(dto);

        // Assert
        var orderNoBanco = await _context.Orders.FirstOrDefaultAsync();
        orderNoBanco.Should().NotBeNull();
        orderNoBanco!.Cliente.Should().Be("Maria");
        orderNoBanco.Status.Should().Be(OrderStatus.Pendente);
    }

    // -------------------------------------------------
    // GET /Order/{id}
    // -------------------------------------------------

    [Fact]
    public async Task BuscarPorId_QuandoExiste_DeveRetornarOk()
    {
        // Arrange
        var order = new Order
        {
            Cliente = "Carlos",
            Produto = "Teclado",
            Valor = 250m,
            Status = OrderStatus.Pendente,
            DataCriacao = DateTime.UtcNow
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.BuscarPorId(order.Id);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedOrder = okResult.Value.Should().BeOfType<Order>().Subject;

        returnedOrder.Id.Should().Be(order.Id);
        returnedOrder.Cliente.Should().Be("Carlos");
    }

    [Fact]
    public async Task BuscarPorId_QuandoNaoExiste_DeveRetornarNotFound()
    {
        // Act
        var result = await _controller.BuscarPorId(999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    // -------------------------------------------------
    // GET /Order
    // -------------------------------------------------

    [Fact]
    public async Task BuscarTodos_SemFiltro_DeveRetornarTodosOsPedidos()
    {
        // Arrange
        _context.Orders.AddRange(
            new Order { Cliente = "A", Produto = "P1", Valor = 10, Status = OrderStatus.Pendente },
            new Order { Cliente = "B", Produto = "P2", Valor = 20, Status = OrderStatus.Finalizado }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.BuscarTodos(null);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var orders = okResult.Value.Should().BeAssignableTo<List<Order>>().Subject;

        orders.Should().HaveCount(2);
    }

    [Fact]
    public async Task BuscarTodos_ComFiltroValido_DeveRetornarApenasStatusCorrespondente()
    {
        // Arrange
        _context.Orders.AddRange(
            new Order { Cliente = "A", Produto = "P1", Valor = 10, Status = OrderStatus.Pendente },
            new Order { Cliente = "B", Produto = "P2", Valor = 20, Status = OrderStatus.Processando },
            new Order { Cliente = "C", Produto = "P3", Valor = 30, Status = OrderStatus.Finalizado }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.BuscarTodos("Pendente");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var orders = okResult.Value.Should().BeAssignableTo<List<Order>>().Subject;

        orders.Should().HaveCount(1);
        orders[0].Status.Should().Be(OrderStatus.Pendente);
    }

    [Fact]
    public async Task BuscarTodos_ComStatusInvalido_DeveRetornarBadRequest()
    {
        // Act
        var result = await _controller.BuscarTodos("StatusQueNaoExiste");

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().BeOfType<string>()
            .Which.Should().Contain("Status inválido");
    }

    // -------------------------------------------------
    // Cleanup
    // -------------------------------------------------

    public void Dispose()
    {
        _context?.Dispose();
    }
}