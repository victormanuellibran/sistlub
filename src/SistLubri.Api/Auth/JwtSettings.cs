namespace SistLubri.Api.Auth;

public sealed record JwtSettings(
    string Issuer,
    string Audience,
    byte[] SigningKey,
    TimeSpan Lifetime);
