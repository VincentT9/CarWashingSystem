namespace BusinessLayer.Dtos.Auth
{
    /// <summary>
    /// Data transfer object returned upon successful login containing JWT token details.
    /// </summary>
    public class LoginResponseDto
    {
        /// <summary>
        /// Gets or sets the authenticated user's unique identifier.
        /// </summary>
        public Guid UserID { get; set; }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        public string Username { get; set; } = null!;

        /// <summary>
        /// Gets or sets the full name of the user.
        /// </summary>
        public string FullName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        public string Email { get; set; } = null!;

        /// <summary>
        /// Gets or sets the role assigned to the user.
        /// </summary>
        public string Role { get; set; } = null!;

        /// <summary>
        /// Gets or sets the bearer access token for authorized API calls.
        /// </summary>
        public string AccessToken { get; set; } = null!;

        /// <summary>
        /// Gets or sets the token expiration UTC timestamp.
        /// </summary>
        public DateTime AccessTokenExpiration { get; set; }
    }
}
