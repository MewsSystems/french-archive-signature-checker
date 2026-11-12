using System.Security.Cryptography;

namespace Mews.Fiscalization.SignatureChecker.Tests;

/// <summary>
/// Pins the embedded Mews public keys, so any change to them fails the build instead of silently changing verification results.
/// </summary>
public sealed class CryptoServiceProviderTests
{
    private const string ProductionModulusSha256 = "00AED01C2D0CAC461F95D903EDCC024B6557A8589ACCB1B5A4D7F1A381DA1DF4";
    private const string DevelopModulusSha256 = "A99ABF10A891F5BBA456CCF5A73203C53830480DC26BEC402221786D5D68C122";

    [Fact]
    public void Production_IsThePinnedPublicKey()
    {
        using var rsa = CryptoServiceProvider.GetProduction();

        AssertKey(rsa, ProductionModulusSha256);
    }

    [Fact]
    public void Develop_IsThePinnedPublicKey()
    {
        using var rsa = CryptoServiceProvider.GetDevelop();

        AssertKey(rsa, DevelopModulusSha256);
    }

    [Fact]
    public void Keys_ArePublicOnly()
    {
        using var production = CryptoServiceProvider.GetProduction();
        using var develop = CryptoServiceProvider.GetDevelop();

        // RSACryptoServiceProvider.PublicOnly is not reliable on Linux and macOS, so check that there is no private key to export.
        Assert.ThrowsAny<CryptographicException>(() => production.ExportParameters(includePrivateParameters: true));
        Assert.ThrowsAny<CryptographicException>(() => develop.ExportParameters(includePrivateParameters: true));
    }

    [Theory]
    [InlineData(nameof(SigningEnvironment.Production), ProductionModulusSha256)]
    [InlineData(nameof(SigningEnvironment.Develop), DevelopModulusSha256)]
    public void GetPublicKey_SelectsKeyForEnvironment(string environment, string expectedFingerprint)
    {
        using var rsa = SignatureVerifier.GetPublicKey(Enum.Parse<SigningEnvironment>(environment));

        AssertKey(rsa, expectedFingerprint);
    }

    [Fact]
    public void GetPublicKey_UnknownEnvironmentThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SignatureVerifier.GetPublicKey((SigningEnvironment)42));
    }

    private static void AssertKey(RSA rsa, string expectedModulusSha256)
    {
        var parameters = rsa.ExportParameters(includePrivateParameters: false);

        Assert.Equal(4096, rsa.KeySize);
        Assert.Equal([1, 0, 1], parameters.Exponent);
        Assert.Equal(expectedModulusSha256, Convert.ToHexString(SHA256.HashData(parameters.Modulus)));
    }
}
