# Derleme bağlamı depo kökü: docker build -f src/TaskBoard.API/Dockerfile .

# 1) Derleme: SDK image'ı büyük (~800 MB) ama sadece bu aşamada kullanılır.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Önce sadece proje dosyaları: paketler değişmedikçe "restore" katmanı Docker cache'inden gelir,
# kod değişikliklerinde paketler yeniden indirilmez.
COPY src/TaskBoard.Domain/TaskBoard.Domain.csproj src/TaskBoard.Domain/
COPY src/TaskBoard.Application/TaskBoard.Application.csproj src/TaskBoard.Application/
COPY src/TaskBoard.Infrastructure/TaskBoard.Infrastructure.csproj src/TaskBoard.Infrastructure/
COPY src/TaskBoard.API/TaskBoard.API.csproj src/TaskBoard.API/
RUN dotnet restore src/TaskBoard.API/TaskBoard.API.csproj

COPY src/ src/
RUN dotnet publish src/TaskBoard.API/TaskBoard.API.csproj -c Release -o /app --no-restore

# 2) Çalıştırma: sadece ASP.NET runtime + derlenmiş uygulama.
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Root olmayan kullanıcıyla çalış (image'da hazır gelen "app" kullanıcısı).
USER $APP_UID
ENTRYPOINT ["dotnet", "TaskBoard.API.dll"]
