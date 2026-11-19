# Contexto: ApiGateway/ApiGateway.Demo/. Restaurar primero el proyecto para aprovechar la caché.
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src
COPY ApiGateway.Demo.csproj ./
RUN dotnet restore ApiGateway.Demo.csproj
COPY . .
RUN dotnet publish ApiGateway.Demo.csproj -c Release -o /out --no-restore /p:UseAppHost=false

# Runtime sin SDK ni fuentes; curl permite el health check HTTP de Compose.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS final
USER root
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_HTTP_PORTS=8080
# Usuario app/APP_UID de la imagen oficial; el puerto 8080 no requiere root.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "ApiGateway.Demo.dll"]
