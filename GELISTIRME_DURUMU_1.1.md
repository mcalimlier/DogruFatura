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
- Rapor/arşiv DI genişletme 1 tamamlandı ve yerel build ile doğrulandı:
  - `MainWindow` artık `FinalReportDocumentService`, `FinalReportPdfService`, `DocumentArchiveStorageService` ve `HistoricalPdfImportService` nesnelerini kendi içinde oluşturmuyor.
  - Bu servisler `App` composition root'unda oluşturulup `MainWindow` constructor'ına veriliyor.
  - `HistoricalPdfImportService`, uygulamanın aynı `TaxCalculationService` örneğini kullanıyor.
  - `MainWindow` DI commit'i: `2c106f6d367574016b3fdc3b8457173628aae1b0`
  - Composition root commit'i: `33804ae702114e8c0b5b2cf8802826f07853b010`
  - 18.09.2026 kullanıcı yerel doğrulaması: **0 warning / 0 error**.
- Rapor/arşiv DI genişletme 2 tamamlandı ve yerel build ile doğrulandı:
  - `ArchivePdfSynchronizationService` artık `MainWindow` içinde oluşturulmuyor.
  - Aynı `DocumentArchiveStorageService` ve `FinalReportPdfService.Save` bağımlılıklarıyla `App.Application_Startup` composition root'unda kurulup constructor ile veriliyor.
  - Composition root commit'i: `d1439a413f924c4a4196f8111b93eb35ec296afe`
  - `MainWindow` DI commit'i: `7c3999cd07bc66daed5334d73e4a955b94906acb`
  - Kullanıcı davranışı, PDF üretimi, arşiv yolu ve hash doğrulama mantığında işlevsel değişiklik amaçlanmadı.
  - 18.09.2026 kullanıcı yerel doğrulaması: .NET SDK `10.0.400`, **0 warning / 0 error**, `[3/3] DogruFatura baslatiliyor...`.
- Kod temizliği / sadeleştirme paketi 1 tamamlandı ve yerel build ile doğrulandı:
  - MainWindow partial taramasında XAML'e bağlı event handler'lar dışında açıkça referanssız/ölü private metot bulunmadı.
  - `UiVisualPolishService` içindeki ikinci `new ApplicationSafetyService()` kaldırıldı; tanılama paketi composition root'taki mevcut `ApplicationSafetyService` örneğini kullanıyor.
  - `MainWindow` içindeki yinelenen `DesignWidth` / `DesignHeight` sabitleri kaldırıldı; `ResponsiveLayoutService.DesignWidth/DesignHeight` tek kaynak oldu.
  - `MainWindow.WindowStartup.cs` ve `MainWindow.WindowChrome.cs` içindeki monolitik dosyadan kalmış kullanılmayan `using` blokları temizlendi.
  - Composition root safety örneği: `c75f74b398fa75e996d5a188b5bd9ae24793f6a1`
  - UI polish aktarımı: `15c8105c88158f768560195a4ed668b02dcfcb4c`
  - Tanılama safety tekrarının kaldırılması: `f382c0acfc1443bebbec2a7dd8c56072f7ce29e9`
  - Yinelenen boyut sabitleri: `fc82ec1b6cd6d820c2c1dbe48a8efebd9ea73235`
  - Window startup using/sabit temizliği: `8a714549a4a979c159df2cd7f89edf4c7bea8deb`
  - Window chrome using temizliği: `a89f9676bb5e54b11eef023fc14c97561c94a82f`
  - 18.09.2026 kullanıcı yerel doğrulaması: .NET SDK `10.0.400`, **0 warning / 0 error**, `[3/3] DogruFatura baslatiliyor...`.
- Kod temizliği / sadeleştirme paketi 2 kodlandı; yeni yerel doğrulama bekliyor:
  - Monolitik `MainWindow.xaml.cs` döneminden kalan geniş using blokları, gerçek bağımlılıklara indirildi.
  - Temizlenen partial dosyalar: `Navigation`, `Dashboard`, `ArchiveView`, `Control`, `InvoiceList`, `ArchiveCommands`, `SchoolsOfficials`, `InvoiceEntry`, `Reports`, `Safety`.
  - İş kuralları, event handler davranışı, veri yazma akışı ve UI mantığı değiştirilmedi.
  - Commitler:
    - `5a33d7547d971074d3e41f6d17e9c0d1613a08c1`
    - `a877ea56d46c535e0ea52ca02dc37af9b88d5316`
    - `c227404991c1859bb4b21517d7f5146efaf49d9f`
    - `70287306a09b4cbc4a187612a1d98dab8c139b98`
    - `f0b2803c00658aa13de97c5976ac59ac5bc2d184`
    - `2fb655f326a98f6a22efb2a4aa67deebe462765f`
    - `b3368bf420da7f67f81377b7c23f63e92ba088ce`
    - `7ab43eea5e15219655c9491e1c0f8a13fbc4b031`
    - `a08a3b1379c9275a53ce40ea2a04272b36aa2c28`
    - `12ea28faa306f8a1de50948e1c4e8e650496c556`

