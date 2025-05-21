namespace CryptoArbitrage.Api.Models
{
    /// <summary>
    /// Represents a response to a save operation.
    /// </summary>
    public class SaveResponse
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
        /// Creates a successful response.
        /// </summary>
        /// <param name="message">Optional success message.</param>
        /// <returns>A successful response.</returns>
        public static SaveResponse Success(string message = "Settings saved successfully")
        {
            return new SaveResponse
            {
                success = true,
                message = message
            };
        }

        /// <summary>
        /// Creates a failure response.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>A failure response.</returns>
        public static SaveResponse Failure(string errorMessage)
        {
            return new SaveResponse
            {
                success = false,
                message = errorMessage
            };
        }
    }
} 