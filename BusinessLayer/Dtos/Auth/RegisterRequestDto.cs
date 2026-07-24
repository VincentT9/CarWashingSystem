using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.Dtos.Auth
{
    /// <summary>
    /// Data transfer object containing registration information for a new user account.
    /// </summary>
    public class RegisterRequestDto
    {
        /// <summary>
        /// Gets or sets the unique username for account login.
        /// </summary>
        public string Username { get; set; } = null!;

        /// <summary>
        /// Gets or sets the raw password string.
        /// </summary>
        public string Password { get; set; } = null!;

        /// <summary>
        /// Gets or sets the confirmation password to match.
        /// </summary>
        public string ConfirmPassword { get; set; } = null!;

        /// <summary>
        /// Gets or sets the full name of the user.
        /// </summary>
        public string FullName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the primary email address for verification.
        /// </summary>
        public string Email { get; set; } = null!;

        /// <summary>
        /// Gets or sets the phone number for customer contact.
        /// </summary>
        public string? PhoneNumber { get; set; }
    }
}
