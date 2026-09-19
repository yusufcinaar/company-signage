# Ekran bilgisayarına Player kurma

Player, televizyona/monitöre bağlı Windows bilgisayarda çalışır. TV'nin içine kurulan bir uygulama değildir.

1. Önce sunucuyu çalıştır ve panelin Ekranlar bölümünde bir ekran oluştur.
2. Sunucuda oluşturulan `artifacts\Server\Packages\CompanySignage-Ekran-Kurulum.zip` dosyasını ekran bilgisayarına kopyala. ZIP'in tamamını bir klasöre çıkar.
3. `KURULUM-BASLAT.bat` dosyasını çalıştır. Yönetici yetkisi ister.
4. Sunucu adresini (`http://SUNUCU_ADI:5000`), paneldeki ekran kodunu ve aynı cihaz tokenını gir. Token boş olamaz.
5. Kurulumdan sonra Player'ı aç; panelden bu ekrana bir içerik veya liste gönder.

Kurulum dosyaları kopyalar, yardımcı servisi ve otomatik başlangıcı ayarlar. .NET Framework 4.8 eksikse Microsoft'tan indirmeyi dener. Yeniden başlatma istenirse bilgisayarı yeniden başlatıp kurulumu tekrar çalıştır.

Ayarlar: `C:\CompanySignagePublic\Player\settings.json`

İndirilen içerikler: `C:\CompanySignagePublic\Player\Cache`

Kurulum günlüğü: `C:\CompanySignagePublic\Player\Logs\install.log`

Bu dosyalar kendi sunucu ve cihaz bilgilerini içerir; kaynak kod deposuna ekleme. Geliştirme sırasında ayrı bir konum kullanmak için `COMPANY_SIGNAGE_DATA_DIR` ortam değişkenini ayarlayabilirsin.

Proje Windows 7 için .NET Framework 4.8 hedefliyor. Windows 7'de SP1 ve gerekli sistem güncellemeleri olmalı. Bu GitHub sürümünün gerçek Windows 7 kurulumu ve video oynatması ayrıca denenmeli; yapılan otomatik kontroller güncel Windows üzerindedir.

Görüntü gelmiyorsa önce sunucu adresini, ekran kodunu/tokenı ve panelde ekranın aktif olup olmadığını kontrol et. Video sorunu varsa aynı dosyanın ekran bilgisayarında oynatılabildiğini de kontrol et.
