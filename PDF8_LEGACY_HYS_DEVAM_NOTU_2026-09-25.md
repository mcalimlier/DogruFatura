# PDF 8 / Legacy HYS İçe Aktarma Devam Notu — 2026-09-25

## Devam edilen nokta

Çalışma `fix/archive-import-legacy-hys` dalındaki `b9e397c56eff44ea7cbbfcdef9461d2f8d130290` commitinden devam ediyor.

Son derleme düzeltmesi:
- `LegacyHysPdfImportService.cs` içindeki `TryMoney(string, out decimal)` metodunda `out` değişkeni başlangıçta `0m` ile başlatıldı.
- Böylece CS0177 derleme hatasının kaynağı giderildi.

## PDF 8 hedefi

PDF 8 için mevcut test kapsamı doğrudan istenen senaryoyu içeriyor:
- 4 fatura aynı ödeme emri altında okunuyor.
- Fatura numaraları benzersiz kontrol ediliyor.
- Brüt + damga + net toplamları ödeme emriyle karşılaştırılıyor.
- Arşiv kaydı oluşturuluyor.
- Beklenen toplam net ödeme: **5.386,97 TL**.
- Bölünmüş başlık, OCR-benzeri para çıktısı ve tarih yerleşimi gibi varyasyonlar için regresyon testleri bulunuyor.

## Mevcut hızlı tarama davranışı

- Önce PDF metin katmanı hızlı okunuyor.
- Başarılı/ doğrulanabilir dosyalar OCR'a girmeden arşivleniyor.
- Başarısız dosyalar güçlü tarama/OCR aşamasına ayrılıyor.
- Aynı PDF'in metin sayfalarının gereksiz yere tekrar render edilmesi engelleniyor.
- OCR sonucu ve tarama durumu raporlanıyor.

## Son CI durumu

`b9e397c56eff44ea7cbbfcdef9461d2f8d130290` commitinin ilk GitHub Actions çalışması başarısız olmuştu. Çalışma yalnızca birkaç saniye içinde sonlandığı için başarısızlığın ayrıntılı logu GitHub connector üzerinden alınamadı.

Aynı workflow için başarısız işler **25.09.2026 18:59 itibarıyla yeniden çalıştırıldı** (attempt 2). Sonuç bekleniyor.

## Sonraki doğrulama

CI tekrar çalışması tamamlandığında:
1. Release build sonucu kontrol edilecek.
2. `LegacyHysPdfImportTests` içindeki PDF 8 / dört fatura regresyonları doğrulanacak.
3. Başarılıysa dal korunacak ve PDF 8'in gerçek arşiv aktarımı kullanıcı tarafında test edilecek.
4. Başarısızsa yalnızca gerçek hata noktasına yönelik yeni düzeltme yapılacak; çalışan PDF import yolları gereksiz yere değiştirilmemeli.

## Değişiklik ilkesi

PDF 8 için hedef yalnızca dosyayı okumak değil; dört faturanın bilgilerini güvenilir biçimde çıkarıp **tek ödeme emri altında**, toplamları doğrulanmış şekilde **arşiv kaydına dönüştürmek**. Hızlı tarama performansı korunurken güçlü fallback yalnızca gerekli dosyalara uygulanmalıdır.
