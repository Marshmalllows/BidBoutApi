using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BidBoutApi.Models;

[Table("BidsHistory")]
public class Bid
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("bid")] 
    public int Amount { get; set; }

    [Required]
    [Column("bidderId")]
    public int BidderId { get; set; }

    [ForeignKey(nameof(BidderId))]
    public User? Bidder { get; set; }

    [Required]
    [Column("lotId")]
    public int LotId { get; set; }

    [ForeignKey(nameof(LotId))]
    public Product? Lot { get; set; }

    [Column("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}