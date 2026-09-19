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

### Kod temizliği / sadeleştirme paketi 6 — TAMAMLANDI, KULLANICI TARAFINDAN MÜKEMMEL OLARAK DOĞRULANDI

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

Kullanıcı 18.09.2026 tarihinde paket 6 sonucunu **mükemmel** olarak bildirdi; görsel sadeleştirme onaylandı.

### Kod temizliği / sadeleştirme paketi 7 — KODLANDI, YEREL BUILD VE GÖRSEL KONTROL BEKLİYOR

Header ve Ana Ekran için runtime görsel-ağaç düzeltmeleri XAML'e taşındı:

- `UiVisualPolishService.SimplifyHeader` tamamen kaldırıldı.
- Header'da runtime gizlenen `SÜRÜM` kutusu XAML'de doğrudan `Visibility="Collapsed"` oldu.
- Header orta kolon genişliği runtime'daki son değer olan `220` olarak XAML'e taşındı.
- Runtime gizlenen `DAMGA ORANI` kutusu XAML'de doğrudan gizlendi.
- Damga kutusuna ayrılan header kolonları doğrudan `0` genişliğe indirildi.
- `UiVisualPolishService.ImproveHomeLayout` tamamen kaldırıldı.
- Ana Ekran'daki eski `AŞAMA 10 • R2` rozeti XAML'de doğrudan gizlendi.
- Akıllı Özet kartının runtime kaldırılan gölgesi XAML'den çıkarıldı.
- Akıllı Özet kartının `CornerRadius=10`, `SnapsToDevicePixels=True` ve `UseLayoutRounding=True` son hali XAML'e taşındı.
- HomeView metin keskinliği:
  - `TextOptions.TextFormattingMode="Display"`
  - `TextOptions.TextRenderingMode="ClearType"`
  - `SnapsToDevicePixels="True"`
  olarak üst kapsayıcıda tanımlandı; 42 TextBlock'ı runtime dolaşma ihtiyacı kaldırıldı.
- Ana Ekran'daki runtime `CornerRadius 11/12 → 10` normalizasyonu yalnız Home kapsamındaki kartlara deklaratif olarak taşındı.
- Koruma kontrolünde Home dışına taşan 2 köşe değeri tespit edilip eski `11` değerlerine geri alındı; diğer ekranların görünümü korunuyor.

Commitler:
- Ana paket: `553a169984d8251084895135b4f7ee4dbb2054e7`
- Home dışı iki kartın koruma düzeltmesi: `590352d5d4d7dbb740f80afcf9f3dc37ff951b29`

Statik kontrol:
- `SimplifyHeader`: **0**
- `ImproveHomeLayout`: **0**
- Home dışındaki `CornerRadius=11` öğeleri önceki sürümle aynı: **2 / 2**
- `UiVisualPolishService` içindeki `UiTree.Descendants` çağrıları artık yalnız **3** yerde:
  - DatePicker normalizasyonu
  - Button normalizasyonu
  - Border/panel normalizasyonu

Sıradaki doğrulama:
1. GitHub Desktop → **Fetch origin → Pull origin**.
2. `DERLE_CALISTIR.bat`.
3. Beklenen: **0 warning / 0 error** ve normal başlatma.
4. Hızlı görsel kontrol:
   - üst bar önceki gibi kompakt,
   - Sürüm ve Damga kutuları görünmüyor,
   - Ana Ekran Akıllı Özet kartında gölge yok,
   - eski `AŞAMA 10 • R2` rozeti görünmüyor,
   - kart yuvarlaklıkları önceki temiz görünümle aynı.

## Sadeleştirme önerileri — TAMAMI KODLANDI, YEREL BUILD + TAM xUNIT BEKLİYOR

Kullanıcının “hepsini uygula” talebiyle önceki 8 sadeleştirme önerisinin tamamı küçük, geri alınabilir commitler halinde uygulandı.

### 1. Hakkında ekranı kompaktlaştırıldı

