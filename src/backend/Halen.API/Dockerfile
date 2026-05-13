# Stage 1: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first so Docker can cache the restore layer.
# If only source changes (not .csproj), this layer is reused — much faster rebuilds.
COPY Halen.Domain/Halen.Domain.csproj Halen.Domain/
COPY Halen.Application/Halen.Application.csproj Halen.Application/
COPY Halen.Infrastructure/Halen.Infrastructure.csproj Halen.Infrastructure/
COPY Halen.API/Halen.API.csproj Halen.API/
RUN dotnet restore Halen.API/Halen.API.csproj

COPY . .
RUN dotnet publish Halen.API/Halen.API.csproj -c Release -o /app/publish

# Stage 2: runtime — only the compiled output, no SDK
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Development
EXPOSE 8080

ENTRYPOINT ["dotnet", "Halen.API.dll"]
