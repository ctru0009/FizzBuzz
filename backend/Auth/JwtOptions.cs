namespace backend.Auth
{
    public class JwtOptions
    {
        public const string SectionName = "Jwt";

        public string Issuer { get; set; } = "fizzbuzz";

        public string Audience { get; set; } = "fizzbuzz";

        public string SigningKey { get; set; } = string.Empty;

        public int LifetimeHours { get; set; } = 8;
    }
}
