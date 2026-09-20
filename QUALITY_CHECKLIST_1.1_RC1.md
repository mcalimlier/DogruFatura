# DoğruFatura 1.1.0 RC1 — Final Kalite Kontrolü

Bu liste 1.1.0 final etiketi verilmeden önce gerçek Windows oturumunda tamamlanır. Otomatik CI kontrollerine ek kullanıcı smoke turudur.

## A. Kalite kapısı

- [ ] `DERLE_CALISTIR.bat`: 0 warning / 0 error
- [ ] `TESTLERI_CALISTIR.bat`: 126/126
- [ ] GitHub `build-and-test` yeşil
- [ ] GitHub `setup-smoke` yeşil
- [ ] NuGet güvenlik taramasında bilinen açık yok

## B. Ana ekran turu

Her ekranda kesik yazı, taşan buton, yatay kaydırma, anlamsız boşluk veya bulanık metin olmamalıdır.

- [ ] Ana Ekran
- [ ] Fatura Girişi
- [ ] Yetkili / İmza
- [ ] Fatura Listesi
- [ ] Raporlar / Çıktı
- [ ] Okul / Kurum
- [ ] İşlem Arşivi
- [ ] Kontrol Merkezi
- [ ] İstatistik
- [ ] Hakkında / Güvenlik
- [ ] Açılış ekranı
- [ ] Arşiv Ekle önizleme / ilerleme penceresi

## C. İstatistik

- [ ] Sayfada gereksiz dikey scrollbar yok
- [ ] 5 sütunlu filtre/işlem kartı düzgün
- [ ] Fatura Türü Dağılımı üstten/alttan kesilmiyor
- [ ] Yüzde etiketleri halka içinde net ve okunur
- [ ] Hover detayı merkez toplam ödeme alanının arkasında kalmıyor
- [ ] ÇIKTI AL → PDF
- [ ] ÇIKTI AL → Yazdır
- [ ] ÇIKTI AL → CSV; Excel'de kolonlar ve Türkçe sayılar doğru
- [ ] ÇIKTI AL → Excel
- [ ] ÇIKTI AL → PNG

## D. Fatura ve mükerrer kontrolü

- [ ] Normal fatura kaydı
- [ ] Fatura numarası dolu kesin mükerrer engeli
- [ ] Boş numaralı benzer kayıt için olası mükerrer uyarısı
- [ ] Olası mükerrer uyarısında varsayılan Hayır kayıt yapmıyor
- [ ] Evet seçildiğinde kullanıcı kayda devam edebiliyor
- [ ] Su faturası damga muafiyeti
- [ ] Elektrik kesinti tazminatı hesabı

## E. Rapor / PDF / arşiv

- [ ] Ödeme listesi PDF oluşuyor
- [ ] PDF görünümü ve sayfa sınırları doğru
- [ ] PDF metadata `Schema=DogruFaturaPdf/1`
- [ ] Aktif liste arşivleniyor
- [ ] HYS ile kapanınca PDF Arşivlenenler'e taşınıyor
- [ ] Kapanış geri alınınca PDF Aktif Çıktılar'a dönüyor
- [ ] Tarihsel PDF ARŞİV EKLE akışı çalışıyor
- [ ] Çoklu import ilerlemesi görünüyor
- [ ] Import iptali güvenli çalışıyor
- [ ] Kaynak PDF korunuyor

## F. Veri güvenliği

- [ ] KONTROL ET bütünlük sonucunu gösteriyor
- [ ] YEDEK OLUŞTUR ZIP oluşturuyor
- [ ] GERİ YÜKLE doğrulayıp geri yüklüyor
- [ ] Geri yükleme öncesi koruyucu yedek oluşuyor
- [ ] GELİŞMİŞ / DESTEK → TANILAMA PAKETİ oluşuyor
- [ ] Tanılama ZIP'inde kullanıcı JSON/PDF dosyaları yok
- [ ] Tanılama logunda kişisel veri örnekleri maskeli
- [ ] Beklenmeyen kapanma kurtarma davranışı çalışıyor

## G. Ekran / ölçekleme

- [ ] 1366×768 / %100
- [ ] 1920×1080 / %100
- [ ] 1920×1080 / %125
- [ ] 1920×1080 / %150
- [ ] Pencere küçültme/büyütme ve tam ekran
- [ ] Mümkünse 2K/4K yüksek DPI kontrolü

## H. Setup turu

- [ ] `SETUP_HAZIRLA.ps1 -AllowPrerelease -AllowUnsignedFinal` RC smoke paketi üretiyor
- [ ] Dosya adı `DogruFatura_Setup_1.1.0-rc1_x64.exe`
- [ ] Kurulum sihirbazı başlığı `DoğruFatura 1.1.0 RC1` gösteriyor
- [ ] Temiz kurulum
- [ ] Mevcut sürüm üstüne güncelleme
- [ ] Program kaldırma
- [ ] Yeniden kurulum
- [ ] Masaüstü\DoğruFatura kullanıcı verileri korunuyor
- [ ] SHA-256 dosyası Setup ile eşleşiyor
- [ ] Release notes ve kullanıcı kılavuzu dağıtım klasöründe

## Final geçişi

Tüm kritik kutular tamamlandıktan ve kullanıcı RC1'i kabul ettikten sonra:
- `Directory.Build.props` → `VersionSuffix` boşaltılır,
- final sürüm `1.1.0` olur,
- final Setup yeniden üretilir,
- final SHA-256 alınır,
- release etiketi/paketi hazırlanır.
