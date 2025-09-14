using FluentValidation;
using CryptoArbitrage.Application.Interfaces;

namespace CryptoArbitrage.Application.Features.Arbitrage.Commands.ExecuteArbitrageOpportunity;

/// <summary>
/// Validator for arbitrage opportunity execution commands.
/// </summary>
public class ExecuteArbitrageOpportunityValidator : AbstractValidator<ExecuteArbitrageOpportunityCommand>
{
    private readonly IExchangeFactory _exchangeFactory;

    public ExecuteArbitrageOpportunityValidator(IExchangeFactory exchangeFactory)
    {
        _exchangeFactory = exchangeFactory ?? throw new ArgumentNullException(nameof(exchangeFactory));

        RuleFor(x => x.TradingPair)
            .NotNull()
            .WithMessage("Trading pair is required");

        RuleFor(x => x.TradingPair.BaseCurrency)
            .NotEmpty()
            .WithMessage("Base currency must be specified");

        RuleFor(x => x.TradingPair.QuoteCurrency)
            .NotEmpty()
            .WithMessage("Quote currency must be specified");

        RuleFor(x => x.BuyExchangeId)
            .NotEmpty()
            .WithMessage("Buy exchange ID is required")
            .Must(BeValidExchange)
            .WithMessage("Buy exchange is not supported");

        RuleFor(x => x.SellExchangeId)
            .NotEmpty()
            .WithMessage("Sell exchange ID is required")
            .Must(BeValidExchange)
            .WithMessage("Sell exchange is not supported");

        RuleFor(x => x)
            .Must(x => x.BuyExchangeId != x.SellExchangeId)
            .WithMessage("Buy and sell exchanges must be different");

        RuleFor(x => x.MaxTradeAmount)
            .GreaterThan(0)
            .WithMessage("Max trade amount must be greater than zero")
            .LessThanOrEqualTo(1000000)
            .WithMessage("Max trade amount is too high (safety limit: 1,000,000)");

        RuleFor(x => x.MinProfitPercentage)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Minimum profit percentage cannot be negative")
            .LessThanOrEqualTo(50)
            .WithMessage("Minimum profit percentage is unrealistic (max 50%)");

        RuleFor(x => x.TimeoutMs)
            .GreaterThan(0)
            .When(x => x.TimeoutMs > 0)
            .WithMessage("Timeout must be positive if specified")
            .LessThanOrEqualTo(300000)
            .When(x => x.TimeoutMs > 0)
            .WithMessage("Timeout too long (max 5 minutes)");

        // Risk validation for auto-execution
        When(x => x.AutoExecute, () =>
        {
            RuleFor(x => x.MaxTradeAmount)
                .LessThanOrEqualTo(10000)
                .WithMessage("Max trade amount for auto-execution is limited to 10,000 for safety");

            RuleFor(x => x.MinProfitPercentage)
                .GreaterThanOrEqualTo(0.1m)
                .WithMessage("Minimum profit percentage for auto-execution should be at least 0.1%");
        });
    }

    private bool BeValidExchange(string exchangeId)
    {
        if (string.IsNullOrEmpty(exchangeId))
            return false;

        var supportedExchanges = _exchangeFactory.GetSupportedExchanges();
        return supportedExchanges.Contains(exchangeId, StringComparer.OrdinalIgnoreCase);
    }
} 