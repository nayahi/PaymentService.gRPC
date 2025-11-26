using System.Collections.Generic;
using System.Reflection.Emit;
using global::PaymentService.gRPC.Models;
using Microsoft.EntityFrameworkCore;
using PaymentService.gRPC.Models;

namespace PaymentService.gRPC.Data
{
    /// <summary>
    /// Contexto de base de datos para el servicio de pagos
    /// </summary>
    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions<PaymentDbContext> options)
            : base(options)
        {
        }

        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de la entidad Payment
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.ToTable("Payments");

                entity.HasKey(e => e.PaymentId);

                entity.Property(e => e.PaymentId)
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.OrderId)
                    .IsRequired();

                entity.Property(e => e.UserId)
                    .IsRequired();

                entity.Property(e => e.Amount)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.Property(e => e.Currency)
                    .HasMaxLength(10)
                    .IsRequired()
                    .HasDefaultValue("USD");

                entity.Property(e => e.PaymentMethod)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.Status)
                    .HasMaxLength(20)
                    .IsRequired()
                    .HasDefaultValue("Pending");

                entity.Property(e => e.TransactionId)
                    .HasMaxLength(100);

                entity.Property(e => e.FailureReason)
                    .HasMaxLength(500);

                entity.Property(e => e.CardLastFourDigits)
                    .HasMaxLength(4);

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.CompletedAt);

                entity.Property(e => e.RefundedAt);

                entity.Property(e => e.RefundedAmount)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.RefundReason)
                    .HasMaxLength(500);

                // Índices
                entity.HasIndex(e => e.OrderId)
                    .HasDatabaseName("IX_Payments_OrderId");

                entity.HasIndex(e => e.UserId)
                    .HasDatabaseName("IX_Payments_UserId");

                entity.HasIndex(e => e.Status)
                    .HasDatabaseName("IX_Payments_Status");

                entity.HasIndex(e => e.TransactionId)
                    .HasDatabaseName("IX_Payments_TransactionId");

                entity.HasIndex(e => e.CreatedAt)
                    .HasDatabaseName("IX_Payments_CreatedAt");
            });
        }
    }
}
