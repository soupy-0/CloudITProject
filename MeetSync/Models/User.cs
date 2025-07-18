using System;
using System.ComponentModel.DataAnnotations;

namespace MeetSync.Models
{
    public class User
    {
        public int Id { get; set; }
        
        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [StringLength(256)]
        public string PasswordHash { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string? Workplace { get; set; }
        
        [StringLength(1000)]
        public string? AboutSection { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
    }
}