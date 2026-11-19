FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
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
RUN dotnet publish Proveedores.API/Proveedores.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Proveedores.API.dll"]
