FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Packages.props", "."]
COPY ["Directory.Build.props", "."]
COPY ["src/Api/Sample.Api.csproj", "Api/"]
COPY ["src/Application/Sample.Application.csproj", "Application/"]
COPY ["src/Domain/Sample.Domain.csproj", "Domain/"]
COPY ["src/Infrastructure/Sample.Infrastructure.csproj", "Infrastructure/"]
RUN dotnet restore "./Api/Sample.Api.csproj"
COPY ./src .
WORKDIR "/src/Api"
RUN dotnet build "./Sample.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Sample.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sample.Api.dll"]