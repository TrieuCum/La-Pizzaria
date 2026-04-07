using System.Collections.Generic;
using LaPizzaria.Models;
using LaPizzaria.Services;
using Xunit;

namespace LaPizzaria.Tests
{
    public class DiscountTests
    {
        [Fact]
        public void Discount_Should_Not_Be_Negative()
        {
            var comboService = new ComboService();

            var orderDetails = new List<OrderDetail>
            {
                new OrderDetail{ ProductId = 1, Quantity = 1, UnitPrice = 100m }
            };

            var combos = new List<Combo>
            {
                new Combo { Id = 1, Name = "Crazy", IsActive = true, DiscountAmount = -1000m }
            };

            var res = comboService.SuggestBestCombos(orderDetails, combos);

            Assert.True(res.DiscountTotal >= 0);
        }
    }
}


