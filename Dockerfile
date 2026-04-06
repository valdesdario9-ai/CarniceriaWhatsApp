# ✅ Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar el archivo de proyecto y restaurar dependencias
COPY ["CarniceriaWhatsApp.csproj", "./"]
RUN dotnet restore "./CarniceriaWhatsApp.csproj"

# Copiar todo el código y compilar
COPY . .
RUN dotnet build "./CarniceriaWhatsApp.csproj" -c Release -o /app/build

# ✅ Publish stage
FROM build AS publish
RUN dotnet publish "./CarniceriaWhatsApp.csproj" -c Release -o /app/publish

# ✅ Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Puerto que usa Render
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Comando de inicio
ENTRYPOINT ["dotnet", "CarniceriaWhatsApp.dll"]
