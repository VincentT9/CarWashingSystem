namespace BusinessLayer.Dtos.Auth
{
    /// <summary>
    /// Data transfer object returned upon successful registration.
    /// </summary>
    public class RegisterResponseDto
    {
        /// <summary>
        /// Gets or sets the unique identifier of the newly created user.
        /// </summary>
        public Guid UserID { get; set; }

        /// <summary>
        /// Gets or sets the registered username.
        /// </summary>
        public string Username { get; set; } = null!;

        /// <summary>
        /// Gets or sets the registered full name.
        /// </summary>
        public string FullName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the registered email address.
        /// </summary>
        public string Email { get; set; } = null!;

        /// <summary>
        /// Gets or sets the registered phone number.
        /// </summary>
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets the assigned role name.
        /// </summary>
        public string Role { get; set; } = null!;

        /// <summary>
        /// Gets or sets the UTC timestamp when account was registered.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
