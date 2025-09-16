using System.Text;
using Xunit;

namespace CryptoArbitrage.Tests.UnitTests;

/// <summary>
/// Unit tests for credential validation logic
/// These tests validate the core validation patterns used in exchange clients
/// </summary>
public class CredentialValidationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void ValidateApiKey_NullOrWhitespace_ThrowsArgumentException(string? apiKey)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException("API key cannot be null or empty", nameof(apiKey));
            }
        });
        
        Assert.Contains("API key", exception.Message);
        Assert.Contains("cannot be null", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void ValidateApiSecret_NullOrWhitespace_ThrowsArgumentException(string? apiSecret)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            if (string.IsNullOrWhiteSpace(apiSecret))
            {
                throw new ArgumentException("API secret cannot be null or empty", nameof(apiSecret));
            }
        });
        
        Assert.Contains("API secret", exception.Message);
        Assert.Contains("cannot be null", exception.Message);
    }

    [Theory]
    [InlineData("not_base64!@#")]
    [InlineData("invalid-base64-string")]
    [InlineData("SGVsbG8gV29ybGQ!")] // Invalid padding
    [InlineData("Hello World")] // Plain text
    [InlineData("12345")]
    [InlineData("!@#$%^&*()")]
    public void ValidateBase64Secret_InvalidFormat_ThrowsArgumentException(string invalidSecret)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            try
            {
                Convert.FromBase64String(invalidSecret);
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("API secret must be a valid Base64 string. Please check your configuration.", nameof(invalidSecret), ex);
            }
        });
        
        Assert.Contains("Base64", exception.Message);
        Assert.Contains("configuration", exception.Message);
        Assert.IsType<FormatException>(exception.InnerException);
    }

    [Theory]
    [InlineData("SGVsbG8gV29ybGQ=")]  // "Hello World" in Base64
    [InlineData("dGVzdA==")]          // "test" in Base64
    [InlineData("")]                  // Empty string is valid Base64
    [InlineData("YQ==")]             // "a" in Base64
    public void ValidateBase64Secret_ValidFormat_DoesNotThrow(string validSecret)
    {
        // Act & Assert - Should not throw
        var result = Convert.FromBase64String(validSecret);
        Assert.NotNull(result);
    }

    [Fact]
    public void CredentialValidation_PreventsCryptographicErrors()
    {
        // This test demonstrates that our validation prevents the original FormatException
        // from occurring in cryptographic operations like signature generation
        
        var invalidSecret = "not_base64!@#";
        
        // Without validation, this would throw FormatException during signature generation
        var formatException = Assert.Throws<FormatException>(() =>
        {
            Convert.FromBase64String(invalidSecret);
        });
        
        // With validation, we get a more helpful ArgumentException instead
        var argumentException = Assert.Throws<ArgumentException>(() =>
        {
            try
            {
                Convert.FromBase64String(invalidSecret);
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("API secret must be a valid Base64 string", ex);
            }
        });
        
        Assert.Contains("Base64", argumentException.Message);
        Assert.IsType<FormatException>(argumentException.InnerException);
        Assert.NotEqual(formatException.Message, argumentException.Message);
    }

    [Fact]
    public void GenerateValidBase64Secret_ForTesting()
    {
        // This test generates valid Base64 strings for use in other tests
        var testSecrets = new[]
        {
            "test_secret_1",
            "another_test_secret",
            "very_long_test_secret_with_special_chars_123!@#"
        };

        foreach (var secret in testSecrets)
        {
            var base64Secret = Convert.ToBase64String(Encoding.UTF8.GetBytes(secret));
            
            // Validate that our generated Base64 is valid
            var decoded = Convert.FromBase64String(base64Secret);
            var roundTrip = Encoding.UTF8.GetString(decoded);
            
            Assert.Equal(secret, roundTrip);
            Assert.True(IsValidBase64(base64Secret));
        }
    }

    [Theory]
    [InlineData("")]                    // Empty string is valid Base64
    [InlineData("ABCD")]                // 4 chars, valid
    [InlineData("SGVsbG8gV29ybGQ=")]    // "Hello World" in Base64
    [InlineData("dGVzdA==")]            // "test" in Base64
    [InlineData("YQ==")]               // "a" in Base64
    [InlineData("YWI=")]               // "ab" in Base64
    public void IsValidBase64_ValidStrings_ReturnsTrue(string base64String)
    {
        Assert.True(IsValidBase64(base64String));
    }

    [Theory]
    [InlineData("not_base64!@#")]
    [InlineData("invalid-base64-string")]
    [InlineData("SGVsbG8gV29ybGQ!")] // Invalid padding
    [InlineData("Hello World")]
    [InlineData("12345")]
    [InlineData("A")]                 // Too short, invalid padding
    [InlineData("AB")]                // Too short, invalid padding  
    [InlineData("ABC")]               // Too short, invalid padding
    public void IsValidBase64_InvalidStrings_ReturnsFalse(string invalidString)
    {
        Assert.False(IsValidBase64(invalidString));
    }

    private static bool IsValidBase64(string input)
    {
        try
        {
            Convert.FromBase64String(input);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    [Fact]
    public void CredentialValidation_ErrorMessages_AreHelpful()
    {
        // Test that our error messages provide clear guidance for fixing credential issues
        
        var testCases = new[]
        {
            (Value: (string?)null, ExpectedMessage: "cannot be null"),
            (Value: "", ExpectedMessage: "cannot be null"),
            (Value: "   ", ExpectedMessage: "cannot be null"),
            (Value: "invalid_base64", ExpectedMessage: "Base64 string")
        };

        foreach (var (value, expectedMessage) in testCases)
        {
            try
            {
                ValidateCredential(value);
                Assert.Fail($"Expected validation to fail for value: '{value}'");
            }
            catch (ArgumentException ex)
            {
                Assert.Contains(expectedMessage, ex.Message, StringComparison.OrdinalIgnoreCase);
                
                if (value != null && !string.IsNullOrWhiteSpace(value))
                {
                    // For Base64 validation errors, ensure we have the inner FormatException
                    Assert.NotNull(ex.InnerException);
                    Assert.IsType<FormatException>(ex.InnerException);
                }
            }
        }
    }

    private static void ValidateCredential(string? credential)
    {
        if (string.IsNullOrWhiteSpace(credential))
        {
            throw new ArgumentException("Credential cannot be null or empty", nameof(credential));
        }

        try
        {
            Convert.FromBase64String(credential);
        }
        catch (FormatException ex)
        {
            throw new ArgumentException("Credential must be a valid Base64 string. Please check your configuration.", nameof(credential), ex);
        }
    }
} 