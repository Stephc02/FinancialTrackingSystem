using Azure.Messaging.ServiceBus;
using FinancialTrackingSystem.Interfaces;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FinancialTrackingSystem.Services
{
    public class AzureServiceBusEventPublisher : IEventPublisher
    {
        private readonly string _connectionString;
        private readonly string _queueName;

        public AzureServiceBusEventPublisher(string connectionString, string queueName)
        {
            _connectionString = connectionString;
            _queueName = queueName;
        }

        public async Task PublishEventAsync(string eventName, object eventData)
        {
            // Create a ServiceBusClient
            var client = new ServiceBusClient(_connectionString);
            var sender = client.CreateSender(_queueName);

            // Prepare the message
            var message = new ServiceBusMessage(JsonSerializer.Serialize(eventData))
            {
                ApplicationProperties =
                {
                    { "EventName", eventName }
                }
            };

            // Send the message to the Azure Service Bus queue
            await sender.SendMessageAsync(message);

            await sender.CloseAsync();
            await client.DisposeAsync();
        }
    }
}

