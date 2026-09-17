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

### Tamamlanan warning paketleri

- `CA1512`: **57 → 53** warning.
  - `4b8979930b29fe08e2d76f8c440f407d2520cb02`
  - `1709dea16c4824eff1233bf5aaf44e3f9d2fe04a`
- Kontrollü tip/kültür paketi: **53 → 12** warning.
  - `a4e60d5aa30151184e0f38dd5bd58ccb6768f7fa` → `a6461692baa761c3a9d901f537a8ca653515f77a`
- `CA1861`: **12 → 10** warning.
  - `2f3216a2b1f42c663f3e8e9a8ddd49df8cd14688`
  - `4d7eb4a097867c019bed7aeca0d714118d683226`
- İlk `CA1859`: **10 → 9** warning.
  - `7410e73bee7a35ea597f0fca823cb1964fe5ff2d`
- Son `CA1859`: **9 → 8** warning.
  - `bc6b93c0c11d8d09a75ceb9e756ebdc61c5c3e64`
- `DiagnosticPackageService.Create` CA1822: **8 → 7** warning.
  - `13eb6f0feddae591ea7d0532fd11977af46159a1`
  - `798126f7ede47ed16cf03a5081a8fdceedc1d25b`
  - Eksik static çağrı düzeltmesi: `1353947c17419e34ac9acf2a6f960a98e25706c8`

### 17.09.2026 son başarılı yerel build

- SDK: `.NET 10.0.400`
- Core: **2 warning**
- WPF: **5 warning**
- Toplam: **7 warning / 0 error**
- Program `[3/3] DogruFatura baslatiliyor...` aşamasına ulaştı.
- Önceki `CS0176` ara derleme hatası tamamen doğrulanarak kapandı.

## Kalan 7 warning

### Core — 2

- `CA1822` — `TaxCalculationService.Calculate`
- `CA1309` — `ControlCenterService` dize karşılaştırması

### WPF — 5

- `CA1822` — `FinalReportDocumentService.Build`
- `CA1822` — `FinalReportPdfService.Save`
- `CA1822` — `ApplicationSafetyService.RuntimeDescription`
- `CA1822` — `HistoricalPdfImportService.Read`
- `CA1309` — `HistoricalPdfImportService` kurum adı karşılaştırması

## Kodlanan fakat yerel doğrulaması bekleyen parça

`TaxCalculationService.Calculate` mevcut DI yapısı korunarak ele alındı:

- Commit: `afbf23eadae45da6a1a3cb976a9c7a982824c723`
- `StampDutyRate` public sabiti korunuyor.
- Aynı oran `_activeStampDutyRate` örnek durumunda tutuluyor ve hesaplama bu aktif servis kuralını kullanıyor.
- Matematik, oran, kullanıcı mesajları ve mevcut çağrı biçimi değişmedi.
- Servisi static yapmadan `CA1822` nedenini ortadan kaldırıyor ve ileride planlanan vergi kural sürümü/yürürlük tarihi mimarisine uyumlu kalıyor.

Beklenen sonraki yerel sonuç: **6 warning / 0 error**.

## CA1822 yaklaşımı

`CA1822` uyarıları topluca static yapılmayacak.

- Gerçekten durumsuz yardımcı davranışlar static yapılabilir.
- DI veya servis sınırı olarak bilinçli biçimde örnek kullanılan sınıflar yalnız warning düşürmek için static yapılmayacak.
- Güvenlik-kritik büyük dosyalarda sırf warning için geniş çağrı zinciri değişikliği yapılmayacak.

## Sıradaki iş

1. GitHub Desktop → **Fetch origin → Pull origin**.
2. `hardening/1.1-data-safety` branch'inde `DERLE_CALISTIR.bat` çalıştır.
3. Beklenen: **6 warning / 0 error**.
4. Doğrulanırsa sıradaki `CA1822` küçük ve izole bir paket olarak ele alınacak.
5. İki `CA1309` en sona bırakılacak; Türkçe kültür/string davranışı test edilmeden değiştirilmeyecek.

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
