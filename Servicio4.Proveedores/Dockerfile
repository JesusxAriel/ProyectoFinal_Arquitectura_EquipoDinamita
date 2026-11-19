# Dockerfile del microservicio Proveedores. Contexto de construcción: Servicio4.Proveedores/
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY Proveedores.Domain/Proveedores.Domain.csproj Proveedores.Domain/
COPY Proveedores.Application/Proveedores.Application.csproj Proveedores.Application/
COPY Proveedores.Infrastructure/Proveedores.Infrastructure.csproj Proveedores.Infrastructure/
COPY Proveedores.API/Proveedores.API.csproj Proveedores.API/
RUN dotnet restore Proveedores.API/Proveedores.API.csproj
COPY Proveedores.Domain/ Proveedores.Domain/
COPY Proveedores.Application/ Proveedores.Application/
COPY Proveedores.Infrastructure/ Proveedores.Infrastructure/
COPY Proveedores.API/ Proveedores.API/
RUN dotnet publish Proveedores.API/Proveedores.API.csproj -c Release -o /app --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS final
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
# INTEGRACIÓN: La API solo recibe configuración por entorno; no se copian bases ni secretos.
ENV ASPNETCORE_HTTP_PORTS=8080
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Proveedores.API.dll"]
