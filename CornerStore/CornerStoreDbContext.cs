using Microsoft.EntityFrameworkCore;
using CornerStore.Models;
public class CornerStoreDbContext : DbContext
{

    public CornerStoreDbContext(DbContextOptions<CornerStoreDbContext> context) : base(context)
    {

    }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<OrderProduct> OrderProducts { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Cashier> Cashiers { get; set; }


    //allows us to configure the schema when migrating as well as seed data
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cashier>().HasData(new Cashier[]
        {
            new Cashier { Id = 1, FirstName = "Rick", LastName = "Flair" },
            new Cashier { Id = 2, FirstName = "Smooth", LastName = "Kev" },
            new Cashier { Id = 3, FirstName = "Ricardo", LastName = "Tessitori" }
        });
        modelBuilder.Entity<Order>().HasData(new Order[]
        {
            new Order { Id = 1, CashierId = 1, PaidOnDate = new DateTime(2025, 1, 1) },
            new Order { Id = 2, CashierId = 1, PaidOnDate = null },
            new Order { Id = 3, CashierId = 2, PaidOnDate = null }
        });
        modelBuilder.Entity<Product>().HasData(new Product[]
        {
            new Product { Id = 1, ProductName = "Cheese", Price = 3.00M, Brand = "Kraft", CategoryId = 1},
            new Product { Id = 2, ProductName = "Bread", Price = 2.00M, Brand = "Wonder", CategoryId = 1},
            new Product { Id = 3, ProductName = "Nacho Cheese Tortilla Chips", Price = 6.00M, Brand = "Doritos", CategoryId = 2},
            new Product { Id = 4, ProductName = "Oil Filter", Price = 4.00M, Brand = "Imperial", CategoryId = 3}
        });
        modelBuilder.Entity<Category>().HasData(new Category[]
        {
            new Category { Id = 1, CategoryName = "Cooking" },
            new Category { Id = 2, CategoryName = "Snacks" },
            new Category { Id = 3, CategoryName = "Auto" }
        });
        modelBuilder.Entity<OrderProduct>().HasData(new OrderProduct[]
        {
            new OrderProduct { Id = 1, ProductId = 1, OrderId = 1, Quantity = 1},
            new OrderProduct { Id = 2, ProductId = 2, OrderId = 1, Quantity = 1},
            new OrderProduct { Id = 3, ProductId = 3, OrderId = 2, Quantity = 2},
            new OrderProduct { Id = 4, ProductId = 3, OrderId = 3, Quantity = 1},
            new OrderProduct { Id = 5, ProductId = 4, OrderId = 3, Quantity = 1}
        });
    }
}