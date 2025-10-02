using System.Collections.Generic;
using LaPizzaria.Models;
using LaPizzaria.Services;
using Xunit;

namespace LaPizzaria.Tests
{
    public class InventoryQrComboTests
    {
        [Fact]
        public void Combo_Basic_Logic_Applies_Discount()
        {
            var comboService = new ComboService();
            var orderDetails = new List<OrderDetail>
            {
                new OrderDetail{ ProductId = 1, Quantity = 2, UnitPrice = 50m },
                new OrderDetail{ ProductId = 2, Quantity = 1, UnitPrice = 100m }
            };
            var combo = new Combo
            {
                Id = 1, Name = "2xP1+1xP2", IsActive = true, DiscountAmount = 20m,
                Items = new List<ComboItem>
                {
                    new ComboItem{ ProductId = 1, MinQuantity = 2 },
                    new ComboItem{ ProductId = 2, MinQuantity = 1 }
                }
            };

            var res = comboService.SuggestBestCombos(orderDetails, new[] { combo });
            Assert.True(res.DiscountTotal >= 20m);
        }

        [Fact]
        public void Qr_Sync_Payload_Format()
        {
            var qr = new QrService();
            var payload = qr.GenerateTableQrPayload("T01");
            Assert.Contains("\"code\":\"T01\"", payload);
        }
    }
}


