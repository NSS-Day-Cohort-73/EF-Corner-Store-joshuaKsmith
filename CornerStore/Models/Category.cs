using System.ComponentModel.DataAnnotations;

namespace CornerStore.Models;

public class Category
{
    public int Id { get; set; }
    [Required]
    public string CategoryName { get; set; }
    public List<Product> Products { get; set; }
}