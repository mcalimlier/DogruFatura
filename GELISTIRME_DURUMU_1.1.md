# DoğruFatura 1.1 — Geliştirme Durumu

Bu dosya DoğruFatura 1.1 sağlamlaştırma çalışmasının güncel durum kaydıdır. Yeni sohbette bu dosya esas alınmalı; tamamlanan işler yeniden yapılmamalıdır.

## Çalışma dalı

- Repo: `mcalimlier/DogruFatura`
- Branch: `hardening/1.1-data-safety`
- `main` başlangıç commit'i: `72963ec0a829f2e4b487393160c7ca93290b470f`
- `main` ile henüz merge yapılmayacak.

## Genel durum

### Aşama 1 — Veri Güvenliği

Kod/test kapsamı tamamlandı:

- Bozuk veri kurtarma davranışı güvenli hale getirildi.
- Veri şema sürümleme ve migration eklendi.
- Restore öncesi semantik doğrulama eklendi.
- Yedek manifest/SHA-256 ve arşiv PDF bütünlük doğrulaması eklendi.
- Tek örnek/yazma güvenliği fail-closed hale getirildi.

GitHub Actions runner sorunu nedeniyle CI doğrulaması halen ayrı olarak bekliyor.

### Ek kullanıcı arayüzü düzeltmeleri

- Sağ üst pencere simgeleri eşitlendi.
- Splash yaklaşık 5,5 saniye görünür ve ana pencere yükünden bağımsız akıcı hale getirildi.
- Yerel build/çalıştırma akışı `DERLE_CALISTIR.bat` üzerinden ve `--no-incremental` tam analizle yapılıyor.

### Aşama 2 — Mimari Temizlik

Tamamlananlar:

- Merkezi `InvoiceRulesEngine` oluşturuldu.
- `InvoiceValidationService` ve Kontrol Merkezi aynı motoru kullanıyor.
- Dependency Injection ilk dilimi tamamlandı.
- Son doğrulanan DI commit'i: `427fb23d309b53db9366056ea27730e546d9eba2`.

Kalan ana işler:

- DI kapsamını rapor/arşiv servislerine genişletmek.
- `MainWindow` sorumluluklarını daha fazla ayırmak.
- Tür/status alanlarını enum/value object yapısına taşımak.
- `SchoolRecord` için değişmez `SchoolId` eklemek.

## Aşama 4 — Warning temizliği

### Başlangıç

İlk tam analiz baseline:

- Core: 16 warning
- WPF: 41 warning
- Toplam: **57 warning / 0 error**

### Paket 1 — CA1512

Commit'ler:

- `4b8979930b29fe08e2d76f8c440f407d2520cb02`
- `1709dea16c4824eff1233bf5aaf44e3f9d2fe04a`

Yerel doğrulama sonucu:

- **57 → 53 warning**
- **0 error**

### Paket 2 — kontrollü warning temizliği

Kod aralığı:

- `a4e60d5aa30151184e0f38dd5bd58ccb6768f7fa`
- `a6461692baa761c3a9d901f537a8ca653515f77a`

17.09.2026 yerel tam analiz sonucu:

- Core: **2 warning**
- WPF: **10 warning**
- Toplam: **12 warning / 0 error**
- Program `[3/3] DogruFatura baslatiliyor...` aşamasına ulaştı.
- Böylece toplam warning sayısı **53 → 12** düştü.

## Kalan 12 warning

### Core — 2

- `CA1822` — `TaxCalculationService.Calculate`
- `CA1309` — `ControlCenterService` dize karşılaştırması

### WPF — 10

- `CA1822` — `DiagnosticPackageService.Create`
- `CA1822` — `FinalReportDocumentService.Build`
- `CA1822` — `ApplicationSafetyService.RuntimeDescription`
- `CA1859` — `ApplicationSafetyService.CollectBackupFiles`
- `CA1822` — `FinalReportPdfService.Save`
- `CA1822` — `HistoricalPdfImportService.Read`
- `CA1309` — `HistoricalPdfImportService` kurum adı karşılaştırması
- `CA1861` — `HistoricalPdfImportService` tarih formatı dizisi
- `CA1859` — `HistoricalPdfImportService.BuildLines`
- `CA1861` — `MainWindow` kurum filtresi `"Tümü"` dizisi

## Kodlanan fakat yerel doğrulaması bekleyen paket

İki düşük riskli `CA1861` düzeltmesi yapıldı:

- `2f3216a2b1f42c663f3e8e9a8ddd49df8cd14688`
  - `MainWindow.xaml.cs`
  - `"Tümü"` önek dizisi `static readonly` alana taşındı.
- `4d7eb4a097867c019bed7aeca0d714118d683226`
  - `HistoricalPdfImportService.cs`
  - Tarih formatı dizisi `static readonly` alana taşındı.

Beklenen sonraki yerel sonuç:

- **12 → yaklaşık 10 warning**
- **0 error**

Bu iki değişiklik iş kuralını veya kullanıcı davranışını değiştirmiyor; yalnız tekrarlanan sabit dizi tahsisini kaldırıyor.

## Sıradaki iş

1. GitHub Desktop → **Fetch origin → Pull origin**.
2. `hardening/1.1-data-safety` branch'inde `DERLE_CALISTIR.bat` çalıştır.
3. Sonuçta toplam warning ve error sayısını kontrol et.
4. Beklenen: yaklaşık **10 warning / 0 error**.
5. Sonuç doğrulanırsa sıradaki küçük paket olarak iki `CA1859` ele alınacak.
6. `CA1822` grubu daha sonra kontrollü biçimde temizlenecek.
7. `CA1309` en sona bırakılacak; Türkçe kültür/string davranışı test edilmeden değiştirilmemeli.

## Aşama 3 — Mali Denetim ve İzlenebilirlik

Warning temizliği sonrasında:

- Vergi kural sürümü/yürürlük tarihi modeli.
- Append-only audit trail.
- Çok dosyalı işlemler için transaction journal.
- Kapalı HYS arşivini değişmez belge modeliyle güçlendirme.

## Aşama 5 — Kullanıcı Deneyimi

Daha sonra:

- PDF içe aktarmayı async + ilerleme + iptal destekli yapmak.
- Boş fatura numarası için olası mükerrer uyarısı.
- Tanılama paketinde daha güçlü kişisel veri temizleme.
- PDF metadata formatı.

## Yerel doğrulama yöntemi

Visual Studio kullanılmıyor.

1. GitHub Desktop → **Fetch origin**
2. Branch: `hardening/1.1-data-safety`
3. **Pull origin**
4. **Repository > Show in Explorer**
5. `DERLE_CALISTIR.bat`

BAT `--no-incremental` tam analiz yaptığı için warning envanteri her build'de yeniden görünür.

Başarılı Debug EXE:

`DogruFatura.Wpf\bin\Debug\net8.0-windows\DogruFatura.exe`

## Kritik notlar

- Warning'ler suppress edilerek gizlenmeyecek; mümkün olduğunda nedeni düzeltilecek.
- Kullanıcı verisini otomatik silen veya geri döndürülemez dönüştüren değişiklik yapılmayacak.
- Restore/hash/manifest güvenliği zayıflatılmayacak.
- Yerel build doğrulaması ile GitHub Actions CI doğrulaması ayrı statülerdir.
- CI runner sorunu çözülmeden “CI doğrulandı” denmeyecek.
- Her tamamlanan paketten sonra bu MD güncellenecek.
