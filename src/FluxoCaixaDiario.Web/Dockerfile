FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["FluxoCaixaDiario.Web/FluxoCaixaDiario.Web.csproj", "FluxoCaixaDiario.Web/"]
RUN dotnet restore "./FluxoCaixaDiario.Web/FluxoCaixaDiario.Web.csproj"
COPY . .
WORKDIR "/src/FluxoCaixaDiario.Web"
RUN dotnet build "./FluxoCaixaDiario.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./FluxoCaixaDiario.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FluxoCaixaDiario.Web.dll"]