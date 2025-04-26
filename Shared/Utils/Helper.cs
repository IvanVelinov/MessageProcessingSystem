using System.Text;
using System.Security.Cryptography;

namespace Shared.Utils
{
    public static class Helper
    {
        public static string ComputeSha1(string input)
        {
            using var sha1 = SHA1.Create();
            var hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
}
