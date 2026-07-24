namespace BusinessLayer.Dtos.Auth
{
    /// <summary>
    /// Data transfer object containing credentials for user authentication.
    /// </summary>
    public class LoginRequestDto
    {
        /// <summary>
        /// Gets or sets the username or registered email address.
        /// </summary>
        public string Username { get; set; } = null!;

        /// <summary>
        /// Gets or sets the account password.
        /// </summary>
        public string Password { get; set; } = null!;
    }
}
