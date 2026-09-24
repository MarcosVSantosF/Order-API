using Azure.Messaging.ServiceBus;

namespace OrderApi.Services;

public class ServiceBusService : IServiceBusService
{
    private readonly ServiceBusClient _client;
    private readonly string _queueName;

    public ServiceBusService(IConfiguration configuration)
    {
        var connectionString = configuration["ServiceBus:ConnectionString"];

        _queueName = configuration["ServiceBus:QueueName"]
            ?? throw new InvalidOperationException(
                "ServiceBus:QueueName não configurado.");

        _client = new ServiceBusClient(connectionString);
    }

    public async Task PublicarPedidoAsync(int orderId)
    {
        await using var sender = _client.CreateSender(_queueName);

        var message = new ServiceBusMessage($"{{\"orderId\":{orderId}}}")
        {
            CorrelationId = orderId.ToString()
        };

        message.ApplicationProperties["EventType"] = "OrderCreated";

        await sender.SendMessageAsync(message);
    }
}