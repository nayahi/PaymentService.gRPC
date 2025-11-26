using Microsoft.EntityFrameworkCore;
using PaymentService.gRPC.Data;
using PaymentService.gRPC.Models;

namespace PaymentService.gRPC.Data
{
    /// <summary>
    /// Inicializador de base de datos con datos de prueba
    /// </summary>
    public static class DbInitializer
    {
        public static async Task InitializeAsync(PaymentDbContext context, ILogger logger)
        {
            try
            {
                // Asegurar que la base de datos esté creada
                logger.LogInformation("Verificando existencia de base de datos...");
                await context.Database.EnsureCreatedAsync();

                // Aplicar migraciones pendientes
                if (context.Database.GetPendingMigrations().Any())
                {
                    logger.LogInformation("Aplicando migraciones pendientes...");
                    await context.Database.MigrateAsync();
                }

                // Verificar si ya existen productos
                if (await context.Payments.AnyAsync())
                {
                    logger.LogInformation("Base de datos ya contiene usuarios. Omitiendo inicialización.");
                    return;
                }

                logger.LogInformation("Inicializando datos de prueba para PaymentService...");

                // Pagos de prueba
                var payments = new List<Payment>
                {
                    // Pago 1: Completado - Order 1 (UserId 2)
                    new Payment
                    {
                        OrderId = 1,
                        UserId = 2,
                        Amount = 1499.98m,
                        Currency = "USD",
                        PaymentMethod = Models.PaymentMethod.CreditCard,
                        Status = PaymentStatus.Completed,
                        TransactionId = "TXN-20241120-001",
                        CardLastFourDigits = "4532",
                        CreatedAt = DateTime.UtcNow.AddDays(-5),
                        CompletedAt = DateTime.UtcNow.AddDays(-5).AddMinutes(2)
                    },

                    // Pago 2: Completado - Order 2 (UserId 3)
                    new Payment
                    {
                        OrderId = 2,
                        UserId = 3,
                        Amount = 989.97m,
                        Currency = "USD",
                        PaymentMethod = Models.PaymentMethod.DebitCard,
                        Status = PaymentStatus.Completed,
                        TransactionId = "TXN-20241123-002",
                        CardLastFourDigits = "8765",
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        CompletedAt = DateTime.UtcNow.AddDays(-2).AddMinutes(1)
                    },

                    // Pago 3: Pendiente - Order 3 (UserId 2) - PARA PRUEBAS INMEDIATAS
                    new Payment
                    {
                        OrderId = 3,
                        UserId = 2,
                        Amount = 449.99m,
                        Currency = "USD",
                        PaymentMethod = Models.PaymentMethod.CreditCard,
                        Status = PaymentStatus.Pending,
                        TransactionId = "TXN-20241125-003",
                        CardLastFourDigits = "4532",
                        CreatedAt = DateTime.UtcNow.AddHours(-2)
                    },

                    // Pago 4: Fallido - Order 4 (UserId 1)
                    new Payment
                    {
                        OrderId = 4,
                        UserId = 1,
                        Amount = 189.99m,
                        Currency = "USD",
                        PaymentMethod = Models.PaymentMethod.CreditCard,
                        Status = PaymentStatus.Failed,
                        TransactionId = "TXN-20241115-004",
                        CardLastFourDigits = "9999",
                        FailureReason = "Insufficient funds",
                        CreatedAt = DateTime.UtcNow.AddDays(-10),
                        CompletedAt = DateTime.UtcNow.AddDays(-10).AddMinutes(1)
                    },

                    // Pago 5: Completado y luego Reembolsado - Order 5
                    new Payment
                    {
                        OrderId = 5,
                        UserId = 2,
                        Amount = 299.99m,
                        Currency = "USD",
                        PaymentMethod = Models.PaymentMethod.PayPal,
                        Status = PaymentStatus.Refunded,
                        TransactionId = "TXN-20241118-005",
                        CreatedAt = DateTime.UtcNow.AddDays(-7),
                        CompletedAt = DateTime.UtcNow.AddDays(-7).AddMinutes(1),
                        RefundedAt = DateTime.UtcNow.AddDays(-6),
                        RefundedAmount = 299.99m,
                        RefundReason = "Customer requested cancellation"
                    },

                    // Pago 6: Completado - BankTransfer (UserId 3)
                    new Payment
                    {
                        OrderId = 6,
                        UserId = 3,
                        Amount = 1899.99m,
                        Currency = "USD",
                        PaymentMethod = Models.PaymentMethod.BankTransfer,
                        Status = PaymentStatus.Completed,
                        TransactionId = "TXN-20241122-006",
                        CreatedAt = DateTime.UtcNow.AddDays(-3),
                        CompletedAt = DateTime.UtcNow.AddDays(-3).AddHours(24) // Transferencia tarda más
                    }
                };

                await context.Payments.AddRangeAsync(payments);
                await context.SaveChangesAsync();

                logger.LogInformation("✓ {Count} pagos de prueba creados exitosamente", payments.Count);
                logger.LogInformation("  • Pago 1: Completado - $1499.98 (CreditCard)");
                logger.LogInformation("  • Pago 2: Completado - $989.97 (DebitCard)");
                logger.LogInformation("  • Pago 3: Pendiente - $449.99 (CreditCard) - PARA PRUEBAS");
                logger.LogInformation("  • Pago 4: Fallido - $189.99 (Insufficient funds)");
                logger.LogInformation("  • Pago 5: Reembolsado - $299.99 (PayPal)");
                logger.LogInformation("  • Pago 6: Completado - $1899.99 (BankTransfer)");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al inicializar la base de datos de pagos");
                throw;
            }
        }
    }
}