Kalan ana işler:

- DI kapsamını kalan uygulama/rapor/arşiv servislerine kontrollü genişletmek.
- `MainWindow` sorumluluklarını ViewModel/application-service katmanlarına ayırmak.
- Tür/status alanlarını enum/value object yapısına taşımak.
- `SchoolRecord` için değişmez `SchoolId` eklemek ve fatura-kurum bağını ID üzerinden güçlendirmek.

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
- Son `CA1309` paketi: **2 → 0** warning.
  - Türkçe karşılaştırıcı: `6d3b00fc3d8db55b3b51138de041109dbff9111e`
  - Türkçe regresyon testleri: `2f9f99a989ade01488cdd265b4ccdd3b638c6d00`
  - `ControlCenterService`: `ff239b3a7f9a64b8de1b6315a750452f21c882a5`
  - `HistoricalPdfImportService`: `601d409a56db4d7c73ec17936ee5a8b816bb763d`
  - `tr-TR` `CompareInfo` + `CompareOptions.IgnoreCase` kullanılarak `I/İ/ı/i` davranışı korundu.

### 17.09.2026 warning temizliği doğrulaması

Kullanıcının yerel `DERLE_CALISTIR.bat` doğrulamasına göre:

- SDK: `.NET 10.0.400`
- Core: **0 warning**
- WPF: **0 warning**
- Toplam: **0 warning / 0 error**
- Program `[3/3] DogruFatura baslatiliyor...` aşamasına ulaştı.

**Aşama 4 warning temizliği yerel build açısından tamamlandı.**

Not: Türkçe regresyon testleri repoya eklendi ancak `DERLE_CALISTIR.bat` test yürütme sonucu değildir; test paketi ayrıca çalıştırılarak doğrulanmalıdır.

## Şu an doğrulama bekleyen adım

### Kod temizliği / sadeleştirme paketi 2 + tam xUnit test paketi

Kullanıcının son paylaştığı build sonucu kod temizliği paketi 1'i doğruladı. Paket 2 bu build sonucundan sonra commit edildiği için ayrıca doğrulanmalıdır.

Yerel doğrulama sırası:

1. GitHub Desktop → **Fetch origin → Pull origin**.
2. `DERLE_CALISTIR.bat`.
3. Beklenen: **0 warning / 0 error** ve uygulamanın normal başlaması.
4. Ardından `TESTLERI_CALISTIR.bat`.
5. Beklenen: tüm xUnit testlerinin başarılı olması.

Testlerde özellikle:

- Türkçe `I/İ/ı/i` regresyonu.
- PDF regression.
- Data safety / restore / recovery.
- Backup manifest / PDF SHA-256 bütünlüğü.
- Single-instance.
- Responsive layout / UI yardımcı testleri.

## Sonraki sadeleştirme adayları

1. `MainWindow.ArchiveDocuments.cs` ve varsa kalan partial dosyalardaki geniş using bloklarını aynı yöntemle temizlemek.
2. `UiVisualPolishService.ApplyVersionAndCopy` içindeki statik runtime metin yamalarını doğrudan XAML'e taşımak; yalnız dinamik sürüm metnini kodda bırakmak.
3. `AddDiagnosticButton` ile runtime'da oluşturulan tanılama düğmesini XAML'e taşımak.
4. `ConfigureFixedWindowForCurrentScreen` ve `ResponsiveLayoutService.Apply/FitToCurrentWorkArea` arasındaki çift pencere boyutlandırma sorumluluğunu görsel regresyon kontrolüyle tekleştirmek.
5. Üretimde kullanılmadığı doğrulanırsa `ColorContrastService` gibi test-odaklı yardımcıları test katmanına taşımak.
6. `MainWindow` constructor'ındaki store yükleme/fallback/bootstrap işlerini küçük bir application-service'e ayırmak; bu daha büyük adımdır ve tam test paketi yeşil olmadan başlanmamalıdır.

## Aşama 3 — Mali Denetim ve İzlenebilirlik

Aşama 2 mimari dilimleri ilerledikten sonra:

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
6. `TESTLERI_CALISTIR.bat`

`DERLE_CALISTIR.bat` `--no-incremental` tam analiz yapar; testleri çalıştırmaz.

## Kritik notlar

- Warning'ler suppress edilerek gizlenmeyecek; mümkün olduğunda nedeni düzeltilecek.
- Türkçe kurum adı karşılaştırmalarında dil davranışı korunacak.
- Kullanıcı verisini otomatik silen veya geri döndürülemez dönüştüren değişiklik yapılmayacak.
- Restore/hash/manifest güvenliği zayıflatılmayacak.
- Yerel build ve GitHub Actions CI statüleri ayrı tutulacak.
- CI runner sorunu çözülmeden “CI doğrulandı” denmeyecek.
- Her tamamlanan veya doğrulama bekleyen paketten sonra bu MD güncellenecek.
