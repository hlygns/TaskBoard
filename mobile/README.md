# TaskBoard Mobil (Expo / React Native)

TaskBoard'un telefon uygulaması. Aynı API'yi kullanır; web ile aynı hesap, panolar ve kartlar.

| Ekran | Ne yapar |
|---|---|
| **Görevlerim** (ana ekran) | Tüm panolardaki açık işler: Gecikmiş / Bugün / Yarın / Bu hafta / Daha sonra. Yuvarlağa dokun → tamamlandı |
| **Panolar** | Renkli pano kartları, ilerleme çubuğu, şablondan yeni pano |
| **Pano** | Üstte sütun sekmeleri, altta o sütunun kartları; alttan hızlı kart ekleme; aşağı çek → yenile |
| **Kart** | Tamamla, sütuna taşı, son tarih (Bugün / Yarın / Gelecek hafta), öncelik, etiketler, açıklama, alt görevler, yorumlar, arşivle/sil — her değişiklik anında kaydedilir |
| **Profil** | Hesap, bağlı sunucu, çıkış |

## Telefonda çalıştırma (aynı Wi-Fi)

1. Telefona **Expo Go** uygulamasını kur (Play Store / App Store).
2. Bilgisayarda Docker ve API'yi başlat — API'yi **lan** profiliyle (ağdaki cihazlar bağlanabilsin diye):
   ```bash
   docker compose up -d
   dotnet run --project src/TaskBoard.API --launch-profile lan
   ```
3. Ayrı bir terminalde:
   ```bash
   cd mobile
   npm install        # ilk seferde
   npx expo start
   ```
4. Terminaldeki **QR kodu** okut: Android'de Expo Go içinden, iPhone'da kamera uygulamasıyla.
5. Giriş ekranında sunucu adresi otomatik dolar (`http://<bilgisayarın IP'si>:5009`). Bağlanamazsa "değiştir" ile düzelt.

**Bağlanamıyorsa:** Windows ağı "Ortak (Public)" olarak tanımlıysa gelen bağlantıları engeller. Ev ağını
*Ayarlar → Ağ ve İnternet → Wi-Fi → (ağın adı) → Ağ profili türü → Özel* yap. İlk açılışta Windows
"erişime izin ver" diye sorarsa *Özel ağlar* için izin ver.

## Teknik notlar

- **Expo SDK 57**, **Expo Router** (dosya tabanlı ekranlar: `src/app/`), giriş koruması `Stack.Protected` ile.
- **Oturum:** Mobil istemci isteklere `X-Client: mobile` başlığını ekler; sunucu refresh token'ı cookie yerine yanıtın
  gövdesinde verir. Token telefonun şifreli deposunda (`expo-secure-store` → iOS Keychain / Android Keystore) tutulur,
  access token sadece bellekte. Rotation ve çalıntı tespiti web'deki gibi çalışır.
- **Neden cookie değil:** Ev ağında HTTPS yok; `Secure` cookie'ler HTTP üzerinden gönderilmez. Ayrıca React Native'de
  cookie yönetimi zahmetli.
- **Sürükle-bırak yerine** kart detayında "Sütun" seçimi: telefonda tek elle daha kullanışlı.
- **İyimser güncelleme:** Tamamla, etiket, alt görev değişiklikleri ekranda hemen görünür; sunucu reddederse kart
  sunucudan yeniden yüklenir.

```bash
npx expo lint      # lint
npx tsc --noEmit   # tip kontrolü
npx expo-doctor    # bağımlılık / yapılandırma kontrolü
```
