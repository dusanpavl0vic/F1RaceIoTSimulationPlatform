FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ./f1-shared/f1-shared.csproj ./f1-shared/
COPY ./race-state-service/race-state-service.csproj ./race-state-service/
RUN dotnet restore ./race-state-service/race-state-service.csproj

COPY . .
RUN dotnet publish ./race-state-service/race-state-service.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "race-state-service.dll"]