- Uzun ve sürekli görünür `✓` özellik listesi kaldırıldı.
- Günlük görünümde 6 kısa başlık kaldı:
  - Veri güvenliği ve bütünlük
  - Fatura giriş ve doğrulama
  - HYS ve işlem arşivi
  - Rapor, PDF ve yazdırma
  - Kontrol Merkezi ve denetim
  - Yedekleme, kurtarma ve destek
- Ayrıntılar kapalı `ÖZELLİKLER / TEKNİK BİLGİLER` Expander altında korunuyor.
- Bilgi silinmedi; yalnız görünür yoğunluk azaltıldı.

Ana commit:
- `2361c9c769a6770754a4c3802c0db212a439eaea`

### 2. Kalıcı gizli Header öğeleri tamamen kaldırıldı

- Daha önce `Collapsed` duran `SÜRÜM` kartı XAML'den kaldırıldı.
- Daha önce `Collapsed` duran `DAMGA ORANI` kartı XAML'den kaldırıldı.
- Header kolonları yeniden sadeleştirildi; sıfır genişlikli eski kolonlar kaldırıldı.
- `HeaderVersionText` referansı artık yok.

### 3. UiVisualPolishService tamamen kaldırıldı

- `NormalizeButtons`, `NormalizePanels`, `PolishDatePickers` için runtime visual-tree taraması kaldırıldı.
- Ortak buton minimum yüksekliği `StandardButtonBase` stiline taşındı.
- Premium panel piksel hizalaması stile taşındı.
- DatePicker ve iç `DatePickerTextBox` renk/caret/selection/padding davranışları XAML stillerine taşındı.
- Automation adlandırması zaten `UiAccessibilityService` tarafından sağlandığı için ikinci kez tarama yapılmıyor.
- Panel/text piksel hizalaması `TextSharpnessService` tarafından zaten merkezi uygulanıyor.
- `UiVisualPolishService.cs` dosyası silindi.

Commitler:
- `a74a4b68315fb957d49eb1d4e76ee4896661d862`
- `a793125890a0777e7198c46160749cfc68595a11`
- `ba060de325d1f518dd2a42e2f1b55b24b98a4b5e`

### 4. Sürüm sunumu tekleştirildi

- Gizli Header sürümü kaldırıldı.
- Alt Hakkında kartındaki yinelenen `AboutProductVersionText` kaldırıldı.
- Görünür dinamik sürüm için `AboutVersionText` tek kaynak olarak kaldı.
- Bu alan zaten `ApplicationSafetyService.InformationalVersion` ile dolduruluyor.

### 5. Pencere başlangıç boyutlandırması tek serviste toplandı

- `MainWindow.ConfigureFixedWindowForCurrentScreen` kaldırıldı.
- Eski davranışın matematiği `ResponsiveLayoutService` içine taşındı:
  - tercih edilen normal ölçek: yaklaşık %90
  - başlangıç çalışma alanı sınırları: genişlik %94, yükseklik %92
- Büyük ekrandaki yaklaşık `1305 × 850.5` davranışı ve küçük ekrandaki kontrollü küçültme korunuyor.
- Saf/test edilebilir `ResponsiveLayoutService.CalculateStartup` eklendi.
- Yeni responsive regresyon testleri eklendi.

Commitler:
- `d205478c02984524677f10ffee35e4296178a7aa`
- `b6c0185e2f5dd7f0872506dcd28b70110ef76467`
- `217808f600ff8cf219874050ed9fe60ee80d3116`

### 6. MainWindow startup/bootstrap yükü ayrıldı

Yeni:
- `MainWindowStartupState`
- `MainWindowStartupStateLoader`

Loader artık:
- startup safety check,
- SchoolDataStore load/fallback,
- InvoiceDataStore load/fallback,
- AuthorityProfileDataStore load/fallback,
- ArchivedInvoiceListDataStore load/fallback

işlerini yapıyor.

