namespace YamyProject.Core.Models
{
    public static class PasswordHelper
    {
        // Hash password with salt
        public static string HashPassword(string password, out string salt)
        {
            // Generate a new 128-bit salt
            byte[] saltBytes = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            salt = Convert.ToBase64String(saltBytes);

            // Generate hash using PBKDF2
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: saltBytes,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            return hashed;
        }
        // Verify password
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
}
