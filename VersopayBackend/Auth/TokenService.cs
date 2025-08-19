using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using VersopayLibrary.Models;

namespace VersopayBackend.Auth
{
    public class TokenService(IOptions<JwtOptions> options) : ITokenService
    {
        private readonly JwtOptions _opt = options.Value;

        public string CreateToken(Usuario u, DateTime nowUtc, out DateTime expiresAtUtc)
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, u.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, u.Email),
                new(JwtRegisteredClaimNames.Name, u.Nome),
                new("tipoCadastro", u.TipoCadastro.ToString()),
                new("isAdmin", u.IsAdmin ? "true" : "false")
            };

            if (u.IsAdmin)
                claims.Add(new Claim(ClaimTypes.Role, "Admin")); // << habilita [Authorize(Roles="Admin")]

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            expiresAtUtc = nowUtc.AddMinutes(_opt.ExpiryMinutes);

            var token = new JwtSecurityToken(
                issuer: _opt.Issuer,
                audience: _opt.Audience,
                claims: claims,
                notBefore: nowUtc,
                expires: expiresAtUtc,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
