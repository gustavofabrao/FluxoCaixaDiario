using FluxoCaixaDiario.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixaDiario.Lancamentos.Tests.Infra.Data.Context
{
    public class MySQLContext : DbContext
    {
        public DbSet<Transaction> Transactions { get; set; }

        public MySQLContext(DbContextOptions<MySQLContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Amount)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();

                entity.Property(t => t.Type)
                      .IsRequired();

                entity.Property(t => t.Date)
                      .IsRequired();

                entity.Property(t => t.Description)
                      .HasMaxLength(500);
            });
        }
    }
}