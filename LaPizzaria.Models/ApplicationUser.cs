using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace LaPizzaria.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
		public string? AvatarUrl { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; } // Male, Female, Other
        public string? DefaultAddress { get; set; }
        public int LoyaltyPoints { get; set; } = 0; // Điểm tích lũy
        public bool IsMember { get; set; } = false; // Đã đăng ký hội viên
        public DateTime? MemberSince { get; set; } // Ngày đăng ký hội viên
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<FavoriteProduct> FavoriteProducts { get; set; } = new List<FavoriteProduct>();
    }
}

