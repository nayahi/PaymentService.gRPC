using ECommerceGRPC.PaymentService;
using global::PaymentService.gRPC.Models;
using Google.Protobuf.WellKnownTypes;
using PaymentService.gRPC.Models;

namespace PaymentService.gRPC.Mappers
{

    /// <summary>
    /// Mapeador entre entidades Payment y mensajes Protocol Buffers
    /// </summary>
    public static class PaymentMapper
    {
        /// <summary>
        /// Convierte una entidad Payment a PaymentResponse
        /// </summary>
        public static PaymentResponse ToPaymentResponse(Payment payment)
        {
            var response = new PaymentResponse
            {
                PaymentId = payment.PaymentId,
                OrderId = payment.OrderId,
                UserId = payment.UserId,
                Amount = (double)payment.Amount,
                Currency = payment.Currency,
                PaymentMethod = payment.PaymentMethod,
                Status = payment.Status,
                TransactionId = payment.TransactionId,
                CreatedAt = payment.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };

            // Campos opcionales
            if (!string.IsNullOrEmpty(payment.FailureReason))
                response.FailureReason = payment.FailureReason;

            if (!string.IsNullOrEmpty(payment.CardLastFourDigits))
                response.CardLastFourDigits = payment.CardLastFourDigits;

            if (payment.CompletedAt.HasValue)
                response.CompletedAt = payment.CompletedAt.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            return response;
        }

        /// <summary>
        /// Convierte ProcessPaymentRequest a entidad Payment
        /// </summary>
        public static Payment ToPaymentEntity(ProcessPaymentRequest request)
        {
            return new Payment
            {
                OrderId = request.OrderId,
                UserId = request.UserId,
                Amount = (decimal)request.Amount,
                Currency = request.Currency,
                PaymentMethod = request.PaymentMethod,
                Status = PaymentStatus.Pending,
                CardLastFourDigits = !string.IsNullOrEmpty(request.CardLastFourDigits)
                    ? request.CardLastFourDigits
                    : null,
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Convierte una lista de Payment a PaymentHistoryResponse
        /// </summary>
        public static PaymentHistoryResponse ToPaymentHistoryResponse(List<Payment> payments)
        {
            var response = new PaymentHistoryResponse
            {
                TotalCount = payments.Count
            };

            foreach (var payment in payments)
            {
                response.Payments.Add(ToPaymentResponse(payment));
            }

            return response;
        }
    }
}
