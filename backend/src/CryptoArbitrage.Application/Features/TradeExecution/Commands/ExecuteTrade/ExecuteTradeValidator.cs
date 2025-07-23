using FluentValidation;
using CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Application.Features.TradeExecution.Commands.ExecuteTrade;

/// <summary>
/// Validator for ExecuteTradeCommand.
/// </summary>
public class ExecuteTradeValidator : AbstractValidator<ExecuteTradeCommand>
{
    public ExecuteTradeValidator()
    {
        RuleFor(x => x.Opportunity)
            .NotNull()
            .WithMessage("Opportunity is required")
            .Must(BeValidOpportunity)
            .WithMessage("Opportunity must be in a valid state for execution");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .When(x => x.Quantity.HasValue)
            .WithMessage("Quantity must be greater than zero when specified");

        RuleFor(x => x.TimeoutMs)
            .GreaterThan(0)
            .LessThanOrEqualTo(300000) // 5 minutes max
            .When(x => x.TimeoutMs.HasValue)
            .WithMessage("Timeout must be between 1ms and 5 minutes");

        RuleFor(x => x)
            .Must(HaveValidQuantity)
            .WithMessage("Specified quantity exceeds opportunity's effective quantity")
            .When(x => x.Opportunity != null && x.Quantity.HasValue);

        RuleFor(x => x.Opportunity.TradingPair)
            .Must(BeValidTradingPair)
            .When(x => x.Opportunity != null)
            .WithMessage("Trading pair must be valid");

        RuleFor(x => x.Opportunity.BuyExchangeId)
            .NotEmpty()
            .When(x => x.Opportunity != null)
            .WithMessage("Buy exchange ID is required");

        RuleFor(x => x.Opportunity.SellExchangeId)
            .NotEmpty()
            .When(x => x.Opportunity != null)
            .WithMessage("Sell exchange ID is required");

        RuleFor(x => x.Opportunity.BuyPrice)
            .GreaterThan(0)
            .When(x => x.Opportunity != null)
            .WithMessage("Buy price must be greater than zero");

        RuleFor(x => x.Opportunity.SellPrice)
            .GreaterThan(0)
            .When(x => x.Opportunity != null)
            .WithMessage("Sell price must be greater than zero");

        RuleFor(x => x.Opportunity.EffectiveQuantity)
            .GreaterThan(0)
            .When(x => x.Opportunity != null)
            .WithMessage("Effective quantity must be greater than zero");
    }

    private static bool BeValidOpportunity(ArbitrageOpportunity opportunity)
    {
        if (opportunity == null) return false;

        // Check opportunity status
        if (opportunity.Status != ArbitrageOpportunityStatus.Detected)
        {
            return false;
        }

        // Check if opportunity is still valid (not too old)
        var maxAge = TimeSpan.FromMinutes(5);
        if (DateTime.UtcNow - opportunity.DetectedAt > maxAge)
        {
            return false;
        }

        // Check if profit is still positive
        if (opportunity.ProfitPercentage <= 0)
        {
            return false;
        }

        return true;
    }

    private static bool HaveValidQuantity(ExecuteTradeCommand command)
    {
        if (!command.Quantity.HasValue || command.Opportunity == null)
            return true;

        return command.Quantity.Value <= command.Opportunity.EffectiveQuantity;
    }

    private static bool BeValidTradingPair(TradingPair tradingPair)
    {
        return !string.IsNullOrWhiteSpace(tradingPair.BaseCurrency) &&
               !string.IsNullOrWhiteSpace(tradingPair.QuoteCurrency) &&
               tradingPair.BaseCurrency != tradingPair.QuoteCurrency;
    }
} 