namespace Chat.API.Configuration;

public class JwtSettings
{
    public string Authority { get; set; } = null!; // The Keycloak URL
    public string Audience { get; set; } = "account"; // From the JWT
}