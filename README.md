# Company Signage

Bu projeyi birden fazla ekrandaki görsel ve videoları tek bir yerden değiştirebilmek için yaptım. Mesela lobide bir karşılama görseli, restoranda menü, toplantı salonunda da günün programı açık olabilir. Bunları panelden seçip istediğim ekrana gönderebiliyorum.

Amacım, içerik değişeceği zaman her ekranın yanına ayrı ayrı gitmek zorunda kalmamaktı.

![Yönetim paneli](docs/images/panel.png)

## Nasıl çalışıyor?

Bir bilgisayar sunucu oluyor, yönetim paneli burada çalışıyor. Paneli tarayıcıdan açıp dosyalarımı yüklüyorum. Sonra bir liste hazırlayıp hangi ekranda oynayacağını seçiyorum.

Ekranın bağlı olduğu bilgisayarda da **Player** adında küçük bir uygulama çalışıyor. Gönderdiğim dosyaları indirip tam ekran gösteriyor. Yani televizyonun kendisine değil, ona bağlı Windows bilgisayara kuruluyor.

Sunucuyla bağlantı kesilirse indirdiği son içeriklerle devam edebiliyor. Yeni bir şey göndermek için bağlantının tekrar gelmesi gerekiyor.

## Panelden neler yapabiliyorum?

- Görsel ve video yükleyebiliyorum.
- Birkaç içeriği liste yapıp sırayla oynatabiliyorum.
- Görsellerin kaç saniye kalacağını ve sesin açık olup olmayacağını seçebiliyorum.
- Aynı listeyi birden fazla ekrana gönderebiliyorum.
- Hangi ekranın bağlı olduğunu görebiliyor, yayını durdurup yenileyebiliyorum.

![İçerik yönetimi](docs/images/icerikler.png)

*Buradaki ekran görüntülerini örnek içeriklerle hazırladım.*

## Denemek istersen

Windows bilgisayarda **.NET 10 SDK** ve **SQL Server Express** kurulu olması gerekiyor. Ekran tarafındaki Player ise **.NET Framework 4.8** kullanıyor.

```powershell
git clone https://github.com/yusufcinaar/company-signage.git
cd company-signage
dotnet build CompanySignage.sln -c Release
dotnet run --project src/CompanySignage.Web
```

SQL bağlantısını `localhost\SQLEXPRESS` olarak ayarladım. Sende farklıysa `src/CompanySignage.Web/appsettings.json` dosyasından değiştirebilirsin. İlk açılışta `CompanySignagePublic` adında bir veritabanı oluşturuyor; kullandığın Windows hesabının SQL'de veritabanı oluşturma yetkisi olmalı.

Panel adresi: **http://localhost:5000**. İlk giriş için kullanıcı adı `admin`, örnek şifre `Admin123!`. Kendi kullanımına geçmeden önce **Ayarlar** sayfasından şifreyi değiştir.

Panelde ekran eklerken bir **ekran kodu** ve **cihaz tokenı** belirliyorsun. Player'a da aynı bilgileri girmen gerekiyor. Tokenı o ekranın bağlantı şifresi gibi düşünebilirsin.

Başka bilgisayarlara kurmak için adımlar: [Kurulum](docs/INSTALLATION.md) · [Player kurulumu](docs/PLAYER_SETUP.md).

## Kullandığım teknolojiler

Projeyi C# ile geliştirdim. Panelde ASP.NET Core, veritabanında SQL Server ve Entity Framework Core var. Ekran uygulaması WPF ile çalışıyor. Panelden gönderilen komutların ekrana ulaşması için de SignalR kullanılıyor.

## Küçük bir not

Yüklemeden önce giriş, yayın gönderme, liste sırası, zamanlama ve bağlantı kesilmesi gibi kısımları test ettim. Testlerin ayrıntıları [burada](tests/README.md).

Player'ı Windows 7'yi de düşünerek hazırladım ama bu sürümü gerçek bir Windows 7 bilgisayarda ve uzun süreli TV/video kullanımında ayrıca denemek gerekiyor.
