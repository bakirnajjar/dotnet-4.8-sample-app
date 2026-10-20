using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using DescopeSampleApp.Core;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DescopeSampleApp.IntegrationTests;

public sealed class TokenValidatorTests
{
    private const long Future = 4102444800;
    private static readonly TimeProvider Clock = new FixedClock();

    // Public test-only key: fixed material keeps all signatures and JWKS assertions reproducible.
    private const string TestKey = """
        <RSAKeyValue><Modulus>sPDUgmX6rv89uFGj2ghzuOPasZ/JYbKlCgXlNEgflHj8mhl3y2thXQkvv+I/AKWvYLaMC7achwvH9vMtBWSZ7XT0aos6xptF2gO3SpUb1KywJ9Ns+JuyRfUATTe1cLMaxUNBHrq5W68KG47HKCKGr5Xf1Pe7xFakwhRFtZLld5JwSVp6L/UjHW5bUCQahESw9f8c8GPotz7Zl8Bl5PBLmZUJAP+x6huqFBc7rdIdJ2zZvO+t0AxrLCGmBRPoUnDrPkmMRPp6ZzoXfE1K/Fv8/GAtbBDiAN9+xZGEfn262lsrECjRL9EJf+B/G2TJ6KARGFrL+D5vYlBmMwm9x6DILQ==</Modulus><Exponent>AQAB</Exponent><P>6Sxbf1MbibC6+8tVh0A+acJ9gLXEoRIlypjsBqvGUiNUdhVI5SQejEVm/kP+G/70LjS7cQs64vecX9jRWRE4ADGO2cDBsNT7ZfC8cl1yRg99tmF8RJQ/yO8jkRipdZFRW5mZqlgF5dGbV3zQiUYcltpeiEqWToQaudDJX68plqc=</P><Q>wkM1UQDEQMSsn02R40ZBkbm7U8pvbu/C3/bKavLXAnl1BwsM8TcyMuh6cOFL/REZu5wfe9pOK+NpD5VSf7HRXJIFmaL2CUFUdC8Cd5D+i7EtsUVHE/OXhKv9HlDHw0kyYmDevsJ90SJr8DvuMzI/w1VQYXjLu5ptz2QlHDr3GQs=</Q><DP>Z+TUy/Gq1noBhom+DSqJ5LO9GeljuGUOo6vplrtY/WYyjv7C4R5r4hJWZrrC8ZRzFld2oVLgpRqw+V/xP59VdXifbtTdOL5dWbmfLdIHRcJHQElFhoULuqsbDN6ROwpJ1uekuWrJDfKMJbo+bLvppymbb1zUhh2EjwDBiTnR7Qc=</DP><DQ>OgbJTkDkXlRhG9SX10ONInJQOl8lmgWUOEIHhJq1DtAo3Mm4O/U40WwP5tflUtpgzWF9Jgm3f/SV1UnNMsdyuiwD8Dh1NZ6GE45XNk/hOOjbtS/0Y0lH4SFeokeO5A5hNnGpF2IRIv8dLs04AaLo2PgK4pKqiIdAdcp8K0qRtLE=</DQ><InverseQ>BD/n+5CeAWZ7MP1ToVn5OVnlvaNwC5tcS/ESHd7nmzA+i3r3taQ7DLrD7YknLfPVk7KKNf3d7qxrsHVJG/4m75eD2oyWBjjFspx+63ELwDikIv7cBwRwqyA5w7uQMbigaxHAxqjhS8j+Dnz0bikZSm13xW2cYmJ/qhY+YBMPpRQ=</InverseQ><D>bUbfSAVpdAiI+rvZAQA1UBvQ5eB0RBVLnkwTyV09u7mY8CcIeCZCRG43Aj/dfy3UiVtUVKucMj9RaS+4P1tpQWD4zpBcZWpiEoo2+iXqwUdBp+cIpuWpjVEJtabRNOA4iVnG7w6NCZWqHWjpkNz555kdWu8BFPoZ2Q8iGki11Ukgegz1DcpVLbi1n6eoXYbOOX3/IOI76Ypnk4RqS8BTb77aDY953M9vkWvqfOvb8v8cF+adWnh0XFebU07BnWRsd1Y/HmfWVGLGMNJyGGGEGuTDO95uJ8SqcLassso9w18trLZ9IaLWdy5Y6WD6bbo0TbBSDSmMlPbrgjgdToodFQ==</D></RSAKeyValue>
        """;

    [Fact]
    public async Task SignedFutureTokenReturnsVerifiedPayloadThroughHostRegistration()
    {
        using var fixture = new OfflineHostFixture(Jwks());
        using var host = fixture.CreateClient();
        var validator = fixture.Services.GetRequiredService<TokenValidator>();
        var payload = """{"sub":"test-user","exp":4102444800}""";
        Assert.Equal(payload, await validator.ValidateSession(Sign(payload)));
    }

