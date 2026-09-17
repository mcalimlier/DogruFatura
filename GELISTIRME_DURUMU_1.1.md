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
- `HistoricalPdfImportService` içinde `TaxCalculationService` bağımlılığı örnek servise taşındı.

Kalan ana işler:

- DI kapsamını rapor/arşiv servislerine daha fazla genişletmek.
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
- `TaxCalculationService.Calculate` CA1822: **7 → 6** warning.
  - `afbf23eadae45da6a1a3cb976a9c7a982824c723`
- `FinalReportDocumentService.Build` CA1822: **6 → 5** warning.
  - `f984e834d36827d7a03da2132a23318cdf0fbe50`
- `FinalReportPdfService.Save` CA1822: **5 → 4** warning.
  - `014df5f68752205ed0201e97015a620036ca1d0f`
- `HistoricalPdfImportService.Read` CA1822: **4 → 3** warning.
  - `b04c2c1cc61cf382a31fc38088305390b1620f9c`
- `ApplicationSafetyService.RuntimeDescription` CA1822: **3 → 2** warning.
  - `71d952b84aac1a5f4599978f135b7d2a77eab815`
  - Runtime açıklaması servis örneği durumuna bağlandı; güvenlik/restore/hash davranışı değişmedi.

### 17.09.2026 son başarılı yerel build

- SDK: `.NET 10.0.400`
- Core: **1 warning**
- WPF: **1 warning**
- Toplam: **2 warning / 0 error**
- Program `[3/3] DogruFatura baslatiliyor...` aşamasına ulaştı.
- Tüm `CA1822` warning'leri yerel build ile doğrulanarak kapandı.

## Kalan 2 warning

- Core `CA1309` — `ControlCenterService` kurum adı karşılaştırması.
- WPF `CA1309` — `HistoricalPdfImportService` kurum adı karşılaştırması.

## Kodlanan fakat yerel doğrulaması bekleyen son warning paketi

İki `CA1309` warning'i Türkçe davranış korunarak merkezi hale getirildi:

- `6d3b00fc3d8db55b3b51138de041109dbff9111e` — `TurkishTextComparison` eklendi.
  - Açık `tr-TR` `CompareInfo` kullanıyor.
  - `CompareOptions.IgnoreCase` ile Türkçe büyük/küçük harf eşleştirmesi deterministik.
- `2f9f99a989ade01488cdd265b4ccdd3b638c6d00` — Türkçe karşılaştırma regresyon testleri eklendi.
  - `İSTANBUL ↔ istanbul` eşleşir.
  - `ISPARTA ↔ ısparta` eşleşir.
  - `ISPARTA ↔ isparta` eşleşmez.
  - `İSTANBUL ↔ ıstanbul` eşleşmez.
  - null davranışı sabitlendi.
- `ff239b3a7f9a64b8de1b6315a750452f21c882a5` — `ControlCenterService` merkezi Türkçe karşılaştırıcıya geçirildi.
- `601d409a56db4d7c73ec17936ee5a8b816bb763d` — `HistoricalPdfImportService` merkezi Türkçe karşılaştırıcıya geçirildi.

Warning suppress edilmedi ve `OrdinalIgnoreCase` ile Türkçe `I/İ/ı/i` davranışı kaybedilmedi.

Beklenen sonraki yerel build sonucu: **0 warning / 0 error**.

Not: Yeni regresyon testleri repoya eklendi; kullanıcının son paylaştığı `DERLE_CALISTIR.bat` çıktısı test yürütme sonucu değildir. Test çalıştırması ayrıca doğrulanmalıdır.

## Sıradaki iş

1. GitHub Desktop → **Fetch origin → Pull origin**.
2. `hardening/1.1-data-safety` branch'inde `DERLE_CALISTIR.bat` çalıştır.
3. Beklenen: **0 warning / 0 error** ve uygulamanın başlaması.
4. Build temizse warning temizliği tamamlandı olarak işaretlenecek.
5. Ardından test paketi ayrıca çalıştırılıp Türkçe regresyon testleri doğrulanacak.
6. Sonrasında Aşama 2 kalan mimari işlere veya Aşama 3 mali izlenebilirlik çalışmalarına geçilebilir.

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
- Türkçe kurum adı karşılaştırmalarında dil davranışı korunacak.
- Kullanıcı verisini otomatik silen veya geri döndürülemez dönüştüren değişiklik yapılmayacak.
- Restore/hash/manifest güvenliği zayıflatılmayacak.
- Yerel build ve GitHub Actions CI statüleri ayrı tutulacak.
- CI runner sorunu çözülmeden “CI doğrulandı” denmeyecek.
- Her tamamlanan paketten sonra bu MD güncellenecek.
