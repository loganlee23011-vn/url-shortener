using System.Security.Cryptography;
using System.Text;

namespace Shorten.Redirect.Security
{
    public static class PasswordUtility
    {
        public static string Hash(string value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(bytes);
        }
    }
}
