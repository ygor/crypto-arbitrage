using CryptoArbitrage.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CryptoArbitrage.Tests.TestInfrastructure;

/// <summary>
/// Custom web application factory for integration testing.
/// TODO: Update to work with the new vertical slice architecture.
/// </summary>
public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    // Mock services - these need to be updated for the new architecture
    public Mock<IExchangeFactory> MockExchangeFactory { get; }
    public Mock<IConfigurationService> MockConfigurationService { get; }
    public Mock<IPaperTradingService> MockPaperTradingService { get; }

    public CustomWebApplicationFactory()
    {
        MockExchangeFactory = new Mock<IExchangeFactory>();
        MockConfigurationService = new Mock<IConfigurationService>();
        MockPaperTradingService = new Mock<IPaperTradingService>();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // TODO: Update service registration for vertical slice architecture
            // For now, just register the available mock services
            services.AddSingleton(MockExchangeFactory.Object);
            services.AddSingleton(MockConfigurationService.Object);
            services.AddSingleton(MockPaperTradingService.Object);
            
            // MediatR should already be registered by the application
        });
    }
} 