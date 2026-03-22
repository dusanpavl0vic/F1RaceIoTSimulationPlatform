FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ./f1-feed-replay-service/f1-feed-replay-service.csproj ./f1-feed-replay-service/
RUN dotnet restore ./f1-feed-replay-service/f1-feed-replay-service.csproj

COPY . .
RUN dotnet publish ./f1-feed-replay-service/f1-feed-replay-service.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "f1-feed-replay-service.dll"]
