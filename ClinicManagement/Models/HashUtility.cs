using System.Security.Cryptography;
using System.Text;

namespace ClinicManagement.Models
{
    public static class HashUtility
    {
        public static string? Base64Encode(string? input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }

            try
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                return Convert.ToBase64String(inputBytes);
            }
            catch (Exception ex)
            {
             
                return null;
            }
        }
        public static string? ComputeSha256Hash(string? input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);
                return Convert.ToHexString(hashBytes).ToLowerInvariant();
            }
        }

    }
}