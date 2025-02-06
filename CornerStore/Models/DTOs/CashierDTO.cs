namespace CornerStore.Models.DTOs;

public class CashierDTO
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string FullName { get; set; }
    public List<OrderDTO> Orders { get; set; }

}