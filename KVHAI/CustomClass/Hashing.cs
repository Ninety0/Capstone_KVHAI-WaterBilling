namespace KVHAI.CustomClass
{
    public class Hashing
    {
        private const int WorkFactor = 12;

        // Hashes a password using bcrypt
        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
        }

        // Verifies a password against a hashed password using bcrypt
        public bool VerifyPassword(string hashedPassword, string password)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}
