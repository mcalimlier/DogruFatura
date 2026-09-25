# DoğruFatura 1.1 Final — EXE Oluşturma Notları

## 25.09.2026

### Tespit
Release kalite kapısında uygulama projeleri başarıyla derlenirken `DogruFatura.Tests` projesinde PDFsharp 6.1+ API değişikliği nedeniyle iki derleme hatası oluştu.

Hatalar:
- `LegacyHysPdfImportTests.cs` satır 436
- `LegacyHysPdfImportTests.cs` satır 437

`XUnit` tipine doğrudan `int` ataması artık obsolete olduğu için test derlemesi duruyordu.

### Uygulanan düzeltme
PDFsharp'ın önerdiği API kullanıldı:

- `page.Width = XUnit.FromPoint(1152)`
- `page.Height = XUnit.FromPoint(576)`

Bu değişiklik yalnızca test PDF'sinin sayfa boyutu oluşturma kodunu düzeltir; uygulamanın arşiv/OCR işleyişini değiştirmez.

### Sonraki adım
GitHub Desktop'ta **Fetch origin** sonrasında `main` dalını güncelleyin ve final EXE oluşturma komutunu yeniden çalıştırın.

Üretim kodundaki mevcut nullable/code-analysis uyarıları hata değildir; EXE üretimini engelleyen iki test derleme hatası giderilmiştir.


## 2026-09-25 — Legacy ADSL sıra numarası testi

- `GrossOnlyLegacyAdslListCreatesSourceEvidenceWithoutInventingTaxes` testindeki tek başarısız senaryo düzeltildi.
- Sorun: ADSL tablosu başlığı ile ilk veri satırı 30 puandan daha yakın olduğunda başlık algılama kodu ilk veri satırını başlık satırına katıyor ve ilk sıra numarasını veri taramasından dışarıda bırakıyordu.
- Çözüm: Başlık satırı kendi başına tam algılanabiliyorsa sonraki satır artık başlığa eklenmiyor; yalnızca başlık parçaları eksikse yakın satır birleştiriliyor.
- `fix/archive-import-legacy-hys` dalı `main` ile senkronlandı; dalda main'e göre ayrıca korunması gereken benzersiz değişiklik bulunmuyordu.
- Beklenen sonuç: 155 otomatik testin tamamının geçmesi ve final EXE kalite kapısının Setup üretimine devam etmesi.


## 2026-09-25 — Final EXE uyarı temizliği

- `LegacyHysPdfImportService.cs` içindeki nullable `invoiceType` uyarıları giderildi; doğrulanmış telefon/İnternet türü için null güvenli değer kullanılıyor.
- Eski telefon HYS başlıklarındaki tekrar oluşturulan dizi bağımsız değişkenleri `static readonly` alias dizilerine taşındı; CA1861 uyarıları giderildi.
- `HistoricalPdfImportReportTests.cs` içindeki xUnit `StartsWith` / `Contains` çağrıları açık `StringComparison.Ordinal` kullanacak şekilde güncellendi; CA1307 uyarıları giderildi.
- Hedef: Release derlemesinde üretim kodu ve testlerde gereksiz analyzer/nullable uyarılarının temizlenmesi; işlevsel testlerin davranışı değiştirilmedi.


## 2026-09-25 — Kalan 21 WPF + 3 test uyarısının doğrudan temizlenmesi

Kullanıcının final EXE çıktısında kalan uyarılar tekrar incelendi. Önceki temizlikte alias dizileri tanımlanmış olmasına rağmen TryFindLegacyTelephoneHeader içinde bazı çağrılar hâlâ inline dizi oluşturuyordu; ayrıca telefon fatura türü akışında nullable değer bazı çağrılara taşınıyordu.

Uygulanan son düzeltmeler:
- TryFindLegacyTelephoneHeader içindeki 10 adet tekrar eden inline alias dizisi, mevcut static readonly alias alanlarını kullanacak şekilde değiştirildi; CA1861 kaynakları kaldırıldı.
- TryParseLegacyTelephoneListPages içinde FindInvoiceType sonucu doğrulandıktan sonra validatedInvoiceType ile tüm devam eden çağrılar null güvenli hâle getirildi; CS8604 kaynakları kaldırıldı.
- HistoricalPdfImportReportTests.cs içinde kalan 3 Assert.Contains çağrısına StringComparison.Ordinal eklendi; CA1307 kaynakları kaldırıldı.

Bu değişikliklerin ardından yerel Windows Release kalite kapısının yeniden çalıştırılması gerekir. Bu ortamda kullanıcının yerel Windows SDK/build çıktısı doğrudan çalıştırılmadığından, 0 uyarı sonucu ancak kullanıcının yeni final BAT çıktısıyla kesinleşmiş sayılacaktır.


## 2026-09-25 — Kapsam hatası düzeltmesi

Son uyarı temizliği uygulanırken `validatedInvoiceType` adı yanlışlıkla `TryParseEmbeddedInvoicePages` metodunun kapsamına da taşınmış ve Release derlemesinde 5 adet CS0103 oluşmuştur. Bu kapsam hatası düzeltildi; gömülü elektrik/diğer fatura ayrıştırıcısı tekrar kendi `invoiceType` değişkenini kullanıyor. Telefon legacy ayrıştırıcısında ise doğrulama sonrası `validatedInvoiceType` kullanılmaya devam ediyor.

Düzeltme commit'i: `e10348fd4cf341b7d689d60a58b3cc56bf068063`.

Yeni final BAT çalıştırmasıyla önce 5 CS0103 hatasının, ardından kalan analyzer uyarılarının 0'a indiği doğrulanmalıdır.
