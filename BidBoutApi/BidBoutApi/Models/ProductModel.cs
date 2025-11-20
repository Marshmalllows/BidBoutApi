using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BidBoutApi.Models;

[Table("Products")]
public class Product
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int CreatorId { get; set; }
    
    [Required]
    public int CategoryId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Title { get; set; }

    public int? ReservePrice { get; set; }

    [Required]
    [MaxLength(50)]
    public string PickupPlace { get; set; }

    public string? Description { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public User Creator { get; set; }
    
    public Category Category { get; set; }
    
    public ICollection<Image> Images { get; set; } = new List<Image>();
}