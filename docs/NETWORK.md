# Ağ bağlantısı

Geliştirme ayarı yalnızca `127.0.0.1:5000` üzerinde dinler. Ekran bilgisayarları başka cihazlarsa sunucu kurulumu sırasında `PublicHost` ile ulaşılabilir adresi belirt. Kurulum scripti sunucuyu 5000 portunda ağdan erişilebilir yapar.

- Ekran bilgisayarında sunucunun panel adresi açılmalı.
- Güvenlik duvarında seçilen TCP portuna izin verilmiş olmalı.
- Ekran kodu ve token her cihazda kendi panel kaydıyla eşleşmeli.
- Medya indirme ve SignalR dahil cihaz istekleri `X-Screen-Code` ve `X-Device-Token` başlıklarını gönderir. Başlıksız bir API denemesinin 401 dönmesi beklenen davranıştır.

Yönetim API'si cookie oturumu kullanır. `GET /api/auth/csrf` ile alınan token, giriş dahil değiştirme isteklerinde `X-CSRF-TOKEN` başlığıyla gönderilir. Girişten sonra token yeniden alınır. Tarayıcı paneli kendi formlarında bunu yapar.

Yayın planlama API'sinde tarihleri ISO 8601 biçiminde, `Z` veya saat dilimi farkıyla gönder. Veritabanında UTC kullanılır. Başlangıç dahil, bitiş hariçtir. Eşzamanlı atamalarda önce yüksek öncelik, sonra yeni başlangıç tarihi ve yeni kayıt seçilir. Player tarih değişikliklerini yaklaşık 5 saniyelik sorgu aralığında fark eder.

Varsayılan HTTP ayarı yerel deneme içindir. İnternete açılacak kurulumda HTTPS kullan; cihaz tokenı ve yönetim oturumu şifreli taşınmalı. Cihazların eriştiği adresi Player ayarlarında da güncelle. Paylaşılan kaynak kodda işletmenin gerçek IP veya şifrelerini saklama.
