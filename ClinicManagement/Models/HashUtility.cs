using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ClinicManagement.Models
{
    public static class HashUtility
    {
        public static string? Base64Decode(string? base64EncodedData)
        {
            if (string.IsNullOrWhiteSpace(base64EncodedData))
            {
                return null;
            }

            try
            {
                byte[] base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
                return Encoding.UTF8.GetString(base64EncodedBytes);
            }
            catch (FormatException ex)
            {
                Console.WriteLine($"Lỗi giải mã Base64: {ex.Message}");
                return null;
            }
        }

        public static async Task<string?> Base64DecodeAsync(string? base64EncodedData)
        {
            if (string.IsNullOrWhiteSpace(base64EncodedData))
            {
                return null;
            }

            try
            {
                byte[] base64EncodedBytes = await Task.Run(() => Convert.FromBase64String(base64EncodedData));
                return Encoding.UTF8.GetString(base64EncodedBytes);
            }
            catch (FormatException ex)
            {
                Console.WriteLine($"Lỗi giải mã Base64: {ex.Message}");
                return null;
            }
        }

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
                Console.WriteLine($"Lỗi mã hóa Base64: {ex.Message}");
                return null;
            }
        }

        public static async Task<string?> Base64EncodeAsync(string? input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }

            try
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                return await Task.Run(() => Convert.ToBase64String(inputBytes));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi mã hóa Base64: {ex.Message}");
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

        public static async Task<string?> ComputeSha256HashAsync(string? input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = await Task.Run(() => sha256.ComputeHash(inputBytes));
                return Convert.ToHexString(hashBytes).ToLowerInvariant();
            }
        }
    }
}