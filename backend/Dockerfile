FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY src/CryptoArbitrage.Domain/*.csproj ./CryptoArbitrage.Domain/
COPY src/CryptoArbitrage.Application/*.csproj ./CryptoArbitrage.Application/
COPY src/CryptoArbitrage.Infrastructure/*.csproj ./CryptoArbitrage.Infrastructure/
COPY src/CryptoArbitrage.Worker/*.csproj ./CryptoArbitrage.Worker/
COPY src/CryptoArbitrage.Api/*.csproj ./CryptoArbitrage.Api/

# Restore packages
RUN dotnet restore ./CryptoArbitrage.Domain/
RUN dotnet restore ./CryptoArbitrage.Application/
RUN dotnet restore ./CryptoArbitrage.Infrastructure/
RUN dotnet restore ./CryptoArbitrage.Worker/
RUN dotnet restore ./CryptoArbitrage.Api/

# Copy all files and build the project
COPY src/CryptoArbitrage.Domain/. ./CryptoArbitrage.Domain/
COPY src/CryptoArbitrage.Application/. ./CryptoArbitrage.Application/
COPY src/CryptoArbitrage.Infrastructure/. ./CryptoArbitrage.Infrastructure/
COPY src/CryptoArbitrage.Worker/. ./CryptoArbitrage.Worker/
COPY src/CryptoArbitrage.Api/. ./CryptoArbitrage.Api/

# Build Api project
RUN dotnet publish -c Release -o /app/api ./CryptoArbitrage.Api/

# Build Worker project
RUN dotnet publish -c Release -o /app/worker ./CryptoArbitrage.Worker/

# Runtime image for API
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS api
WORKDIR /app
COPY --from=build /app/api ./
EXPOSE 80
EXPOSE 443
ENTRYPOINT ["dotnet", "CryptoArbitrage.Api.dll"]

# Runtime image for Worker
FROM mcr.microsoft.com/dotnet/runtime:7.0 AS worker
WORKDIR /app
COPY --from=build /app/worker ./
ENTRYPOINT ["dotnet", "CryptoArbitrage.Worker.dll"] 