using Azure.Messaging.ServiceBus;
using FinancialTrackingSystem.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CsvHelper;  // Add this namespace for CSV parsing
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;

public class PositionsService
{
    private readonly ILogger<PositionsService> _logger;
    private readonly ServiceBusProcessor _processor;
    private readonly Queue<Position> _positionsQueue; // In-memory queue for positions

    public PositionsService(IConfiguration configuration, ILogger<PositionsService> logger)
    {
        _logger = logger;
        string connectionString = configuration["AzureServiceBus:ConnectionString"];
        string topicName = configuration["AzureServiceBus:TopicName"];
        string subscriptionName = configuration["AzureServiceBus:SubscriptionName"];

        var client = new ServiceBusClient(connectionString);
        _processor = client.CreateProcessor(topicName, subscriptionName, new ServiceBusProcessorOptions());

        _positionsQueue = new Queue<Position>();  // Initialize in-memory queue
    }

    public async Task StartListeningAsync()
    {
        _processor.ProcessMessageAsync += MessageHandler;
        _processor.ProcessErrorAsync += ErrorHandler;

        await _processor.StartProcessingAsync();
        _logger.LogInformation("PositionsService started listening for RateChangeEvent.");
    }

    private async Task MessageHandler(ProcessMessageEventArgs args)
    {
        string messageBody = args.Message.Body.ToString();
        _logger.LogInformation($"Received RateChangeEvent: {messageBody}");

        try
        {
            var rateChangeEvent = JsonSerializer.Deserialize<RateChangeEvent>(messageBody);
            if (rateChangeEvent != null)
            {
                await UpdatePositionsAsync(rateChangeEvent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error processing RateChangeEvent: {ex.Message}");
        }

        await args.CompleteMessageAsync(args.Message);
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        _logger.LogError($"Error in Service Bus Subscription: {args.Exception.Message}");
        return Task.CompletedTask;
    }

    public async Task StopListeningAsync()
    {
        await _processor.StopProcessingAsync();
        _logger.LogInformation("PositionsService stopped listening.");
    }

    public async Task UpdatePositionsAsync(RateChangeEvent rateChangeEvent)
    {
        _logger.LogInformation($"Updating positions for {rateChangeEvent.Symbol}: {rateChangeEvent.OldRate} → {rateChangeEvent.NewRate}");

        // Update all positions in the in-memory queue based on the rate change event
        foreach (var position in _positionsQueue)
        {
            if (position.Symbol == rateChangeEvent.Symbol)
            {
                position.UpdatePosition(rateChangeEvent.NewRate);
                _logger.LogInformation($"Position {position.InstrumentId} updated to new rate: {rateChangeEvent.NewRate}");
            }
        }

        // Optionally, persist updated positions to a database or external storage if required
        // await SaveUpdatedPositionsAsync();
    }

    // Method to load positions from a CSV file
    public async Task LoadCsv(string filePath)
    {
        try
        {
            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var records = csv.GetRecords<PositionCsvRecord>().ToList(); // Assuming PositionCsvRecord represents a row in your CSV file

                foreach (var record in records)
                {
                    // Assuming PositionCsvRecord maps correctly to Position class
                    var position = new Position(
                        record.InstrumentId,
                        record.Symbol,
                        record.Quantity,
                        record.InitialRate
                    );

                    _positionsQueue.Enqueue(position); // Add position to queue
                    _logger.LogInformation($"Position {record.InstrumentId} loaded from CSV.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error loading CSV: {ex.Message}");
        }
    }

    // Method to get positions from the queue
    public async Task<IEnumerable<Position>> GetPositionsFromQueue()
    {
        // Return positions from the in-memory queue
        return _positionsQueue.AsEnumerable();
    }

    // Optional method to add a position to the queue
    public void AddPosition(Position position)
    {
        _positionsQueue.Enqueue(position);
    }

    // Optional method to fetch all positions
    public IEnumerable<Position> GetAllPositions()
    {
        return _positionsQueue;
    }

    // Optionally, method to save updated positions to external storage (e.g., a database or file)
    private Task SaveUpdatedPositionsAsync()
    {
        // Implement saving logic here if needed
        return Task.CompletedTask;
    }
}

// Class representing a row in the CSV file
public class PositionCsvRecord
{
    public string InstrumentId { get; set; }
    public string Symbol { get; set; }
    public decimal Quantity { get; set; }
    public decimal InitialRate { get; set; }
}

// Position class for completeness
public class Position
{
    public string InstrumentId { get; set; }
    public string Symbol { get; set; }
    public decimal Quantity { get; set; }
    public decimal InitialRate { get; set; }
    public decimal CurrentRate { get; set; }
    public decimal? TotalValue => Quantity * CurrentRate;

    public Position(string instrumentId, string symbol, decimal quantity, decimal initialRate)
    {
        InstrumentId = instrumentId;
        Symbol = symbol;
        Quantity = quantity;
        InitialRate = initialRate;
        CurrentRate = initialRate; // Assuming the initial rate is the current rate when the position is created
    }

    // Method to update the position based on the new rate
    public void UpdatePosition(decimal newRate)
    {
        CurrentRate = newRate;
    }
}

