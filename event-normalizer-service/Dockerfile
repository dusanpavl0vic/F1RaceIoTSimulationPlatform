FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ./f1-shared/f1-shared.csproj ./f1-shared/
COPY ./event-normalizer-service/event-normalizer-service.csproj ./event-normalizer-service/
RUN dotnet restore ./event-normalizer-service/event-normalizer-service.csproj

COPY . .
RUN dotnet publish ./event-normalizer-service/event-normalizer-service.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "event-normalizer-service.dll"]
