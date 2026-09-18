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
- Kod temizliği / sadeleştirme paketi 2 tamamlandı ve yerel build ile doğrulandı:
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
  - 18.09.2026 kullanıcı yerel doğrulaması: .NET SDK `10.0.400`, **0 warning / 0 error**, `[3/3] DogruFatura baslatiliyor...`.

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

Not: Tam xUnit paketi 18.09.2026 tarihinde ayrıca çalıştırıldı ve **107/107 test başarılı, 0 başarısız, 0 atlandı** sonucu alındı.

## Tam xUnit doğrulaması — TAMAMLANDI

18.09.2026 tarihinde analyzer düzeltmelerinden sonra `TESTLERI_CALISTIR.bat` yeniden çalıştırıldı.

Sonuç:
- SDK: `.NET 10.0.400`
- `DogruFatura.Core`: başarılı
- `DogruFatura.Wpf`: başarılı
- `DogruFatura.Tests`: başarılı
- Toplam test: **107**
- Başarılı: **107**
- Başarısız: **0**
- Atlandı: **0**
- BAT sonucu: **[OK] TUM TESTLER BASARILI**
- Test özeti süresi: yaklaşık **18,5 sn**; toplam BAT/build süresi yaklaşık **33,9 sn**.

Bu test paketi şunları kapsayan mevcut regresyonları yeşil doğruladı:
- Türkçe `I/İ/ı/i` karşılaştırmaları.
- PDF sayfa planı / gerçek PDF regresyonu / historical importer geri okuma.
- Data safety, restore, recovery ve schema migration.
- Backup manifest ve PDF SHA-256 bütünlüğü.
- Single-instance fail-closed.
- Responsive layout.
- Diagnostic/recovery.
- Core vergi/HYS kuralları.
- Performans testleri.

Önceki analyzer düzeltme commit'leri:
- `07042842af89e0fdd26ca0cbd38f04e8ac270c9d`
- `af418ec9d2bb0d21128139deefcfd3abd22b6cce`
- `d31913ab73f0b42e6021805e75264e06298fc851`

### Kod temizliği / sadeleştirme paketi 3 — TAMAMLANDI VE YEREL BUILD İLE DOĞRULANDI

Tam test paketi yeşil olduktan sonra kalan düşük riskli using temizliği tamamlandı:

- `MainWindow.ArchiveDocuments.cs` geniş using bloğu gerçek bağımlılıklara indirildi.
- Ana `MainWindow.xaml.cs` geniş using bloğu gerçek bağımlılıklara indirildi.
- Constructor, event akışı, store yükleme, PDF/arşiv işlemleri ve veri güvenliği davranışına dokunulmadı.

Commitler:
- `03fc8f7d51db0016ad863ce9102759e8ecdb7dc8`
- `d4730f603e1e633e4beef2f9c27d58a157d4f6c0`

Sıradaki doğrulama:
1. GitHub Desktop → **Fetch origin → Pull origin**.
2. `DERLE_CALISTIR.bat`.
3. Beklenen: **0 warning / 0 error** ve normal başlatma.
4. 18.09.2026 kullanıcı yerel doğrulaması: .NET SDK `10.0.400`, **0 warning / 0 error**, `[3/3] DogruFatura baslatiliyor...`.

### Kod temizliği / sadeleştirme paketi 4 — TAMAMLANDI, KULLANICI TARAFINDAN TEMİZ OLARAK DOĞRULANDI

Runtime sabit UI metin yamaları XAML'e taşındı:

- `HESAP MOTORU` metni XAML'de doğrudan `HESAPLAMA` oldu.
- Ana ekran HYS açıklaması güncel metinle doğrudan XAML'e taşındı.
- Raporlar/Çıktılar açıklaması güncel metinle doğrudan XAML'e taşındı.
- Veri Güvenliği açıklaması güncel metinle doğrudan XAML'e taşındı.
- `ArchivePdfSyncButton` içeriği ve tooltip'i XAML'de doğrudan `PDF KONTROL` olarak tanımlandı.
- Header ve Hakkında sürüm alanlarına `x:Name` eklendi.
- `UiVisualPolishService.ApplyVersionAndCopy` kaldırıldı; yerine yalnız iki dinamik sürüm alanını doğrudan güncelleyen `ApplyVersionText` kaldı.
- Sabit metinler için tüm `TextBlock` ağacını dolaşan runtime switch artık yok.

Commit:
- `17c2d27694929f2c499d87e7fa856bce11f4e699`

