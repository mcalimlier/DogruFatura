# DogruFatura — PDF Tarama Uyarı ve Kapanma Düzeltmesi

## Tarih
25.09.2026

## Yapılan düzeltmeler

### Derleme uyarıları
`LegacyHysPdfImportService.cs` içindeki bildirilen 17 uyarı düzeltildi:
- `CS8604` nullability uyarıları
- `CA1861` tekrar oluşturulan alias dizileri
- `CA1826` gereksiz `FirstOrDefault()`
- `CA1822` static yapılabilecek yöntem

### Arşiv PDF aktarımında kapanma koruması
Paralel hızlı taramada WPF UI thread ile arka plan PDF tarama thread'lerinin etkileşimi güvenli hale getirildi:
- Progress nesnesi UI thread üzerinde oluşturuluyor.
- Arka plan progress mesajları Dispatcher üzerinden UI'ya aktarılıyor.
- Tek PDF'deki beklenmeyen hata tüm aktarımı düşürmüyor.
- Hatalı PDF güçlü taramaya ayrılıyor.
- SemaphoreSlim yalnız gerçekten alınmışsa serbest bırakılıyor.
- UI güncellemelerinde async Dispatcher kullanılıyor.

### Korunan tarama düzeni
- Hızlı tarama: 4 paralel PDF.
- Hızlı tarama bütçesi: 20 saniye/PDF.
- Güçlü tarama bütçesi: 45 saniye/PDF.
- Zaten arşivde olan HYS/MYS PDF'leri yeniden okunmuyor.
- OCR denendi/atlandı bilgisi raporlanıyor.
- PDF #9 için dosyaya özel OCR istisnası yok.

## GitHub commitleri
- `87964e15ad8bdcd24bce44d4eb91d90b07691395` — analyzer uyarıları
- `3e33400397c6b9f6e415f72bfe8ec49c37e08e66` — paralel import kapanma koruması
- `f5f3c41b338e4fcf5a48eb71a5cd07f522559d88` — kod bloğu yapısal düzeltme

## Windows doğrulaması
Bu ortamda gerçek WPF uygulaması çalıştırılamadığından kapanma probleminin tamamen çözüldüğü burada doğrulanamaz.

Windows'ta önce:
`TESTLERI_CALISTIR.bat`
`DERLE_CALISTIR.bat`

sonra daha önce programı kapatan arşiv PDF aktarımı tekrar denenmeli.

Beklenen sonuç: program kapanmamalı; sorunlu PDF hata olarak tutulmalı ve güçlü taramaya/rapora devam etmeli.
