namespace LaPizzaria.Helpers
{
    public static class NumberToWordsVietnamese
    {
        private static readonly string[] Ones = { "", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín", "mười" };
        private static readonly string[] OnesCap = { "", "Một", "Hai", "Ba", "Bốn", "Năm", "Sáu", "Bảy", "Tám", "Chín", "Mười" };

        /// <summary>Chuyển số (nguyên, &lt; 1 tỷ) thành chữ tiếng Việt, hậu tố "đồng".</summary>
        public static string ToWords(decimal amount, string suffix = "đồng")
        {
            var n = (long)Math.Round(amount, 0);
            if (n <= 0) return "Không " + suffix;
            var s = n.ToString();
            var parts = new List<string>();
            if (n >= 1_000_000_000)
            {
                parts.Add(ReadBlock((int)(n / 1_000_000_000)) + " tỷ");
                n %= 1_000_000_000;
            }
            if (n >= 1_000_000)
            {
                parts.Add(ReadBlock((int)(n / 1_000_000)) + " triệu");
                n %= 1_000_000;
            }
            if (n >= 1_000)
            {
                parts.Add(ReadBlock((int)(n / 1_000)) + " nghìn");
                n %= 1_000;
            }
            if (n > 0 || parts.Count == 0)
                parts.Add(ReadBlock((int)n));
            var result = string.Join(" ", parts).Trim() + " " + suffix;
            return char.ToUpperInvariant(result[0]) + result.Substring(1);
        }

        private static string ReadBlock(int n)
        {
            if (n <= 0) return "";
            if (n < 10) return Ones[n];
            if (n < 20) return "mười " + (n == 10 ? "" : Ones[n % 10]);
            if (n < 100)
                return Ones[n / 10] + " mươi " + (n % 10 == 0 ? "" : (n % 10 == 1 ? "mốt" : Ones[n % 10]));
            var hundred = n / 100;
            var rest = n % 100;
            return Ones[hundred] + " trăm " + (rest == 0 ? "" : (rest < 10 ? "lẻ " + Ones[rest] : ReadBlock(rest)));
        }
    }
}
