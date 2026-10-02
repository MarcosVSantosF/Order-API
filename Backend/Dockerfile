# Etapa 1: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["OrderApi.csproj", "./"]
RUN dotnet restore "OrderApi.csproj"

COPY . .
RUN dotnet build "OrderApi.csproj" -c Release -o /app/build

# Etapa 2: publish
FROM build AS publish
RUN dotnet publish "OrderApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa 3: imagem final (runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "OrderApi.dll"]