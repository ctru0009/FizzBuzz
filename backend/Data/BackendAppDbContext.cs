using backend.Models;
using Microsoft.EntityFrameworkCore;


namespace backend.Data
{
    public class BackendAppDbContext(DbContextOptions<BackendAppDbContext> options) : DbContext(options)
    {
        public virtual required DbSet<Rule> Rules { get; set; }

        public virtual required DbSet<Player> Players { get; set; }

        public virtual required DbSet<Game> Games { get; set; }

        public virtual required DbSet<Session> Sessions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Game>()
                .HasMany(g => g.Rules)
                .WithOne(r => r.Game)
                .HasForeignKey(r => r.GameId)
                .IsRequired();

            modelBuilder.Entity<Session>()
                .HasOne(s => s.Game)
                .WithMany(g => g.Sessions)
                .HasForeignKey(s => s.GameId)
                .IsRequired();

            modelBuilder.Entity<Session>()
                .HasOne(s => s.Player)
                .WithMany(p => p.Sessions)
                .HasForeignKey(s => s.PlayerId)
                .IsRequired();

            modelBuilder.Entity<Player>()
                .HasMany(p => p.Games)
                .WithOne(g => g.Player)
                .HasForeignKey(g => g.PlayerId)
                .IsRequired();
        }
    }
}
