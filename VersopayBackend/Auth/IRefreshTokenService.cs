namespace VersopayBackend.Auth
{
    public interface IRefreshTokenService
    {
        // retorna (token puro para o cliente, hash para BD, expiração)
        (string rawToken, string tokenHash, DateTime expiresUtc) Create(TimeSpan lifetime);
        string Hash(string rawToken);
    }
}
