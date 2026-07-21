FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY nuget.config .
COPY proto/ proto/
COPY src/MetricService/MetricService.csproj src/MetricService/
RUN dotnet restore src/MetricService/MetricService.csproj

COPY src/MetricService/ src/MetricService/
RUN dotnet publish src/MetricService/MetricService.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .

EXPOSE 8080 8082

ENTRYPOINT ["dotnet", "MetricService.dll"]
