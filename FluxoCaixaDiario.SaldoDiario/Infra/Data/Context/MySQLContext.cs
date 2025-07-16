using FluxoCaixaDiario.SaldoDiario.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixaDiario.SaldoDiario.Infra.Data.Context
{
    public class MySQLContext : DbContext
    {
        public virtual DbSet<DailyBalance> DailyBalances { get; set; }

        public MySQLContext(DbContextOptions<MySQLContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DailyBalance>(entity =>
            {
                entity.HasKey(db => db.Date); // Data como chave primária (saldo único por dia)
                entity.Property(db => db.TotalCredit)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();

                entity.Property(db => db.TotalDebit)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();

                entity.Property(db => db.Balance)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();
            });
        }
    }
}