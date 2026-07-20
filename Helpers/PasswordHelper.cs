using System;
using System.Security.Cryptography;

namespace MyPersonalWebsite.Helpers
{
    public static class PasswordHelper
    {
        public static string HashPassword(string password)
        {
            // 使用 .NET 10 推荐的 Pbkdf2 方法
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                100000,
                HashAlgorithmName.SHA256,
                32
            );

            byte[] hashBytes = new byte[48];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 32);

            return Convert.ToBase64String(hashBytes);
        }

       public static bool VerifyPassword(string password, string storedHash)
{
    byte[] hashBytes = Convert.FromBase64String(storedHash);
    byte[] salt = new byte[16];
    Array.Copy(hashBytes, 0, salt, 0, 16);

    byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
        password,
        salt,
        100000,
        HashAlgorithmName.SHA256,
        32
    );

    for (int i = 0; i < 32; i++)
    {
        if (hashBytes[i + 16] != hash[i])
            return false;
    }
    return true;
}
    }
}
