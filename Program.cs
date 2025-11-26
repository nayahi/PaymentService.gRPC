using FluentValidation;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using PaymentService.gRPC.Data;
using PaymentService.gRPC.Services;
using PaymentService.gRPC.Validators;
using Serilog;

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
.WriteTo.Console()
.CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    Log.Information("Iniciando PaymentService.gRPC");

    builder.Host.UseSerilog();

    // Configurar DbContext con SQL Server
    // Configurar DbContext
    //builder.Services.AddDbContext<PaymentDbContext>(options =>
    //    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddDbContext<PaymentDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sqlOptions => sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null)));

    // Configurar MassTransit con RabbitMQ
    builder.Services.AddMassTransit(x =>
    {
        x.UsingRabbitMq((context, cfg) =>
        {
            // Leer configuración de RabbitMQ
            var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
            var rabbitUser = builder.Configuration["RabbitMQ:Username"] ?? "admin";
            var rabbitPass = builder.Configuration["RabbitMQ:Password"] ?? "admin123";

            cfg.Host(rabbitHost, "/", h =>
            {
                h.Username(rabbitUser);
                h.Password(rabbitPass);
            });

            cfg.ConfigureEndpoints(context);
        });
    });

    // Registrar validators de FluentValidation
    builder.Services.AddValidatorsFromAssemblyContaining<ProcessPaymentRequestValidator>();

    // Registrar servicios gRPC
    builder.Services.AddGrpc(options =>
    {
        options.EnableDetailedErrors = true;
        options.MaxReceiveMessageSize = 4 * 1024 * 1024; // 4MB
        options.MaxSendMessageSize = 4 * 1024 * 1024;
    });

    // Configurar reflexión de gRPC para herramientas de desarrollo
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddGrpcReflection();
    }

    // Configurar CORS (para desarrollo)
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader()
                   .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding");
        });
    });

    var app = builder.Build();

    // Inicializar base de datos con datos de prueba
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<PaymentDbContext>();
            var logger = services.GetRequiredService<ILogger<Program>>();
            await DbInitializer.InitializeAsync(context, logger);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Error durante la inicialización de la base de datos");
        }
    }

    // Configurar pipeline HTTP
    if (app.Environment.IsDevelopment())
    {
        app.MapGrpcReflectionService();
    }

    // Configurar middleware
    app.UseCors("AllowAll");
    app.UseRouting();

    // Mapear servicios gRPC
    app.MapGrpcService<PaymentGrpcService>();

    app.MapGet("/", () => "PaymentService gRPC - Puerto 7004. Usa un cliente gRPC para comunicarte.");

    // Health check endpoint
    app.MapGet("/health", async (PaymentDbContext dbContext) =>
    {
        try
        {
            await dbContext.Database.CanConnectAsync();
            return Results.Ok(new
            {
                status = "Healthy",
                service = "PaymentService.gRPC",
                timestamp = DateTime.UtcNow,
                database = "Connected"
            });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: 503,
                title: "Service Unhealthy"
            );
        }
    });

    app.Logger.LogInformation("╔═══════════════════════════════════════════════════════════╗");
    app.Logger.LogInformation("║           PaymentService.gRPC INICIADO                    ║");
    app.Logger.LogInformation("╚═══════════════════════════════════════════════════════════╝");
    app.Logger.LogInformation("🚀 Servicio escuchando en puerto 7004");
    app.Logger.LogInformation("📊 Base de datos: ECommercePayments");
    app.Logger.LogInformation("💳 Mock habilitado: 10% de pagos fallan aleatoriamente");
    app.Logger.LogInformation("");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "La aplicación falló al iniciar");
}
finally
{
    Log.CloseAndFlush();
}
