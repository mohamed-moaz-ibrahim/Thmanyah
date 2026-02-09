namespace Thmanyah.Shared.Configurations;

public class JWTSettings
{
    public string Key { get; set; }
    public string Issuer { get; set; }
    public int ExpiryMinutes { get; set; }
}


