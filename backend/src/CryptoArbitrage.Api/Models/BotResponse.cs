namespace CryptoArbitrage.Api.Models
{
    /// <summary>
    /// Represents a response from the trading bot operations.
    /// </summary>
    public class BotResponse
    {
        /// <summary>
        /// Gets or sets a value indicating whether the operation was successful.
        /// </summary>
        public bool success { get; set; }

        /// <summary>
        /// Gets or sets the message describing the result of the operation.
        /// </summary>
        public required string message { get; set; }

        /// <summary>
        /// Gets or sets additional data related to the operation.
        /// </summary>
        public object? data { get; set; }

        /// <summary>
        /// Creates a successful response.
        /// </summary>
        /// <param name="message">Optional success message.</param>
        /// <param name="data">Optional data to include in the response.</param>
        /// <returns>A successful response.</returns>
        public static BotResponse Success(string message = "Operation completed successfully", object? data = null)
        {
            return new BotResponse
            {
                success = true,
                message = message,
                data = data
            };
        }

        /// <summary>
        /// Creates a failure response.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="data">Optional data to include in the response.</param>
        /// <returns>A failure response.</returns>
        public static BotResponse Failure(string errorMessage, object? data = null)
        {
            return new BotResponse
            {
                success = false,
                message = errorMessage,
                data = data
            };
        }
    }
} 