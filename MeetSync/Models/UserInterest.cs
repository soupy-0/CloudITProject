using System;
using System.ComponentModel.DataAnnotations;

namespace MeetSync.Models
{
    public class UserInterest
    {
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Interest { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}