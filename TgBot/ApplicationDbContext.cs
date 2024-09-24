using Microsoft.EntityFrameworkCore;

namespace TgBot;

public class ApplicationDbContext : DbContext
{
    public DbSet<Electrocity> Electrocities { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseMySql("server=localhost;uid=root;pwd=Pashok0103;database=peskostruyka",
        new MySqlServerVersion(new Version(8, 0, 22)));
    }
}