using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Jose;
using Newtonsoft.Json.Linq;
using Xunit;

[CollectionDefinition("Console output", DisableParallelization = true)]
public class ConsoleOutputCollection { }

[Collection("Console output")]
public class TokenValidatorCharacterizationTests
{
    [Fact]
    public void SignedTokenWithPastExpirationReturnsFalseViaDiagnosticFallback()
    {
        const long past = 946684800L; // 2000-01-01 UTC
        var token = SignedToken(new Dictionary<string, object> { { "exp", past } });

        Assert.Equal(past, (long)TokenClaims(token)["exp"]);
        Assert.Equal(ExpectedFallback, VerifyAndCaptureDiagnostic(token));
    }

    [Fact]
    public void SignedTokenWithFutureExpirationReturnsFalseViaDiagnosticFallback()
    {
        const long future = 4102444800L; // 2100-01-01 UTC
        var token = SignedToken(new Dictionary<string, object> { { "exp", future } });

        Assert.Equal(future, (long)TokenClaims(token)["exp"]);
        Assert.Equal(ExpectedFallback, VerifyAndCaptureDiagnostic(token));
    }

    [Fact]
    public void MissingExpirationReturnsFalseViaSameDiagnosticFallback()
    {
        var token = SignedToken(new Dictionary<string, object>());

        Assert.Null(TokenClaims(token)["exp"]);
        Assert.Equal(ExpectedFallback, VerifyAndCaptureDiagnostic(token));
    }

    // These tests pin the observable failure path, NOT the DateTimeOffset expiration comparison.
    private static readonly string ExpectedFallback =
        "Error verifying token expiration: Value cannot be null." + Environment.NewLine + "Parameter name: value";

    private static string VerifyAndCaptureDiagnostic(string token)
    {
        var original = Console.Out;
        using (var output = new StringWriter())
        {
            try
            {
                Console.SetOut(output);
                Assert.False(new TokenValidator("offline-test").VerifyTokenExpiration(token));
                return output.ToString().TrimEnd();
            }
            finally
            {
                Console.SetOut(original);
            }
        }
    }

    private static JObject TokenClaims(string token)
    {
        var segment = token.Split('.')[1];
        var padded = segment.PadRight((segment.Length + 3) / 4 * 4, '=');
        return JObject.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/'))));
    }

    private static string SignedToken(object payload)
    {
        return JWT.Encode(payload, Encoding.UTF8.GetBytes("offline-characterization-secret"), JwsAlgorithm.HS256);
    }
}
