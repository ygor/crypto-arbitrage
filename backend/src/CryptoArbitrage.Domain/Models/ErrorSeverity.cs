namespace CryptoArbitrage.Domain.Models;

/// <summary>
/// Enum representing the severity level of system errors.
/// </summary>
public enum ErrorSeverity
{
    /// <summary>
    /// Informational message, not an actual error.
    /// </summary>
    Information = 0,
    
    /// <summary>
    /// Low severity warning that doesn't affect operations.
    /// </summary>
    Warning = 1,
    
    /// <summary>
    /// Low severity error, informational only.
    /// </summary>
    Low = 2,
    
    /// <summary>
    /// Medium severity error that requires attention but doesn't impact critical functions.
    /// </summary>
    Medium = 3,
    
    /// <summary>
    /// High severity error that requires immediate attention.
    /// </summary>
    High = 4,
    
    /// <summary>
    /// Serious error that may affect multiple operations or compromise system stability.
    /// </summary>
    Severe = 5,
    
    /// <summary>
    /// Critical error that prevents the system from functioning properly.
    /// </summary>
    Critical = 6
} 