# Build context is the repository root: docker build -f src/Depot.Api/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo
COPY Directory.Build.props .editorconfig ./
COPY src/Depot.Domain/Depot.Domain.csproj src/Depot.Domain/
COPY src/Depot.Application/Depot.Application.csproj src/Depot.Application/
COPY src/Depot.Infrastructure/Depot.Infrastructure.csproj src/Depot.Infrastructure/
COPY src/Depot.Api/Depot.Api.csproj src/Depot.Api/
RUN dotnet restore src/Depot.Api/Depot.Api.csproj
COPY src/ src/
RUN dotnet publish src/Depot.Api/Depot.Api.csproj -c Release --no-restore -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Depot.Api.dll"]
