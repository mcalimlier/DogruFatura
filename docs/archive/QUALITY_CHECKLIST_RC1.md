# DoğruFatura 1.0.0 RC1 — Final Kalite Kontrolü

Bu liste otomatik CI kontrollerine ek olarak gerçek Windows oturumunda yapılacak son kullanıcı testidir. Final `v1.0.0` etiketi verilmeden önce tamamlanır.

## A. Görsel ekran turu

Her ekranda kesik yazı, taşan buton, bulanık metin, yatay kaydırma ve gereksiz boşluk olmamalıdır.

- [ ] Ana Ekran
- [ ] Fatura Girişi
- [ ] Yetkili / İmza
- [ ] Fatura Listesi
- [ ] Raporlar / Çıktı
- [ ] Okul / Kurum
- [ ] İşlem Arşivi
- [ ] Kontrol Merkezi
- [ ] Hakkında / Güvenlik
- [ ] Açılış ekranı
- [ ] Arşiv Ekle önizleme penceresi

## B. Ekran / ölçekleme

- [ ] 1366×768 / %100
- [ ] 1920×1080 / %100
- [ ] 1920×1080 / %125
- [ ] 1920×1080 / %150
- [ ] Mümkünse 2K / %150–175
- [ ] Mümkünse 4K / %200
- [ ] Pencere küçültme/büyütme ve tam ekran

## C. Klavye ve erişilebilirlik

- [ ] TAB sırası doğal ilerliyor
- [ ] Klavye odağı altın kenarlıkla net görünüyor
- [ ] Enter uygun formda ana işlemi çalıştırıyor
- [ ] Esc düzenlemeyi/temizlemeyi güvenli biçimde iptal ediyor
- [ ] Ctrl+F açık ekrandaki aramaya gidiyor
- [ ] Ctrl+P rapor ekranında yazdırmayı açıyor
- [ ] Alt+A/F/Y/L/R/O/I/K/H menü kısayolları çalışıyor
- [ ] Windows Yüksek Karşıtlık modunda temel kontroller okunuyor

## D. İşlevsel uçtan uca tur

- [ ] Test faturası oluşturma
- [ ] Mükerrer fatura engeli
- [ ] PDF Aktif Çıktılar klasörüne oluşuyor
- [ ] Aktif liste arşivleniyor
- [ ] HYS ile kapanınca PDF Arşivlenenler'e taşınıyor
- [ ] Kapanış geri alınınca PDF Aktif Çıktılar'a dönüyor
- [ ] ARŞİV EKLE sıra numaralı HYS PDF'yi kabul ediyor
- [ ] Kaynak PDF korunuyor
- [ ] Güvenlik yedeği oluşuyor
- [ ] Yedekten geri yükleme çalışıyor
- [ ] Tanılama Paketi oluşuyor ve içinde kullanıcı JSON/PDF verisi yok
- [ ] Kontrol Merkezi beklenen hataları engelliyor/gösteriyor

## E. Setup turu

CI otomatik olarak temiz kurulum, tekrar/güncelleme kurulumu ve kaldırma sonrası kullanıcı verisi korumasını test eder. Final imzalı paket hazırlandığında ayrıca gerçek kullanıcı bilgisayarında:

- [ ] Temiz Setup kurulumu
- [ ] Mevcut sürüm üstüne güncelleme
- [ ] Program kaldırma
- [ ] Yeniden kurulum
- [ ] Masaüstü\DoğruFatura kullanıcı verileri tüm aşamalarda korunuyor
- [ ] SHA-256 dosyası Setup ile eşleşiyor
- [ ] Kod imzası kullanılıyorsa Windows özelliklerinde imza geçerli görünüyor

## Final şartı

Otomatik `build-and-test` ve `setup-smoke` kontrolleri yeşil, bu manuel liste kritik hata olmadan tamamlanmış ve kullanıcı RC1'i onaylamış olmalıdır. Bundan sonra `VersionSuffix` kaldırılır, `v1.0.0` oluşturulur ve final Setup üretilir.
