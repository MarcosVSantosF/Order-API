using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderApi.Data;
using OrderApi.Enum;

namespace OrderApi.Services
{
    public class OrderConsumer : BackgroundService
    {
        private readonly ServiceBusClient _client;
        private readonly ServiceBusProcessor _processor;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OrderConsumer> _logger;

        public OrderConsumer(
            IConfiguration configuration,
            IServiceScopeFactory scopeFactory,
            ILogger<OrderConsumer> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;

            var connectionString = configuration["ServiceBus:ConnectionString"];
            var queueName = configuration["ServiceBus:QueueName"]
                ?? throw new InvalidOperationException("ServiceBus:QueueName não configurado.");

            _client = new ServiceBusClient(connectionString);
            _processor = _client.CreateProcessor(queueName, new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = 1,
                AutoCompleteMessages = false
            });

            _processor.ProcessMessageAsync += ProcessMessageHandler;
            _processor.ProcessErrorAsync += ProcessErrorHandler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _processor.StartProcessingAsync(stoppingToken);

            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Esperado durante o shutdown da aplicação
            }
        }

        private async Task ProcessMessageHandler(ProcessMessageEventArgs args)
        {
            var eventType = args.Message.ApplicationProperties.TryGetValue("EventType", out var et)
                ? et?.ToString()
                : null;

            if (eventType != "OrderCreated")
            {
                _logger.LogWarning("EventType desconhecido ({EventType}), ignorando mensagem.", eventType);
                await args.CompleteMessageAsync(args.Message);
                return;
            }

            if (!int.TryParse(args.Message.CorrelationId, out var orderId))
            {
                _logger.LogWarning("CorrelationId inválido: {CorrelationId}", args.Message.CorrelationId);
                await args.CompleteMessageAsync(args.Message);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var order = await db.Orders.FindAsync(new object[] { orderId }, args.CancellationToken);

            if (order is null)
            {
                _logger.LogWarning("Pedido {OrderId} não encontrado.", orderId);
                await args.CompleteMessageAsync(args.Message);
                return;
            }

            // Idempotência: só processa se ainda estiver Pendente.
            // Se a mensagem for entregue de novo (reprocessamento do Service Bus),
            // o pedido já vai estar em Processando/Finalizado e é ignorado.
            if (order.Status != OrderStatus.Pendente)
            {
                _logger.LogInformation(
                    "Pedido {OrderId} já está em {Status}, mensagem ignorada (idempotência).",
                    orderId, order.Status);
                await args.CompleteMessageAsync(args.Message);
                return;
            }

            order.Status = OrderStatus.Processando;
            await db.SaveChangesAsync(args.CancellationToken);
            _logger.LogInformation("Pedido {OrderId} -> Processando", orderId);

            // Simula o tempo de processamento do pedido (remova ou ajuste como quiser)
            await Task.Delay(TimeSpan.FromSeconds(5), args.CancellationToken);

            order.Status = OrderStatus.Finalizado;
            await db.SaveChangesAsync(args.CancellationToken);
            _logger.LogInformation("Pedido {OrderId} -> Finalizado", orderId);

            await args.CompleteMessageAsync(args.Message);
        }

        private Task ProcessErrorHandler(ProcessErrorEventArgs args)
        {
            _logger.LogError(args.Exception, "Erro ao processar mensagem do Service Bus.");
            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _processor.StopProcessingAsync(cancellationToken);
            await _processor.DisposeAsync();
            await _client.DisposeAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}