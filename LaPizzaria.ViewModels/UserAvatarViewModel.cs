namespace LaPizzaria.ViewModels
{
    public class UserAvatarViewModel
    {
        public string? AvatarUrl { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? UserName { get; set; }

        /// <summary>CSS classes cho thẻ img hoặc ô chữ (vd. profile-avatar, rounded-circle).</summary>
        public string ElementClass { get; set; } = "";

        public string? ElementStyle { get; set; }

        /// <summary>Chỉ dùng khi cần preview upload: cặp id cho img và ô chữ.</summary>
        public string? ImgElementId { get; set; }

        public string? InitialElementId { get; set; }
    }
}