`MainWindow` constructor:
- hazır startup state'i tüketiyor,
- doğrudan store load/fallback try/catch bloklarını artık taşımıyor.

Composition root:
- store örneklerini tek kez oluşturuyor,
- startup state'i bu aynı store örnekleriyle hazırlıyor,
- aynı store örneklerini `MainWindow`a veriyor.

Commit:
- `0a83c3bfebe903b98a0949c748bd87557c5779cd`

### 7. Tür/status için geriye uyumlu typed domain katmanı eklendi

Yeni typed değerler:
- `InvoiceKind`
- `InstitutionKind`
- `ArchiveState`
- `DomainValueText`

Önemli uyumluluk kararı:
- Kalıcı JSON'daki mevcut `InvoiceType` / `InstitutionType` string alanları **değiştirilmedi**.
- Typed görünümler model üzerinde `[JsonIgnore]` olarak sunuluyor.
- Böylece mevcut kullanıcı JSON'u, restore/backuplar ve historical PDF akışı kırılmadan typed koda kontrollü geçiş başladı.
- `SchoolRecord.GetServiceNumber` artık typed `InvoiceKind` overload'u üzerinden çalışabiliyor.

İlk commitler:
- `29731301d5f446e4e099a398747bc917864ed5bb`
- `e2a0ea4326f22470b68892fa7c3ffa4b0ad4527e`
- `f5f80c39f4fd063d298c65d91a0ea5fa62190462`
- `184d898ae0fa08f7c3faf30bb814868143311daa`

### 8. Değişmez SchoolId bağı eklendi — legacy uyumlu

Model:
- `SchoolRecord` sonuna opsiyonel `Guid SchoolId = default`.
- `InvoiceRecord` sonuna opsiyonel `Guid SchoolId = default`.
- Eski constructor çağrıları bozulmuyor.
- Eski JSON'da alan yoksa `Guid.Empty` olarak okunabiliyor.

Legacy kimlik:
- `SchoolIdentityService` eklendi.
- Eski kurum kaydında ID yoksa kurumun mevcut alanlarından SHA-256 tabanlı deterministik Guid üretiliyor.
- Aynı eski kayıt her açılışta aynı ID'yi alıyor.
- Yeni kurumlar `Guid.NewGuid()` ile gerçek kalıcı kimlik alıyor.
- Kurum düzenlenirken mevcut `SchoolId` korunuyor.

Store:
- `SchoolDataStore.LoadOrCreate` legacy/boş ID kayıtlarını bellekte normalize ediyor.
- `SchoolDataStore.Save` her kaydı yazmadan önce normalize ederek boş `SchoolId` yazılmasını engelliyor.
- Seed kurumlar da kimlikli üretiliyor.

Fatura bağı:
- Yeni/güncellenen faturalar seçili kurumun `SchoolId` değerini taşıyor.
- Fatura düzenleme önce `SchoolId` ile kurum buluyor.
- Kontrol Merkezi önce `SchoolId` ile bağ kuruyor.
- `SchoolId == Guid.Empty` olan eski faturalar mevcut kurum türü/ad/VKN/hizmet legacy eşleşmesini kullanmaya devam ediyor.

Commitler:
- `d509569b9c28e447f05e602d37cf209750ba0fc1`
- `2df0309e905181cdaf9b8e768bca3a170f1c4513`
- `209a88dc4428b1d542f51510261d13b0d57ba64f`
- `9347670417ba6a91b7b21d5cc31ed3a17eca879a`
- `dd26950fd3d8013c3236130750532dc728ddee40`
- `a7838bb127fa25c4b890e1757e03700d11b59199`
- `e2464a58b431d7d004dca576d060d87f1a8948aa`
- `7cbf084e5fbe00c5bfa17c406eca09b84a3124e9`
- `40c1e3dc021fc9e44eeebe6a3c2e8b3cc330f114`

### Yeni regresyon testleri

