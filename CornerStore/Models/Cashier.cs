using System.ComponentModel.DataAnnotations;

namespace CornerStore.Models;


public class Cashier
{
    public int Id { get; set; }
    [Required]
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string FullName => FirstName + " " + LastName;
    public List<Order> Orders { get; set; }

}