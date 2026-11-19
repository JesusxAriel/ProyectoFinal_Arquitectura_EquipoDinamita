FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/PuntoMarket.Usuarios.Api/PuntoMarket.Usuarios.Api.csproj src/PuntoMarket.Usuarios.Api/
COPY src/PuntoMarket.Usuarios.Application/PuntoMarket.Usuarios.Application.csproj src/PuntoMarket.Usuarios.Application/
COPY src/PuntoMarket.Usuarios.Domain/PuntoMarket.Usuarios.Domain.csproj src/PuntoMarket.Usuarios.Domain/
COPY src/PuntoMarket.Usuarios.Infrastructure/PuntoMarket.Usuarios.Infrastructure.csproj src/PuntoMarket.Usuarios.Infrastructure/

RUN dotnet restore src/PuntoMarket.Usuarios.Api/PuntoMarket.Usuarios.Api.csproj

COPY src/ src/
RUN dotnet publish src/PuntoMarket.Usuarios.Api/PuntoMarket.Usuarios.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PuntoMarket.Usuarios.Api.dll"]
