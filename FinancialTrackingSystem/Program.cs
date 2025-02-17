using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;  
using FinancialTrackingSystem.Services;
using FinancialTrackingSystem.Interfaces;  
using Serilog;  
using Azure.Messaging.ServiceBus;  

namespace FinancialTrackingSystem
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Set up Serilog with file logging directed to "Logs" folder
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console() 
                .WriteTo.File("Logs/log.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)  
                .CreateLogger();

            var builder = WebApplication.CreateBuilder(args);

            // Bind configuration to services
            var connectionString = builder.Configuration.GetValue<string>("AzureServiceBus:ConnectionString");
            var queueName = builder.Configuration.GetValue<string>("AzureServiceBus:QueueName");

            // Register services in the container
            builder.Services.AddHttpClient<RatesService>(); // Register HttpClient for RatesService
            builder.Services.AddScoped<RatesService>(); // Register RatesService as a scoped service
            builder.Services.AddScoped<PositionsService>(); // Register PositionsService as a scoped service
            builder.Services.AddControllers(); // Register controllers

            // Register Swagger for API documentation
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Register IEventPublisher to use AzureServiceBusEventPublisher implementation with the connection string and queue name
            builder.Services.AddScoped<IEventPublisher>(provider =>
            {
                return new AzureServiceBusEventPublisher(connectionString, queueName);
            });

            // Use Serilog for logging
            builder.Host.UseSerilog();

            var app = builder.Build();

            // Configure Swagger for development environment
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();

            app.MapControllers(); // Map controller routes

            app.Run(); // Start the web app
        }
    }
}