Doğrulama durumu:
- Kod statik olarak kontrol edildi.
- Eski sabit metin yamaları XAML'de: **0**
- Eski `ApplyVersionAndCopy` / `text.Text switch`: **0**
- Kullanıcı 18.09.2026 tarihinde paket 4'ü **temiz** olarak bildirdi; yeni hata/uyarı veya görsel sorun bildirilmedi.

Sıradaki işlem:
1. GitHub Desktop → **Fetch origin → Pull origin**.
2. `DERLE_CALISTIR.bat`.
3. Beklenen: **0 warning / 0 error** ve normal başlatma.
4. Uygulamada özellikle üst bar, Ana Ekran, Raporlar/Çıktılar, İşlem Arşivi ve Hakkında ekranındaki metinlerin doğru göründüğü hızlıca kontrol edilmeli.

### Kod temizliği / sadeleştirme paketi 5 — TAMAMLANDI, KULLANICI TARAFINDAN ÇALIŞIR OLARAK DOĞRULANDI

Runtime'da oluşturulan `TANILAMA PAKETİ` düğmesi XAML'e taşındı:

- Veri Güvenliği `WrapPanel` içine statik XAML düğmesi eklendi.
- Düğmenin içerik, boyut, renk, tooltip ve AutomationProperties bilgileri XAML'e taşındı.
- `CreateDiagnosticPackage_Click` handler'ı `MainWindow.Safety.cs` içine eklendi.
- Handler mevcut injected `_safetyService` örneğini kullanıyor.
- `UiVisualPolishService.AddDiagnosticButton` tamamen kaldırıldı.
- `UiVisualPolishService.Apply` artık `ApplicationSafetyService` parametresi almıyor.
- `UiPolishService.Apply` artık `ApplicationSafetyService` parametresi almıyor.
- `App.Application_Startup` içindeki çağrı `UiPolishService.Apply(main)` olarak sadeleştirildi.
- Tanılama paketi üretim mantığı ve kullanıcıya gösterilen bildirim/hata davranışı korunuyor.

Commit:
- `574bec3568c25702b02832dbbf520c1f047011ce`

Statik kontrol:
- XAML'de `TANILAMA PAKETİ`: mevcut.
- `CreateDiagnosticPackage_Click`: mevcut.
- `DiagnosticPackageService.Create(_safetyService)`: mevcut.
- Eski `AddDiagnosticButton`: **0**.
- Eski safety parametreli UI polish çağrıları: **0**.

Sıradaki işlem:
1. GitHub Desktop → **Fetch origin → Pull origin**.
2. `DERLE_CALISTIR.bat`.
3. Beklenen: **0 warning / 0 error** ve normal başlatma.
4. Kullanıcı 18.09.2026 tarihinde paket 5'in çalıştığını doğruladı; `TANILAMA PAKETİ` düğmesi ve ilgili akış çalışır durumda bildirildi.

### Veri Güvenliği alanı için sadeleştirme kararı — ÖNERİ AŞAMASI

Kullanıcı mevcut Veri Güvenliği bölümünü görsel olarak kalabalık ve gereksiz buldu.

Mevcut durum:
- Üstte bütünlük durumu + son yedek bilgisi.
- `KONTROL ET`.
- `GÜVENLİK YEDEĞİ`.
- `YEDEKTEN GERİ YÜKLE`.
- `VERİ KLASÖRÜ`.
- `PDF KLASÖRÜ`.
- `YEDEK KLASÖRÜ`.
- `LOG KLASÖRÜ`.
- `TANILAMA PAKETİ`.
- Ayrıca sol taraftaki `SÜRÜM / ÇALIŞMA ORTAMI` kartında veri/PDF/yedek klasör yolları tekrar gösteriliyor.

Önerilen sadeleştirme yönü:
- Veri Güvenliği kartında yalnız 3 ana eylem görünür tutulsun:
  1. `KONTROL ET`
  2. `YEDEK OLUŞTUR`
  3. `GERİ YÜKLE`
- İkincil araçlar `GELİŞMİŞ / DESTEK` altında toplanmalı:
  - Veri Klasörü
  - PDF Klasörü
  - Yedek Klasörü
  - Log Klasörü
  - Tanılama Paketi
- Sol taraftaki çalışma ortamı kartında tam klasör yolları sürekli gösterilmek yerine yalnız sürüm/runtime bilgisi tutulmalı; klasör yolları gerekirse gelişmiş alana taşınmalı.
- Veri bütünlüğü durumu tek satırlık kompakt durum bandı şeklinde gösterilmeli.
- İşlevlerin kendisi silinmemeli; yalnız görünürlük ve gruplanma sadeleştirilmeli.

Henüz kod değişikliği yapılmadı; önce önerilen yerleşim kullanıcı kararıyla netleştirilecek.

