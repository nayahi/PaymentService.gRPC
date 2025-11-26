# PaymentService.gRPC - Servicio de Procesamiento de Pagos

## Descripción

Servicio gRPC para procesamiento de pagos del sistema e-commerce. Incluye simulación de gateway de pagos con 10% de probabilidad de fallo aleatorio para testing de saga distribuida.

## Características

- ✅ Procesamiento de pagos con múltiples métodos (CreditCard, DebitCard, PayPal, BankTransfer)
- ✅ Reembolsos completos y parciales
- ✅ Historial de transacciones
- ✅ Mock realista con 10% de fallos aleatorios
- ✅ Validación completa con FluentValidation
- ✅ Logging estructurado con Serilog
- ✅ Entity Framework Core con SQL Server
- ✅ Datos de prueba pre-cargados

## Estructura de Carpetas

```
C:\Users\nayah\source\repos\PaymentService.gRPC\
├── Protos\
│   └── payment_service.proto
├── Models\
│   └── Payment.cs
├── Data\
│   ├── PaymentDbContext.cs
│   └── DbInitializer.cs
├── Services\
│   └── PaymentGrpcService.cs
├── Validators\
│   └── PaymentValidators.cs
├── Mappers\
│   └── PaymentMapper.cs
├── Program.cs
├── appsettings.json
└── PaymentService.gRPC.csproj
```

## Instalación y Configuración

### Paso 1: Crear la estructura de carpetas

```powershell
# Crear directorio principal
mkdir "C:\Users\nayah\source\repos\PaymentService.gRPC"
cd "C:\Users\nayah\source\repos\PaymentService.gRPC"

# Crear subdirectorios
mkdir Protos
mkdir Models
mkdir Data
mkdir Services
mkdir Validators
mkdir Mappers
```

### Paso 2: Copiar archivos

Copie cada archivo descargado a su ubicación correspondiente:

- `PaymentService_payment_service.proto` → `Protos\payment_service.proto`
- `PaymentService_Payment.cs` → `Models\Payment.cs`
- `PaymentService_PaymentDbContext.cs` → `Data\PaymentDbContext.cs`
- `PaymentService_DbInitializer.cs` → `Data\DbInitializer.cs`
- `PaymentService_PaymentGrpcService.cs` → `Services\PaymentGrpcService.cs`
- `PaymentService_PaymentValidators.cs` → `Validators\PaymentValidators.cs`
- `PaymentService_PaymentMapper.cs` → `Mappers\PaymentMapper.cs`
- `PaymentService_Program.cs` → `Program.cs`
- `PaymentService_appsettings.json` → `appsettings.json`
- `PaymentService.gRPC.csproj` → `PaymentService.gRPC.csproj`

### Paso 3: Restaurar paquetes NuGet

```powershell
dotnet restore
```

### Paso 4: Crear y aplicar migración inicial

```powershell
# Instalar herramienta EF Core CLI (si no la tiene)
dotnet tool install --global dotnet-ef

# Crear migración inicial
dotnet ef migrations add InitialCreate

# Aplicar migración (la base de datos se creará automáticamente)
dotnet ef database update
```

### Paso 5: Compilar el proyecto

```powershell
dotnet build
```

### Paso 6: Ejecutar el servicio

```powershell
dotnet run
```

Verá una salida similar a:
```
╔═══════════════════════════════════════════════════════════╗
║           PaymentService.gRPC INICIADO                    ║
╚═══════════════════════════════════════════════════════════╝
🚀 Servicio escuchando en puerto 7004
📊 Base de datos: PaymentDb
💳 Mock habilitado: 10% de pagos fallan aleatoriamente
```

## Configuración de Base de Datos

