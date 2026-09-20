# DoğruFatura 1.1.0 — Final Dağıtım Kontrolü

## Mevcut durum — 20.09.2026

- Kullanıcı final kodun çalıştığını bildirdi.
- `release/1.1.0` dalı oluşturuldu.
- Release dalı head: `7db72bdfcec1a8a3c581fd6d2a14c116c2298569`.
- GitHub Actions run #505 ve #507, `build-and-test` adımına ait hiçbir step/log oluşturmadan birkaç saniyede failure oldu.
- `setup-smoke` bu nedenle çalışmadı.
- Bu durum gerçek bir C# build/test hatası olarak kabul edilmemektedir; GitHub Actions job başlangıç/runner tarafında ayrı bir engeldir.
- Final tag ve kullanıcıya dağıtılacak Setup, gerçek Windows build/test/setup-smoke kapısı tamamlanana kadar bekletilir.

RC1 kullanıcı doğrulaması tamamlandıktan sonra 1.1.0 sürüm numarası finale alınmıştır. Bu liste gerçek dağıtım paketi oluşturulmadan önce tamamlanır.

## A. Final kod kapısı

- [ ] `DERLE_CALISTIR.bat`: **0 warning / 0 error**
- [ ] Uygulama sürüm göstergesi: **v1.1.0**
- [ ] `TESTLERI_CALISTIR.bat`: **126/126**
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
- [ ] Yüzde etiketleri halka içinde ve okunur
- [ ] Açık dilimde koyu / koyu dilimde beyaz yüzde etiketi doğru
- [ ] Hover detayı merkez kartın arkasında kalmıyor
- [ ] PDF çıktı
- [ ] Yazdır
- [ ] CSV
- [ ] Excel
- [ ] PNG

## D. Final Setup

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
