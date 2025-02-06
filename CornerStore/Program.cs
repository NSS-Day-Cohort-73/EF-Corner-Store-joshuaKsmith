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

app.MapPost("/api/cashiers", (CornerStoreDbContext db, Cashier cashier) => 
{
    db.Cashiers.Add(cashier);
    db.SaveChanges();
    return Results.Created($"/api/cashiers/{cashier.Id}", cashier);
});

app.MapGet("/api/cashiers/{id}", (CornerStoreDbContext db, int id) => 
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


app.MapGet("/api/products", (CornerStoreDbContext db, string? q) =>
{
    IQueryable<Product> query = db.Products.Include(p => p.Category);

    if (!string.IsNullOrEmpty(q))
    {
        // Using EF.Functions.Like for case-insensitive search
        string queryLower = q.ToLower(); // Convert the search query to lowercase
        query = query.Where(p => 
            EF.Functions.Like(p.ProductName.ToLower(), $"%{queryLower}%") || 
            EF.Functions.Like(p.Category.CategoryName.ToLower(), $"%{queryLower}%"));
    }

    return query.Select(p => new ProductDTO
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
    }).ToList();
});






////////////////////////////////////////////////////////////////////
app.Run();

//don't move or change this!
public partial class Program { }