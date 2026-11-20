FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore PuntoMarket.Clientes.slnx
RUN dotnet publish src/PuntoMarket.Clientes.Adapters.In.Api/PuntoMarket.Clientes.Adapters.In.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "PuntoMarket.Clientes.Adapters.In.Api.dll"]
