using DiarioLivre.Core.Services;

namespace DiarioLivre.Core.Tests;

public sealed class PinHasherTests
{
    [Theory]
    [InlineData("1234", true)]
    [InlineData("000000", true)]
    [InlineData("12345678", true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("123", false)]
    [InlineData("123456789", false)]
    [InlineData("12a4", false)]
    [InlineData("12 4", false)]
    public void IsValidPinFormat_ValidatesLengthAndDigits(string? pin, bool expected)
    {
        Assert.Equal(expected, PinHasher.IsValidPinFormat(pin));
    }

    [Fact]
    public void HashPin_ProducesUniqueSaltedHashesAndVerifiesCorrectPin()
    {
        string hash1 = PinHasher.HashPin("2580");
        string hash2 = PinHasher.HashPin("2580");

        Assert.NotEqual(hash1, hash2);
        Assert.Contains(':', hash1);
        Assert.True(PinHasher.VerifyPin("2580", hash1));
        Assert.True(PinHasher.VerifyPin("2580", hash2));
        Assert.False(PinHasher.VerifyPin("0852", hash1));
    }

    [Theory]
    [InlineData("123")]
    [InlineData("abcd")]
    [InlineData("")]
    public void HashPin_InvalidFormat_ThrowsArgumentException(string invalidPin)
    {
        Assert.Throws<ArgumentException>(() => PinHasher.HashPin(invalidPin));
    }

    [Theory]
    [InlineData(null, "salt:hash")]
    [InlineData("", "salt:hash")]
    [InlineData("1234", null)]
    [InlineData("1234", "")]
    [InlineData("1234", "missing-separator")]
    [InlineData("1234", "not-base64!:not-base64!")]
    [InlineData("1234", "AA==:")]
    public void VerifyPin_MalformedOrMissingInputs_ReturnsFalse(string? pin, string? storedHash)
    {
        Assert.False(PinHasher.VerifyPin(pin, storedHash));
    }
}
