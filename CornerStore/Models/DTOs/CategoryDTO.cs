namespace CornerStore.Models.DTOs;

public class CategoryDTO
{
    public int Id { get; set; }
    public string CategoryName { get; set; }
    public List<ProductDTO> Products { get; set; }
}