Yeni `DomainIdentityTests.cs`:
- legacy okul normalize edildiğinde stabil/non-empty SchoolId,
- explicit ID'nin normalize sırasında korunması,
- SchoolId içermeyen legacy JSON'un yüklenmesi,
- typed domain görünümünün eski storage text ile uyumu,
- Kontrol Merkezi'nin SchoolId önceliği,
- SchoolId'siz legacy faturanın isim tabanlı fallback'i.

Commitler:
- `f08a5846617e06b88089974d7603572482d1bb71`
- test düzeltmesi: `0661bbbb1257901db6662cfaf16aa9f04ffa3542`

Responsive alana ayrıca 4 test vakası eklendi:
- büyük ekranda %90 başlangıç profili,
- 1366×768 / 1920×1080 / 2560×1440 startup çalışma alanı sınırları.

Önceki tam xUnit sonucu **107/107** idi.
Bu paket **10 yeni xUnit vakası** eklediği için, test discovery aynı kalırsa yeni beklenen toplam **117 test**tir.
19.09.2026 yerel `TESTLERI_CALISTIR.bat` doğrulamasında test discovery beklendiği gibi **117** oldu ve **117/117 test başarılı, 0 başarısız, 0 atlandı** sonucu alındı.

### Statik kapanış kontrolü

18.09.2026 kod taramasında:
- `UiVisualPolishService` dosyası: **silindi**
- `HeaderVersionText`: **0 referans**
- `AboutProductVersionText`: **0 referans**
- `ConfigureFixedWindowForCurrentScreen`: **0 referans**
- yeni testlerde alt çizgili public test adı: **0**
- yeni testlerde `Assert.Single(...Where(...))`: **0**
- `SchoolDataStore.Save` içinde save öncesi ID normalize: mevcut
- DatePicker iç TextBox stili: mevcut
- branch, paket 7 MD commit'ine göre **23 commit ileride** ve değişiklikler yalnız beklenen Core/WPF/Test dosyalarıyla sınırlı.

### Şimdi zorunlu doğrulama kapısı

Bu toplu paket UI + startup + domain model davranışı içerdiği için yeni mimari değişikliğe geçmeden önce **iki doğrulama birden** zorunlu:

1. GitHub Desktop → **Fetch origin**
2. **Pull origin**
3. `DERLE_CALISTIR.bat`
   - beklenen: Core **0 warning**
   - WPF **0 warning**
   - toplam **0 warning / 0 error**
   - `[3/3] DogruFatura baslatiliyor...`
4. Uygulamada kısa smoke test:
   - Ana Ekran
   - Fatura Girişi
   - kurum ekleme/düzenleme
   - aktif faturayı ekleme/düzenleme
   - Hakkında / Güvenlik
   - Gelişmiş / Destek
   - pencere boyutlandırma / maximize-restore
5. `TESTLERI_CALISTIR.bat`
   - beklenen: tüm testler başarılı
   - discovery değişmediyse yaklaşık **117 / 117**
6. Bu iki kapı yeşil olmadan sonraki Aşama 3 davranışsal geliştirmelerine geçilmeyecek.

## Toplu sadeleştirme sonrası ilk xUnit denemesi — TESTLER BAŞLAMADAN SYNTAX HATASINDA DURDU

18.09.2026 tarihinde `TESTLERI_CALISTIR.bat` çalıştırıldı.

Sonuç:
- SDK: `.NET 10.0.400`
- restore: başarılı
- `DogruFatura.Core`: başarılı
- `DogruFatura.Wpf`: başarılı
- `DogruFatura.Tests`: **derleme aşamasında 2 hata ile durdu**
- xUnit testleri gerçek anlamda başlamadı; bu nedenle bu koşu bir assertion/test sonucu değildir.

Hatalar:
- `DomainIdentityTests.cs(57,27): CS1003 ',' bekleniyor`
- `DomainIdentityTests.cs(57,35): CS1003 ',' bekleniyor`

