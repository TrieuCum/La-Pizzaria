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
        public DbSet<ProductIngredient> ProductIngredients { get; set; }
        public DbSet<Combo> Combos { get; set; }
        public DbSet<ComboItem> ComboItems { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
        }
    }
}
