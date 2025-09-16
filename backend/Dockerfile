FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY src/CryptoArbitrage.Domain/*.csproj ./CryptoArbitrage.Domain/
COPY src/CryptoArbitrage.Application/*.csproj ./CryptoArbitrage.Application/
COPY src/CryptoArbitrage.Infrastructure/*.csproj ./CryptoArbitrage.Infrastructure/
COPY src/CryptoArbitrage.Worker/*.csproj ./CryptoArbitrage.Worker/
COPY src/CryptoArbitrage.Api/*.csproj ./CryptoArbitrage.Api/
COPY src/CryptoArbitrage.Blazor/*.csproj ./CryptoArbitrage.Blazor/

# Restore packages
RUN dotnet restore ./CryptoArbitrage.Domain/
RUN dotnet restore ./CryptoArbitrage.Application/
RUN dotnet restore ./CryptoArbitrage.Infrastructure/
RUN dotnet restore ./CryptoArbitrage.Worker/
RUN dotnet restore ./CryptoArbitrage.Api/
RUN dotnet restore ./CryptoArbitrage.Blazor/

# Copy all files and build the project
COPY src/CryptoArbitrage.Domain/. ./CryptoArbitrage.Domain/
COPY src/CryptoArbitrage.Application/. ./CryptoArbitrage.Application/
COPY src/CryptoArbitrage.Infrastructure/. ./CryptoArbitrage.Infrastructure/
COPY src/CryptoArbitrage.Worker/. ./CryptoArbitrage.Worker/
COPY src/CryptoArbitrage.Api/. ./CryptoArbitrage.Api/
COPY src/CryptoArbitrage.Blazor/. ./CryptoArbitrage.Blazor/

# Publish projects
RUN dotnet publish -c Release -o /app/api ./CryptoArbitrage.Api/
RUN dotnet publish -c Release -o /app/worker ./CryptoArbitrage.Worker/
RUN dotnet publish -c Release -o /app/blazor ./CryptoArbitrage.Blazor/

# Runtime image for API
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS api
WORKDIR /app
COPY --from=build /app/api ./
ENV ASPNETCORE_URLS=http://+:5001
EXPOSE 5001
ENTRYPOINT ["dotnet", "CryptoArbitrage.Api.dll"]

# Runtime image for Worker
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS worker
WORKDIR /app
COPY --from=build /app/worker ./
ENTRYPOINT ["dotnet", "CryptoArbitrage.Worker.dll"]

# Runtime image for Blazor
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS blazor
WORKDIR /app
COPY --from=build /app/blazor ./
ENV ASPNETCORE_URLS=http://+:7001
EXPOSE 7001
ENTRYPOINT ["dotnet", "CryptoArbitrage.Blazor.dll"] 