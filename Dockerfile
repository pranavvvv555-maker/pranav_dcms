# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["DCMSApp.csproj", "./"]
RUN dotnet restore "DCMSApp.csproj"
COPY . .
RUN dotnet publish "DCMSApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Create persistent storage folder for SQLite database and data protection keys
RUN mkdir -p /app/data
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/dcms.db"
ENV ASPNETCORE_URLS="http://0.0.0.0:5025"

VOLUME ["/app/data"]
EXPOSE 5025

ENTRYPOINT ["dotnet", "DCMSApp.dll"]
