using ECommerceGRPC.PaymentService;
using FluentValidation;
using global::PaymentService.gRPC.Data;
using global::PaymentService.gRPC.Mappers;
using global::PaymentService.gRPC.Models;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using PaymentService.gRPC.Data;
using PaymentService.gRPC.Mappers;
using PaymentService.gRPC.Models;


namespace PaymentService.gRPC.Services
{

    /// <summary>
    /// Implementación del servicio gRPC de pagos con simulación de gateway
    /// Mock: 10% de pagos fallan aleatoriamente para testing de saga
    /// </summary>
    public class PaymentGrpcService : ECommerceGRPC.PaymentService.PaymentService.PaymentServiceBase
    {
        private readonly PaymentDbContext _context;
        private readonly ILogger<PaymentGrpcService> _logger;
        private readonly IValidator<ProcessPaymentRequest> _processValidator;
        private readonly IValidator<RefundPaymentRequest> _refundValidator;
        private readonly IValidator<GetPaymentStatusRequest> _statusValidator;
        private readonly IValidator<GetPaymentHistoryRequest> _historyValidator;
        private readonly IValidator<GetPaymentRequest> _getValidator;
        private static readonly Random _random = new Random();

        public PaymentGrpcService(
            PaymentDbContext context,
            ILogger<PaymentGrpcService> logger,
            IValidator<ProcessPaymentRequest> processValidator,
            IValidator<RefundPaymentRequest> refundValidator,
            IValidator<GetPaymentStatusRequest> statusValidator,
            IValidator<GetPaymentHistoryRequest> historyValidator,
            IValidator<GetPaymentRequest> getValidator)
        {
            _context = context;
            _logger = logger;
            _processValidator = processValidator;
            _refundValidator = refundValidator;
            _statusValidator = statusValidator;
            _historyValidator = historyValidator;
            _getValidator = getValidator;
        }

        /// <summary>
        /// Procesa un pago - Mock con 10% de probabilidad de fallo
        /// </summary>
        public override async Task<PaymentResponse> ProcessPayment(
            ProcessPaymentRequest request,
            ServerCallContext context)
        {
            _logger.LogInformation("Procesando pago para Order {OrderId}, Amount: ${Amount}",
                request.OrderId, request.Amount);

            // Validar request
            var validationResult = await _processValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Validación fallida para ProcessPayment: {Errors}", errors);
                throw new RpcException(new Status(StatusCode.InvalidArgument, errors));
            }

            try
            {
                // Crear entidad de pago
                var payment = PaymentMapper.ToPaymentEntity(request);

                // Simular procesamiento de pago (agregar delay realista)
                await Task.Delay(_random.Next(500, 1500));

                // Mock: 10% de probabilidad de fallo para testing de saga
                var shouldFail = _random.Next(1, 101) <= 10;

                if (shouldFail)
                {
                    // Simular fallo de pago
                    payment.Status = PaymentStatus.Failed;
                    payment.FailureReason = GetRandomFailureReason();
                    payment.CompletedAt = DateTime.UtcNow;
                    payment.TransactionId = $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{_random.Next(1000, 9999)}-FAIL";

                    _logger.LogWarning(
                        "💳 Pago FALLIDO (simulado) - Order {OrderId}, Reason: {Reason}",
                        request.OrderId, payment.FailureReason);
                }
                else
                {
                    // Pago exitoso
                    payment.Status = PaymentStatus.Completed;
                    payment.CompletedAt = DateTime.UtcNow;
                    payment.TransactionId = $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{_random.Next(1000, 9999)}-OK";

                    _logger.LogInformation(
                        "✓ Pago procesado exitosamente - Order {OrderId}, TransactionId: {TxnId}",
                        request.OrderId, payment.TransactionId);
                }

                // Guardar en base de datos
                await _context.Payments.AddAsync(payment);
                await _context.SaveChangesAsync();

                return PaymentMapper.ToPaymentResponse(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar pago para Order {OrderId}", request.OrderId);
                throw new RpcException(new Status(StatusCode.Internal,
                    $"Error interno al procesar el pago: {ex.Message}"));
            }
        }

        /// <summary>
        /// Reembolsa un pago
        /// </summary>
        public override async Task<PaymentResponse> RefundPayment(
            RefundPaymentRequest request,
            ServerCallContext context)
        {
            _logger.LogInformation("Procesando reembolso para Payment {PaymentId}, Amount: ${Amount}",
                request.PaymentId, request.Amount);

            // Validar request
            var validationResult = await _refundValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Validación fallida para RefundPayment: {Errors}", errors);
                throw new RpcException(new Status(StatusCode.InvalidArgument, errors));
            }

            try
            {
                // Buscar pago
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.PaymentId == request.PaymentId);

                if (payment == null)
                {
                    _logger.LogWarning("Pago {PaymentId} no encontrado", request.PaymentId);
                    throw new RpcException(new Status(StatusCode.NotFound,
                        $"Pago con ID {request.PaymentId} no encontrado"));
                }

