FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ./f1-shared/f1-shared.csproj ./f1-shared/
COPY ./telemetry-analytics-service/telemetry-analytics-service.csproj ./telemetry-analytics-service/
RUN dotnet restore ./telemetry-analytics-service/telemetry-analytics-service.csproj

COPY . .
RUN dotnet publish ./telemetry-analytics-service/telemetry-analytics-service.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "telemetry-analytics-service.dll"]
