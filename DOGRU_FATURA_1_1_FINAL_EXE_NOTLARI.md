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