    [Fact]
    public async Task BadSignatureIsRejected()
    {
        var token = Sign("""{"exp":4102444800}""");
        var parts = token.Split('.');
        parts[2] = (parts[2][0] == 'A' ? "B" : "A") + parts[2][1..];
        await Assert.ThrowsAsync<TokenValidationException>(() => Validator(Jwks()).ValidateSession(string.Join(".", parts)));
    }

    [Theory]
    [InlineData("""{"exp":946684800}""")]
    [InlineData("""{"exp":1760961600}""")]
    [InlineData("""{"sub":"no-exp"}""")]
    [InlineData("""{"exp":"4102444800"}""")]
    public async Task ExpiredOrMissingExpiryIsRejected(string payload)
    {
        await Assert.ThrowsAsync<TokenValidationException>(() => Validator(Jwks()).ValidateSession(Sign(payload)));
    }

    [Fact]
    public async Task FindsMatchingKeyAmongMultipleKeys()
    {
        var wrong = JsonSerializer.Serialize(new { kid = "other", kty = "RSA", n = "AQ", e = "AQAB" });
        var jwks = """{"keys":[""" + wrong + "," + Key() + "]}";
        Assert.Equal("""{"exp":4102444800}""", await Validator(jwks).ValidateSession(Sign("""{"exp":4102444800}""")));
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("""{"keys":{}}""")]
    [InlineData("""{"keys":[null]}""")]
    [InlineData("""{"keys":[{"kid":"test","kty":"RSA"}]}""")]
    public async Task MalformedJwksHasExplicitFailure(string jwks)
    {
        await Assert.ThrowsAsync<JwksException>(() => Validator(jwks).ValidateSession(Sign("""{"exp":4102444800}""")));
    }

    [Fact]
    public async Task FetchFailureHasExplicitFailureAndCannotReachNetwork()
    {
        using var client = new HttpClient(new RejectingJwksHandler(Jwks()));
        var validator = new TokenValidator(client, new Uri("https://unexpected.example.test/jwks"), Clock);
        var error = await Assert.ThrowsAsync<JwksException>(
            () => validator.ValidateSession(Sign("""{"exp":4102444800}""")));
        Assert.IsType<InvalidOperationException>(error.InnerException);
    }

    [Fact]
    public async Task UnsupportedAlgorithmAndUnknownKidAreRejected()
    {
        var valid = Sign("""{"exp":4102444800}""");
        await Assert.ThrowsAsync<TokenValidationException>(
            () => Validator(Jwks()).ValidateSession(Sign("""{"exp":4102444800}""", "unknown")));
        var segments = valid.Split('.');
        segments[0] = Base64(Encoding.UTF8.GetBytes("""{"alg":"none","kid":"test"}"""));
        await Assert.ThrowsAsync<TokenValidationException>(
            () => Validator(Jwks()).ValidateSession(string.Join(".", segments)));
    }

    private static TokenValidator Validator(string jwks) =>
        new(new HttpClient(new RejectingJwksHandler(jwks)), new Uri(OfflineHostFixture.JwksUrl), Clock);

    private static string Jwks() => """{"keys":[""" + Key() + "]}";

    private static string Key()
    {
        using var rsa = PrivateKey();
        var publicKey = rsa.ExportParameters(false);
        return JsonSerializer.Serialize(new { kid = "test", kty = "RSA", use = "sig", alg = "RS256",
            n = Base64(publicKey.Modulus!), e = Base64(publicKey.Exponent!) });
    }

    private static string Sign(string payload, string kid = "test")
    {
        using var rsa = PrivateKey();
        var header = Base64(JsonSerializer.SerializeToUtf8Bytes(new { alg = "RS256", kid }));
        var input = header + "." + Base64(Encoding.UTF8.GetBytes(payload));
        return input + "." + Base64(rsa.SignData(Encoding.ASCII.GetBytes(input),
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    }

    private static RSA PrivateKey()
    {
        var xml = XElement.Parse(TestKey);
        byte[] Part(string name) => Convert.FromBase64String(xml.Element(name)!.Value);
        var rsa = RSA.Create();
        rsa.ImportParameters(new RSAParameters
        {
            Modulus = Part("Modulus"), Exponent = Part("Exponent"), D = Part("D"),
            P = Part("P"), Q = Part("Q"), DP = Part("DP"), DQ = Part("DQ"), InverseQ = Part("InverseQ"),
        });
        return rsa;
    }

    private static string Base64(byte[] bytes) => Convert.ToBase64String(bytes)
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.FromUnixTimeSeconds(1760961600);
    }
}