                // Verificar que el pago esté completado
                if (payment.Status != PaymentStatus.Completed)
                {
                    _logger.LogWarning("Intento de reembolso de pago no completado: {PaymentId}", request.PaymentId);
                    throw new RpcException(new Status(StatusCode.FailedPrecondition,
                        $"Solo se pueden reembolsar pagos completados. Estado actual: {payment.Status}"));
                }

                // Verificar que el monto no exceda el monto original
                if ((decimal)request.Amount > payment.Amount)
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument,
                        $"El monto del reembolso (${request.Amount}) no puede exceder el monto original (${payment.Amount})"));
                }

                // Simular procesamiento de reembolso
                await Task.Delay(_random.Next(300, 800));

                // Actualizar el pago
                payment.Status = PaymentStatus.Refunded;
                payment.RefundedAt = DateTime.UtcNow;
                payment.RefundedAmount = (decimal)request.Amount;
                payment.RefundReason = request.Reason;

                _context.Payments.Update(payment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✓ Reembolso procesado exitosamente - Payment {PaymentId}, Amount: ${Amount}",
                    request.PaymentId, request.Amount);

                return PaymentMapper.ToPaymentResponse(payment);
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar reembolso para Payment {PaymentId}", request.PaymentId);
                throw new RpcException(new Status(StatusCode.Internal,
                    $"Error interno al procesar el reembolso: {ex.Message}"));
            }
        }

        /// <summary>
        /// Obtiene el estado de un pago
        /// </summary>
        public override async Task<PaymentResponse> GetPaymentStatus(
            GetPaymentStatusRequest request,
            ServerCallContext context)
        {
            _logger.LogInformation("Consultando estado de Payment {PaymentId}", request.PaymentId);

            // Validar request
            var validationResult = await _statusValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                throw new RpcException(new Status(StatusCode.InvalidArgument, errors));
            }

            try
            {
                var payment = await _context.Payments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PaymentId == request.PaymentId);

                if (payment == null)
                {
                    throw new RpcException(new Status(StatusCode.NotFound,
                        $"Pago con ID {request.PaymentId} no encontrado"));
                }

                return PaymentMapper.ToPaymentResponse(payment);
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estado de Payment {PaymentId}", request.PaymentId);
                throw new RpcException(new Status(StatusCode.Internal,
                    $"Error interno: {ex.Message}"));
            }
        }

        /// <summary>
        /// Obtiene historial de pagos de una orden
        /// </summary>
        public override async Task<PaymentHistoryResponse> GetPaymentHistory(
            GetPaymentHistoryRequest request,
            ServerCallContext context)
        {
            _logger.LogInformation("Consultando historial de pagos para Order {OrderId}", request.OrderId);

            // Validar request
            var validationResult = await _historyValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                throw new RpcException(new Status(StatusCode.InvalidArgument, errors));
            }

            try
            {
                var payments = await _context.Payments
                    .AsNoTracking()
                    .Where(p => p.OrderId == request.OrderId)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                _logger.LogInformation("✓ Encontrados {Count} pagos para Order {OrderId}",
                    payments.Count, request.OrderId);

                return PaymentMapper.ToPaymentHistoryResponse(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener historial de pagos para Order {OrderId}", request.OrderId);
                throw new RpcException(new Status(StatusCode.Internal,
                    $"Error interno: {ex.Message}"));
            }
        }

        /// <summary>
        /// Obtiene un pago por ID
        /// </summary>
        public override async Task<PaymentResponse> GetPayment(
            GetPaymentRequest request,
            ServerCallContext context)
        {
            _logger.LogInformation("Consultando Payment {PaymentId}", request.PaymentId);

            // Validar request
            var validationResult = await _getValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                throw new RpcException(new Status(StatusCode.InvalidArgument, errors));
            }

            try
            {
                var payment = await _context.Payments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PaymentId == request.PaymentId);

                if (payment == null)
                {
                    throw new RpcException(new Status(StatusCode.NotFound,
                        $"Pago con ID {request.PaymentId} no encontrado"));
                }

                return PaymentMapper.ToPaymentResponse(payment);
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener Payment {PaymentId}", request.PaymentId);
                throw new RpcException(new Status(StatusCode.Internal,
                    $"Error interno: {ex.Message}"));
            }
        }

        /// <summary>
        /// Obtiene una razón de fallo aleatoria para simulación
        /// </summary>
        private static string GetRandomFailureReason()
        {
            var reasons = new[]
            {
                "Insufficient funds",
                "Card expired",
                "Card declined by issuer",
                "Invalid card number",
                "Daily transaction limit exceeded",
                "Suspected fraud",
                "Invalid CVV",
                "Card blocked"
            };

            return reasons[_random.Next(reasons.Length)];
        }
    }
}
