using BidBoutApi.Models;
using Microsoft.EntityFrameworkCore;

namespace BidBoutApi.Data;

public class MyDbContext : DbContext
{
    public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
}