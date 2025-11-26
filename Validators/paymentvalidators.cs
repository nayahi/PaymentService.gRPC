using FluentValidation;
using ECommerceGRPC.PaymentService;
using PaymentService.gRPC.Models;

namespace PaymentService.gRPC.Validators
{

    /// <summary>
    /// Validador para ProcessPaymentRequest
    /// </summary>
    public class ProcessPaymentRequestValidator : AbstractValidator<ProcessPaymentRequest>
    {
        public ProcessPaymentRequestValidator()
        {
            RuleFor(x => x.OrderId)
                .GreaterThan(0)
                .WithMessage("Order ID debe ser mayor a 0");

            RuleFor(x => x.UserId)
                .GreaterThan(0)
                .WithMessage("User ID debe ser mayor a 0");

            RuleFor(x => x.Amount)
                .GreaterThan(0)
                .WithMessage("El monto debe ser mayor a 0")
                .LessThanOrEqualTo(100000)
                .WithMessage("El monto no puede exceder $100,000");

            RuleFor(x => x.PaymentMethod)
                .NotEmpty()
                .WithMessage("Payment method es requerido")
                .Must(BeValidPaymentMethod)
                .WithMessage("Payment method debe ser: CreditCard, DebitCard, PayPal, o BankTransfer");

            RuleFor(x => x.Currency)
                .NotEmpty()
                .WithMessage("Currency es requerida")
                .Length(3)
                .WithMessage("Currency debe tener 3 caracteres (ej: USD, EUR)")
                .Must(BeValidCurrency)
                .WithMessage("Currency debe ser USD, EUR, GBP, o CRC");

            RuleFor(x => x.CardLastFourDigits)
                .Length(4)
                .When(x => !string.IsNullOrEmpty(x.CardLastFourDigits))
                .WithMessage("Card last four digits debe tener exactamente 4 dígitos")
                .Matches(@"^\d{4}$")
                .When(x => !string.IsNullOrEmpty(x.CardLastFourDigits))
                .WithMessage("Card last four digits debe contener solo números");
        }

        private bool BeValidPaymentMethod(string paymentMethod)
        {
            var validMethods = new[]
            {
                Models.PaymentMethod.CreditCard,
                Models.PaymentMethod.DebitCard,
                Models.PaymentMethod.PayPal,
                Models.PaymentMethod.BankTransfer
            };

            return validMethods.Contains(paymentMethod);
        }

        private bool BeValidCurrency(string currency)
        {
            var validCurrencies = new[] { "USD", "EUR", "GBP", "CRC" };
            return validCurrencies.Contains(currency.ToUpper());
        }
    }

    /// <summary>
    /// Validador para RefundPaymentRequest
    /// </summary>
    public class RefundPaymentRequestValidator : AbstractValidator<RefundPaymentRequest>
    {
        public RefundPaymentRequestValidator()
        {
            RuleFor(x => x.PaymentId)
                .GreaterThan(0)
                .WithMessage("Payment ID debe ser mayor a 0");

            RuleFor(x => x.Reason)
                .NotEmpty()
                .WithMessage("Reason es requerido para el reembolso")
                .MaximumLength(500)
                .WithMessage("Reason no puede exceder 500 caracteres");

            RuleFor(x => x.Amount)
                .GreaterThan(0)
                .WithMessage("El monto del reembolso debe ser mayor a 0")
                .LessThanOrEqualTo(100000)
                .WithMessage("El monto del reembolso no puede exceder $100,000");
        }
    }

    /// <summary>
    /// Validador para GetPaymentStatusRequest
    /// </summary>
    public class GetPaymentStatusRequestValidator : AbstractValidator<GetPaymentStatusRequest>
    {
        public GetPaymentStatusRequestValidator()
        {
            RuleFor(x => x.PaymentId)
                .GreaterThan(0)
                .WithMessage("Payment ID debe ser mayor a 0");
        }
    }

    /// <summary>
    /// Validador para GetPaymentHistoryRequest
    /// </summary>
    public class GetPaymentHistoryRequestValidator : AbstractValidator<GetPaymentHistoryRequest>
    {
        public GetPaymentHistoryRequestValidator()
        {
            RuleFor(x => x.OrderId)
                .GreaterThan(0)
                .WithMessage("Order ID debe ser mayor a 0");
        }
    }

    /// <summary>
    /// Validador para GetPaymentRequest
    /// </summary>
    public class GetPaymentRequestValidator : AbstractValidator<GetPaymentRequest>
    {
        public GetPaymentRequestValidator()
        {
            RuleFor(x => x.PaymentId)
                .GreaterThan(0)
                .WithMessage("Payment ID debe ser mayor a 0");
        }
    }
}