Kök neden:
- Yeni legacy JSON testindeki beklenen JSON property adı yanlış C# string kaçışıyla yazılmıştı:
  - hatalı: `Assert.Contains(""SchoolId"", ...)`
  - doğru: `Assert.Contains("\\"SchoolId\\"", ...)`

Düzeltme:
- `DomainIdentityTests.cs` satır 57 geçerli string literal olacak şekilde düzeltildi.
- Aynı dosyada benzer yanlış çift tırnak kalıbı ayrıca tarandı; başka syntax adayı bulunmadı.

Fix commit:
- `cb50d23fbc9f31735792c34210fe041f2e977f5e`

Sıradaki tek doğrulama:
1. GitHub Desktop → **Fetch origin → Pull origin**
2. `TESTLERI_CALISTIR.bat`
3. Test assembly artık derlenirse gerçek xUnit sonucu alınacak.
4. Önceki 107 teste 10 yeni vaka eklendiği için discovery aynı kalırsa beklenen toplam yaklaşık **117**.
5. Bu rerun yeşil olmadan yeni mimari/işlevsel pakete geçilmeyecek.

## 19.09.2026 — Toplu sadeleştirme test kapısı TAMAMLANDI

Kullanıcının yerel `TESTLERI_CALISTIR.bat` sonucu:

- Toplam test: **117**
- Başarılı: **117**
- Başarısız: **0**
- Atlandı: **0**
- `DogruFatura.Tests` (`net8.0-windows`): başarılı
- Test özeti: yaklaşık **13,3 sn**
- Test projesi: yaklaşık **13,6 sn**
- Toplam build/test süresi: yaklaşık **33,0 sn**
- BAT sonucu: **[OK] TUM TESTLER BASARILI**

Böylece önceki toplu sadeleştirme + responsive + typed domain + legacy uyumlu `SchoolId` paketi için zorunlu tam xUnit kapısı kapanmıştır.

### Kod temizliği / sadeleştirme paketi 9 — KODLANDI, YEREL BUILD + TAM xUNIT BEKLİYOR

117/117 doğrulamasından sonra bir sonraki değişiklik küçük ve davranış koruyucu tutuldu.

`MainWindow.xaml.cs` constructor'ındaki uzun başlangıç blokları aynı sıra korunarak isimlendirilmiş private adımlara ayrıldı:

- belge/arşiv klasörlerinin hazırlanması,
- pencere shell kurulumu,
- fatura giriş kaynakları,
- kurum görünümü,
- arşiv görünümü,
- Kontrol Merkezi görünümü,
- hesap motoru self-test,
- ilk UI durumu,
- startup uyarılarının gösterimi.

Amaç:
- constructor okunabilirliğini artırmak,
- başlangıç akışının sırasını görünür kılmak,
- bir sonraki kontrollü MainWindow sorumluluk azaltmasına daha temiz zemin hazırlamak.

Değiştirilmeyen alanlar:
- veri modeli ve JSON şeması,
- `SchoolId` uyumluluğu,
- store/save/restore akışı,
- SHA-256 / manifest güvenliği,
- PDF üretimi ve arşiv davranışı,
- kullanıcıya gösterilen başlangıç uyarılarının sırası ve metinleri,
- engine self-test davranışı.

Commit:
- `eabebc5c9d850179c54221ef3ec6b77b15145a9f`

Sıradaki doğrulama:
1. GitHub Desktop → **Fetch origin → Pull origin**
2. `DERLE_CALISTIR.bat`
   - beklenen: **0 warning / 0 error**
   - normal başlatma
3. Kısa smoke test:
   - Ana Ekran
   - Kurumlar
   - Fatura Girişi
   - İşlem Arşivi
   - Kontrol Merkezi
   - Hakkında / Güvenlik
4. `TESTLERI_CALISTIR.bat`
   - beklenen: **117/117** veya yalnız bilinçli yeni test eklenmişse yeni toplamın tamamı başarılı
