namespace YamyProject.Core.Models
{
   
        public static class PasswordHelper
        {
            public static string HashPassword(string password, out string salt)
            {
                using (var rng = new RNGCryptoServiceProvider())
                {
                    byte[] saltBytes = new byte[16];
                    rng.GetBytes(saltBytes);
                    salt = Convert.ToBase64String(saltBytes);
                }
                string saltedPassword = salt + password;
                using (var sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
                    return Convert.ToBase64String(hashBytes);
                }
            }

            public static bool VerifyPassword(string enteredPassword, string storedHash, string storedSalt)
            {
                string saltedPassword = storedSalt + enteredPassword;
                using (var sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
                    string enteredHash = Convert.ToBase64String(hashBytes);
                    return enteredHash == storedHash;
                }
            }
        }

        public static class CryptoHelper
        {
            // Simple fixed key and IV. Ensure these are 16 bytes for AES-128.
            private static readonly string key = "1234567890123456";  // 16 bytes key for AES-128
            private static readonly string iv = "1234567890123456";   // 16 bytes IV

            // Encrypt method
            public static string Encrypt(string plainText)
            {
                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = Encoding.UTF8.GetBytes(key);
                    aesAlg.IV = Encoding.UTF8.GetBytes(iv);

                    using (MemoryStream msEncrypt = new MemoryStream())
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, aesAlg.CreateEncryptor(), CryptoStreamMode.Write))
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                        swEncrypt.Flush();
                        csEncrypt.FlushFinalBlock();
                        return Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }

            // Decrypt method
            public static string Decrypt(string cipherText)
            {
                byte[] cipherBytes = Convert.FromBase64String(cipherText);  // Convert Base64 to byte array

                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = Encoding.UTF8.GetBytes(key);  // Ensure key is 16 bytes
                    aesAlg.IV = Encoding.UTF8.GetBytes(iv);    // Ensure IV is 16 bytes

                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                    using (MemoryStream msDecrypt = new MemoryStream(cipherBytes))
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();  // Return the decrypted string
                    }
                }
            }
        }
  
}