### Kod temizliği / sadeleştirme paketi 6 — KODLANDI, YEREL BUILD VE GÖRSEL KONTROL BEKLİYOR

Kullanıcının onayladığı orta seviye Veri Güvenliği sadeleştirmesi uygulandı:

- Veri Güvenliği kartındaki birincil eylemler 3'e indirildi:
  1. `KONTROL ET`
  2. `YEDEK OLUŞTUR`
  3. `GERİ YÜKLE`
- Teknik/ikincil araçlar kapalı `GELİŞMİŞ / DESTEK` alanına taşındı:
  - Veri Klasörü
  - PDF Klasörü
  - Yedek Klasörü
  - Log Klasörü
  - Tanılama Paketi
- `SÜRÜM / ÇALIŞMA ORTAMI` kartındaki uzun veri/PDF/yedek klasör yolları kaldırıldı.
- Sol kartta yalnız sürüm + runtime bilgisi ve teknik araçların Gelişmiş/Destek altında olduğu kısa not kaldı.
- Veri bütünlüğü sonucu kompakt duruma çevrildi:
  - sağlıklı: `✓ Veriler ve PDF belgeleri sağlıklı`
  - sorunlu: `⚠ Veri bütünlüğü kontrolü dikkat gerektiriyor`
- Tam teknik bütünlük mesajı kaybolmadı:
  - durum satırının tooltip'inde korunuyor,
  - `KONTROL ET` sorun bulursa dialog'da tam `result.Message` gösteriliyor.
- Son yedek satırı artık tam dosya yolu yerine mümkünse yedek dosyasının tarih/saatini gösteriyor.
- Restore, backup, SHA-256, manifest, diagnostic ve klasör açma davranışları değiştirilmedi.

Commit:
- `563b5653c5155c7e7b7a30a4c1aa4ecc30c47308`

Statik kontrol:
- 3 birincil eylem mevcut.
- `GELİŞMİŞ / DESTEK` mevcut.
- 5 ikincil araç gelişmiş alanda mevcut.
- Kaldırılan klasör yolu TextBlock referansları: **0**.
- Kompakt durum + ayrıntılı teknik mesaj korunuyor.
- Son yedek zaman gösterimi mevcut.

Sıradaki doğrulama:
1. GitHub Desktop → **Fetch origin → Pull origin**.
2. `DERLE_CALISTIR.bat`.
3. Beklenen: **0 warning / 0 error** ve normal başlatma.
4. Hakkında / Güvenlik ekranında:
   - kartın daha ferah görünmesi,
   - 3 ana eylemin tek satırda düzgün görünmesi,
   - `GELİŞMİŞ / DESTEK` aç/kapa davranışı,
   - klasör ve Tanılama düğmelerinin hâlâ çalışması,
   - `KONTROL ET`, `YEDEK OLUŞTUR`, `GERİ YÜKLE` akışlarının korunması
   hızlıca doğrulanmalı.

## Sonraki sadeleştirme adayları

Düşük riskten yükseğe:

1. Veri Güvenliği paket 6 görsel/build doğrulaması sonrası `SimplifyHeader` / `ImproveHomeLayout` içindeki kalan runtime visual-tree taramalarını kontrollü azaltmak.
2. `ConfigureFixedWindowForCurrentScreen` ile `ResponsiveLayoutService.Apply/FitToCurrentWorkArea` arasındaki çift boyutlandırma sorumluluğunu görsel regresyonla tekleştirmek.
3. Üretim kullanımı olmadığı doğrulanırsa test-odaklı yardımcıları test katmanına taşımak.
4. Daha sonra `MainWindow` constructor bootstrap/store yükleme sorumluluklarını küçük application-service'e ayırmak.

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
5. Build için `DERLE_CALISTIR.bat`
6. Test için `TESTLERI_CALISTIR.bat`

`DERLE_CALISTIR.bat` test sonucu değildir.

## Kritik notlar

- Warning'ler körlemesine suppress edilerek gizlenmeyecek.
- Test projesindeki CA1707 uyarıları suppress edilmedi; test metodu adları düzeltildi.
- Türkçe kurum adı karşılaştırmalarında dil davranışı korunacak.
- Kullanıcı verisini otomatik silen veya geri döndürülemez dönüştüren değişiklik yapılmayacak.
- Restore/hash/manifest güvenliği zayıflatılmayacak.
- Yerel build ve GitHub Actions CI statüleri ayrı tutulacak.
- CI runner sorunu çözülmeden “CI doğrulandı” denmeyecek.
- Her paket ve doğrulama sonucu bu MD'ye işlenecek.
