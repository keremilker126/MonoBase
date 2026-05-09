using Microsoft.EntityFrameworkCore;
using MonoBase.Models;

namespace MonoBase.Data;
public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options) { }
    
    // Kullanıcının "NoSQL" mantığındaki verileri burada duracak
    public DbSet<DynamicEntry> Collections { get; set; }
}