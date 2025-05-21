using System;

namespace CryptoArbitrage.Domain.Models.Events;

/// <summary>
/// Event arguments for error events.
/// </summary>
public class ErrorEventArgs : EventArgs
{
    /// <summary>
    /// Gets the error code associated with this error.
    /// </summary>
    public ErrorCode ErrorCode { get; }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the exception associated with this error, if any.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorEventArgs"/> class.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="exception">The exception, if any.</param>
    public ErrorEventArgs(ErrorCode errorCode, string message, Exception? exception = null)
    {
        ErrorCode = errorCode;
        Message = message ?? string.Empty;
        Exception = exception;
    }
} 