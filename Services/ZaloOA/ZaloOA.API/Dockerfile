FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY ["ZaloOA.API/ZaloOA.API.csproj", "ZaloOA.API/"]
COPY ["ZaloOA.Infrastructure/ZaloOA.Infrastructure.csproj", "ZaloOA.Infrastructure/"]
COPY ["ZaloOA.Application/ZaloOA.Application.csproj", "ZaloOA.Application/"]
COPY ["ZaloOA.Domain/ZaloOA.Domain.csproj", "ZaloOA.Domain/"]
RUN dotnet restore "ZaloOA.API/ZaloOA.API.csproj"

# Copy all source code and build
COPY . .
WORKDIR "/src/ZaloOA.API"
RUN dotnet build "ZaloOA.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ZaloOA.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ZaloOA.API.dll"]
