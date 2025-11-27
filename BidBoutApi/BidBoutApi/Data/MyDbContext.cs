using BidBoutApi.Models;
using Microsoft.EntityFrameworkCore;

namespace BidBoutApi.Data;

public class MyDbContext : DbContext
{
    public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    
    public DbSet<Category> Categories { get; set; }
    
    public DbSet<Product> Products { get; set; }
    
    public DbSet<Image> Images { get; set; }
    
    public DbSet<Bid> BidsHistory { get; set; }
    
    public DbSet<AutoBid> AutoBids { get; set; }
}