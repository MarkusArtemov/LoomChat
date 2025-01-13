namespace De.Hsfl.LoomChat.Auth.Services
{
    /// <summary>
    /// This class is responsible for hashing passwords and verifying them.
    /// </summary>
    public class PasswordHasher
    {
        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}
