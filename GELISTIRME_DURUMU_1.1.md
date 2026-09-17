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

- Bozuk veri kurtarma güvenliği.
- Veri şema sürümleme/migration.
- Restore öncesi semantik doğrulama.
- Yedek manifest/SHA-256 ve PDF bütünlük doğrulaması.
- Tek örnek/yazma güvenliği fail-closed.

GitHub Actions runner sorunu nedeniyle CI doğrulaması ayrı olarak bekliyor.

### Aşama 2 — Mimari Temizlik

Tamamlananlar:

- Merkezi `InvoiceRulesEngine`.
- `InvoiceValidationService` ve Kontrol Merkezi ortak kural motoru.
- Dependency Injection ilk dilimi.
- Son doğrulanan DI commit'i: `427fb23d309b53db9366056ea27730e546d9eba2`.

Kalan ana işler:

- DI kapsamını rapor/arşiv servislerine genişletmek.
- `MainWindow` sorumluluklarını daha fazla ayırmak.
- Tür/status alanlarını enum/value object yapısına taşımak.
- `SchoolRecord` için değişmez `SchoolId` eklemek.

## Aşama 4 — Warning temizliği

### Baseline

- Core: 16 warning
- WPF: 41 warning
- Toplam: **57 warning / 0 error**

### Paket 1 — CA1512

- Commit'ler: `4b8979930b29fe08e2d76f8c440f407d2520cb02`, `1709dea16c4824eff1233bf5aaf44e3f9d2fe04a`
- Yerel sonuç: **57 → 53 warning / 0 error**.

### Paket 2 — kontrollü warning temizliği

- Kod aralığı: `a4e60d5aa30151184e0f38dd5bd58ccb6768f7fa` → `a6461692baa761c3a9d901f537a8ca653515f77a`
- Yerel sonuç: **53 → 12 warning / 0 error**.

### Paket 3 — CA1861

- Commit'ler: `2f3216a2b1f42c663f3e8e9a8ddd49df8cd14688`, `4d7eb4a097867c019bed7aeca0d714118d683226`
- Yerel sonuç: **12 → 10 warning / 0 error**.

### Paket 4A — ilk CA1859

- Commit: `7410e73bee7a35ea597f0fca823cb1964fe5ff2d`
- `HistoricalPdfImportService.BuildLines` dönüş tipi `PdfLineData[]` yapıldı.
- 17.09.2026 yerel tam analiz sonucu:
  - Core: **2 warning**
  - WPF: **7 warning**
  - Toplam: **9 warning / 0 error**
  - Program `[3/3] DogruFatura baslatiliyor...` aşamasına ulaştı.

## Kalan 9 warning

### Core — 2

- `CA1822` — `TaxCalculationService.Calculate`
- `CA1309` — `ControlCenterService` dize karşılaştırması

### WPF — 7

- `CA1822` — `DiagnosticPackageService.Create`
- `CA1822` — `FinalReportDocumentService.Build`
- `CA1822` — `FinalReportPdfService.Save`
- `CA1822` — `HistoricalPdfImportService.Read`
- `CA1822` — `ApplicationSafetyService.RuntimeDescription`
- `CA1859` — `ApplicationSafetyService.CollectBackupFiles`
- `CA1309` — `HistoricalPdfImportService` kurum adı karşılaştırması

## Kodlanan fakat yerel doğrulaması bekleyen parça

Son `CA1859` tek başına ele alındı:

- Commit: `bc6b93c0c11d8d09a75ceb9e756ebdc61c5c3e64`
- Dosya: `ApplicationSafetyService.cs`
- `CollectBackupFiles` zaten dizi döndürdüğü için private dönüş tipi `IReadOnlyList<BackupSourceFile>` yerine `BackupSourceFile[]` yapıldı.
- Yedek içeriği, sırası, manifest ve restore davranışı değişmedi.

Beklenen sonraki sonuç: **8 warning / 0 error**.

## Sıradaki iş

1. GitHub Desktop → **Fetch origin → Pull origin**.
2. `hardening/1.1-data-safety` branch'inde `DERLE_CALISTIR.bat` çalıştır.
3. Beklenen: **8 warning / 0 error**.
4. Doğrulanırsa kalan warning'ler yalnız **6 adet CA1822 + 2 adet CA1309** olacak.
5. `CA1822` grubu kontrollü küçük paketlerle ele alınacak.
6. İki `CA1309` en sona bırakılacak; Türkçe kültür/string davranışı test edilmeden değiştirilmemeli.

## Aşama 3 — Mali Denetim ve İzlenebilirlik

Warning temizliği sonrasında:

- Vergi kural sürümü/yürürlük tarihi modeli.
- Append-only audit trail.
- Transaction journal.
- Kapalı HYS arşivini değişmez belge modeliyle güçlendirme.

## Aşama 5 — Kullanıcı Deneyimi

Daha sonra:

- PDF içe aktarmayı async + ilerleme + iptal destekli yapmak.
- Boş fatura numarası için olası mükerrer uyarısı.
- Tanılama paketinde daha güçlü kişisel veri temizleme.
- PDF metadata formatı.

## Yerel doğrulama

Visual Studio kullanılmıyor.

1. GitHub Desktop → **Fetch origin**
2. Branch: `hardening/1.1-data-safety`
3. **Pull origin**
4. **Repository > Show in Explorer**
5. `DERLE_CALISTIR.bat`

BAT `--no-incremental` tam analiz yapar.

## Kritik notlar

- Warning'ler suppress edilerek gizlenmeyecek; mümkün olduğunda nedeni düzeltilecek.
- Kullanıcı verisini otomatik silen veya geri döndürülemez dönüştüren değişiklik yapılmayacak.
- Restore/hash/manifest güvenliği zayıflatılmayacak.
- Yerel build ve GitHub Actions CI statüleri ayrı tutulacak.
- CI runner sorunu çözülmeden “CI doğrulandı” denmeyecek.
- Her tamamlanan paketten sonra bu MD güncellenecek.
