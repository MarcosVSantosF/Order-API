using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using OrderApi.Model;
using OrderApi.DTOs;
using OrderApi.Enum;
using OrderApi.Data;
using OrderApi.Services;



namespace OrderApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrderController : Controller
    {

        private readonly AppDbContext _context;
        private readonly IServiceBusService _serviceBusService;

        public OrderController(
            AppDbContext context,
            IServiceBusService serviceBusService)
        {
            _context = context;
            _serviceBusService = serviceBusService;
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> BuscarPorId(int id)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            return Ok(order);
        }

        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] CreateOrderDto dto)
        {
            var order = new Order
            {
                Cliente = dto.Cliente,
                Produto = dto.Produto,
                Valor = dto.Valor,
                Status = OrderStatus.Pendente,
                DataCriacao = DateTime.UtcNow
            };

            _context.Orders.Add(order);

            await _context.SaveChangesAsync();
            await _serviceBusService.PublicarPedidoAsync(order.Id);

            return CreatedAtAction(
                nameof(BuscarPorId),
                new { id = order.Id },
                order
            );
        }

        [HttpGet]
        public async Task<IActionResult> BuscarTodos([FromQuery] string? status)
        {
            var query = _context.Orders.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                if (!System.Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var statusEnum))
                {
                    return BadRequest($"Status inválido. Valores aceitos: {string.Join(", ", System.Enum.GetNames<OrderStatus>())}");
                }

                query = query.Where(o => o.Status == statusEnum);
            }

            var orders = await query.ToListAsync();

            return Ok(orders);
        }
        

    }
}