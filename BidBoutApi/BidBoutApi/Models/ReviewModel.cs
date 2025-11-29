using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BidBoutApi.Models;

[Table("Reviews")]
public class Review
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int ReviewerId { get; set; }
    [ForeignKey(nameof(ReviewerId))]
    public User Reviewer { get; set; } = null!;

    [Required]
    public int TargetUserId { get; set; }
    [ForeignKey(nameof(TargetUserId))]
    public User TargetUser { get; set; } = null!;

    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}