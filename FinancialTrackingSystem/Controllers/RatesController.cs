using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FinancialTrackingSystem.Services;
using System.Threading.Tasks;
using System;
using System.Linq;
using FinancialTrackingSystem.Events;
using FinancialTrackingSystem.Interfaces;

namespace FinancialTrackingSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RatesController : ControllerBase
    {
        private readonly RatesService _ratesService;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<RatesController> _logger;

   
        public RatesController(RatesService ratesService, IEventPublisher eventPublisher, ILogger<RatesController> logger)
        {
            _ratesService = ratesService;
            _eventPublisher = eventPublisher;
            _logger = logger;  // Inject the logger
        }

        // Endpoint to trigger the rate fetching
        [HttpGet("test")]
        public async Task<IActionResult> GetCryptoRates()
        {
            _logger.LogInformation("Request to fetch cryptocurrency rates received.");

            try
            {
                // Call the RatesService to fetch the latest rates
                var rates = await _ratesService.GetRatesAsync();
                _logger.LogInformation("Successfully fetched cryptocurrency rates.");

                // Check for significant rate changes and publish events
                foreach (var rate in rates)
                {
                    if (Math.Abs(rate.PercentChange24h) > 5)
                    {
                        _logger.LogInformation($"Significant rate change detected for {rate.Symbol}: {rate.PercentChange24h}%");

                        // Notify PositionsService of the rate change
                        var rateChangeEvent = new RateChangeEvent
                        {
                            Symbol = rate.Symbol,
                            OldRate = rate.Rate,
                            NewRate = rate.Rate
                        };

                        // Publish event to Service Bus
                        await _eventPublisher.PublishEventAsync("RateChangeEvent", rateChangeEvent);
                    }
                }

                return Ok(rates); // Return the rates to the client
            }
            catch (Exception ex)
            {
                // Log the error and return a server error
                _logger.LogError(ex, "An error occurred while fetching cryptocurrency rates.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
