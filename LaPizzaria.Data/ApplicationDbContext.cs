using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using LaPizzaria.Models;

namespace LaPizzaria.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Topping> Toppings { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<ProductTopping> ProductTopping { get; set; }
        public DbSet<OrderDetailTopping> OrderDetailTopping { get; set; }
        public DbSet<Table> Tables { get; set; }
        public DbSet<OrderTable> OrderTables { get; set; }
        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<IngredientCategory> IngredientCategories { get; set; }
        public DbSet<ProductIngredient> ProductIngredients { get; set; }
        public DbSet<Combo> Combos { get; set; }
        public DbSet<ComboItem> ComboItems { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }
		public DbSet<Voucher> Vouchers { get; set; }
		public DbSet<OrderVoucher> OrderVouchers { get; set; }
		public DbSet<UserSavedVoucher> UserSavedVouchers { get; set; }
		public DbSet<Employee> Employees { get; set; }
        public DbSet<LoyaltyTransaction> LoyaltyTransactions { get; set; }

        public DbSet<Customer> Customers { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Decimal precisions
            modelBuilder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
            modelBuilder.Entity<Product>().Property(p => p.ProfitMarginPercent).HasPrecision(18, 2);
            modelBuilder.Entity<Topping>().Property(t => t.Price).HasPrecision(18, 2);
            modelBuilder.Entity<Order>().Property(o => o.TotalPrice).HasPrecision(18, 2);
            modelBuilder.Entity<Order>().Property(o => o.ShipperLocationIp).HasMaxLength(100);
            modelBuilder.Entity<OrderDetail>().Property(od => od.UnitPrice).HasPrecision(18, 2);
            modelBuilder.Entity<OrderDetail>().Property(od => od.Subtotal).HasPrecision(18, 2);
            modelBuilder.Entity<Invoice>().Property(i => i.Subtotal).HasPrecision(18, 2);
            modelBuilder.Entity<Invoice>().Property(i => i.DiscountTotal).HasPrecision(18, 2);
            modelBuilder.Entity<Invoice>().Property(i => i.TaxTotal).HasPrecision(18, 2);
            modelBuilder.Entity<Invoice>().Property(i => i.GrandTotal).HasPrecision(18, 2);
            modelBuilder.Entity<InvoiceItem>().Property(ii => ii.UnitPrice).HasPrecision(18, 2);
            modelBuilder.Entity<InvoiceItem>().Property(ii => ii.Discount).HasPrecision(18, 2);
            modelBuilder.Entity<InvoiceItem>().Property(ii => ii.Total).HasPrecision(18, 2);
            modelBuilder.Entity<Ingredient>().Property(i => i.StockQuantity).HasPrecision(18, 2);
            modelBuilder.Entity<Ingredient>().Property(i => i.ReorderLevel).HasPrecision(18, 2);
            modelBuilder.Entity<Ingredient>().Property(i => i.UnitPrice).HasPrecision(18, 2);
            modelBuilder.Entity<Combo>().Property(c => c.DiscountAmount).HasPrecision(18, 2);
            modelBuilder.Entity<Combo>().Property(c => c.DiscountPercent).HasPrecision(18, 2);
            modelBuilder.Entity<ComboItem>().Property(ci => ci.ItemDiscountAmount).HasPrecision(18, 2);
            modelBuilder.Entity<ComboItem>().Property(ci => ci.ItemDiscountPercent).HasPrecision(18, 2);
            modelBuilder.Entity<Employee>().Property(e => e.Salary).HasPrecision(18, 2);

            // Order-User relationship
            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId);

