using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BidBoutApi.Models;

[Table("AutoBids")]
public class AutoBid
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column("lotId")]
    public int LotId { get; set; }
    
    [Required]
    [Column("bidderId")]
    public int UserId { get; set; }
    
    [Required]
    public int MaxAmount { get; set; } 
    
    [Column("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}