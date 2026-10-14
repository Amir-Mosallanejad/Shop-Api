FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
USER app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/ShopApi.API/ShopApi.API.csproj", "src/ShopApi.API/"]
COPY ["src/ShopApi.Application/ShopApi.Application.csproj", "src/ShopApi.Application/"]
COPY ["src/ShopApi.Infrastructure/ShopApi.Infrastructure.csproj", "src/ShopApi.Infrastructure/"]
COPY ["src/ShopApi.Domain/ShopApi.Domain.csproj", "src/ShopApi.Domain/"]
RUN dotnet restore "src/ShopApi.API/ShopApi.API.csproj"
COPY . .
WORKDIR "/src/src/ShopApi.API"
RUN dotnet build -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
RUN dotnet publish -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ShopApi.API.dll"]