            // Order-Shipper relationship
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Shipper)
                .WithMany()
                .HasForeignKey(o => o.ShipperId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure many-to-many for ProductTopping
            modelBuilder.Entity<ProductTopping>()
                .HasKey(pt => new { pt.ProductId, pt.ToppingId });
            modelBuilder.Entity<ProductTopping>()
                .HasOne(pt => pt.Product)
                .WithMany(p => p.ProductToppings)
                .HasForeignKey(pt => pt.ProductId);
            modelBuilder.Entity<ProductTopping>()
                .HasOne(pt => pt.Topping)
                .WithMany(t => t.ProductToppings)
                .HasForeignKey(pt => pt.ToppingId);

            // Configure many-to-many for OrderDetailTopping
            modelBuilder.Entity<OrderDetailTopping>()
                .HasKey(odt => new { odt.OrderDetailId, odt.ToppingId });
            modelBuilder.Entity<OrderDetailTopping>()
                .HasOne(odt => odt.OrderDetail)
                .WithMany(od => od.OrderDetailToppings)
                .HasForeignKey(odt => odt.OrderDetailId);
            modelBuilder.Entity<OrderDetailTopping>()
                .HasOne(odt => odt.Topping)
                .WithMany(t => t.OrderDetailToppings)
                .HasForeignKey(odt => odt.ToppingId);

            // Configure many-to-many for Order-Table (merge tables)
            modelBuilder.Entity<OrderTable>()
                .HasKey(ot => new { ot.OrderId, ot.TableId });
            modelBuilder.Entity<OrderTable>()
                .HasOne(ot => ot.Order)
                .WithMany(o => o.OrderTables)
                .HasForeignKey(ot => ot.OrderId);
            modelBuilder.Entity<OrderTable>()
                .HasOne(ot => ot.Table)
                .WithMany(t => t.OrderTables)
                .HasForeignKey(ot => ot.TableId);

            // Configure ProductIngredient mapping
            modelBuilder.Entity<ProductIngredient>()
                .HasKey(pi => new { pi.ProductId, pi.IngredientId });
            modelBuilder.Entity<ProductIngredient>()
                .HasOne(pi => pi.Product)
                .WithMany(p => p.ProductIngredients)
                .HasForeignKey(pi => pi.ProductId);
            modelBuilder.Entity<ProductIngredient>()
                .HasOne(pi => pi.Ingredient)
                .WithMany(i => i.ProductIngredients)
                .HasForeignKey(pi => pi.IngredientId);

            modelBuilder.Entity<ProductIngredient>().Property(pi => pi.QuantityPerUnit).HasPrecision(18, 2);

            modelBuilder.Entity<IngredientCategory>()
                .HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ingredient>()
                .HasOne(i => i.Category)
                .WithMany(c => c.Ingredients)
                .HasForeignKey(i => i.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Combo-Items
            modelBuilder.Entity<ComboItem>()
                .HasOne(ci => ci.Combo)
                .WithMany(c => c.Items)
                .HasForeignKey(ci => ci.ComboId);
            modelBuilder.Entity<ComboItem>()
                .HasOne(ci => ci.Product)
                .WithMany()
                .HasForeignKey(ci => ci.ProductId);

            // Configure Invoice-Items
            modelBuilder.Entity<InvoiceItem>()
                .HasOne(ii => ii.Invoice)
                .WithMany(i => i.Items)
                .HasForeignKey(ii => ii.InvoiceId);
            modelBuilder.Entity<InvoiceItem>()
                .HasOne(ii => ii.OrderDetail)
                .WithMany()
                .HasForeignKey(ii => ii.OrderDetailId)
                .OnDelete(DeleteBehavior.Restrict); // prevent multiple cascade paths via Order -> OrderDetails and Order -> Invoices

			// Configure Order-Voucher mapping (each order can apply up to 2 vouchers)
			modelBuilder.Entity<OrderVoucher>()
				.HasKey(ov => new { ov.OrderId, ov.VoucherId });
			modelBuilder.Entity<OrderVoucher>()
				.HasOne(ov => ov.Order)
				.WithMany(o => o.OrderVouchers)
				.HasForeignKey(ov => ov.OrderId);
			modelBuilder.Entity<OrderVoucher>()
				.HasOne(ov => ov.Voucher)
				.WithMany()
				.HasForeignKey(ov => ov.VoucherId);

            modelBuilder.Entity<Voucher>().Property(v => v.DiscountPercent).HasPrecision(18, 2);
            modelBuilder.Entity<Voucher>().Property(v => v.DiscountAmount).HasPrecision(18, 2);
            modelBuilder.Entity<Voucher>().Property(v => v.MinOrderValue).HasPrecision(18, 2);

            modelBuilder.Entity<Voucher>()
                .HasOne(v => v.TargetUser)
                .WithMany()
                .HasForeignKey(v => v.TargetUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // UserSavedVoucher: user đã lưu voucher vào tài khoản
            modelBuilder.Entity<UserSavedVoucher>()
                .HasKey(usv => new { usv.UserId, usv.VoucherId });
            modelBuilder.Entity<UserSavedVoucher>()
                .HasOne(usv => usv.User)
                .WithMany(u => u.UserSavedVouchers)
                .HasForeignKey(usv => usv.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<UserSavedVoucher>()
                .HasOne(usv => usv.Voucher)
                .WithMany()
                .HasForeignKey(usv => usv.VoucherId)
                .OnDelete(DeleteBehavior.Cascade);

            // Customer -> ApplicationUser (UserId)
            modelBuilder.Entity<Customer>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Loyalty transactions
            modelBuilder.Entity<LoyaltyTransaction>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<LoyaltyTransaction>()
                .HasIndex(t => new { t.UserId, t.CreatedAtUtc });
            modelBuilder.Entity<LoyaltyTransaction>()
                .HasIndex(t => new { t.UserId, t.ReferenceCode })
                .IsUnique()
                .HasFilter("[ReferenceCode] IS NOT NULL");

            // OrderDetail relationships
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Product)
                .WithMany(p => p.OrderDetails)
                .HasForeignKey(od => od.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Product2)
                .WithMany()
                .HasForeignKey(od => od.ProductId2)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
