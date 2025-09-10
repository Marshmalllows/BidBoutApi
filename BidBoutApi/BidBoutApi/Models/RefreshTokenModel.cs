using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BidBoutApi.Models;

[Table("RefreshTokens")]
public class RefreshToken
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [ForeignKey("User")]
    public int UserId { get; set; }

    [Required]
    [Column(TypeName = "TEXT")]
    public string Token { get; set; }

    [Required]
    public DateTime ExpiresAt { get; set; }

    [MaxLength(20)]
    public string? DeviceType { get; set; }

    [MaxLength(50)]
    public string? Browser { get; set; }

    [MaxLength(50)]
    public string? Os { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public virtual User User { get; set; }
}