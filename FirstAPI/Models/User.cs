using System.ComponentModel.DataAnnotations;

namespace FirstAPI.Models
{
    public class User
    {
        [Key]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        public byte[] Password { get; set; } = Array.Empty<byte>();

        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = "Employee";

        // Navigation
        public Employee? Employee { get; set; }
    }
}
