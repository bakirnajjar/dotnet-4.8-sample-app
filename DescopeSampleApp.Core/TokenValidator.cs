using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DescopeSampleApp.Core;

/// <summary>Validates RS256 session JWTs against the configured JWKS; never trusts unsigned claims.</summary>
public sealed class TokenValidator(HttpClient httpClient, Uri jwksUrl, TimeProvider clock)
{
    public const string JwksClientName = "Jwks";

    public async Task<string> ValidateSession(string sessionToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
            throw new TokenValidationException("Token is missing.");

        var segments = sessionToken.Split('.');
        if (segments.Length != 3 || segments.Any(string.IsNullOrEmpty))
            throw new TokenValidationException("Token must have three nonempty segments.");

        JsonDocument header;
        byte[] signature;
        try
        {
            header = JsonDocument.Parse(Decode(segments[0]));
            signature = Decode(segments[2]);
        }
        catch (Exception ex) when (ex is JsonException or FormatException)
        {
            throw new TokenValidationException("Token encoding or header is invalid.", ex);
        }

        using (header)
        {
            var h = header.RootElement;
            if (h.ValueKind != JsonValueKind.Object ||
                !HasString(h, "alg", "RS256") ||
                !TryString(h, "kid", out var kid))
                throw new TokenValidationException("Token algorithm or key id is invalid.");

            string jwks;
            try
            {
                jwks = await httpClient.GetStringAsync(jwksUrl, cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
            {
                throw new JwksException("Unable to fetch JWKS.", ex);
            }

            JsonDocument keys;
            try
            {
                keys = JsonDocument.Parse(jwks);
            }
            catch (JsonException ex)
            {
                throw new JwksException("JWKS is malformed.", ex);
            }

            using (keys)
            {
                if (keys.RootElement.ValueKind != JsonValueKind.Object ||
                    !keys.RootElement.TryGetProperty("keys", out var entries) ||
                    entries.ValueKind != JsonValueKind.Array)
                    throw new JwksException("JWKS must contain a keys array.");

                var signingInput = Encoding.ASCII.GetBytes(segments[0] + "." + segments[1]);
                var verified = false;
                foreach (var entry in entries.EnumerateArray())
                {
                    if (entry.ValueKind != JsonValueKind.Object)
                        throw new JwksException("JWKS key is malformed.");
                    if (!HasString(entry, "kid", kid))
                        continue;
                    if (!HasString(entry, "kty", "RSA") ||
                        (entry.TryGetProperty("use", out _) && !HasString(entry, "use", "sig")) ||
                        (entry.TryGetProperty("alg", out _) && !HasString(entry, "alg", "RS256")) ||
                        !TryString(entry, "n", out var n) ||
                        !TryString(entry, "e", out var e))
                        throw new JwksException("Matching JWKS key is malformed or unsuitable.");

                    try
                    {
                        using var rsa = RSA.Create();
                        rsa.ImportParameters(new RSAParameters { Modulus = Decode(n), Exponent = Decode(e) });
                        verified |= rsa.VerifyData(signingInput, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                    }
                    catch (Exception ex) when (ex is CryptographicException or FormatException)
                    {
                        throw new JwksException("Matching JWKS key is invalid.", ex);
                    }
                }
                if (!verified)
                    throw new TokenValidationException("Token signature could not be verified.");
            }
        }

        try
        {
            var payloadBytes = Decode(segments[1]);
            using var payload = JsonDocument.Parse(payloadBytes);
            if (payload.RootElement.ValueKind != JsonValueKind.Object ||
                !payload.RootElement.TryGetProperty("exp", out var exp) ||
                exp.ValueKind != JsonValueKind.Number ||
                !exp.TryGetInt64(out var expiration))
                throw new TokenValidationException("Token expiry is missing or invalid.");
            if (expiration <= clock.GetUtcNow().ToUnixTimeSeconds())
                throw new TokenValidationException("Token has expired.");
            return Encoding.UTF8.GetString(payloadBytes);
        }
        catch (Exception ex) when (ex is JsonException or FormatException)
        {
            throw new TokenValidationException("Token payload is invalid.", ex);
        }
    }

    private static byte[] Decode(string segment) => Convert.FromBase64String(
        segment.Replace('-', '+').Replace('_', '/').PadRight((segment.Length + 3) / 4 * 4, '='));

    private static bool HasString(JsonElement value, string property, string expected) =>
        TryString(value, property, out var actual) && actual == expected;

    private static bool TryString(JsonElement value, string property, out string text)
    {
        text = "";
        if (!value.TryGetProperty(property, out var item) || item.ValueKind != JsonValueKind.String)
            return false;
        text = item.GetString()!;
        return !string.IsNullOrEmpty(text);
    }
}

public sealed class TokenValidationException(string message, Exception? inner = null) : Exception(message, inner);
public sealed class JwksException(string message, Exception? inner = null) : Exception(message, inner);
