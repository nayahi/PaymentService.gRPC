using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaymentService.gRPC.Models
{

    /// <summary>
    /// Entidad que representa un pago en el sistema
    /// </summary>
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(10)]
        public string Currency { get; set; } = "USD";

        [Required]
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        [MaxLength(100)]
        public string TransactionId { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? FailureReason { get; set; }

        [MaxLength(4)]
        public string? CardLastFourDigits { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        public DateTime? RefundedAt { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? RefundedAmount { get; set; }

        [MaxLength(500)]
        public string? RefundReason { get; set; }
    }

    /// <summary>
    /// Enum para estados de pago
    /// </summary>
    public static class PaymentStatus
    {
        public const string Pending = "Pending";
        public const string Completed = "Completed";
        public const string Failed = "Failed";
        public const string Refunded = "Refunded";
    }

    /// <summary>
    /// Enum para métodos de pago
    /// </summary>
    public static class PaymentMethod
    {
        public const string CreditCard = "CreditCard";
        public const string DebitCard = "DebitCard";
        public const string PayPal = "PayPal";
        public const string BankTransfer = "BankTransfer";
    }
}
