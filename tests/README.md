# Testler

Windows, .NET 10 SDK, .NET Framework 4.8, Python 3 ve yerel SQL Server Express gerekir. Python için ek paket gerekmez.

Proje kökünde:

```powershell
.\tests\run-integration.ps1
# Python farklı bir yerdeyse:
.\tests\run-integration.ps1 -Python 'C:\Python313\python.exe'
```

Script çözümü derler, rastgele isimli ayrı bir test veritabanı açar ve sunucuyu yalnızca localhost üzerinde çalıştırır. Otomatik yedeklemeyi bu test sunucusunda kapatır. Sonunda test sürecini durdurur ve yalnızca kendi oluşturduğu veritabanını kaldırır. Geçici test dosyaları ve günlüklerin yolu konsolda yazılır; bunları Git'e ekleme.

`integration.py`: 56 HTTP kontrolü. Yönetim oturumu/CSRF, cihaz tokenı, korunan medya indirme, tarih ve öncelik, silme, durdurma, liste sırası.

`PlayerRegression`: 11 kontrol. Gerçek Player sınıfları ile kimlik doğrulama, indirme/hash, bozuk cache, çevrimdışı kayıt, SignalR ekran grubu ayrımı, boş yayın ve eksik dosyalar.

Son doğrulama: 19 Eylül 2026, Windows üzerinde .NET 10.0.302 SDK. Derleme: 0 hata, 0 uyarı. HTTP: 56/56. Player: 11/11. Bu testler gerçek Windows 7, TV bağlantısı, video kodekleri veya yedekten geri yükleme testi yerine geçmez.

Paket oluşturma kontrolü:

```powershell
.\scripts\publish-all.ps1
```