5. Bu kapı yeşil olmadan bir sonraki MainWindow/DI paketi yapılmayacak.

## 19.09.2026 — Sadeleştirme paketi 9 ilk doğrulama ve compile fix

Kullanıcının yerel doğrulamasında Paket 9'un ilk sürümü WPF derleme aşamasında durdu.

İlk sonuç:
- SDK: `.NET 10.0.400`
- Core: başarılı
- WPF: **3 hata + 3 uyarı**
- xUnit gerçek test çalışması başlamadı; test projesi WPF derleme hatası nedeniyle çalıştırılamadı.
- Bu sonuç bir assertion/test başarısızlığı değildir.

Hatalar:
- `CS0191`: `_schoolView`, `_archiveView`, `_controlView` readonly alanlarına yardımcı metotlardan atama yapılmaya çalışıldı.
- `CS8618`: aynı üç alanın constructor çıkışında atanmış olduğu derleyici tarafından kanıtlanamadı.

Kök neden:
- Paket 9 constructor sadeleştirmesinde bu üç `readonly ICollectionView` alanının ataması constructor gövdesinden private yardımcı metotların içine taşınmıştı.
- C# readonly alan atamalarına yalnız alan başlatıcısında veya doğrudan constructor gövdesinde izin verir.

Düzeltme:
- `readonly` koruması kaldırılmadı.
- `InitializeSchoolView`, `InitializeArchiveView`, `InitializeControlCenter` artık oluşturdukları `ICollectionView` nesnesini döndürüyor.
- `_schoolView`, `_archiveView`, `_controlView` atamaları yeniden doğrudan constructor içinde yapılıyor.
- `StartNewSchool`, `RefreshSchoolView`, `LoadAuthorityProfileToForm`, `RefreshArchiveView` ve `RefreshControlCenter` çağrıları alan atamalarından sonra ve önceki davranış sırasıyla çalışıyor.
- Veri modeli, JSON, restore, SHA-256/manifest, SchoolId ve PDF/arşiv davranışına dokunulmadı.

Fix commit:
- `74eb97dc123ca2e80325bbd4ee9456eb10de1c91`

Sıradaki doğrulama:
1. GitHub Desktop → **Fetch origin → Pull origin**
2. `DERLE_CALISTIR.bat`
   - beklenen: **0 warning / 0 error**
   - normal başlatma
3. Yalnız build yeşil olursa `TESTLERI_CALISTIR.bat`
   - beklenen: **117/117**
4. Bu iki kapı yeşil olmadan Paket 10'a geçilmeyecek.

## 19.09.2026 — Sadeleştirme paketi 9 TAMAMLANDI VE DOĞRULANDI

Compile fix sonrasında kullanıcı yerel doğrulaması:

Build:
- SDK: `.NET 10.0.400`
- `DogruFatura.Core`: başarılı
- `DogruFatura.Wpf`: başarılı
- Toplam: **0 warning / 0 error**
- `[3/3] DogruFatura baslatiliyor...` aşamasına ulaşıldı.

Tam xUnit:
- `DogruFatura.Core`: başarılı
- `DogruFatura.Wpf`: başarılı
- `DogruFatura.Tests`: başarılı
- Toplam test: **117**
- Başarılı: **117**
- Başarısız: **0**
- Atlandı: **0**
- BAT sonucu: **[OK] TUM TESTLER BASARILI**
- Test çalıştırması: yaklaşık **13,9 sn**
- Test özeti: yaklaşık **14,9 sn**
- Toplam test build/koşu: yaklaşık **22,3 sn**

Sonuç:
- Paket 9 constructor sadeleştirmesi ve readonly-view compile fix'i yerel build + tam regresyon paketiyle yeşil doğrulandı.
- Yeni güvenilir baseline: **0 warning / 0 error + 117/117 xUnit**.
- Paket 10 için yeni küçük, geri alınabilir sadeleştirme dilimine geçilebilir.

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
