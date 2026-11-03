namespace OrderInventory.Infrastructure.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKeyBase64 { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 30;

    public static bool IsValid(JwtOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Issuer) || string.IsNullOrWhiteSpace(options.Audience) ||
            options.AccessTokenMinutes != 30)
            return false;
        try
        {
            return Convert.FromBase64String(options.SigningKeyBase64).Length >= 32;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
