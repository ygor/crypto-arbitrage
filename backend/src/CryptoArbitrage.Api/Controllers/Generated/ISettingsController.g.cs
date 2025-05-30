//----------------------
// <auto-generated>
//     Generated using controller method definitions
// </auto-generated>
//----------------------

#pragma warning disable 108 // Disable "CS0108 '{derivedDto}.ToJson()' hides inherited member '{dtoBase}.ToJson()'. Use the new keyword if hiding was intended."
#pragma warning disable 114 // Disable "CS0114 '{derivedDto}.RaisePropertyChanged(String)' hides inherited member 'dtoBase.RaisePropertyChanged(String)'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword."
#pragma warning disable 472 // Disable "CS0472 The result of the expression is always 'false' since a value of type 'Int32' is never equal to 'null' of type 'Int32?'"
#pragma warning disable 612 // Disable "CS0612 '...' is obsolete"
#pragma warning disable 1573 // Disable "CS1573 Parameter '...' has no matching param tag in the XML comment for ...
#pragma warning disable 1591 // Disable "CS1591 Missing XML comment for publicly visible type or member ..."
#pragma warning disable 8073 // Disable "CS8073 The result of the expression is always 'false' since a value of type 'T' is never equal to 'null' of type 'T?'"
#pragma warning disable 3016 // Disable "CS3016 Arrays as attribute arguments is not CLS-compliant"
#pragma warning disable 8603 // Disable "CS8603 Possible null reference return"
#pragma warning disable 8604 // Disable "CS8604 Possible null reference argument for parameter"

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ApiModels = CryptoArbitrage.Api.Models;

namespace CryptoArbitrage.Api.Controllers.Interfaces
{
    /// <summary>
    /// Interface for the Settings controller
    /// </summary>
    public interface ISettingsController
    {
        /// <summary>
        /// Gets exchange configurations.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of exchange configurations.</returns>
        Task<ICollection<ApiModels.ExchangeConfiguration>> GetExchangeConfigurationsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves exchange configurations.
        /// </summary>
        /// <param name="exchangeConfigs">The exchange configurations to save.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A save response indicating success or failure.</returns>
        Task<ApiModels.SaveResponse> SaveExchangeConfigurationsAsync([FromBody] ICollection<ApiModels.ExchangeConfiguration> exchangeConfigs, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the arbitrage configuration.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The arbitrage configuration.</returns>
        Task<ApiModels.ArbitrageConfiguration> GetArbitrageConfigurationAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves the arbitrage configuration.
        /// </summary>
        /// <param name="arbitrageConfig">The arbitrage configuration to save.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A save response indicating success or failure.</returns>
        Task<ApiModels.SaveResponse> SaveArbitrageConfigurationAsync([FromBody] ApiModels.ArbitrageConfiguration arbitrageConfig, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the risk profile.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The risk profile.</returns>
        Task<ApiModels.RiskProfileData> GetRiskProfileAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves the risk profile.
        /// </summary>
        /// <param name="riskProfile">The risk profile to save.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A save response indicating success or failure.</returns>
        Task<ApiModels.SaveResponse> SaveRiskProfileAsync([FromBody] ApiModels.RiskProfileData riskProfile, CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts the arbitrage bot.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A bot response indicating success or failure.</returns>
        Task<ApiModels.BotResponse> StartBotAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops the arbitrage bot.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A bot response indicating success or failure.</returns>
        Task<ApiModels.BotResponse> StopBotAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the bot status.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The bot status.</returns>
        Task<ApiModels.BotStatus> GetBotStatusAsync(CancellationToken cancellationToken = default);
    }
}
