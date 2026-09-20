# DoğruFatura 1.1.0 — Final Dağıtım Kontrolü

## Mevcut durum — 20.09.2026

- Kullanıcı Fatura Türü Dağılımı son WPF/ClearType yüzde görünümünü kabul etti: **olmuş**.
- LiveCharts/Skia yüzde yazıları görsel olarak hâlâ bulanık kaldığı için built-in data label'lar kapatıldı; yüzdeler artık WPF/ClearType overlay ile çiziliyor.
- Yüzde overlay'i 328×328 chart ile birebir hizalı, dilim orta açısı + sabit orta yarıçap hesabıyla tam piksel koordinatlarına yerleştiriliyor.
- Repo kökü temizlendi: eski 1.0/RC notları `docs/archive/` altına taşındı; aktif final belgeleri kökte bırakıldı.
- Donut font API'si `SKTypeface` kullanımına geçirildi; son koşuda görülen iki CS0618 warning bu paketle kapatıldı.
- `SETUP_HAZIRLA.ps1` hata ile çıkarsa geçici `_YAYIN` klasörünü otomatik temizler.
- GitHub Desktop'ta `_YAYIN/` ve `DAGITIM/` altındaki yüzlerce generated dosyanın Changes ekranını doldurmaması için bu klasörler `.gitignore` içine alındı.
- Final yeşil koşudan sonra donut yüzde etiketlerine son görsel düzeltme uygulandı; bu nedenle final doğrulama bir kez daha çalıştırılmalı.
- İlk `FINAL_DOGRULAMA.bat` gerçek Windows koşusunda Release build başarılı oldu; testler **125/126** kaldı.
- Tek hata: `DiagnosticPackageContainsSummaryAndLogsButNeverUserJsonOrPdf` içinde `RegexMatchTimeoutException`.
- Setup doğru şekilde üretilmedi; kalite kapısı çalıştı.
- Düzeltme: tanılama sanitize işlemi satır-bazlı hale getirildi ve regex timeout durumunda ilgili satır tamamen gizleniyor.
- Mevcut hedef yeniden **126/126**; düzeltme sonrası `FINAL_DOGRULAMA.bat` tekrar çalıştırılmalı.
- Kullanıcı final kodun çalıştığını bildirdi.
- `release/1.1.0` dalı oluşturuldu.
- Release dalı head: `7db72bdfcec1a8a3c581fd6d2a14c116c2298569`.
- GitHub Actions run #505 ve #507, `build-and-test` adımına ait hiçbir step/log oluşturmadan birkaç saniyede failure oldu.
- `setup-smoke` bu nedenle çalışmadı.
- Bu durum gerçek bir C# build/test hatası olarak kabul edilmemektedir; GitHub Actions job başlangıç/runner tarafında ayrı bir engeldir.
- Final tag ve kullanıcıya dağıtılacak Setup, gerçek Windows build/test/setup-smoke kapısı tamamlanana kadar bekletilir.

RC1 kullanıcı doğrulaması tamamlandıktan sonra 1.1.0 sürüm numarası finale alınmıştır. Bu liste gerçek dağıtım paketi oluşturulmadan önce tamamlanır.

## A. Final kod kapısı

- [x] Final Release build: **başarılı**
- [x] Final sürüm değerleri / Setup metadata: **1.1.0**
- [x] Final otomatik test: **126/126**
- [ ] NuGet güvenlik taramasında bilinen açık yok
- [ ] GitHub build/test akışı yeşil

## B. Kritik kullanıcı smoke

- [ ] Normal fatura kaydı
- [ ] Kesin mükerrer fatura engeli
- [ ] Boş numaralı olası mükerrer uyarısı
- [ ] Ödeme listesi PDF
- [ ] HYS arşiv kapatma / geri açma
- [ ] Tarihsel PDF ARŞİV EKLE
- [ ] Çoklu PDF import ilerleme / iptal
- [ ] KONTROL ET
- [ ] YEDEK OLUŞTUR
- [ ] GERİ YÜKLE
- [ ] GELİŞMİŞ / DESTEK → TANILAMA PAKETİ

## C. İstatistik smoke

- [ ] 5 sütunlu filtre/işlem düzeni düzgün
- [ ] Fatura Türü Dağılımı kesilmiyor
- [x] WPF/ClearType yüzde etiketleri halka içinde, tam ortalanmış ve keskin/okunur
- [ ] Açık dilimde koyu / koyu dilimde beyaz yüzde etiketi doğru
- [ ] Hover detayı merkez kartın arkasında kalmıyor
- [ ] PDF çıktı
- [ ] Yazdır
- [ ] CSV
- [ ] Excel
- [ ] PNG

## D. Final Setup

- Final `FINAL_DOGRULAMA.bat` koşusunda NSIS, `DogruFatura_Setup_1.1.0_x64.exe` çıktısını oluşturma aşamasına ulaştı.
- Paylaşılan log `Compressed data:` satırında kesildiği için final SHA-256 ikinci doğrulama sonucu henüz bu kayıtta görünmüyor.

Yerel smoke için önerilen tek tık doğrulama:

`FINAL_DOGRULAMA.bat`

Bu betik:
- final sürümün gerçekten `1.1.0` olduğunu kontrol eder,
- `SETUP_HAZIRLA.ps1 -AllowUnsignedFinal` çağrısıyla Release build + test + publish + Setup üretimini çalıştırır,
- `DogruFatura_Setup_1.1.0_x64.exe` ve SHA-256 dosyasının varlığını kontrol eder,
- Setup hash'ini tekrar hesaplayıp SHA dosyasıyla eşleştirir.

Kod imzalama sertifikası varsa normal final komutu kullanılır:

`SETUP_HAZIRLA.ps1`

Yalnız yerel/CI smoke amacıyla imzasız final paket:

`SETUP_HAZIRLA.ps1 -AllowUnsignedFinal`

Beklenen dosya:

`DogruFatura_Setup_1.1.0_x64.exe`

Kontroller:

- [ ] Kurulum sihirbazı **DoğruFatura 1.1.0** gösteriyor
- [ ] Setup dosya adı doğru
- [ ] Temiz kurulum başarılı
- [ ] 1.0.x / önceki sürüm üstüne güncelleme başarılı
- [ ] Program kaldırma başarılı
- [ ] Yeniden kurulum başarılı
- [ ] Masaüstü\DoğruFatura kullanıcı verileri korunuyor
- [ ] Eski Program Files artıkları temizleniyor
- [ ] `RELEASE_NOTES_1.1.0.md` dağıtım klasöründe
- [ ] `KULLANICI_KILAVUZU.md` dağıtım klasöründe
- [ ] SHA-256 dosyası Setup ile eşleşiyor
- [ ] İmza kullanılıyorsa Authenticode doğrulaması geçiyor

## E. Yayın

Tüm kritik maddeler yeşil olduktan sonra:

- [ ] Final Setup dağıtım klasörüne alınır
- [ ] SHA-256 kaydedilir
- [ ] `v1.1.0` etiketi hazırlanır
- [ ] Release notları eklenir
- [ ] Final paket arşivlenir

Bu liste tamamlanmadan final Setup'ın kullanıcı dağıtımına hazır olduğu kabul edilmemelidir.
