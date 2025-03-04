using CornerStore.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using CornerStore.Models.DTOs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// allows passing datetimes without time zone data 
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// allows our api endpoints to access the database through Entity Framework Core and provides dummy value for testing
builder.Services.AddNpgsql<CornerStoreDbContext>(builder.Configuration["CornerStoreDbConnectionString"] ?? "testing");

// Set the JSON serializer options
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
///////////////////////////////////////////////////////////////////
//endpoints go here

app.MapPost("/cashiers", (CornerStoreDbContext db, Cashier cashier) => 
{
    db.Cashiers.Add(cashier);
    db.SaveChanges();
    return Results.Created($"/cashiers/{cashier.Id}", cashier);
});

app.MapGet("/cashiers/{id}", (CornerStoreDbContext db, int id) => 
{
    var cashier = db.Cashiers
        .Where(c => c.Id == id)
        .Select(c => new CashierDTO
        {
            Id = c.Id,
            FirstName = c.FirstName,
            LastName = c.LastName,
            FullName = c.FullName,
            Orders = db.Orders
                .Where(o => o.CashierId == id)
                .Include(o => o.OrderProducts)
                .ThenInclude(op => op.Product)
                .Select(o => new OrderDTO
                {
                    Id = o.Id,
                    CashierId = o.CashierId,
                    Total = o.Total,
                    PaidOnDate = o.PaidOnDate,
                    OrderProducts = o.OrderProducts
                        .Select(op => new OrderProductDTO
                        {
                            Id = op.Id,
                            ProductId = op.ProductId,
                            Product = new ProductDTO
                            {
                                Id = op.Product.Id,
                                ProductName = op.Product.ProductName,
                                Price = op.Product.Price,
                                Brand = op.Product.Brand,
                                CategoryId = op.Product.CategoryId
                            },
                            OrderId = op.OrderId,
                            Quantity = op.Quantity
                        }).ToList()
                }).ToList()
        })
        .FirstOrDefault();
    if (cashier == null)
    {
        return Results.NotFound();
    }
    return Results.Ok(cashier);
});


app.MapGet("/products", (CornerStoreDbContext db, string? search) =>
{
    IQueryable<Product> query = db.Products.Include(p => p.Category);

    if (!string.IsNullOrEmpty(search))
    {
        // Using EF.Functions.Like for case-insensitive search
        string queryLower = search.ToLower(); // Convert the search query to lowercase
        query = query.Where(p => 
            EF.Functions.Like(p.ProductName.ToLower(), $"%{queryLower}%") || 
            EF.Functions.Like(p.Category.CategoryName.ToLower(), $"%{queryLower}%"));
    }

    return Results.Ok(query.Select(p => new ProductDTO
    {
        Id = p.Id,
        ProductName = p.ProductName,
        Price = p.Price,
        Brand = p.Brand,
        CategoryId = p.CategoryId,
        Category = new CategoryDTO
        {
            Id = p.Category.Id,
            CategoryName = p.Category.CategoryName
        }
    }).ToList());
});


app.MapPost("/products", (CornerStoreDbContext db, Product product) => 
{
    db.Products.Add(product);
    db.SaveChanges();
    return Results.Created($"/products/{product.Id}", product);
});

app.MapPut("/products/{id}", (CornerStoreDbContext db, int id, Product product) => 
{
    Product productToUpdate = db.Products.SingleOrDefault(p => p.Id == id);
    if (productToUpdate == null)
    {
        return Results.NotFound();
    }
    productToUpdate.Id = product.Id;
    productToUpdate.ProductName = product.ProductName;
    productToUpdate.Price = product.Price;
    productToUpdate.Brand = product.Brand;
    productToUpdate.CategoryId = product.CategoryId;
    
    db.SaveChanges();
    return Results.NoContent();
});


app.MapDelete("/orders/{id}", (CornerStoreDbContext db, int id) => 
{
    Order order = db.Orders.SingleOrDefault(o => o.Id == id);
    if (order == null)
    {
        return Results.NotFound();
    }
    db.Orders.Remove(order);
    db.SaveChanges();
    return Results.NoContent();
});


////////////////////////////////////////////////////////////////////////////////
app.MapPost("/orders", (CornerStoreDbContext db, Order order) =>
{
    foreach (OrderProduct op in order.OrderProducts)
    {
        Product product = db.Products.Find(op.ProductId);
        if (product == null)
        {
            return Results.BadRequest();
        }
    }

    Order newOrder = new Order
    {
        CashierId = order.CashierId,
        OrderProducts = order.OrderProducts.Select(op => new OrderProduct
        {
            ProductId = op.ProductId,
            Quantity = op.Quantity
        }).ToList(),
        PaidOnDate = order.PaidOnDate
    };

    db.Orders.Add(newOrder);
    db.SaveChanges();
    return Results.Created($"/orders/{newOrder.Id}", newOrder);
});

app.MapGet("/orders", (CornerStoreDbContext db, string orderDate) => 
{
    IQueryable<Order> query = db.Orders
        .Include(o => o.Cashier)
        .Include(o => o.OrderProducts)
        .ThenInclude(op => op.Product)
        .ThenInclude(p => p.Category);        

    if (!string.IsNullOrEmpty(orderDate))
    {
        var parsedDate = DateTime.Parse(orderDate);
        query = query.Where(o => o.PaidOnDate == parsedDate);
    }

    return query
        .Select(o => new OrderDTO
        {
            Id = o.Id,
            CashierId = o.CashierId,
            Cashier = new CashierDTO
            {
                Id = o.Cashier.Id,
                FirstName = o.Cashier.FirstName,
                LastName = o.Cashier.LastName,
                FullName = o.Cashier.FullName
            },
            Total = o.Total,
            PaidOnDate = o.PaidOnDate
        }).ToList();
});
///////////////////////////////////////////////////////////////////////////////


app.MapGet("/orders/{id}", (CornerStoreDbContext db, int id) => 
{
    var order = db.Orders
        .Where(o => o.Id == id)
        .Include(o => o.Cashier)
        .Include(o => o.OrderProducts)
        .ThenInclude(op => op.Product)
        .ThenInclude(p => p.Category)
        .Select(o => new OrderDTO
        {
            Id = o.Id,
            CashierId = o.CashierId,
            Cashier = new CashierDTO
            {
                Id = o.Cashier.Id,
                FirstName = o.Cashier.FirstName,
                LastName = o.Cashier.LastName,
                FullName = o.Cashier.FullName
            },
            Total = o.Total,
            PaidOnDate = o.PaidOnDate,
            OrderProducts = o.OrderProducts
                .Select(op => new OrderProductDTO
                {
                    Id = op.Id,
                    ProductId = op.ProductId,
                    Product = new ProductDTO
                        {
                            Id = op.Product.Id,
                            ProductName = op.Product.ProductName,
                            Price = op.Product.Price,
                            Brand = op.Product.Brand,
                            CategoryId = op.Product.CategoryId,
                            Category = new CategoryDTO
                            {
                                Id = op.Product.Category.Id,
                                CategoryName = op.Product.Category.CategoryName
                            } 
                        },
                    OrderId = op.OrderId,
                    Quantity = op.Quantity
                }).ToList()
        })
        .FirstOrDefault();
    if (order == null)
    {
        return Results.NotFound();
    }
    return Results.Ok(order);
});










////////////////////////////////////////////////////////////////////
app.Run();

//don't move or change this!
public partial class Program { }