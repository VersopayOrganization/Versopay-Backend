using System.Security.Claims;

namespace VersopayBackend.Utils
{
    public static class HttpUser
    {
        public static int GetUserId(ClaimsPrincipal user)
        {
            var id = user.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? user.FindFirstValue("sub");
            if (string.IsNullOrWhiteSpace(id))
                throw new InvalidOperationException("Usuário sem claim de identificação.");
            return int.Parse(id);
        }
    }
}
