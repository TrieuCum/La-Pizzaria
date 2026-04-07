using System.Collections.Generic;
using LaPizzaria.Models;

namespace LaPizzaria.Services
{
    public interface IComboService
    {
        ComboSuggestionResult SuggestBestCombos(IEnumerable<OrderDetail> orderDetails, IEnumerable<Combo> availableCombos);
    }

    public class ComboSuggestionResult
    {
        public decimal DiscountTotal { get; set; }
        public List<AppliedCombo> AppliedCombos { get; set; } = new();
    }

    public class AppliedCombo
    {
        public int ComboId { get; set; }
        public string ComboName { get; set; } = string.Empty;
        public decimal DiscountAmount { get; set; }
    }
}


