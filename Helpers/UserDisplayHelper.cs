namespace LaPizzaria.Helpers
{
    public static class UserDisplayHelper
    {
        /// <summary>
        /// Chữ hiển thị khi không có ảnh: ký tự đầu tiên (không phải khoảng trắng) của họ tên, hoặc tên đăng nhập.
        /// </summary>
        public static string GetAvatarInitial(string? firstName, string? lastName, string? userName)
        {
            var combined = $"{firstName ?? ""} {lastName ?? ""}".Trim();
            if (string.IsNullOrEmpty(combined))
                combined = userName?.Trim() ?? "";
            if (string.IsNullOrEmpty(combined))
                return "?";

            foreach (var c in combined)
            {
                if (!char.IsWhiteSpace(c))
                {
                    if (char.IsLetter(c))
                        return c.ToString().ToUpperInvariant();
                    return c.ToString();
                }
            }

            return "?";
        }
    }
}
