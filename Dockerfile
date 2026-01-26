# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.
# Etapa 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar archivo de proyecto y restaurar dependencias
COPY PaymentService.gRPC.csproj .
RUN dotnet restore "PaymentService.gRPC.csproj"

# Copiar todo el código fuente
COPY . .

# Compilar el proyecto
RUN dotnet build "PaymentService.gRPC.csproj" -c Release -o /app/build

# Publicar la aplicación
RUN dotnet publish "PaymentService.gRPC.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Crear directorio para logs
RUN mkdir -p /app/logs

# Copiar los archivos publicados desde la etapa de build
COPY --from=build /app/publish .

# Exponer puerto gRPC
EXPOSE 7004

# Variables de entorno
ENV ASPNETCORE_URLS=http://+:7004
ENV ASPNETCORE_ENVIRONMENT=Production

# Punto de entrada
ENTRYPOINT ["dotnet", "PaymentService.gRPC.dll"]