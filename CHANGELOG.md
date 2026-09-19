# Değişiklikler

## v0.1.0 — İlk GitHub sürümü

- Yönetim API'sine oturum ve CSRF kontrolü eklendi.
- Player, medya indirme ve SignalR bağlantılarında cihaz tokenı doğrulanıyor.
- Bir ekran başka ekranın SignalR grubuna katılamıyor.
- Yayınlar tarih ve önceliğe göre seçiliyor; API tarihleri UTC'ye dönüştürülüyor.
- Atama silinince uygun yayın yeniden seçiliyor; yayın kalmadıysa ekran temizleniyor.
- Durdurulan yayın sonraki sorguda veya yeniden açılışta geri gelmiyor. Gelecek tarihli yayınlar korunuyor.
- Önbellekteki tüm liste dosyaları eksikse sonsuz çağrı döngüsü oluşmuyor.
- Temiz ortamda paket oluşturmayı engelleyen restore sorunu düzeltildi.
- Kurulum notları güncellendi, otomatik kontroller ve örnek ekran görüntüleri eklendi.

Git geçmişi bu sürümle başlıyor. Önceki geliştirme adımları için sonradan tarih veya commit oluşturulmadı.