El servicio usa SQL Server. Asegúrese de que SQL Server esté corriendo y actualice la cadena de conexión en `appsettings.json` si es necesario:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost,1433;Database=PaymentDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```

## Datos de Prueba

El servicio incluye 6 pagos de prueba pre-cargados:

1. **Pago 1**: Completado - $1,499.98 (CreditCard) - Order 1
2. **Pago 2**: Completado - $989.97 (DebitCard) - Order 2
3. **Pago 3**: Pendiente - $449.99 (CreditCard) - Order 3 (PARA PRUEBAS)
4. **Pago 4**: Fallido - $189.99 - Insufficient funds
5. **Pago 5**: Reembolsado - $299.99 (PayPal)
6. **Pago 6**: Completado - $1,899.99 (BankTransfer)

## Métodos gRPC Disponibles

### 1. ProcessPayment
Procesa un nuevo pago. Mock con 10% de probabilidad de fallo aleatorio.

```proto
rpc ProcessPayment (ProcessPaymentRequest) returns (PaymentResponse);
```

### 2. RefundPayment
Reembolsa un pago completado.

```proto
rpc RefundPayment (RefundPaymentRequest) returns (PaymentResponse);
```

### 3. GetPaymentStatus
Consulta el estado de un pago.

```proto
rpc GetPaymentStatus (GetPaymentStatusRequest) returns (PaymentResponse);
```

### 4. GetPaymentHistory
Obtiene historial de pagos de una orden.

```proto
rpc GetPaymentHistory (GetPaymentHistoryRequest) returns (PaymentHistoryResponse);
```

### 5. GetPayment
Obtiene detalles completos de un pago.

```proto
rpc GetPayment (GetPaymentRequest) returns (PaymentResponse);
```

## Testing con grpcurl

### Listar servicios disponibles
```bash
grpcurl -plaintext localhost:7004 list
```

### Procesar un pago (Order 10, monto $500)
```bash
grpcurl -plaintext -d '{
  "order_id": 10,
  "user_id": 2,
  "amount": 500.00,
  "payment_method": "CreditCard",
  "currency": "USD",
  "card_last_four_digits": "4532"
}' localhost:7004 paymentservice.PaymentService/ProcessPayment
```

### Consultar estado de un pago
```bash
grpcurl -plaintext -d '{
  "payment_id": 1
}' localhost:7004 paymentservice.PaymentService/GetPaymentStatus
```

### Obtener historial de pagos de una orden
```bash
grpcurl -plaintext -d '{
  "order_id": 1
}' localhost:7004 paymentservice.PaymentService/GetPaymentHistory
```

### Reembolsar un pago
```bash
grpcurl -plaintext -d '{
  "payment_id": 1,
  "reason": "Customer request",
  "amount": 1499.98
}' localhost:7004 paymentservice.PaymentService/RefundPayment
```

## Health Check

Verificar estado del servicio:
```
http://localhost:7004/health
```

## Estados de Pago

- **Pending**: Pago en proceso
- **Completed**: Pago completado exitosamente
- **Failed**: Pago fallido
- **Refunded**: Pago reembolsado

## Métodos de Pago Soportados

- **CreditCard**: Tarjeta de crédito
- **DebitCard**: Tarjeta de débito
- **PayPal**: PayPal
- **BankTransfer**: Transferencia bancaria

## Mock de Fallos

El servicio simula un gateway de pagos real con:

- **10% de probabilidad de fallo** aleatorio en cada transacción
- **Razones de fallo realistas**:
  - Insufficient funds
  - Card expired
  - Card declined by issuer
  - Invalid card number
  - Daily transaction limit exceeded
  - Suspected fraud
  - Invalid CVV
  - Card blocked

- **Delays realistas**: 500-1500ms por transacción

## Logs

El servicio genera logs estructurados con Serilog:

- ✓ Pagos procesados exitosamente
- 💳 Pagos fallidos con razón
- ⚠️ Validaciones fallidas
- ❌ Errores internos

## Integración con Saga

Este servicio está diseñado para integrarse con la saga distribuida de compra:

1. OrderService publica evento `OrderCreated` a SNS
2. PaymentService recibe mensaje de SQS
3. PaymentService procesa pago (10% fallan)
4. Si fallo → Publica evento `PaymentFailed` para compensación
5. Si éxito → Publica evento `PaymentCompleted` para continuar saga

## Troubleshooting

### Error: No se puede conectar a SQL Server
Verifique que SQL Server esté corriendo y que la cadena de conexión sea correcta.

### Error: Puerto 7004 ya está en uso
Cambie el puerto en `appsettings.json` en la sección `Kestrel.Endpoints.Http.Url`.

### Error: No se generan las clases desde .proto
Ejecute `dotnet build` para forzar la generación de código desde Protocol Buffers.

## Próximos Pasos

1. **NotificationService.gRPC** - Envío de notificaciones (email/SMS)
2. **ShippingService.gRPC** - Gestión de envíos
3. **Saga Orchestrator** - Coordinación de flujo completo

---

**Puerto:** 7004  
**Base de Datos:** PaymentDb  
**Protocolo:** gRPC (HTTP/2)  
**Framework:** .NET 8.0

Health Checks
PaymentService: http://localhost:7004/health


Resumen de metodos de PaymentService.gRPC:
ProcessPayment(orderId, amount, paymentMethod) → Success/Failure
RefundPayment(paymentId) → Success/Failure
GetPaymentStatus(paymentId) → Pending/Completed/Failed/Refunded
GetPaymentHistory(orderId) → List de transacciones
Mock: Simular 10% de pagos fallidos aleatoriamente para testing