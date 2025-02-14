using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;
using FinancialTrackingSystem.Interfaces;

namespace FinancialTrackingSystem.Services
{
    public class CryptoRate
    {
        public string Symbol { get; set; }
        public decimal Rate { get; set; }
        public decimal PercentChange24h { get; set; }
    }

    public class RatesService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<RatesService> _logger;
        private readonly IEventPublisher _eventPublisher;
        private const string ApiKey = "a0885c1c-588c-4e9c-9320-d8371f8ad01c";
        private const string Url = "https://pro-api.coinmarketcap.com/v1/cryptocurrency/listings/latest?convert=USD";

        public RatesService(HttpClient httpClient, ILogger<RatesService> logger, IEventPublisher eventPublisher)
        {
            _httpClient = httpClient;
            _logger = logger;
            _eventPublisher = eventPublisher;
        }

        public async Task<List<CryptoRate>> GetRatesAsync()
        {
            _logger.LogInformation("Fetching cryptocurrency rates...");

            var request = new HttpRequestMessage(HttpMethod.Get, Url);
            request.Headers.Add("X-CMC_PRO_API_KEY", ApiKey);
            request.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var ratesData = JsonConvert.DeserializeObject<dynamic>(jsonResponse);

            var rates = new List<CryptoRate>();
            foreach (var data in ratesData.data)
            {
                rates.Add(new CryptoRate
                {
                    Symbol = data.symbol,
                    Rate = data.quote.USD.price,
                    PercentChange24h = data.quote.USD.percent_change_24h
                });
            }

            return rates;
        }

        public async Task CheckForSignificantRateChanges()
        {
            var rates = await GetRatesAsync();

            foreach (var rate in rates)
            {
                if (Math.Abs(rate.PercentChange24h) > 5)
                {
                    _logger.LogInformation($"Significant rate change detected for {rate.Symbol}: {rate.PercentChange24h}%");

                    // Create event message with rate information
                    var rateChangeEvent = new
                    {
                        InstrumentId = rate.Symbol,
                        NewRate = rate.Rate,
                        PercentChange24h = rate.PercentChange24h
                    };

                    // Publish event to Azure Service Bus
                    await _eventPublisher.PublishEventAsync("RateChangeEvent", rateChangeEvent);
                }
            }
        }
    }
}

