# Sunucu kurulumu

Önce kaynak kodu bilgisayarında denemek için README'deki komutları kullan. Aşağıdaki adımlar Windows servisi olarak kurmak içindir; mevcut bir CompanySignage servisi varsa aynı servis adı kullanılır. Ayrı bir deneme bilgisayarında başlamak daha kolaydır.

1. Derleme bilgisayarına .NET 10 SDK kur. Sunucu bilgisayarında SQL Server Express ve `sqlcmd` bulunmalı. Kurulum SQL Server'ı kendisi indirmez.
2. Proje kökünde PowerShell aç:

```powershell
.\scripts\publish-all.ps1
```

3. Oluşan `artifacts\Server` klasörünü sunucu bilgisayarına kopyala. İçindeki `appsettings.json` dosyasında `AdminSetup:InitialPassword` değerini kendi ilk giriş şifrenle değiştir. Bu değer yalnızca ilk kullanıcı oluşturulurken kullanılır; sonraki şifre değişiklikleri panelde yapılır.
4. Kopyaladığın Server klasöründe yönetici PowerShell aç:

```powershell
.\install-server.ps1 -SqlServer 'localhost\SQLEXPRESS' -DatabaseName 'CompanySignagePublic' -PublicHost 'SUNUCU_ADI'
```

`SUNUCU_ADI` yerine ekran bilgisayarlarının ulaşabildiği sunucu adını yaz. Kurulum 5000 portunu açar, dosyaları `C:\CompanySignagePublic\Server\Server` konumuna kopyalar ve `CompanySignageServer` servisini kurar. Varsayılan kurulum yerel SQLEXPRESS içindir.

5. `http://SUNUCU_ADI:5000` adresinden `admin` ve belirlediğin şifreyle giriş yap.
6. Panelde bir ekran kaydı oluştur. Ekran kodunu ve uzun, tahmin edilmesi zor bir cihaz tokenını belirle. Sonra [Player kurulumunu](PLAYER_SETUP.md) yap.

Medya dosyaları `C:\CompanySignagePublic\Server\Uploads` altında tutulur. Veritabanı SQL Server'dadır. Sunucu paketi .NET çalışma zamanını yanında taşır; kaynak kodu derlemek için yine SDK gerekir.

HTTP ayarları yerel ağdaki denemeyi kolaylaştırmak içindir. İnternetten kullanım için [ağ notlarını](NETWORK.md) oku.
