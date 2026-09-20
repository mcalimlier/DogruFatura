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

- Aşama 2 mimari kapanış envanteri tamamlandı: `MainWindow` artık servis örnekleri üretmiyor; kalan çağrıların çoğu stateless domain/application helper. Sırf DI uğruna constructor bağımlılığı artırılmayacak.
- `MainWindow` sorumluluk ayrımı yalnız yeni davranış eklenirken gerçek bir uygulama-servisi sınırı oluşursa sürdürülecek.
- Typed domain geçişi gerekli alanlarda tamamlandı: fatura türü, kurum türü, arşiv durumu ve kontrol önem derecesi typed katmanı kullanıyor.
- `SchoolId` işi tamamlandı; yeni/güncellenen faturalar ID bağı kullanıyor, legacy kayıtlar uyumlu fallback ile korunuyor.
- Aşama 3 ilerliyor: vergi kural metadata'sı + InvoiceRecord kalıcılığı, append-only audit trail ve transaction journal doğrulandı; kapalı HYS arşivi değişmez belge modeli kodlandı.

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

## 19.09.2026 — Kod temizliği / sadeleştirme paketi 10 — KODLANDI, DOĞRULAMA BEKLİYOR

Paket 9'un **0 warning / 0 error + 117/117 xUnit** baseline'ından sonra typed domain katmanının UI tarafındaki kullanımı küçük bir adımla genişletildi.

Değişiklikler:
- `MainWindow.xaml.cs` içindeki fatura türü UI listesi artık ham `"Elektrik" / "Telefon" / "İnternet" / "Su"` dizisini ikinci kez tanımlamıyor.
- `InvoiceTypes`, `DomainValueText.ToStorageValue(InvoiceKind...)` üzerinden oluşturuluyor.
- Kurum türü listesi de mevcut `InstitutionKind + DomainValueText` typed katmanından üretiliyor.
- Fatura girişindeki `InvoiceTypeBox` aynı ortak `InvoiceTypes` kaynağını kullanıyor.
- Arşiv tür filtresi `"Tümü"` prefix'i + aynı ortak `InvoiceTypes` kaynağından üretiliyor.
- Dashboard fatura türü renk seçimi artık storage string'leri üzerinde ikinci bir switch taşımıyor; önce `DomainValueText.ParseInvoiceKind` ile typed değere dönüyor, sonra `InvoiceKind` üzerinden renk seçiyor.
- Kalıcı JSON/storage metinleri değiştirilmedi; `DomainValueText` mevcut storage değerlerini aynen üretmeye devam ediyor.

Commitler:
- UI tür kaynakları: `f92aed4f39f943d6da639a6fa748876722aabd1f`
- Dashboard typed eşleme: `071f1302f8487fba137d10cfb05e99592b87ecbf`
- Dashboard Core.Models import/statik temizlik fix'i: `78110d3b191c7277aea2b87a67dca4ed78161b0c`

Statik kontrol:
- `InvoiceTypeBox.ItemsSource = InvoiceTypes`: mevcut
- kurum türleri `InstitutionKind` üzerinden: mevcut
- arşiv fatura türü filtresi ortak `InvoiceTypes` kaynağını kullanıyor
- dashboard `DomainValueText.ParseInvoiceKind` + `InvoiceKind` switch kullanıyor
- dashboard'da ham `"Elektrik" / "Telefon" / "İnternet" / "Su"` renk switch'i: **0**
- veri modeli, JSON şeması, restore, SHA-256/manifest, SchoolId, PDF/arşiv akışına dokunulmadı.

Sıradaki doğrulama:
1. GitHub Desktop → **Fetch origin → Pull origin**
2. `DERLE_CALISTIR.bat`
   - beklenen: **0 warning / 0 error**
   - normal başlatma
3. Hızlı smoke:
   - Fatura Girişi tür listesi: Elektrik / Telefon / İnternet / Su
   - İşlem Arşivi tür filtresi: Tümü + aynı dört tür
   - Ana Ekran fatura türü grafik renkleri
   - Kurum türü listesi
4. `TESTLERI_CALISTIR.bat`
   - yeni test eklenmedi; beklenen **117/117**
5. Bu iki kapı yeşil olmadan Paket 11'e geçilmeyecek.

## 19.09.2026 — Kod temizliği / sadeleştirme paketi 10 TAMAMLANDI VE DOĞRULANDI

Kullanıcının yerel doğrulaması:

Build:
- SDK: `.NET 10.0.400`
- `DogruFatura.Core`: başarılı
- `DogruFatura.Wpf`: başarılı
- Toplam: **0 warning / 0 error**
- `[3/3] DogruFatura baslatiliyor...` aşamasına ulaşıldı.
- Toplam build: yaklaşık **8,5 sn**

Tam xUnit:
- Toplam test: **117**
- Başarılı: **117**
- Başarısız: **0**
- Atlandı: **0**
- `DogruFatura.Tests`: başarılı
- Test çalıştırması: yaklaşık **10,9 sn**
- Test özeti: yaklaşık **11,8 sn**
- Toplam test build/koşu: yaklaşık **17,3 sn**
- BAT sonucu: **[OK] TUM TESTLER BASARILI**

Sonuç:
- Paket 10 typed UI sadeleştirmesi yerel build + tam regresyon paketiyle yeşil doğrulandı.
- Yeni güvenilir baseline: **0 warning / 0 error + 117/117 xUnit**.

## 19.09.2026 — Kod temizliği / sadeleştirme paketi 11 — KODLANDI, DOĞRULAMA BEKLİYOR

Paket 10'un yeşil baseline'ından sonra mevcut `ArchiveState` typed katmanının arşiv UI filtresine uygulanması tamamlandı.

Değişiklikler:
- `DomainValueText.ParseArchiveState(string?)` eklendi.
  - `"Açık"` → `ArchiveState.Open`
  - `"Kapalı"` → `ArchiveState.Closed`
  - diğer değerler / `"Tümü"` → `null`
- `DomainValueText.ToStorageValue(ArchiveState)` eklendi.
- `ArchivedInvoiceList.ClosureStatus` artık doğrudan `IsClosed ? "Kapalı" : "Açık"` tekrarını taşımıyor; `State` üzerinden typed kaynağı kullanıyor.
- Arşiv durum filtresi seçenekleri `ArchiveState.Open/Closed` üzerinden üretiliyor.
- `FilterArchivedList` içinde ham `"Açık"/"Kapalı"` karşılaştırmaları kaldırıldı; seçili değer typed olarak parse edilip `archive.State` ile karşılaştırılıyor.
- Mevcut domain regresyon testine ArchiveState metin round-trip kontrolleri eklendi; yeni test metodu eklenmedi, bu yüzden toplam test sayısı değişmemeli.

Commitler:
- Domain metin dönüşümü: `740c5d929ede796f7a1a3af5a08783192c05f82a`
- Arşiv model durum metni: `ec2018a744c49aa2ac35c358f4e0b384cebeba08`
- UI durum filtre kaynağı: `ddea0eca4c6b6548b20ac78e980082b29552aa22`
- Typed filtre karşılaştırması: `b699eb9b26bd5f4faf56a777d769934f0b82c980`
- Regresyon assertion'ları: `d47f0d2a48703b9d4187bf5ab2bafa6720e71525`

Statik kontrol:
- ArchiveState parser: mevcut
- ArchiveState storage metni: mevcut
- `ClosureStatus` typed kaynaktan: mevcut
- arşiv filtre seçenekleri typed kaynaktan: mevcut
- filtre karşılaştırması `archive.State` üzerinden: mevcut
- eski `selectedStatus == "Açık"/"Kapalı"` karşılaştırmaları: **0**
- veri modeli JSON şeması, restore, SHA-256/manifest, SchoolId ve PDF/arşiv dosya akışı değiştirilmedi.

Sıradaki doğrulama:
1. GitHub Desktop → **Fetch origin → Pull origin**
2. `DERLE_CALISTIR.bat`
   - beklenen: **0 warning / 0 error**
   - normal başlatma
3. Hızlı smoke:
   - İşlem Arşivi → durum filtresi: Tümü / Açık / Kapalı
   - Açık filtresi yalnız açık listeleri göstermeli
   - Kapalı filtresi yalnız kapalı listeleri göstermeli
   - arşiv listesinde durum metni aynı görünmeli
4. `TESTLERI_CALISTIR.bat`
   - yeni test metodu eklenmedi; beklenen toplam **117/117**
5. Bu iki kapı yeşil olmadan Paket 12'ye geçilmeyecek.

## 19.09.2026 — Üst şerit kompakt sürüm bilgisi — KODLANDI, DOĞRULAMA BEKLİYOR

Kullanıcı talebiyle ana pencerenin üst şeridine küçük sürüm bilgisi eklendi.

Uygulama:
- Eski büyük `SÜRÜM` kartı geri getirilmedi.
- `TEMİZLE` düğmesinin üstünde küçük, pasif `HeaderVersionText` etiketi eklendi.
- Görünür metin `AppVersionInfo.DisplayVersion` üzerinden geliyor.
- Tooltip tam `ApplicationSafetyService.InformationalVersion` değerini gösteriyor.
- Mevcut tek sürüm kaynağı `Directory.Build.props`; şu an `VersionPrefix=1.0.1`, dolayısıyla etiket mevcut build'de **v1.0.1** gösterir.
- Branch adı/1.1 geliştirme çalışması nedeniyle sürüm numarası otomatik olarak 1.1'e yükseltilmedi; sürüm bump ayrı yayın kararı olarak bırakıldı.
- Hakkında ekranındaki sürüm alanı korunuyor.

Commitler:
- XAML kompakt etiket: `753ba4cce7cbbcf40bb83d35f2b6d2dc5bbd0933`
- Dinamik sürüm doldurma: `96c76ed5a72332a38b23f7eedf299c9b094a5d46`

Statik kontrol:
- `HeaderVersionText`: mevcut
- kompakt font: 10.5
- görünür sürüm: `AppVersionInfo.DisplayVersion`
- tam sürüm tooltip: mevcut
- ikinci bağımsız sürüm sabiti eklenmedi

Bu değişiklik Paket 11 doğrulama kapısına dahil edilmiştir.

Doğrulama:
1. GitHub Desktop → **Fetch origin → Pull origin**
2. `DERLE_CALISTIR.bat`
3. Üst şeritte küçük **v1.0.1** etiketinin görünümünü kontrol et
4. İşlem Arşivi → Tümü / Açık / Kapalı filtresini kontrol et
5. `TESTLERI_CALISTIR.bat`
6. Beklenen: **0 warning / 0 error + 117/117**

## 19.09.2026 — Üst şerit görsel sadeleştirme revizyonu — KODLANDI, DOĞRULAMA BEKLİYOR

Kullanıcının ekran görüntüsündeki işaretlere göre üst şerit yeniden dengelendi.

Sorunlar:
- `HESAPLAMA` kartı gereğinden büyük görünüyordu.
- `AKTİF KURUM` alanı yıldız (`*`) kolonu doldurduğu için içerikten çok daha uzun boş bir kutu oluşturuyordu.
- Küçük sürüm etiketi `TEMİZLE` düğmesinin üstüne yerleştirildiği için düğmeyle tek bileşen gibi ve sıkışık görünüyordu.

Uygulanan düzen:
- Logo alanı korunuyor.
- `HESAPLAMA` durumu 148 px genişliğinde daha kompakt bir karta indirildi.
- `AKTİF KURUM` alanı 280 px sabit kolona alındı; artık kalan bütün boşluğu işgal etmiyor.
- Orta alan gerçek boşluk/spacer olarak bırakıldı; üst şerit daha ferah.
- `FORM HAZIRLIK` göstergesi korunuyor.
- `TEMİZLE` yeniden bağımsız düğme oldu.
- `HeaderVersionText` ayrı 58 px kolona taşındı; pencere kontrollerinden hemen önce küçük `v...` etiketi olarak gösteriliyor.
- Sürüm tooltip'i artık etkileşim alabiliyor; tam informational version korunuyor.
- Minimize / maximize / close kontrolleri aynı işlev ve isimlerle korunuyor.

Davranış koruması:
- `EngineStatusDot`, `EngineStatusText`
- `ActiveInstitutionText`
- `HeaderReadinessArc`, `HeaderReadinessText`
- `HeaderVersionText`
- `MaximizeRestoreButton`, `MaximizeRestoreIcon`

XAML içinde bu isimlerin her biri **tek kez** bulunuyor; event handler veya canlı güncelleme mantığı değiştirilmedi.

Commit:
- `f1b7db244f31106060cb6e84c3d2d28318bb94e8`

Bu revizyon Paket 11 + header version doğrulama kapısına dahildir.

Sıradaki doğrulama:
1. GitHub Desktop → **Fetch origin → Pull origin**
2. `DERLE_CALISTIR.bat`
3. Ana Ekran üst şerit görsel kontrolü:
   - HESAPLAMA kompakt,
   - AKTİF KURUM gereksiz uzamıyor,
   - orta boşluk temiz,
   - FORM HAZIRLIK hizalı,
   - TEMİZLE bağımsız,
   - sürüm etiketi pencere düğmelerinden önce küçük ve okunaklı.
4. İşlem Arşivi → Tümü / Açık / Kapalı filtresi
5. `TESTLERI_CALISTIR.bat`
6. Beklenen: **0 warning / 0 error + 117/117**

## 19.09.2026 — Üst şerit sürüm konumu revizyonu — KODLANDI, DOĞRULAMA BEKLİYOR

Kullanıcının ekran görüntüsündeki kırmızı işarete göre küçük sürüm etiketi sağ pencere kontrollerinin yanından alınarak orta boş alana taşındı.

Yeni yerleşim:
- `AKTİF KURUM` kartı solda kompakt kalıyor.
- `HeaderVersionText` artık `AKTİF KURUM` ile `FORM HAZIRLIK` arasındaki esnek boş kolonda ortalanıyor.
- Sürüm etiketi `11.5` font boyutuyla küçük ama daha rahat okunur.
- `FORM HAZIRLIK` ve `TEMİZLE` sağ grupta korunuyor.
- Minimize / maximize / close düğmeleri yeniden doğrudan sağ kenarda yan yana.
- Dinamik sürüm kaynağı ve tooltip mantığı değiştirilmedi.

Commit:
- `9932899665ea93d15bdb40ee90ee65989e3acd7b`

Bu değişiklik de Paket 11 doğrulama kapısına dahildir.

Doğrulama:
1. GitHub Desktop → **Fetch origin → Pull origin**
2. `DERLE_CALISTIR.bat`
3. Üst şeritte sürüm etiketinin orta boşlukta dengeli görünmesini kontrol et
4. İşlem Arşivi Tümü / Açık / Kapalı filtresini kontrol et
5. `TESTLERI_CALISTIR.bat`
6. Beklenen: **0 warning / 0 error + 117/117**

## 19.09.2026 — Üst şerit sürüm kartı revizyonu — KODLANDI, DOĞRULAMA BEKLİYOR

Kullanıcı talebiyle üst şeritteki sürüm bilgisi düz metin olmaktan çıkarılıp `AKTİF KURUM` kartıyla aynı görsel dilde karta dönüştürüldü.

Uygulanan düzen:
- `AKTİF KURUM`: 280 px
- aralık: 12 px
- `SÜRÜM`: 140 px
- kalan orta alan: esnek boşluk
- `FORM HAZIRLIK`: 120 px
- `TEMİZLE`: 116 px
- pencere kontrolleri sağ kenarda korunuyor

Sürüm kartı stili:
- arka plan: `#082744`
- border: `#24496B`
- border kalınlığı: 1
- `CornerRadius=8`
- `Padding=11,6`
- başlık: `SÜRÜM`
- başlık fontu: **10.5** — `AKTİF KURUM` ile aynı
- değer fontu: **12.5** — `ActiveInstitutionText` ile aynı
- değer rengi: beyaz
- `HeaderVersionText` dinamik sürüm kaynağını kullanmaya devam ediyor.

Davranış:
- `HeaderVersionText.Text = AppVersionInfo.DisplayVersion`
- tam informational version tooltip korunuyor
- ikinci sürüm kaynağı eklenmedi
- Hakkında ekranındaki sürüm bilgisi değiştirilmedi

Commit:
- `3072b4fc8b07751dbcdb49d343a759a3286add56`

Bu değişiklik Paket 11 doğrulama kapısına dahildir.

Sıradaki doğrulama:
1. GitHub Desktop → **Fetch origin → Pull origin**
2. `DERLE_CALISTIR.bat`
3. Üst şerit:
   - SÜRÜM kartı AKTİF KURUM ile aynı stil ailesinde görünmeli
   - SÜRÜM başlığı ile AKTİF KURUM başlığı aynı boyutta olmalı
   - `v1.0.1` ile kurum değer metni aynı boyutta olmalı
   - FORM HAZIRLIK / TEMİZLE / pencere kontrolleri hizalı olmalı
4. İşlem Arşivi → Tümü / Açık / Kapalı filtresi
5. `TESTLERI_CALISTIR.bat`
6. Beklenen: **0 warning / 0 error + 117/117**

## 19.09.2026 — Üst şerit sürüm kartı GÖRSEL OLARAK DOĞRULANDI

Kullanıcı yeni sürüm kartının çalıştığını doğruladı.

- `SÜRÜM` kartı üst şeritte görünür.
- `AKTİF KURUM` ile aynı başlık/değer font ölçüleri kullanılıyor.
- Dinamik sürüm bilgisi çalışıyor.
- Bu bildirim görsel/çalışma doğrulamasıdır; Paket 11 için tam xUnit çıktısı ayrıca paylaşılmadığı sürece tam test kapısı ayrı tutulur.

## 19.09.2026 — Kod temizliği / sadeleştirme paketi 12 — KODLANDI, DOĞRULAMA BEKLİYOR

Okul/Kurum ekranındaki tür filtresinde kalan ham string karşılaştırması mevcut typed domain katmanına taşındı.

Değişiklikler:
- `FilterSchool` artık seçili ComboBox metnini `DomainValueText.ParseInstitutionKind` ile `InstitutionKind` değerine çeviriyor.
- Filtre, `school.InstitutionType != selectedType` yerine `school.Kind != selectedKind` karşılaştırması kullanıyor.
- `Tümü` veya geçersiz/boş seçim `InstitutionKind.Unknown` olarak yorumlanıp filtre uygulanmıyor; mevcut kullanıcı davranışı korunuyor.
- `StartNewSchool` içinde filtre seçiliyse yeni kayıt türü typed değerden `DomainValueText.ToStorageValue` ile geri üretiliyor.
- Kalıcı `InstitutionType` JSON string alanı değişmedi.
- Kayıt ekleme/düzenleme/silme, SchoolId, store/save, restore veya invoice bağına dokunulmadı.
- Aynı dosyada monolitik dönemden kalmış `{    private` biçimleme artığı da temizlendi.

Commit:
- `535a6ac5d9fea7bba00b6895272659fd2df51230`

Statik kontrol:
- typed institution parse: mevcut
- `school.Kind` filtresi: mevcut
- eski `selectedType != "Tümü"` filtresi: **0**
- yeni kayıt varsayılanı typed kaynaktan storage metnine dönüyor
- JSON/storage sözleşmesi değişmedi

Sıradaki doğrulama:
1. GitHub Desktop → **Fetch origin → Pull origin**
2. `DERLE_CALISTIR.bat`
3. OKUL / KURUM ekranı:
   - Tümü filtresi tüm kayıtları göstermeli
   - İlkokul / Ortaokul / Anasınıfı-Anaokulu / Kurum filtreleri doğru çalışmalı
   - belirli tür filtresindeyken YENİ KAYIT o türü varsayılan seçmeli
4. İşlem Arşivi → Tümü / Açık / Kapalı filtresini de hızlıca kontrol et
5. `TESTLERI_CALISTIR.bat`
6. Beklenen: **0 warning / 0 error + 117/117**

## 19.09.2026 — Paket 11–12 kullanıcı doğrulaması BAŞARILI

Kullanıcı build, görsel/smoke ve test akışlarının başarılı olduğunu bildirdi.

- Üst şerit sürüm kartı çalışıyor.
- Paket 11 ArchiveState filtresi çalışıyor.
- Paket 12 InstitutionKind okul/kurum filtresi çalışıyor.
- Bu kullanıcı bildirimi başarılı doğrulama olarak kaydedildi.
- Ayrıntılı sayaç/log bu mesajda yeniden paylaşılmadığı için önceki doğrulanmış **117/117** baseline referans olarak korunuyor.

## 19.09.2026 — Kod temizliği / sadeleştirme paketi 13 — KODLANDI, DOĞRULAMA BEKLİYOR

Kontrol Merkezi önem derecesi filtresinde kalan `"Hata"/"Uyarı"` string mantığı mevcut `ControlIssueSeverity` enum'una bağlandı.

Değişiklikler:
- `DomainValueText.ParseControlIssueSeverity(string?)` eklendi.
- `DomainValueText.ToDisplayValue(ControlIssueSeverity)` eklendi.
- `ControlIssue.SeverityLabel` artık ortak typed metin kaynağını kullanıyor.
- `ControlSeverityFilterBox` seçenekleri enum değerlerinden üretiliyor.
- `FilterControlIssue` ham label/string karşılaştırması yerine `issue.Severity` enum karşılaştırması yapıyor.
- `Tümü` seçimi `null` parse edilerek mevcut “filtre yok” davranışını koruyor.
- Mevcut domain regresyon testine Hata/Uyarı parse + display round-trip assertion'ları eklendi; yeni test metodu eklenmedi.
- `MainWindow.Control.cs` içindeki monolitik dönemden kalmış `{    private` biçimleme artığı temizlendi.

Commitler:
- Domain severity metinleri: `5f56c17dce4d774ea199b64691f5655f327d9f9d`
- ControlIssue label: `78aecee0ae41443c28a0220700fd7d9d8ab46150`
- UI severity kaynağı: `1402018baa0f0787c8d263fe36abd82f410c4f98`
- Typed severity filtresi: `3a55718432b32319ac7bddc64b8885e0ce48db50`
- Regresyon assertion'ları: `917606ce6e34029aa30db37063407d6d47a4f81c`

Statik kontrol:
- severity parse: mevcut
- severity display: mevcut
- `SeverityLabel` typed kaynaktan: mevcut
- UI filtre seçenekleri typed kaynaktan: mevcut
- filtre enum karşılaştırması: mevcut
- eski `severity != "Tümü"` / label karşılaştırması: **0**

Sıradaki doğrulama:
1. GitHub Desktop → **Fetch origin → Pull origin**
2. `DERLE_CALISTIR.bat`
3. Kontrol Merkezi → önem filtresi:
   - Tümü
   - Hata
   - Uyarı
4. `TESTLERI_CALISTIR.bat`
5. Beklenen: **0 warning / 0 error + 117/117**
6. Bu kapı yeşil olursa sadeleştirme paketlerini burada durdurup Aşama 3'e geçilecek.

## 19.09.2026 — Paket 13 kullanıcı doğrulaması BAŞARILI

Kullanıcı Paket 13 sonrasında her şeyin yolunda olduğunu bildirdi.

- Kontrol Merkezi önem filtresi typed enum geçişi çalışıyor.
- Build/smoke/test akışında sorun bildirilmedi.
- Ayrıntılı sayaç tekrar paylaşılmadığı için son açık sayısal baseline **117/117** olarak korunuyor.

## 19.09.2026 — Aşama 2 mimari kapanış envanteri TAMAMLANDI

`MainWindow*.cs` taramasında:
- yeni `Service/Store/Engine/Guard` nesnesi üretimi kalmadı,
- rapor/arşiv/safety/tax servisleri composition root veya injected alanlar üzerinden kullanılıyor,
- kalan `ControlCenterService.Scan`, `DashboardStatisticsService.Build`, `FinalReportValidationService.Validate`, `InvoiceValidationService.Validate` vb. çağrılar stateless domain/application helper niteliğinde.

Karar:
- Bu helper'ları yalnız “DI kapsamı büyüsün” diye constructor'a taşımak ek karmaşıklık yaratacağı için yapılmayacak.
- Aşama 2 mevcut kapsamda mimari olarak yeterli kabul edildi.
- Yeni bir davranış gerçek state/IO bağımlılığı gerektirirse o noktada application-service/ViewModel ayrımı yapılacak.

## 19.09.2026 — Aşama 3.1 Vergi kural sürümü ve yürürlük metadata'sı — KODLANDI, DOĞRULAMA BEKLİYOR

Amaç:
- hesap sonucunun hangi vergi kural setiyle üretildiğini izlenebilir hale getirmek,
- gelecekte oran/yürürlük değişikliklerini tarihli kural kataloğuna taşıyabilecek bir temel oluşturmak,
- mevcut 0,00948 hesabını ve legacy davranışı değiştirmemek.

Yeni model:
- `TaxRuleDefinition`
  - `Version`
  - `EffectiveFrom`
  - `StampDutyRate`
  - `IsEffectiveOn(DateOnly)`

Yeni katalog:
- `TaxRuleCatalog`
- mevcut teknik kural kimliği: `DV-0.00948-R1`
- mevcut oran: `0.00948`
- `EffectiveFrom = null`
  - nedeni: legacy uygulamada doğrulanmış/hukuki yürürlük tarihi metadata'sı tutulmuyordu,
  - bu aşamada doğrulanmamış bir hukuki tarih **uydurulmadı**.

`TaxCalculationResult` artık ayrıca taşıyor:
- `RuleVersion`
- `RuleEffectiveFrom`
- `AppliedStampDutyRate`

`TaxCalculationService`:
- aktif `TaxRuleDefinition` ile çalışıyor,
- default olarak `TaxRuleCatalog.Current` kullanıyor,
- geçersiz hesap sonuçlarında bile hangi kuralın kullanıldığını metadata olarak koruyor,
- mevcut `TaxCalculationService.StampDutyRate` sabiti geriye uyumluluk için korunuyor,
- kullanıcı mesajındaki oran aktif kuraldan `tr-TR` formatında üretiliyor; mevcut kuralda görünür metin yine `0,00948`.

Test:
- mevcut `ElectricityCalculationUsesApprovedStampDutyFormula` testine kural version/rate/effective-date assertion'ları eklendi.
- yeni test metodu eklenmedi; beklenen toplam hâlâ **117**.

Commitler:
- model: `fea04730c7e04ef7024fa1c1632c2ab093e4f350`
- katalog: `7eac055e9df851370151e54e01c1e7e59d5a248c`
- result metadata: `2ed2a801c1afd7018bb8387bcf17507bfe416e3d`
- calculation service: `40950eec02c834ab729e4a5680870fa87dfd208e`
- test: `6f398e33a55104cbf289bfe27605fc713ff59126`
- aktif oran mesajı düzeltmesi: `b98f1e1d505b9ae037aeb3022f8d9d267e5c557c`

Statik kontrol:
- kural modeli: mevcut
- version: mevcut
- nullable yürürlük tarihi: mevcut
- sonuç metadata'sı: mevcut
- valid/invalid hesaplarda rule metadata: mevcut
- aktif kural oranı hesaplamada tek kaynak: mevcut
- hukuki tarih varsayımı: **yok**
- mevcut oran/formül: değişmedi

Sıradaki doğrulama:
1. GitHub Desktop → **Fetch origin → Pull origin**
2. `DERLE_CALISTIR.bat`
   - beklenen: **0 warning / 0 error**
3. Fatura Girişi:
   - Elektrik normal hesap
   - Su damga vergisi istisnası
   - Elektrik kesinti tazminatı
   hızlı smoke
4. `TESTLERI_CALISTIR.bat`
   - yeni test metodu yok
   - beklenen: **117/117**
5. Yeşil olursa Aşama 3.2'de kural metadata'sı yeni/güncellenen `InvoiceRecord` kayıtlarına legacy uyumlu opsiyonel alanlarla taşınacak.

## 19.09.2026 — Aşama 3.1 kullanıcı doğrulaması BAŞARILI

Kullanıcı Aşama 3.1 sonrasında build/smoke/test akışlarının başarılı olduğunu bildirdi.

- Vergi kural metadata modeli çalışıyor.
- Mevcut 0,00948 hesap davranışında sorun bildirilmedi.
- Ayrıntılı sayaç tekrar paylaşılmadığı için son açık sayısal baseline **117/117** olarak korunuyor.

## 19.09.2026 — Aşama 3.2 InvoiceRecord vergi kural metadata kalıcılığı — KODLANDI, DOĞRULAMA BEKLİYOR

Amaç:
- yeni/güncellenen kullanıcı faturalarında hangi vergi kuralıyla hesap yapıldığını kalıcı kayıtta tutmak,
- legacy ve tarihsel veriye geriye dönük yanlış kural atfetmemek.

`InvoiceRecord` sonuna geriye uyumlu opsiyonel alanlar eklendi:
- `TaxRuleVersion = ""`
- `TaxRuleEffectiveFrom = null`
- `AppliedStampDutyRate = 0m`

Ayrıca:
- `HasTaxRuleMetadata` JSON dışı görünümü eklendi.
- Positional constructor'ın mevcut parametre sırası bozulmadı; yeni alanlar en sona eklendi.

Yeni/güncellenen kullanıcı faturası:
- `MainWindow.InvoiceEntry` hesap sonucundaki
  - `RuleVersion`
  - `RuleEffectiveFrom`
  - `AppliedStampDutyRate`
  değerlerini `InvoiceRecord` içine yazıyor.
- Fatura güncellemesinde mevcut ID ve CreatedAt korunurken kural metadata'sı yeni hesap sonucuna göre güncelleniyor.

Legacy veri:
- Yeni alanları hiç içermeyen eski JSON kayıtları yüklenebiliyor.
- `InvoiceDataStore.Normalize` eksik/null `TaxRuleVersion` değerini boş string olarak normalize ediyor.
- Eski fatura değerleri değişmeden geçerliyse metadata **bilinmiyor** olarak korunuyor:
  - `TaxRuleVersion = ""`
  - `TaxRuleEffectiveFrom = null`
  - `AppliedStampDutyRate = 0`
- Geçmişe dönük olarak mevcut kuralın uygulanmış olduğu varsayılmıyor.

Su faturası legacy migration:
- Eğer store mevcut su faturasının vergi alanlarını gerçekten yeniden hesaplayıp güncelliyorsa, o yeni hesapla birlikte rule metadata da yazılıyor.
- Böylece metadata yalnız gerçekten yapılan yeniden hesaplamaya bağlanıyor.

Tarihsel PDF import:
- PDF tutarları güncel motorla doğrulansa bile özgün PDF'nin hangi kural sürümünde üretildiği kanıtlanamadığı için rule metadata bilinçli olarak boş bırakılıyor.
- Bu davranış kod içine açıklayıcı yorumla kaydedildi.

Test kapsamı:
- `LegacyInvoiceFileIsLoadedUpgradedAndOriginalFormatIsBackedUp` artık yeni tax metadata alanlarını hiç içermeyen gerçek legacy JSON biçimini kuruyor ve yükleme sonrası metadata'nın bilinmiyor kaldığını doğruluyor.
- `CurrentSchemaSaveAndReloadRoundTripsWithoutMigrationWarning` yeni metadata'nın save/reload round-trip ile aynen korunduğunu doğruluyor.
- `TypedDomainViewsPreserveExistingStorageText` yeni kullanıcı faturası modelinde rule metadata görünümünü doğruluyor.
- Yeni test metodu eklenmedi; beklenen toplam hâlâ **117**.

Commitler:
- InvoiceRecord metadata: `266850af44cb50470f9dd669487cc857af454bde`
- kullanıcı fatura kayıt yolu: `af7deff9fb56d18b128f68859cfe031552d4d015`
- store legacy/migration davranışı: `4dd003c118e23c037be265e2db3fa2700b03de98`
- schema migration/roundtrip testleri: `9e3ed427f9492f09bff0accb5f415412f3b557d4`
- domain metadata testleri: `19444757e4eb9938710027558055e1b9158f0365`
- tarihsel PDF metadata açıklaması: `b46446cc643ee15d84caabaac02a8b2a4b8c1178`

Sıradaki doğrulama:
1. GitHub Desktop → **Fetch origin → Pull origin**
2. `DERLE_CALISTIR.bat`
   - beklenen: **0 warning / 0 error**
3. Smoke:
   - yeni Elektrik faturası ekle
   - mevcut faturayı düzenle/kaydet
   - uygulamayı kapat/aç ve aktif faturanın korunmasını kontrol et
   - tarihsel PDF import akışında davranış değişikliği olmadığını kontrol et
4. `TESTLERI_CALISTIR.bat`
   - yeni test metodu yok
   - beklenen: **117/117**
5. Bu kapı yeşil olursa Aşama 3.3 olarak **append-only audit trail** temel modeli ve güvenli store dilimine geçilecek.

## 19.09.2026 — Aşama 3.2 kullanıcı doğrulaması BAŞARILI

Kullanıcı Aşama 3.2 sonrasında tüm build/smoke/test akışlarının başarılı olduğunu bildirdi.

- InvoiceRecord tax rule metadata kalıcılığı çalışıyor.
- Legacy ve tarihsel kayıt davranışında sorun bildirilmedi.
- Son açık sayısal baseline **117/117** olarak korunuyordu.

## 19.09.2026 — Aşama 3.3 append-only audit trail — KODLANDI, DOĞRULAMA BEKLİYOR

Amaç:
- kritik kullanıcı veri değişikliklerini ayrı, yalnız ekleme yapan bir denetim günlüğünde izlenebilir hale getirmek,
- ana JSON veri dosyalarını veya kullanıcı işlemlerinin rollback semantiğini değiştirmemek,
- PII içeriğini audit detail'e taşımamak.

Yeni model:
- `AuditTrailEventType`
  - InvoiceCreated / InvoiceUpdated / InvoiceDeleted / InvoiceReordered
  - SchoolCreated / SchoolUpdated / SchoolDeleted
  - AuthorityProfileUpdated / AuthorityProfileDefaultsRestored
  - ArchiveCreated / ArchiveClosed / ArchiveReopened / ArchiveImported
- `AuditTrailEntityType`
  - Invoice / School / AuthorityProfile / Archive
- `AuditTrailEntry`
  - `EventId`
  - `OccurredAtUtc`
  - `EventType`
  - `EntityType`
  - `EntityId`
  - `Detail`

Yeni store:
- `AuditTrailStore`
- varsayılan dosya: `DoğruFatura/Veri/DenetimKaydi.jsonl`
- format: JSON Lines; her olay tek satır
- `FileMode.Append` kullanıyor
- `FileOptions.WriteThrough` + `Flush(flushToDisk: true)`
- mevcut satırları rewrite etmiyor
- `ReadAll()` denetim/test amaçlı mevcut
- enum değerleri JSON'da string olarak saklanıyor

Yedekleme:
- `ApplicationSafetyService.CollectBackupFiles()` Veri klasöründeki **tüm dosyaları** güvenlik ZIP'ine dahil ediyor.
- Bu nedenle `DenetimKaydi.jsonl` ayrıca özel backup kodu olmadan manuel/güncelleme öncesi/restore öncesi yedeklere girer.

Composition:
- `App.xaml.cs` `AuditTrailStore` oluşturuyor.
- `MainWindow` store'u constructor injection ile alıyor.
- `MainWindow.Audit.cs` içindeki `AppendAudit` helper:
  - append hatasını yakalıyor,
  - ana kullanıcı işlemini geri çevirmiyor,
  - teknik hatayı `ApplicationCrashLogger` üzerinden logluyor.

Audit edilen başarıyla kalıcılaşmış olaylar:
- fatura oluşturma
- fatura güncelleme
- fatura silme
- fatura sıralama
- kurum oluşturma
- kurum güncelleme
- kurum silme
- yetkili profil güncelleme
- yetkili varsayılanlarını geri yükleme
- aktif listeyi açık arşive alma
- arşivi HYS ile kapatma
- arşiv kapanışını geri alma
- tarihsel PDF'yi kapalı arşiv olarak import etme

PII koruması:
- audit detail içinde kurum adı, VKN, fatura numarası veya yetkili adları yazılmıyor.
- yalnız GUID/entity anahtarı ve teknik özetler tutuluyor:
  - type
  - rule version
  - position
  - institution kind
  - archive state / invoice count

Rollback güvenliği:
- Audit çağrıları ilgili ana store yazımı başarıyla tamamlandıktan sonra çalışıyor.
- Veri yazımı rollback olursa audit olayı üretilmiyor.
- Audit yazımı başarısız olursa ana veri işlemi bozulmuyor.

Yeni test:
- `AuditTrailStoreTests.AppendPreservesExistingBytesAndAddsReadableEvent`
- ilk append sonrası dosyanın byte'ları saklanıyor,
- ikinci append sonrası ilk byte prefix'in birebir korunduğu doğrulanıyor,
- `ReadAll()` ile iki olayın doğru sırada ve UTC offset ile okunması doğrulanıyor.

Bu paketle test sayısı:
- önce: **117**
- yeni: **118**

Commitler:
- model: `54f262b002ba3518e8159a045c3b6e8862536ef7`
- append-only store: `42647934ee0a3f0be20740d8639998d6e3f50893`
- composition root: `2c3fa576f1e3adcb3517bc2dd6b58c11b1127fff`
- MainWindow DI: `0419ec24702e595c419faba56f62e30a35df6669`
- audit helper: `09804c2f717660d5957d9245aa26305279f85699`
- invoice create/update audit: `8dd5e2c5b6e071f07f5e9b318f17f495d70b3c34`
- invoice delete/reorder audit: `4f7e0ede86cc44682bef1c9b81671a1f1c0e7e1f`
- school/authority audit: `e35ed15a270b31b58099a571426bcc7c7b8c2ce1`
- archive state/create audit: `6e02b493ff0cc89c5e014ff77484984bbfd2a298`
- historical import audit: `ab1e84170daf79b659d3dc1123d628732fca09ec`
- append-only regression test: `98fa7fc02334ce7e3e2ec8778b5be4364a803994`

Sıradaki doğrulama:
1. GitHub Desktop → **Fetch origin → Pull origin**
2. `DERLE_CALISTIR.bat`
   - beklenen: **0 warning / 0 error**
3. Smoke:
   - bir fatura ekle veya güncelle
   - bir kurum ekle/güncelle
   - mümkünse arşiv oluştur/kapat akışını dene
   - `Masaüstü/DoğruFatura/Veri/DenetimKaydi.jsonl` dosyasının oluştuğunu ve her işlemde yeni satır eklendiğini kontrol et
   - mevcut eski satırların silinmediğini kontrol et
4. `TESTLERI_CALISTIR.bat`
   - beklenen: **118/118**
5. Yeşil olursa Aşama 3.4 olarak **transaction journal** tasarım/uygulama dilimine geçilecek.

## 19.09.2026 — Aşama 3.3 ilk derleme hatası ve düzeltmesi

Kullanıcının ilk Aşama 3.3 build çıktısı:

- Core: başarılı
- WPF: **4 hata**
- Testler: derleme başarısız olduğu için çalışmadı

Hatalar:
- `MainWindow.InvoiceList.cs(142,13)` CS0103: `AuditTrailEventType`
- `MainWindow.InvoiceList.cs(143,13)` CS0103: `AuditTrailEntityType`
- `MainWindow.InvoiceList.cs(178,13)` CS0103: `AuditTrailEventType`
- `MainWindow.InvoiceList.cs(179,13)` CS0103: `AuditTrailEntityType`

Neden:
- `MainWindow.InvoiceList.cs` audit enum'larını kullanıyordu ancak `using DogruFatura.Core.Models;` importu eksikti.

Düzeltme:
- eksik Models namespace importu eklendi.
- aynı dosyada eski `{    private` biçimleme artığı da temizlendi.

Fix commit:
- `37050a07d1e743be678db4299ddbeca48965daa5`

Statik kontrol:
- AuditTrail kullanan WPF dosyalarının Models importları kontrol edildi.
- Paket/Aşama ilerletilmedi; Aşama 3.3 hâlâ doğrulama bekliyor.

Sıradaki kapı:
1. GitHub Desktop → **Fetch origin → Pull origin**
2. `DERLE_CALISTIR.bat`
3. Başarılıysa audit smoke
4. `TESTLERI_CALISTIR.bat`
5. Beklenen: **0 warning / 0 error + 118/118**

## 19.09.2026 — Aşama 3.3 kullanıcı doğrulaması BAŞARILI

Kullanıcı compile fix sonrasında Aşama 3.3 için tüm doğrulama akışlarının başarılı olduğunu bildirdi.

- Build: başarılı
- Audit smoke: başarılı
- Test kapısı: **118/118**
- Append-only `DenetimKaydi.jsonl` paketi doğrulandı.

## 19.09.2026 — Aşama 3.4 transaction journal — KODLANDI, DOĞRULAMA BEKLİYOR

Amaç:
- gerçek çok-dosyalı kritik işlem olan **aktif listeyi işlem arşivine taşıma** akışını journal ile izlenebilir/güvenli hale getirmek,
- audit trail'den farklı olarak transaction'ın hangi kalıcı adımda kaldığını bilmek,
- uygulama kapanması veya I/O sorunu sonrasında yarım kalmış işlemi otomatik veri değişikliği yapmadan tespit etmek.

Yeni model:
- `TransactionJournalOperation`
  - `ArchiveActiveList`
- `TransactionJournalState`
  - `Started`
  - `ArchivePersisted`
  - `ActiveListCleared`
  - `Completed`
  - `RolledBack`
  - `RecoveryRequired`
- `TransactionJournalEntry`
  - `EntryId`
  - `TransactionId`
  - `OccurredAtUtc`
  - `Operation`
  - `State`
  - `EntityId`
  - `Detail`
  - `IsTerminal`

Yeni store:
- `TransactionJournalStore`
- varsayılan dosya: `DoğruFatura/Veri/IslemJournal.jsonl`
- JSON Lines / append-only
- `FileMode.Append`
- `FileOptions.WriteThrough`
- `Flush(flushToDisk: true)`
- `ReadAll()`
- `GetIncompleteTransactions()`
- ani kapanmada son append satırı yarım kalırsa yalnız **son bozuk satır** tolere edilir; önceki sağlam journal prefix'i kullanılarak yarım işlem tespiti sürdürülür.

Composition:
- `App.xaml.cs` `TransactionJournalStore` oluşturuyor.
- `MainWindow` constructor injection ile store'u alıyor.
- açılışta `ShowPendingTransactionJournalWarning()` çalışıyor.

Açılış güvenliği:
- terminal state'i olmayan transaction bulunursa kullanıcıya uyarı gösteriliyor.
- uygulama **otomatik veri değiştirmiyor**.
- kullanıcıdan Aktif Fatura Listesi ile İşlem Arşivi'ni karşılaştırması ve yeni işlemden önce güvenlik yedeği alması isteniyor.
- journal okunamazsa teknik hata crash log'a yazılıyor ve güvenli uyarı gösteriliyor.

Aktif liste → arşiv transaction akışı:
1. Arşiv modeli doğrulanır.
2. Veri dosyalarında hiçbir değişiklik yapılmadan önce journal `Started` yazılır.
3. `IslemArsivi.json` başarıyla yazılır.
4. journal `ArchivePersisted` yazılır.
5. `AktifFaturalar.json` boş liste olarak başarıyla yazılır.
6. journal `ActiveListCleared` yazılır.
7. journal `Completed` yazılır.
8. mevcut audit trail `ArchiveCreated` olayı üretilir.

Hata/rollback davranışı:
- `Started` yazılamazsa transaction **hiç başlamaz**.
- arşiv store yazımı başarısızsa in-memory ekleme geri alınır ve `RolledBack` best-effort yazılır.
- arşiv kalıcılaştıktan sonra journal ilerletilemezse arşiv yazımı geri alınmaya çalışılır:
  - rollback başarılı → `RolledBack`
  - rollback başarısız → `RecoveryRequired`
- aktif liste temizleme başarısızsa mevcut arşiv rollback semantiği korunur:
  - rollback başarılı → `RolledBack`
  - rollback başarısız → `RecoveryRequired`
- iki veri dosyası başarıyla tamamlandıktan sonra journal kapanış satırı yazılamazsa ana veri işlemi geri çevrilmez; kullanıcıya journal uyarısı gösterilir ve sonraki açılışta incomplete transaction tespiti devreye girer.

PII:
- journal detail içinde kurum adı, VKN, fatura numarası veya yetkili adı tutulmuyor.
- yalnız archive GUID, invoice count ve invoice type gibi teknik transaction özeti bulunuyor.

Yedekleme:
- journal dosyası `Veri` klasöründe olduğu için mevcut `ApplicationSafetyService` tam güvenlik ZIP'lerine otomatik dahil olur.

Yeni test:
- `TransactionJournalStoreTests.IncompleteTransactionIsDetectedUntilCompleted`
- append-only prefix korumasını doğruluyor.
- `Started -> ArchivePersisted` sonrası transaction'ın incomplete bulunduğunu doğruluyor.
- `Completed` sonrası incomplete listesinden çıktığını doğruluyor.
- UTC timestamp davranışını doğruluyor.

Test sayısı:
- önce: **118**
- yeni: **119**

Commitler:
- transaction model: `8a4bb8cdbedff224c80fcb92e189dfc98cd1ed9b`
- journal store: `e3a8652aa02487579ed48745595aa66b5676d814`
- composition root: `b1223976799166afdcef2a9eea94b5204d3f74ff`
- MainWindow DI: `d6d40d7b635be4d940775e569f3ba010efd0fa2e`
- helper + startup warning: `79e4ea9f8829aa31d69f5206eb150768e55abd0c`
- startup warning çağrısı: `59612e50792da6f14e5b81803b7aab62481b2080`
- archive transaction entegrasyonu: `9b74b474500496af2c8d71af18bbcceae1038b4c`
- regression test: `6bfc14f8cfa6bc8b5c636c84a954123f2d7ca3bd`

Statik kontrol:
- composition wiring: geçti
- `Started` kalıcı veri değişikliğinden önce: geçti
- `Completed` aktif liste kalıcı temizliğinden sonra: geçti
- rollback / recovery-required yolları: mevcut
- append-only + disk flush: mevcut
- incomplete detection: mevcut
- son yarım satır toleransı: mevcut
- otomatik recovery/veri değiştirme: **yok**

Sıradaki doğrulama:
1. GitHub Desktop → **Fetch origin → Pull origin**
2. `DERLE_CALISTIR.bat`
   - beklenen: **0 warning / 0 error**
3. Smoke:
   - aktif listede geçerli birkaç fatura oluştur
   - **İşlem Arşivine Al** akışını tamamla
   - `Veri/IslemJournal.jsonl` oluşmalı
   - aynı transaction için sırasıyla `Started`, `ArchivePersisted`, `ActiveListCleared`, `Completed` satırları görünmeli
   - arşiv kaydı oluşmalı ve aktif liste boşalmalı
   - uygulamayı kapat/aç; tamamlanmış transaction için yarım işlem uyarısı **çıkmamalı**
4. `TESTLERI_CALISTIR.bat`
   - beklenen: **119/119**
5. Yeşil olursa Aşama 3.5 olarak **kapalı HYS arşivini değişmez belge modeliyle güçlendirme** işine geçilecek.

## 19.09.2026 — Aşama 3.4 kullanıcı doğrulaması BAŞARILI

Kullanıcı transaction journal paketi sonrasında tüm doğrulama akışlarının başarılı olduğunu bildirdi.

- Build: başarılı
- Transaction journal smoke: başarılı
- Tamamlanmış transaction sonrası açılışta yanlış yarım işlem uyarısı bildirilmedi
- Test kapısı: **119/119**

## 19.09.2026 — Aşama 3.5 kapalı HYS arşivi değişmez belge modeli — KODLANDI, DOĞRULAMA BEKLİYOR

Amaç:
- yeni kapatılan HYS arşivlerinin iş/veri içeriğini SHA-256 snapshot ile mühürlemek,
- mühürlü kayıtların uygulama üzerinden değiştirilmesini, yeniden açılmasını veya silinmesini store seviyesinde engellemek,
- mevcut PDF SHA-256 bütünlük mekanizmasını ayrı ve uyumlu tutmak,
- legacy kapalı fakat snapshot'sız kayıtları zorla dönüştürmeden geriye uyumluluğu korumak.

Yeni servis:
- `ClosedArchiveSnapshotService`
- kapalı arşivin deterministik snapshot SHA-256 özetini üretir.
- `Seal(archive)`:
  - yalnız kapalı,
  - HYS numarası olan,
  - kapanış tarihi bulunan
  arşivi mühürler.
- `Matches(archive)` mevcut snapshot ile güncel iş/veri içeriğini karşılaştırır.

Snapshot kapsamı:
- arşiv ID
- arşiv numarası
- arşiv tarihi
- fatura türü
- fatura kayıtlarının tamamı
- yetkili/imza profili
- HYS Ödeme Emri Belge No
- kapalı durumu
- kapanış tarihi

Snapshot dışında bilinçli bırakılan alanlar:
- `PdfFileName`
- `PdfSha256`

Neden:
- PDF bütünlüğü zaten ayrı `PdfSha256` mekanizmasıyla korunuyor.
- kapatma anında PDF mevcut olmayabilir ve sonradan güvenli biçimde üretilebilir/eşitlenebilir.
- PDF referans/hash güncellemesi iş/veri snapshot mühürünü bozmamalı.

`ArchivedInvoiceList`:
- sona geriye uyumlu opsiyonel `ClosedSnapshotSha256 = ""` alanı eklendi.
- `HasClosedSnapshot` JSON-dışı görünümü eklendi.
- normalize aşamasında snapshot hash uppercase/trim uygulanıyor.
- positional constructor'ın eski parametre sırası bozulmadı.

Validation:
- snapshot varsa geçerli SHA-256 formatı zorunlu.
- snapshot bulunan kayıt açık duruma getirilemez.
- snapshot bulunan kaydın hesaplanan içeriği kayıtlı hash ile eşleşmek zorunda.
- snapshot'sız legacy kapalı kayıtlar geçerli olmaya devam ediyor.

Store seviyesinde değişmezlik:
- `ArchivedInvoiceListDataStore.Save` artık mevcut dosyadaki mühürlü kayıtları ayrıca kontrol ediyor.
- daha önce mühürlenmiş kayıt:
  - listeden çıkarılamaz,
  - snapshot hash'i değiştirilemez,
  - iş/veri içeriği değiştirilemez.
- aday kayıt değiştirildikten sonra yeni bir hash hesaplanmaya çalışılsa bile mevcut diskteki mühür authoritative kabul edildiği için save reddedilir.
- PDF dosya adı veya PDF SHA-256 değişiklikleri snapshot dışında olduğundan güvenli PDF eşitleme akışı çalışmaya devam edebilir.

Yeni HYS kapanışı:
- açık arşiv HYS ile kapatılırken `ClosedArchiveSnapshotService.Seal` uygulanıyor.
- yeni kapalı kayıt ilk save anından itibaren mühürlü oluyor.
- mühürlü kayıt için kapanışı geri alma kod yolu başlamadan engelleniyor.

UI:
- mühürlü kapalı kayıtta bilgi satırına `Mühürlü` ekleniyor.
- kapanış düğmesi `MÜHÜRLÜ` metnine dönüyor.
- düğme devre dışı.
- durum mesajı iş/veri içeriğinin değiştirilemez olduğunu açıklıyor.
- legacy kapalı snapshot'sız kayıtta eski `KAPANIŞI GERİ AL` davranışı geriye uyumluluk için korunuyor.

Tarihsel PDF import:
- yeni import edilen kapalı HYS kayıtları da snapshot ile mühürleniyor.
- özgün PDF'nin vergi kural metadata davranışı önceki Aşama 3.2 kararına göre değişmedi.

Bütünlük/restore:
- `DataDirectoryValidationService` zaten `ArchivedInvoiceListValidationService` kullandığı için snapshot uyuşmazlığı veri bütünlüğü kontrolü ve restore doğrulamasında da hata olarak yakalanacak.
- mevcut PDF SHA-256 kontrolü aynen korunuyor.

Yeni test:
- `ClosedArchiveImmutabilityTests.SealedClosedArchiveCannotBeChangedOrDeleted`
- mühürlü kayıt kaydediliyor.
- yetkili profili değiştirilip **yeni snapshot hash hesaplanmasına rağmen** store değişikliği reddediyor.
- mühürlü kaydı listeden silme denemesi reddediliyor.
- başarısız denemelerden sonra diskteki özgün mühürlü kayıt aynen yükleniyor ve snapshot doğrulanıyor.

Test sayısı:
- önce: **119**
- yeni hedef: **120**

Commitler:
- snapshot servis: `36a0c61e7a6e20149e56ec57a6064b91ca647859`
- model: `033f7ff6bb6b37272b90031d32dd88dc68da7453`
- validation: `091a619dc1077485bda381d9c3f310a2761b3c81`
- store değişmezlik: `d2089e182a8b9bc16146790401284d1a98430acf`
- HYS kapanış mühürü: `21667955afee74a0c964aa2ebbdb2d5fed0c21b4`
- UI mühür durumu: `b4172af9dc29819c5e40dcc12fcc6b1ddf9d9c33`
- tarihsel import mühürü: `17700c5115c56bd84d0bb4d7e5662e5bd89a7fc7`
- değişmezlik testi: `e1f01063c7f2ad525cdff3d57c8741c93d450116`

Statik kontrol:
- snapshot PDF alanlarını içermiyor: geçti
- yeni kapanış mühürleniyor: geçti
- tarihsel import mühürleniyor: geçti
- snapshot validation: geçti
- store değişiklik/silme engeli: geçti
- UI mühürlü durum: geçti
- legacy snapshot'sız kapalı kayıt uyumu: korundu

Sıradaki doğrulama:
1. GitHub Desktop → **Fetch origin → Pull origin**
2. `DERLE_CALISTIR.bat`
   - beklenen: **0 warning / 0 error**
3. Smoke:
   - aktif listeyi arşive al
   - HYS numarasıyla kapat
   - seçili arşivde `Mühürlü` bilgisi görünmeli
   - düğme `MÜHÜRLÜ` olmalı ve devre dışı kalmalı
   - uygulamayı kapat/aç; mühürlü durum korunmalı
   - arşiv PDF görüntüleme/eşitleme çalışmaya devam etmeli
4. `TESTLERI_CALISTIR.bat`
   - beklenen: **120/120**
5. Yeşil olursa Aşama 3 mali denetim/izlenebilirlik paketi kapanış değerlendirmesi yapılacak; ardından Aşama 5 UX işlerine geçilecek.

## 19.09.2026 — Aşama 3.5 kullanıcı geri bildirimi: mühür kilit davranışı düzeltildi

Kullanıcı mühürlemenin çalıştığını ancak mühürlü arşiv seçildiğinde önceki davranıştaki değişiklik/yeniden açma ve silme yeteneğinin kaybolduğunu bildirdi.

Sorun:
- İlk Aşama 3.5 tasarımında `HasClosedSnapshot` UI ve store seviyesinde **kalıcı kilit** olarak yorumlandı.
- `KAPANIŞI GERİ AL` devre dışı bırakıldı.
- store mevcut mühürlü kaydın değiştirilmesini ve listeden silinmesini reddetti.
- Bu, kullanıcının önceki iş akışını gereksiz yere bozdu.

Düzeltilen tasarım:
- snapshot mühür artık **kilit değil, bütünlük doğrulaması**.
- uygulama içinden yetkili değişiklik yapılabilir.
- değişiklik mühürlü kapalı içerikte yapılacaksa uygulama önce kaydı yeniden açar / eski snapshot'ı temizler; tekrar kapatıldığında yeni snapshot üretilir.
- mühür güncellenmeden içerik dosya/nesne seviyesinde değiştirilirse mevcut validation yine hash uyuşmazlığını yakalar.

Yeniden açma:
- mühürlü kapalı kayıt için `KAPANIŞI GERİ AL` tekrar aktif.
- reopen sırasında:
  - `IsClosed = false`
  - `ClosedAt = null`
  - `ClosedSnapshotSha256 = ""`
- mevcut HYS bilgisi önceki davranış gibi korunur.
- kullanıcı HYS bilgisini açık durumda değiştirebilir ve tekrar kapatınca yeni snapshot üretilir.

Silme:
- arşiv detayına `ARŞİVİ SİL` düğmesi eklendi.
- seçili arşiv açık veya kapalı/mühürlü olsa da kullanıcı onayıyla listeden silinebilir.
- store save başarısız olursa kayıt eski indeksine geri konur.
- `ArchiveDeleted` audit olayı eklenmiştir.
- kayıtlı PDF fiziksel dosyası **otomatik silinmez**; veri kaybını önlemek için disk üzerinde korunur.
- silme sonrası dashboard/arşiv görünümü yenilenir.
- açık rapor referansı silinen arşive aitse temizlenir.

Store:
- `ValidateSealedArchiveImmutability` kaldırıldı.
- mühürlü kayıt için “silinemez/değiştirilemez” kalıcı store kilidi kaldırıldı.
- `ArchivedInvoiceListValidationService` içindeki snapshot hash eşleşme kontrolü aynen korunuyor.
- dolayısıyla mühür güncellenmeden yapılan yetkisiz/tutarsız değişiklik save sırasında reddedilir.

UI:
- `Mühürlü` bilgi etiketi korunuyor.
- mühürlü kapalı kayıtta düğme artık tekrar `KAPANIŞI GERİ AL`.
- kapanış düğmesi tekrar etkin.
- yeni `ARŞİVİ SİL` düğmesi seçili kayıtta etkin.
- durum mesajı mühürün bütünlük doğrulaması olduğunu ve reopen sonrası yeniden kapatmada yeni mühür üretileceğini açıklıyor.

Test:
- mevcut `ClosedArchiveImmutabilityTests` içeriği yeni doğru semantiğe çevrildi:
  - mühür güncellenmeden değiştirilmiş içerik reddedilir,
  - uygulama içinden yeniden mühürlenmiş değişiklik kaydedilebilir,
  - arşiv silme store tarafından kabul edilir.
- yeni test metodu eklenmedi; hedef test sayısı **120** olarak kalır.

Commitler:
- audit ArchiveDeleted: `bf1fb6d0d04807900ff0b4ee593995cc17fcc405`
- store kilit düzeltmesi: `a1a73995331f56a280eb76807c2f5cf36ac746da`
- reopen + delete command: `1bc68b02a5d8f3706db42eb667e6b15acb24e306`
- UI davranışı: `f36dc7bfda634b80152cabfe3328302f84916ee5`
- ARŞİVİ SİL düğmesi: `6eb7ac0a2b293a89f1ccea600a6c0edee46b4049`
- test semantiği: `cf354294553b210da42832dca40af8ea57661492`

Statik kontrol:
- store kalıcı mühür kilidi: kaldırıldı
- snapshot hash doğrulaması: korunuyor
- mühürlü reopen: aktif
- reopen snapshot temizliği: mevcut
- yeniden kapatma/reseal: mevcut
- arşiv silme + rollback: mevcut
- silme audit: mevcut
- PDF otomatik silme: **yok**
- test hedefi: **120**

Sıradaki doğrulama:
1. GitHub Desktop → **Fetch origin → Pull origin**
2. `DERLE_CALISTIR.bat`
   - beklenen: **0 warning / 0 error**
3. Smoke:
   - mühürlü kapalı arşiv seç
   - `KAPANIŞI GERİ AL` çalışmalı
   - açık hale geldikten sonra HYS bilgisini değiştirip tekrar kapatabilmelisin
   - tekrar kapatınca `Mühürlü` görünmeli
   - başka bir arşivde `ARŞİVİ SİL` kullan; kayıt listeden kalkmalı
   - PDF varsa fiziksel PDF dosyası korunmalı
4. `TESTLERI_CALISTIR.bat`
   - beklenen: **120/120**
5. Bu revizyon doğrulanmadan Aşama 3 kapanışı yapılmayacak.

## 19.09.2026 — Aşama 3.5 arşiv silme ve tam düzenleme akışı revizyonu — KODLANDI, DOĞRULAMA BEKLİYOR

Kullanıcı iki davranış istedi:
1. Arşiv silmede ayrıca yedek/kopya tutulmasın; yalnız kesin silme onayı gösterilsin ve onaylanırsa kayıt doğrudan silinsin.
2. Arşiv içeriğinin yalnız HYS değil, faturalar dahil tamamı düzenlenebilsin; bunun için seçili arşiv Aktif Fatura Listesi'ne geri alınsın ve normal fatura düzenleme araçları kullanılsın.

### Arşiv silme

Yeni davranış:
- `SİL` düğmesinde yalnız kalıcı silme onayı gösterilir:
  - arşiv kaydı,
  - ona bağlı PDF
  kalıcı olarak silineceği açıkça belirtilir.
- Onay verilmezse hiçbir işlem yapılmaz.
- Onay verilirse:
  1. arşiv kaydı `IslemArsivi.json` içinden kaldırılır,
  2. store save başarılıysa bağlı PDF doğrudan silinir,
  3. ayrıca backup/kopya dosyası oluşturulmaz,
  4. `ArchiveDeleted` audit olayı yazılır.
- Arşiv store save başarısızsa in-memory kayıt eski indeksine geri konur.
- PDF silme ayrı I/O hatası verirse arşiv kaydı silinmiş olarak kalır, teknik hata loglanır ve kullanıcıya PDF'nin silinemediği bildirilir.

`DocumentArchiveStorageService.DeleteStoredPdf`:
- kayıtlı `PdfFileName` ve HYS tabanlı PDF adını kontrol eder,
- yalnız DoğruFatura Aktif Çıktılar / Arşivlenenler klasörlerinde arar,
- bulunan ilgili PDF'yi doğrudan siler,
- yedek dosya üretmez.

### Arşivi düzenlemeye alma

Arşiv detayında üç eylem:
- `AÇ`
- `DÜZENLE`
- `SİL`

`DÜZENLE` davranışı:
- seçili arşivin faturalarını **Aktif Fatura Listesi'ne geri alır**,
- kullanıcı bundan sonra mevcut normal fatura listesinde:
  - kurum,
  - fatura numarası,
  - hizmet numarası,
  - tarihler,
  - tutarlar/KDV,
  - diğer fatura alanları
  dahil kayıtları normal düzenleme akışıyla değiştirebilir.
- Aktif Fatura Listesi doluyken işlem başlamaz; mevcut aktif kayıtlarla arşiv faturaları karıştırılmaz.
- Kullanıcıdan açık onay alınır.
- onay sonrası eski arşiv kaydı kaldırılır.
- eski arşiv PDF'si kaldırılır.
- HYS kapanış bilgisi arşiv kaydıyla birlikte sona erer; kullanıcı düzenleme sonrası listeyi tekrar arşivleyip HYS ile yeniden kapatabilir ve yeni snapshot mühürü oluşur.

### Arşivden aktife transaction journal

Yeni operation:
- `TransactionJournalOperation.RestoreArchiveToActive`

Yeni state'ler:
- `ActiveListRestored`
- `ArchiveRemoved`

Akış:
1. `Started`
2. arşiv faturaları `AktifFaturalar.json` içine yazılır
3. `ActiveListRestored`
4. arşiv kaydı `IslemArsivi.json` içinden kaldırılır
5. `ArchiveRemoved`
6. `Completed`
7. eski PDF silinir
8. `ArchiveRestoredToActive` audit olayı yazılır
9. kullanıcı Fatura Listesi ekranına yönlendirilir

Rollback:
- aktif liste yazımı başarısız → transaction `RolledBack`
- journal ilerleyemez → aktif liste mümkünse boşaltılarak rollback
- archive save başarısız → arşiv kayıt listesine geri eklenir ve aktif liste boşaltılır
- iki rollback birlikte tamamlanamazsa `RecoveryRequired`
- journal kapanışı yazılamazsa kalıcı veri işlemi geri çevrilmez; sonraki açılış kontrolü devreye girer.

PII:
- transaction/audit detail içinde kurum adı, VKN veya fatura no eklenmedi.
- yalnız archive ID, invoice count/type gibi teknik bilgi tutuluyor.

### UI

Arşiv detay alt eylemleri artık:
- `AÇ`
- `DÜZENLE`
- `SİL`

`DÜZENLE` tooltip:
- arşivdeki faturaları Aktif Fatura Listesi'ne geri alarak normal düzenleme araçlarıyla değiştirme davranışını açıklıyor.

### Test

Yeni test:
- `ArchiveStorageIntegrationTests.DeleteStoredPdfRemovesArchiveFileWithoutBackupCopy`
- bağlı PDF'nin gerçekten silindiğini doğrular.
- temp kökünde başka dosya/yedek kopya kalmadığını doğrular.

Test sayısı:
- önce hedef: **120**
- yeni hedef: **121**

Commitler:
- transaction operation/state: `87aa9f6be69233390b1925939fb5478e19ffef32`
- audit restore olayı: `8b70bdf530f4960b09decee0f1788661df0b3ee7`
- doğrudan PDF silme: `21a1e2a319418fc6834ac02233d03aee8d2b5e2c`
- journal helper genelleştirme: `50ef052803ecf24afdfb32feda33313b00c04a48`
- arşiv düzenle + doğrudan sil akışı: `055b71c45679822b0cfa545a5a89128df04bbb99`
- edit düğme state: `be89424cfeff1d978fd7075756cefdc8d4bb693d`
- AÇ/DÜZENLE/SİL UI: `88fd4abc0a05edab2ae2e6859f2f03440bedb5e8`
- PDF silme regresyon testi: `8d6491017fd307acdd62f126c7d9b74e875d8638`

Sıradaki doğrulama:
1. GitHub Desktop → **Fetch origin → Pull origin**
2. `DERLE_CALISTIR.bat`
   - beklenen: **0 warning / 0 error**
3. Silme smoke:
   - bir arşiv seç
   - `SİL`
   - yalnız “emin misiniz / geri alınamaz” onayı görünmeli
   - Evet → kayıt ve bağlı PDF silinmeli
   - yedek/kopya dosyası oluşmamalı
4. Düzenleme smoke:
   - Aktif Fatura Listesi boşken bir arşiv seç
   - `DÜZENLE`
   - onayla
   - arşiv kaydı listeden kalkmalı
   - faturalar Aktif Fatura Listesi'nde görünmeli
   - normal `DÜZENLE` ile fatura içeriği değiştirilebilmeli
   - tekrar arşivleyip HYS ile kapatılabilmeli
5. Aktif listede kayıt varken arşiv `DÜZENLE` denendiğinde işlem başlamamalı.
6. `TESTLERI_CALISTIR.bat`
   - beklenen: **121/121**
7. Bu revizyon doğrulanmadan Aşama 3 kapanışı yapılmayacak.

## 19.09.2026 — Aşama 3 kapanışı BAŞARILI

Kullanıcı son arşiv düzenle/sil revizyonunun çalıştığını bildirdi ve devam edilmesini istedi.

Aşama 3 kapsamında tamamlanan ana güvenlik/izlenebilirlik işleri:
- vergi kural sürümü ve hesap metadata'sı,
- `InvoiceRecord` üzerinde legacy uyumlu vergi kural kalıcılığı,
- append-only audit trail,
- transaction journal,
- kapalı HYS arşivi SHA-256 snapshot bütünlük mührü,
- mühürü kullanıcı iş akışını kilitlemeden yeniden açma/reseal semantiği,
- arşivden Aktif Fatura Listesi'ne güvenli geri alma/düzenleme,
- onaylı arşiv + bağlı PDF kalıcı silme.

Son Aşama 3 test hedefi:
- **121/121**

Aşama 3 bu kullanıcı doğrulamasıyla kapatıldı.

## 19.09.2026 — Aşama 5.1 tarihsel PDF async okuma + ilerleme + iptal — KODLANDI, DOĞRULAMA BEKLİYOR

Amaç:
- çoklu tarihsel PDF okuma/doğrulama sırasında WPF UI thread'ini bloklamamak,
- kullanıcıya dosya bazında ilerleme göstermek,
- veri dosyalarına yazım başlamadan güvenli iptal imkanı vermek,
- mevcut PDF parse/doğrulama ve arşiv kayıt kurallarını değiştirmemek.

`HistoricalPdfImportService.Read`:
- sona opsiyonel `CancellationToken cancellationToken = default` parametresi eklendi.
- mevcut 3 parametreli çağrı semantiği geriye uyumlu.
- iptal kontrolleri:
  - parse başlamadan önce,
  - PDF açıldıktan sonra,
  - her sayfa extraction öncesi,
  - her sayfanın fatura satırları işlenirken,
  - sıra numarası doğrulaması öncesi
  çalışıyor.
- `OperationCanceledException` normal “hatalı PDF” sonucuna çevrilmiyor; üst UI katmanına yeniden fırlatılıyor.
- PDF iş kuralları, HYS parse, tutar/vergi doğrulaması ve mühürleme davranışı değişmedi.

UI:
- `ARŞİV EKLE` düğmesi artık `ArchiveImportButton` adıyla yönetiliyor.
- yalnız okuma/doğrulama sürerken görünen `İPTAL` düğmesi eklendi.
- import preview işlemi `async void` WPF event üzerinden yürütülüyor.
- ağır `HistoricalPdfImportService.Read` işi `Task.Run` ile UI thread dışına taşındı.
- ilerleme durum alanında dosya bazında gösteriliyor:
  - `1/N PDF okunuyor ve doğrulanıyor: ...`
  - `1/N PDF doğrulandı.`
- işlem sürerken:
  - ARŞİV EKLE devre dışı,
  - PDF KONTROL devre dışı,
  - İPTAL görünür/etkin.
- iptal tıklanınca düğme tekrar tıklanamayacak şekilde devre dışı kalır ve cancellation token tetiklenir.

İptal güvenliği:
- iptal yalnız preview/okuma/doğrulama aşamasında kullanılabilir.
- bu aşamada henüz:
  - `ArchivedLists` içine kayıt eklenmez,
  - PDF Arşivlenenler klasörüne kopyalanmaz,
  - `IslemArsivi.json` kaydedilmez.
- iptal sonucu:
  - `PDF okuma/doğrulama işlemi iptal edildi. Arşiv verilerinde değişiklik yapılmadı.`
  mesajı gösterilir.
- mevcut önizleme onayı sonrasındaki atomik kopyalama/save/rollback akışı değiştirilmedi.

Statik kontrolde yakalanıp doğrulama öncesi düzeltilen hata:
- sayfa buffer tipi ilk değişiklikte yanlışlıkla `ExtractedPage` yazılmıştı.
- gerçek iç model `PdfPageData`; doğrulama kapısı öncesi düzeltildi.

Yeni test:
- `HistoricalPdfImportCancellationTests.ReadThrowsWhenCancellationWasRequestedBeforeParsing`
- önceden iptal edilmiş token ile `Read` çağrısının `OperationCanceledException` ürettiğini doğruluyor.
- böylece iptalin “hatalı PDF preview” olarak yutulmadığı sabitleniyor.

Test sayısı:
- önce: **121**
- yeni hedef: **122**

Commitler:
- service cancellation: `107d44243f703e546126c584f5c3b8d82420b89c`
- UI İPTAL düğmesi: `889f3ecfa13161ed7b7a5427ed95723c3b693b8f`
- async/progress/cancel WPF akışı: `0176f8af05ed7569c01b7797acda910f0ef19f04`
- PdfPageData compile fix: `1c647a640ff131edd83e0d197bbfb1d332bbe67d`
- cancellation regression testi: `923d07d34896966330c113c7316f1381a4eb4a92`

Statik kontrol:
- optional cancellation token: mevcut
- OperationCanceledException swallow edilmiyor: geçti
- sayfa/satır cancellation noktaları: mevcut
- UI thread dışı parse: mevcut
- dosya bazlı progress: mevcut
- cancel UI: mevcut
- iptal öncesi kalıcı veri yazımı: **yok**
- eski import commit/rollback akışı: korundu

Sıradaki doğrulama:
1. GitHub Desktop → **Fetch origin → Pull origin**
2. `DERLE_CALISTIR.bat`
   - beklenen: **0 warning / 0 error**
3. Smoke:
   - birden fazla tarihsel PDF seç
   - okuma sırasında UI cevap vermeye devam etmeli
   - durum alanında `1/N, 2/N...` ilerleme görülmeli
   - ikinci denemede okuma sırasında `İPTAL` tıkla
   - işlem iptal edilmeli ve arşiv listesine/PDF klasörüne yeni kayıt yazılmamalı
   - normal denemede önizleme → onay → import önceki gibi tamamlanmalı
4. `TESTLERI_CALISTIR.bat`
   - beklenen: **122/122**
5. Yeşil olursa Aşama 5.2 için **boş fatura numarası mükerrer uyarısı** ele alınacak.

## 19.09.2026 — Aşama 5.1 gerçek PDF smoke SONRAYA BIRAKILDI

Kullanıcının şu anda uygun deneme arşiv/PDF seti bulunmadığı için:
- çoklu tarihsel PDF async ilerleme,
- okuma sırasında İPTAL,
- normal tarihsel PDF import smoke

manuel doğrulaması sonraya bırakıldı.

Kod ve statik kontroller korunuyor; Aşama 5.1 tamamen kapanmış sayılmayacak, ancak uygun test verisi gelene kadar UX çalışmalarını engellemeyecek.

## 19.09.2026 — Aşama 5 UI ölçü revizyonu — KODLANDI, DOĞRULAMA BEKLİYOR

Kullanıcı geri bildirimi:
1. `PDF KONTROL` düğmesinde yazı yatayda kesiliyordu.
2. Arşiv detayındaki `AÇ / DÜZENLE / SİL` düğmeleri eşit genişlikte değildi.
3. Form hazırlık göstergesi `%100` olduğunda yazı çember sınırına taşıyordu.

Düzeltmeler:
- `ArchivePdfSyncButton` genişliği `112 -> 132`.
- arşiv alt eylem grid'i üç eşit `*` kolona çevrildi:
  - `AÇ`
  - `DÜZENLE`
  - `SİL`
- aradaki 8 px boşluklar korundu.
- form hazırlık çember geometrisine dokunulmadı.
- `HeaderReadinessText` fontu `15 -> 12.5` düşürüldü; `%100` metni 48x48 çember içine güvenli sığacak şekilde küçültüldü.

Commit:
- `a9778c9d1891cca4c318fecacb69c1bfe2386e23`

Statik kontrol:
- PDF KONTROL width 132: geçti
- AÇ/DÜZENLE/SİL eşit yıldız kolonlar: geçti
- form hazırlık font 12.5: geçti

Doğrulama:
1. Fetch origin → Pull origin
2. `DERLE_CALISTIR.bat`
3. Görsel smoke:
   - PDF KONTROL yazısı tam görünmeli
   - AÇ / DÜZENLE / SİL yatayda eşit genişlikte görünmeli
   - Form Hazırlık `%100` metni çember dışına taşmamalı
4. `TESTLERI_CALISTIR.bat`
   - kod tarafındaki son hedef **122/122**
   - gerçek PDF import smoke'u uygun test dosyası bulunduğunda ayrıca yapılacak

## 19.09.2026 — Aşama 5.2 Fatura Listesi arşivleme + İstatistik ekranı + UI ölçü rev2 — KODLANDI, DOĞRULAMA BEKLİYOR

Kullanıcı talepleri:
1. `PDF KONTROL` yazısı hâlâ kesildiği için düğmenin biraz daha genişletilmesi.
2. Fatura Listesi ekranından doğrudan arşiv kaydı oluşturulması ve başarılı arşivleme sonrası aktif listenin kapanması.
3. Kontrol Merkezi ile Hakkında / Güvenlik arasına yeni **İSTATİSTİK** bölümü eklenmesi.
4. İstatistiklerde örneğin “A Kurumu / 2025 / Elektrik” toplamı ve genel toplamların görülebilmesi.

### PDF KONTROL rev2

- `ArchivePdfSyncButton.Width`:
  - önce: `132`
  - yeni: `160`
- amaç: mevcut ModernButton padding/font yapısında `PDF KONTROL` metninin yatayda kesilmemesi.

### Fatura Listesi → doğrudan ARŞİVLE

Aktif Fatura Listesi araç çubuğuna:
- `ArchiveFromListButton`
- metin: `ARŞİVLE`
- yeşil aksiyon rengi
eklendi.

Davranış:
- aktif liste boşsa düğme disabled.
- kayıt varsa enabled.
- tıklama mevcut güvenli `TryArchiveActiveList(true)` akışını kullanır.
- bu nedenle mevcut:
  - doğrulama,
  - kullanıcı onayı,
  - transaction journal,
  - archive store save,
  - aktif liste kalıcı temizliği,
  - audit trail,
  - PDF eşitleme
  semantiği aynen korunur.
- başarılı arşivleme sonrası `ActiveInvoices.Clear()` mevcut akıştan çalışır; yani aktif liste kapanır/boşalır.
- Fatura Listesi status alanında başarılı arşivleme mesajı gösterilir.

### Yeni İSTATİSTİK bölümü

Sol menü sırası:
- KONTROL MERKEZİ
- **İSTATİSTİK**
- HAKKINDA / GÜVENLİK

Yeni görünüm:
- `StatisticsView`
- navigasyon: `NavStatistics`
- ekran açıldığında `RefreshStatistics()` çalışır.

Veri kaynağı:
- PDF dosyasını tekrar parse etmek yerine PDF'yi oluşturan/temsil eden yapılandırılmış `ArchivedInvoiceList -> InvoiceRecord` verileri kullanılır.
- yalnız `IsStatisticsEligible` olan kayıtlar:
  - HYS ile kapalı,
  - istatistiğe dahil olmaya uygun
  arşivler mali istatistiğe katılır.
- bu yaklaşım PDF metni parse etmeye göre daha hızlı ve deterministiktir.
- fiziksel PDF'nin geçici eksik olması mali arşiv kaydını istatistikten yanlışlıkla düşürmez.

Yeni Core modeller:
- `ArchiveStatisticsFilter`
  - `Year`
  - `InstitutionName`
  - `InvoiceType`
- `ArchiveStatisticsRow`
  - kurum
  - yıl
  - fatura türü
  - fatura sayısı
  - fatura toplamı
  - KDV toplamı
  - damga toplamı
  - ödeme toplamı
- `ArchiveStatisticsResult`
  - arşiv sayısı
  - fatura sayısı
  - kurum sayısı
  - genel mali toplamlar
  - mevcut yıl/kurum/tür seçenekleri
  - kırılım satırları

Yeni servis:
- `ArchiveStatisticsService.Build(...)`

Filtreler:
- **Yıl**
- **Kurum**
- **Fatura Türü**
- her biri `Tümü` seçeneğine sahip.
- `TEMİZLE` tüm filtreleri sıfırlar.
- `YENİLE` arşiv verisini yeniden hesaplar.

Örnek kullanım:
- Yıl = `2025`
- Kurum = `A Kurumu`
- Fatura Türü = `Elektrik`

sonucunda üst kartlar ve tablo yalnız bu seçime ait:
- fatura sayısı,
- fatura toplamı,
- KDV,
- damga,
- ödeme toplamını
gösterir.

Genel görünüm:
- filtreler `Tümü` olduğunda tüm uygun kapalı arşivlerin genel toplamı gösterilir.

Özet kartları:
- ARŞİV
- FATURA
- KURUM
- TOPLAM ÖDEME

Ek toplam satırı:
- Fatura toplamı
- KDV
- Damga
- Ödeme

Detay tablosu:
- Kurum
- Yıl
- Tür
- Adet
- Fatura Tutarı
- KDV
- Damga
- Ödeme

Legacy / mükerrer güvenliği:
- aynı non-empty `Invoice.Id` birden fazla uygun arşivde görülürse bir kez sayılır.
- legacy `Guid.Empty` fatura kimlikleri birbirine yanlışlıkla tek kayıt olarak birleştirilmez.

### Sadeleştirme kararı

Bu ilk istatistik paketinde yeni chart/graph kütüphanesi eklenmedi.
Neden:
- gereksiz bağımlılık oluşturmamak,
- mali hesapları önce test edilebilir Core servisinde sabitlemek,
- grafik ihtiyacını daha sonra aynı sonuç modeli üzerinden eklemek.

### Yeni test

- `ArchiveStatisticsServiceTests.BuildFiltersByInstitutionYearAndInvoiceType`
- doğrulanan örnek:
  - A Kurumu
  - 2025
  - Elektrik
- fatura sayısı ve Payable/KDV/Damga/Payment toplamları kontrol ediliyor.
- filtre seçeneklerinin de üretildiği doğrulanıyor.

Test sayısı:
- önce hedef: **122**
- yeni hedef: **123**

Commitler:
- istatistik modelleri: `53f8a18a6f282d40a203d8fcc83b7e58a731c2e0`
- istatistik servis: `c7aeb54fc1c576b2ba3bb9f5fb65694c1f10a0bb`
- istatistik test: `eb876b224f770b43e443982c61edc439826f259e`
- legacy empty ID düzeltmesi: `0253ed6df501b72ac879983569dbf34116d41943`
- XAML / PDF width / ARŞİVLE / İSTATİSTİK görünümü: `35fd31b924be0ee3d1d37077c3953bcfa1edc968`
- statistics code-behind: `b3eb2881c2bbea15fd5310d62ef8af718fb5e872`
- statistics navigation: `5ef0ed6425c4e42a5c28027309991cac97741b57`
- Fatura Listesi ARŞİVLE state: `30bfa290feef088dd6f71f2566b92ce99b75e1bf`
- Fatura Listesi ARŞİVLE handler: `f882fe14d506b9e61fc8df1571a5ebbd730b1762`

Statik kontrol:
- PDF KONTROL width 160: geçti
- İSTATİSTİK menüsü Control/About arasında: geçti
- StatisticsView XAML/code-behind isimleri: geçti
- filtre event bağlantıları: geçti
- Fatura Listesi ARŞİVLE → mevcut güvenli arşiv akışı: geçti
- ARŞİVLE aktif liste boşken disabled: geçti
- istatistik yalnız kapalı/HYS uygun arşivlerden: geçti
- legacy empty Invoice.Id davranışı: geçti

### İstatistik için sonraki öneriler

İlk sürüm doğrulandıktan sonra en faydalı genişletmeler:
1. **Aylık trend**: seçili kurum/yıl/türde Ocak–Aralık ödeme grafiği.
2. **Yıllar arası karşılaştırma**: ör. A Kurumu Elektrik 2024 vs 2025 toplam ve yüzde fark.
3. **Kurum sıralaması**: seçili yılda en yüksek ödeme toplamına sahip kurumlar.
4. **Fatura türü dağılımı**: Elektrik / Su / Telefon / İnternet oranları.
5. **Ortalama fatura**: kurum/tür bazında adet başına ortalama ödeme.
6. **CSV/Excel dışa aktarım**: filtrelenmiş tabloyu analiz için dışarı verme.
7. **Anomali görünümü**: kurumun kendi geçmişine göre olağandışı yüksek aylık tutarları sadece işaretleme; otomatik karar/vergi yorumu yapmadan.
8. **PDF kapsam göstergesi**: mali kayıt var ama fiziksel PDF eksik olan arşivleri ayrı sayaçla göstermek.

### Zorunlu doğrulama

1. GitHub Desktop → **Fetch origin → Pull origin**
2. `DERLE_CALISTIR.bat`
   - beklenen: **0 warning / 0 error**
3. Görsel/işlev smoke:
   - `PDF KONTROL` metni tam görünmeli.
   - Fatura Listesi'nde `ARŞİVLE` görünmeli.
   - aktif liste doluyken `ARŞİVLE` → mevcut onay → arşiv kaydı oluşmalı → aktif liste boşalmalı.
   - sol menüde KONTROL MERKEZİ ile HAKKINDA / GÜVENLİK arasında `İSTATİSTİK` görünmeli.
   - İSTATİSTİK açılmalı.
   - Yıl / Kurum / Fatura Türü filtreleri çalışmalı.
   - filtre `Tümü` iken genel toplamlar görünmeli.
   - örnek kurum + yıl + tür seçildiğinde tablo/özet yalnız seçili veriyi göstermeli.
4. `TESTLERI_CALISTIR.bat`
   - beklenen: **123/123**
5. Aşama 5.1 gerçek tarihsel PDF smoke, uygun test PDF'leri bulunduğunda ayrıca yapılacak.

## 19.09.2026 — Aşama 5.2 ilk build: 6 warning, düzeltildi

Kullanıcının build sonucu:
- Core: başarılı, **2 warning**
- WPF: başarılı, **4 warning**
- toplam: **6 warning / 0 error**
- uygulama başlatıldı.

Warningler:
- `ArchiveStatisticsService.cs`
  - 2 x CA1309
- `MainWindow.Statistics.cs`
  - 1 x CA1305
  - 3 x CA1309

Neden:
- istatistik filtre/sıralama karşılaştırmalarında `CurrentCultureIgnoreCase`,
- yıl string dönüşümünde açık kültür kullanılmaması.

Düzeltme:
- istatistik servisindeki comparer/comparison kullanımları `OrdinalIgnoreCase` yapıldı.
- UI filtre karşılaştırmaları `OrdinalIgnoreCase` yapıldı.
- yıl seçenekleri `CultureInfo.InvariantCulture` ile string'e çevriliyor.
- yıl parse işlemi `NumberStyles.Integer + CultureInfo.InvariantCulture` ile açık hale getirildi.

Commitler:
- Core warning fix: `6f1662e15093a8fea07734b545b135737561baab`
- WPF warning fix: `f998c7615acb9ba3c4bbcdc86cf263b2a4838542`

Sıradaki kapı:
1. Fetch origin → Pull origin
2. `DERLE_CALISTIR.bat`
   - beklenen: **0 warning / 0 error**
3. İstatistik / Fatura Listesi ARŞİVLE görsel-işlev smoke
4. `TESTLERI_CALISTIR.bat`
   - beklenen: **123/123**

Aşama 5.2 bu kapılar geçmeden tamamlanmış sayılmayacak.

## 19.09.2026 — Aşama 5.2 build kapısı YEŞİL + yerel WPF donut görselleştirme KODLANDI

Kullanıcının warning fix sonrası build sonucu:
- Core: başarılı
- WPF: başarılı
- toplam: **0 warning / 0 error**
- uygulama başarıyla başlatıldı.

Test kapısı henüz ayrıca raporlanmadı; mevcut hedef **123/123**.

### İstatistik görselleştirme kararı

Kullanıcı:
- Top 5 kurum yatay çubuk grafiğini istemedi.
- harici grafik kütüphanesi eklenmemesini özellikle tercih etti.
- diğer görsel önerilerden Fatura Türü Dağılımı donut grafiğinin uygulanmasını istedi.

Karar:
- **harici chart/NuGet kütüphanesi eklenmedi**.
- donut grafik yalnız WPF'nin kendi:
  - `Canvas`
  - `Path`
  - `PathGeometry`
  - `ArcSegment`
  - `Ellipse`
  bileşenleriyle çiziliyor.

### Donut grafik

Yeni alan:
- başlık: `FATURA TÜRÜ DAĞILIMI`
- açıklama: seçili filtrelere göre ödeme tutarının fatura türlerine dağılımı.
- merkez:
  - `TOPLAM ÖDEME`
  - filtrelenmiş toplam ödeme tutarı.
- legend:
  - fatura türü
  - fatura adedi
  - ödeme tutarı
  - yüzde payı.

Türler için sabit görsel eşleme:
- Elektrik
- Su
- Telefon
- İnternet
- diğer türler için güvenli fallback.

Filtre entegrasyonu:
- Yıl
- Kurum
- Fatura Türü

filtreleri değiştiğinde:
- özet kartlar,
- toplam satırı,
- donut,
- legend,
- detay tablosu

aynı `ArchiveStatisticsResult` üzerinden birlikte yenilenir.

Tek tür filtrelenirse:
- halka %100 tek tür olarak çizilir.

Veri yoksa:
- donut boş kalır,
- `Dağılım verisi yok` durumu gösterilir.

### Yerleşim düzeltmesi

İlk donut XAML commitinde generic satır deseni yanlışlıkla `ReportsView` içine fazladan bir `Auto` satır eklemişti.
Bu durum build kapısından önce statik commit farkı kontrolünde yakalandı ve düzeltildi:
- `ReportsView` tekrar özgün **4 satır** düzenine döndürüldü.
- `StatisticsView` doğru şekilde **5 satır** düzenine geçirildi.
- donut `Grid.Row=3`
- detay tablo `Grid.Row=4`.

### Analyzer/format güvenliği

- yüzde gösterimi açık `tr-TR` kültürüyle formatlanıyor.
- tek dilim 360° olduğunda `ArcSegment` yerine doğrudan `Ellipse` halka çiziliyor.
- harici chart package referansı yok.

Commitler:
- donut XAML: `e545e3d090c076fc508a18f7f1821b75d6289f82`
- yanlış satır konumu düzeltmesi: `8f8a9e493851786b7ba64b37458ffd9a4cc8f7dc`
- WPF donut çizim/filter entegrasyonu: `a92080bcfb9a50ad305f327c8a44a37d9d08dc70`
- tam halka + yüzde format düzeltmesi: `065965fb292bf8ce1abde4927471f734a8af8744`

Test sayısı:
- yeni test metodu eklenmedi.
- hedef **123/123** olarak kalır.

Sıradaki doğrulama:
1. GitHub Desktop → **Fetch origin → Pull origin**
2. `DERLE_CALISTIR.bat`
   - beklenen: **0 warning / 0 error**
3. İSTATİSTİK görsel smoke:
   - donut görünmeli,
   - merkez toplam ödeme göstermeli,
   - legend tür + adet + tutar + yüzde göstermeli,
   - Yıl/Kurum/Tür filtreleri donut ve tabloyu birlikte değiştirmeli,
   - tek tür seçildiğinde halka %100 tek renk olmalı,
   - veri olmayan filtrede boş durum görünmeli.
4. `TESTLERI_CALISTIR.bat`
   - beklenen: **123/123**
5. Aylık trend grafiği bu paket doğrulandıktan sonra ayrı küçük dilim olarak değerlendirilebilir.

## 19.09.2026 — Aşama 5.2 donut görsel kalite revizyonu — LiveCharts2

Kullanıcının yerel build sonucu:
- Core: başarılı
- WPF: başarılı, **1 warning**
- toplam: **1 warning / 0 error**
- warning:
  - `MainWindow.Statistics.cs(256,26) CA1859`
  - `StatisticsBrush` dönüş tipinin `Brush` yerine `SolidColorBrush` olması önerildi.

Kullanıcı ayrıca:
- donut grafiğinin daha büyük olmasını,
- grafik üzerindeki yazıların daha okunaklı olmasını,
- harici grafik kütüphanesi görsel kaliteyi artıracaksa kullanılmasını
istedi.

### Grafik kütüphanesi kararı

Önceki “harici chart kütüphanesi ekleme” tercihi kullanıcı tarafından geri çekildi.

Yeni bağımlılık:
- `LiveChartsCore.SkiaSharpView.WPF`
- sürüm: `2.0.5`

Amaç:
- profesyonel WPF pie/donut render,
- daha iyi ölçekleme,
- daha okunaklı data label,
- hover/tooltip,
- elle `Canvas/Path/ArcSegment` geometri bakımını kaldırmak.

### Donut revizyonu

Eski:
- WPF `Canvas`
- `PathGeometry`
- `ArcSegment`
- 220x220 manuel donut

Yeni:
- `LiveChartsCore.SkiaSharpView.WPF.PieChart`
- boyut: **300x300**
- `InitialRotation=-90`
- built-in legend kapalı; mevcut detay legend korunuyor.
- `PieSeries<double>`
- `InnerRadius = 92`
- `HoverPushout = 8`
- dilimler arasında beyaz stroke.
- data label:
  - beyaz,
  - 16 px,
  - dilim ortasında,
  - yüzde olarak.
- %6'dan küçük dilimlerde okunaksızlığı önlemek için dilim üzerindeki yüzde gizlenir; kesin oran/tutar legend ve tooltip'te görünür.
- tooltip:
  - fatura türü,
  - ödeme tutarı,
  - yüzde.

Merkez:
- iç panel 136x136,
- `TOPLAM ÖDEME`,
- toplam tutar fontu **20 px**.

Legend okunaklılık artışı:
- tür adı 15 px,
- tutar 15 px bold,
- yüzde 13 px semibold,
- işaret 13x13.

### CA1859 fix

`StatisticsBrush`:
- önce: `Brush`
- yeni: `SolidColorBrush`

Böylece bildirilen CA1859 kaynağı kaldırıldı.

### Eski manuel donut temizliği

Kaldırıldı:
- `StatisticsDonutCanvas`
- `StatisticsDonutPoint`
- `ArcSegment`
- `PathGeometry`
- manuel açı/sweep hesapları.

Core istatistik hesabı ve filtre semantiği değişmedi.

### LiveCharts API düzeltmesi

Statik doküman/API kontrolünde tooltip property adı doğrulandı:
- doğru: `ToolTipLabelFormatter`
- yanlış ilk yazım doğrulama öncesi düzeltildi.

### Commitler

- LiveCharts2 package: `3bdd44078715928beae5a71b02484bc8c2c69656`
- 300x300 LiveCharts XAML: `57925c18bf608f6a2871bfd5700893a644bce5ce`
- LiveCharts PieSeries / labels / legend entegrasyonu: `e4be60af506bb72bf2c8ea437ba73065fff3b2b1`
- tooltip property fix: `69c5ac674fe52e029998053bd22bf3493b7c1b9a`

Test sayısı:
- yeni test metodu yok.
- hedef **123/123**.

### Sıradaki doğrulama

1. GitHub Desktop → **Fetch origin → Pull origin**
2. `DERLE_CALISTIR.bat`
   - NuGet restore LiveCharts2 bağımlılıklarını indirebilir.
   - beklenen: **0 warning / 0 error**
3. İSTATİSTİK görsel smoke:
   - donut önceki halden belirgin büyük olmalı,
   - yüzde yazıları net okunmalı,
   - merkez toplam ödeme rahat okunmalı,
   - hover tooltip çalışmalı,
   - legend tür/tutar/yüzde okunaklı olmalı,
   - Yıl/Kurum/Tür filtreleri grafiği ve tabloyu birlikte değiştirmeli.
4. `TESTLERI_CALISTIR.bat`
   - beklenen: **123/123**

## 19.09.2026 — Aşama 5.2 İstatistik görsel sadeleştirme revizyonu — KODLANDI, DOĞRULAMA BEKLİYOR

Kullanıcı LiveCharts2 donut görünümünü başarılı buldu ve iki sadeleştirme istedi:
1. grafiğin bulunduğu beyaz alanların köşeleri yuvarlatılsın,
2. İstatistik sayfasının en altındaki detay döküm tablosu kaldırılıp kazanılan alan donut ve sağ dağılım listesine verilsin.

### XAML sadeleştirmesi

`StatisticsView`:
- 5 ana satırdan **4 ana satıra** indirildi.
- kaldırılan alt detay tablo satırı artık yok.
- yeni yapı:
  1. başlık
  2. özet kartları
  3. filtreler/toplam satırı
  4. büyük donut + dağılım detay alanı

Kaldırıldı:
- `StatisticsGrid`
- `StatisticsEmptyState`
- kurum/yıl/tür kırılımını gösteren alt `DataGrid`.

Core istatistik servisi ve `ArchiveStatisticsResult.Rows` korunuyor:
- donut dağılımı hâlâ aynı hesap sonucunu kullanıyor,
- mali hesaplama mantığı değişmedi,
- ileride CSV/Excel veya başka detay görünümü eklemek için kırılım verisi kaybolmadı.

### Yuvarlatılmış kartlar

Ana görsel alan:
- açık arka plan,
- `CornerRadius=20`,
- `ClipToBounds=True`.

Sol donut kartı:
- beyaz,
- `CornerRadius=20`,
- ayrı border,
- clip açık.

Sağ dağılım detay kartı:
- beyaz,
- `CornerRadius=20`,
- ayrı border,
- clip açık.

Donut merkez paneli:
- `176x176`,
- `CornerRadius=88`,
- tam daire beyaz merkez.

### Kazanılan alanın kullanımı

Donut:
- önce: **300x300**
- yeni: **420x420**

Sol kolon:
- **520 px**

LiveCharts halka:
- `InnerRadius: 92 -> 128`
- `HoverPushout: 8 -> 10`
- `DataLabelsSize: 16 -> 18`

Merkez toplam:
- `20 px -> 23 px`

Sağ dağılım listesi:
- yeni başlık: `DAĞILIM DETAYI`
- açıklama eklendi.
- kart padding artırıldı.
- legend satır aralığı `12 -> 16`.
- renk işareti `13x13 -> 15x15`.
- tür adı `15 -> 16 px`.
- fatura adedi `13 -> 13.5 px`.
- tutar `15 -> 16 px`.
- yüzde `13 -> 13.5 px`.

### Code-behind temizliği

Alt tablo kaldırıldığı için:
- `StatisticsGrid.ItemsSource = ...`
- `StatisticsEmptyState.Visibility = ...`

kodları kaldırıldı.

Artık İstatistik ekranının görsel çıktısı:
- özet kartları,
- filtreler,
- toplam satırı,
- büyük donut,
- sağ dağılım detayı

üzerinden çalışıyor.

### Doğrulama sırasında yakalanan XAML kapanış problemi

Alt tablo blok olarak çıkarılırken `StatisticsView` kapanış `</Grid>` etiketi de ilk committe düşmüştü.
Build kapısından önce statik kontrolle yakalandı ve ayrı fix commitinde düzeltildi.

Son statik kontrol:
- StatisticsView 4 satır: geçti
- alt DataGrid tamamen kaldırıldı: geçti
- code-behind tablo referansı yok: geçti
- donut 420x420: geçti
- üç ana beyaz/açık panel yuvarlatılmış: geçti
- StatisticsView About görünümünden önce düzgün kapanıyor: geçti

### Commitler

- tablo kaldırma + büyük yuvarlatılmış yerleşim: `6569af4b67c12040ce61269fc01fa9cc0897622c`
- StatisticsView kapanış fix: `f82d69a3e7d0b364a4bc79981ee00323e921bf6e`
- donut/legend ölçü büyütme + tablo code-behind temizliği: `0614920f2d071f6a55dfe966c0277a43cb434f6a`

Test sayısı:
- yeni test eklenmedi.
- hedef **123/123**.

### Sıradaki doğrulama

1. GitHub Desktop → **Fetch origin → Pull origin**
2. `DERLE_CALISTIR.bat`
   - beklenen: **0 warning / 0 error**
3. İSTATİSTİK görsel smoke:
   - beyaz kart köşeleri yuvarlak görünmeli,
   - alt detay tablosu artık görünmemeli,
   - donut belirgin biçimde daha büyük olmalı,
   - sağ dağılım listesi daha geniş ve okunaklı olmalı,
   - merkez toplam ödeme taşmamalı,
   - filtreler donut + legend + üst toplamları birlikte değiştirmeli.
4. `TESTLERI_CALISTIR.bat`
   - beklenen: **123/123**

## 19.09.2026 — Aşama 5.2 donut üst/alt kırpılma düzeltmesi — KODLANDI, DOĞRULAMA BEKLİYOR

Kullanıcı görsel smoke sırasında:
- LiveCharts2 donut tasarımını başarılı buldu,
- grafiğin üst ve alt dış kenarlarının hafif kırpıldığını bildirdi.

Neden:
- 420x420 PieChart kullanılabilir maksimum dış yarıçapa kadar çiziliyordu.
- beyaz stroke ve hover pushout, üst/alt kontrol sınırına fazla yaklaşıyordu.

Düzeltme:
- grafik kontrol boyutu **420x420 olarak korundu**.
- `PieSeries<double>.OuterRadiusOffset = 18` eklendi.
- `HoverPushout = 10 -> 8`.
- `InnerRadius = 128` korundu.
- `DataLabelsSize = 18` korundu.

Sonuç hedefi:
- donut algılanan büyük boyutunu korur,
- normal durumda dış halka üst/alt kırpılmaz,
- hover sırasında da dilim için güvenli kenar payı kalır.

Commit:
- `c8c4f3111ca0ca4c03e30666e368c4bd1c1723a4`

Statik kontrol:
- OuterRadiusOffset 18: geçti
- HoverPushout 8: geçti
- InnerRadius 128: korundu
- DataLabelsSize 18: korundu
- test hedefi değişmedi: **123/123**

Doğrulama:
1. Fetch origin → Pull origin
2. `DERLE_CALISTIR.bat`
   - beklenen: **0 warning / 0 error**
3. İstatistik:
   - donut üst ve alt kenarı artık kesilmemeli,
   - yüzde etiketleri görünür kalmalı,
   - hover sırasında dilim kart dışına taşmamalı.
4. `TESTLERI_CALISTIR.bat`
   - beklenen: **123/123**

### İstatistik çıktı/rapor önerisi

Kullanıcı istatistiklerden çıktı alma ihtimalini belirtti.

Önerilen sonraki ayrı paket:
**Aşama 5.3 — İstatistik Raporu**

Önerilen ilk kapsam:
- İstatistik ekranına `PDF RAPORU` ve `YAZDIR` aksiyonları.
- çıktı başlığında rapor tarihi ve aktif filtreler:
  - yıl,
  - kurum,
  - fatura türü.
- özet:
  - arşiv sayısı,
  - fatura sayısı,
  - kurum sayısı,
  - toplam fatura,
  - KDV,
  - damga,
  - toplam ödeme.
- mevcut donut görselinin rapora alınması.
- tür dağılımı:
  - tür,
  - adet,
  - ödeme,
  - yüzde.
- rapor yalnız hesaplanmış istatistik verisinden üretilmeli; PDF'ler tekrar parse edilmemeli.
- mevcut rapor/PDF altyapısı mümkün olduğunca yeniden kullanılmalı; ayrı ağır rapor bağımlılığı eklenmemeli.

İkinci aşama için aday:
- `CSV DIŞA AKTAR`:
  - ham/filtrelenmiş mali veriyi Excel'de açılabilir biçimde dışa vermek.
- doğrudan XLSX ancak gerçek ihtiyaç oluşursa eklenmeli; ilk aşamada CSV daha sade ve bağımlılıksızdır.

## 19.09.2026 — Aşama 5.3 kapsamlı İstatistik genişletmesi — KODLANDI, DOĞRULAMA BEKLİYOR

Kullanıcı önceki öneri listesinden:
- **6 — Yıllar Arası Karşılaştırma**: istemedi.
- **8 — En Yüksek / En Düşük Fatura**: istemedi.
- diğer tüm maddelerin uygulanmasını istedi.

Bu paket 6 ve 8 hariç aşağıdaki özellikleri içerir.

### 1 — İstatistik PDF Raporu

İstatistik ekranına `PDF RAPORU` düğmesi eklendi.

PDF:
- A4 yatay,
- rapor tarihi,
- aktif filtre açıklaması,
- arşiv/fatura/kurum sayıları,
- toplam fatura,
- KDV,
- damga,
- toplam ödeme,
- ortalama fatura,
- donut grafik,
- tür bazlı adet/tutar/yüzde,
- PDF sağlık bilgisi,
- önceki dönem özeti,
- kurum profili,
- ikinci sayfada aylık trend ve tür karşılaştırma grafikleri
içerir.

Kaynak:
- yeniden PDF parse edilmez,
- mevcut `ArchiveStatisticsResult` + ekrandaki LiveCharts görselleri kullanılır.

### 2 — Yazdır

`YAZDIR` düğmesi eklendi.

- WPF `PrintDialog`
- `FlowDocument`
- başlık + filtre + özet + donut kartı + tür dağılım tablosu
kullanılır.

### 3 — CSV dışa aktar

`CSV` düğmesi eklendi.

CSV:
- UTF-8 BOM,
- `sep=;`,
- aktif filtre ve özet,
- kurum/yıl/tür/adet/tutar/KDV/damga/ödeme detayları.

CSV formül enjeksiyon koruması:
- hücre metni `=`, `+`, `-`, `@` ile başlıyorsa başına apostrof eklenir.

### 4 — Excel .xlsx dışa aktar

Yeni NuGet:
- `ClosedXML 0.105.1`

`EXCEL` düğmesi eklendi.

Workbook:
- `Özet` sayfası,
- `Detay` sayfası,
- `Aylık Trend` sayfası.

Detay sayfası:
- başlık satırı,
- AutoFilter,
- frozen header,
- sayı formatları,
- otomatik kolon genişliği.

### 5 — Aylık Ödeme Trend Grafiği

Core:
- yeni `ArchiveStatisticsMonthlyRow`.
- aylık:
  - yıl,
  - ay,
  - fatura sayısı,
  - fatura toplamı,
  - ödeme toplamı.

UI:
- LiveCharts2 `LineSeries<double>`.
- ay/yıl etiketleri.
- seçili Yıl/Kurum/Tür/Tarih filtresine göre anlık güncellenir.

### 7 — Ortalama Fatura

`ArchiveStatisticsResult`:
- `AveragePayableAmount`
- `AveragePaymentAmount`

UI:
- `ORTALAMA FATURA` kartı.
- gösterilen değer `AveragePayableAmount`.

### 9 — Kurum Profil İstatistiği

`KURUM PROFİLİ` kartı eklendi.

Belirli kurum seçildiğinde:
- kurum adı,
- filtrelenmiş fatura adedi,
- fatura türü sayısı,
- toplam ödeme
özetlenir.

Kurum seçilmemişse:
- `Kurum seçilmedi`.

### 10 — Fatura Türü Karşılaştırması

LiveCharts2 `ColumnSeries<double>` eklendi.

- x ekseni: fatura türleri
- y değeri: toplam ödeme
- data label açık
- donut ile aynı filtre sonucundan beslenir.

### 11 — Önceki Döneme Göre Değişim

`ÖNCEKİ DÖNEM` kartı eklendi.

Semantik:
- Yıl seçildiyse -> önceki yıl.
- Başlangıç + bitiş seçildiyse -> aynı gün sayısındaki hemen önceki dönem.
- dönem filtresi yoksa -> karşılaştırma yapılmaz.
- tüm yıllar toplamı tek önceki yılla yanlış kıyaslanmaz.

Gösterim:
- ↑ / ↓ / →
- yüzde fark
- önceki dönem toplam ödeme.

### 12 — PDF Eksikliği / Arşiv Sağlığı

`PDF SAĞLIĞI` kartı eklendi.

Yalnız istatistiğe uygun kapalı HYS arşivleri için:
- kayıtlı `PdfFileName`,
- fallback HYS.pdf
kontrol edilir.

Gösterim:
- `N/N PDF mevcut`
veya
- `X eksik • N arşiv`.

### 13 — Tarih Aralığı Filtresi

`ArchiveStatisticsFilter` sonuna geriye uyumlu:
- `DateTime? StartDate = null`
- `DateTime? EndDate = null`
eklendi.

UI:
- `Başlangıç Tarihi`
- `Bitiş Tarihi`

Mevcut Yıl/Kurum/Tür filtreleriyle birlikte kesişim olarak uygulanır.

### 14 — Hazır Rapor Şablonları

Yeni seçim:
- `Seçiniz`
- `Yıllık Genel Rapor`
- `Kurum Bazlı Rapor`
- `Elektrik Giderleri`
- `Tüm Fatura Türleri`

`UYGULA` düğmesiyle ilgili filtreler hazırlanır.

Davranış:
- Yıllık Genel -> en güncel arşiv yılını seçer, kurum/tür/tarih filtresini temizler.
- Kurum Bazlı -> önceden kurum seçilmesini ister, tür/tarih filtrelerini temizler.
- Elektrik -> türü Elektrik yapar.
- Tüm Fatura Türleri -> türü Tümü yapar.

### 15 — PNG grafik çıktısı

`PNG` düğmesi eklendi.

- `StatisticsDonutCard` yüksek çözünürlükte `RenderTargetBitmap` ile alınır.
- PNG olarak kullanıcı seçtiği konuma kaydedilir.
- yalnız donut değil, başlık + donut + sağ dağılım detayı birlikte görsel olur.

### Yeni analiz ekran yerleşimi

Üst aksiyonlar:
- PDF RAPORU
- YAZDIR
- CSV
- EXCEL
- PNG
- YENİLE

Filtreler:
- Yıl
- Kurum
- Fatura Türü
- Başlangıç
- Bitiş
- Hazır Rapor

İkinci özet satırı:
- Ortalama Fatura
- Önceki Dönem
- PDF Sağlığı
- Kurum Profili

Kaydırılabilir analiz alanı:
1. büyük donut + dağılım detayı
2. Aylık Ödeme Trendi
3. Fatura Türü Karşılaştırması

### Yeni test

`ArchiveStatisticsServiceTests.BuildAppliesDateRangeAndCreatesMonthlyBreakdown`

Doğrular:
- başlangıç/bitiş filtresi,
- fatura sayısı,
- ortalama fatura,
- ortalama ödeme,
- aylık satır sayısı,
- ay ve ödeme toplamları.

Test hedefi:
- önce: **123**
- yeni: **124/124**

### Commitler

Core:
- model/date/monthly/average: `04753ac631c3c2827e23e1ce9c6125f770400136`
- service date/monthly/average: `00f944bfb508248a6af21523a0a2ceb72a54baa0`
- test: `ad00c027995ecab9765e2343cdf0d7764571304b`

UI / analiz:
- geniş XAML filtre/grafik/çıktı yapısı: `5dcbf5b5f66a830c0e1d960f8d96625b007cf0d5`
- dönem karşılaştırma/PDF sağlık/profil/trend/tür grafik logic: `e23f6d0832da8230e29bed0381e45da92361589d`
- önceki dönem semantik + data labels: `f8865ffd1b8714f986b29813fdb19902c08870e1`

Çıktı:
- ClosedXML package: `8eb724322d8fa2f1ec006aa2e0ba37fe968f8618`
- PDF/Yazdır/CSV/Excel/PNG: `23991c154fcdc8f8880c216bef217c1751cdef2d`
- export kültür/CSV/PdfSharp güvenlik düzeltmeleri: `45c1d961db9aeb60ea714d983b68d138b11fecb0`

### Statik kontrol

Geçti:
- XAML control ↔ handler eşleşmesi
- tarih filter model/service
- monthly rows
- averages
- previous period
- PDF health
- institution profile
- LineSeries
- ColumnSeries
- ready templates
- PDF/Print/CSV/XLSX/PNG handlers
- CSV injection guard
- 6 uygulanmadı
- 8 uygulanmadı

Not:
- çalışma ortamındaki container internet erişimi olmadığı için repo clone + yerel `dotnet build` burada çalıştırılamadı.
- gerçek compile kapısı kullanıcı Windows ortamında zorunlu.

### Zorunlu doğrulama

1. GitHub Desktop → **Fetch origin → Pull origin**
2. `DERLE_CALISTIR.bat`
   - beklenen: **0 warning / 0 error**
   - ilk restore ClosedXML bağımlılıklarını indirebilir.
3. İSTATİSTİK smoke:
   - mevcut donut çalışmalı,
   - aylık trend görünmeli,
   - tür karşılaştırma görünmeli,
   - ortalama kartı doğru,
   - yıl seçince önceki dönem karşılaştırması,
   - tarih aralığı seçince eşit uzunluktaki önceki dönem karşılaştırması,
   - PDF sağlık sayacı,
   - kurum seçince kurum profil kartı,
   - şablon UYGULA.
4. Çıktı smoke:
   - PDF RAPORU açılabilir PDF oluşturmalı,
   - YAZDIR print dialog açmalı,
   - CSV Excel'de açılmalı,
   - EXCEL .xlsx üç sheet ile açılmalı,
   - PNG donut+legend kartını kaydetmeli.
5. `TESTLERI_CALISTIR.bat`
   - beklenen: **124/124**
6. Aşama 5.1 tarihsel PDF gerçek dosya smoke testi hâlâ uygun örnek PDF seti bulunduğunda ayrıca yapılacak.

## 19.09.2026 — Aşama 5.3 ilk build BAŞARISIZ — CSV syntax + LiveCharts target fix uygulandı

Kullanıcının yerel build sonucu:
- restore: **3 warning**
- Core: başarılı
- WPF: **4 error + 3 warning**
- build başarısız.

### Derleme hataları

Dosya:
- `DogruFatura.Wpf/MainWindow.StatisticsExports.cs`
- satır: yaklaşık `672`

Hatalar:
- CS1002
- CS1513
- CS8997
- CS1002

Neden:
`CsvCell` metodunun quote escape dönüş satırı yanlış üretilmişti:

yanlış:
```
return """ + safe.Replace(""", """", StringComparison.Ordinal) + """;
```

doğru:
```
return "\"" + safe.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
```

Fix commit:
- `2b10316ee48afd8b7fd625bbdaa928dee432efbf`

### NU1701 warningleri

Restore warningleri:
- `OpenTK 3.3.1`
- `OpenTK.GLWpfControl 3.3.0`
- `SkiaSharp.Views.WPF 3.119.0`

Kullanıcı logunda proje effective TFM:
- `net8.0-windows7.0`

LiveCharts2 2.0.5, `SkiaSharp.Views.WPF 3.119.0` getiriyor.
Bu SkiaSharp WPF paketi gerçek .NET 8 WPF assetini:
- `net8.0-windows10.0.19041`
altında sağlıyor.

`net8.0-windows7.0` hedefinde NuGet .NET Framework assetine fallback ettiği için NU1701 oluşuyordu.

Warning suppress edilmedi.

Düzeltme:
- WPF:
  - `net8.0-windows`
  - → `net8.0-windows10.0.19041`
- Tests:
  - `net8.0-windows`
  - → `net8.0-windows10.0.19041`

Tests de WPF projesine ProjectReference verdiği için iki proje aynı Windows platform minimumuyla hizalandı.

Core:
- `net8.0`
- değişmedi.

Commitler:
- WPF TFM: `4e9fd13c896f7d6cba437828d29240bfbb1f1311`
- Tests TFM: `d0ea3daabc411f28b40f27bf18b502439b9db695`

### Export kültür temizliği

Dosya adlarında doğrudan interpolation tarih formatı yerine:
- `StatisticsExportTimestamp()`
- `CultureInfo.InvariantCulture`

kullanıldı.

CSV/XLSX/PNG/PDF dosya adlarının tümü bu helper üzerinden oluşturuluyor.

Commit:
- `634f8432fc914dc9ac513967efd29619e1ba7f8f`

### Sıradaki zorunlu kapı

1. GitHub Desktop → **Fetch origin → Pull origin**
2. `DERLE_CALISTIR.bat`
3. Beklenen:
   - restore NU1701: **0**
   - Core warning/error: **0 / 0**
   - WPF warning/error: **0 / 0**
4. Build yeşil olmadan UI/output smoke veya test sonucuna geçilmeyecek.
5. Build yeşil olursa:
   - İstatistik ekranı smoke
   - PDF/CSV/XLSX/PNG output smoke
   - `TESTLERI_CALISTIR.bat`
   - hedef **124/124**

## 19.09.2026 — Aşama 5.3 ikinci build BAŞARISIZ — 10 error + 14 warning düzeltildi

Kullanıcının ikinci yerel build sonucu:
- restore: başarılı
- Core: başarılı
- WPF: **10 error + 14 warning**
- build başarısız.

### 10 derleme hatası — NumberParser görünmüyor

Dosya:
- `DogruFatura.Wpf/MainWindow.StatisticsExports.cs`

Hatalar:
- 10 x CS0103
- `NumberParser` adı geçerli bağlamda yok.

Neden:
- `MainWindow.Statistics.cs` içinde `using DogruFatura.Core.Services;` vardı.
- yeni export partial dosyasında bu using eksikti.
- `NumberParser.Money(...)` bu nedenle çözümlenemiyordu.

Düzeltme:
- `using DogruFatura.Core.Services;` eklendi.

### 14 analyzer warning düzeltmesi

#### CA1305 — 10 adet CSV AppendLine

Önce:
- interpolated `StringBuilder.AppendLine($"...")`

Yeni:
- sabit label `Append(...)`
- sayılar için açık `CultureInfo.InvariantCulture`
- tarih için açık `tr-TR`

Amaç:
- CSV değerlerinin makine kültürüne göre değişmemesi,
- analyzer warning üretmemesi.

#### CA1822 — PreviousStatisticsFilter

- instance veri kullanmıyordu.
- `private static` yapıldı.

#### CA1822 — StatisticsDistribution

- instance veri kullanmıyordu.
- `private static` yapıldı.

#### CA1826 — AvailableYears.FirstOrDefault

Önce:
- indexlenebilir `IReadOnlyList<int>` üzerinde LINQ `FirstOrDefault()`.

Yeni:
- `availableYears.Count > 0 ? availableYears[0] : 0`

#### CA1859 — CaptureStatisticsElementBitmap

Önce dönüş tipi:
- `BitmapSource`

Gerçek oluşturulan tip:
- `RenderTargetBitmap`

Yeni dönüş tipi:
- `RenderTargetBitmap`

### Framework uyumluluğu önceki fix ile korunuyor

WPF:
- `net8.0-windows10.0.19041`

Tests:
- `net8.0-windows10.0.19041`

Core:
- `net8.0`

### Commitler

- Statistics analyzer cleanup: `ce20e77213f646678bda51887788a83d1322d6f8`
- StatisticsExports compile + analyzer cleanup: `32ddb4b6debb8ebf22c11e7eb912f7ccbba4642c`

### Statik doğrulama

Geçti:
- NumberParser namespace import mevcut
- 10 NumberParser.Money çağrısı namespace ile çözümlenebilir
- interpolated AppendLine kalmadı
- CSV sayıları explicit invariant culture
- PreviousStatisticsFilter static
- latest year FirstOrDefault kaldırıldı
- StatisticsDistribution static
- RenderTargetBitmap concrete return
- WPF/Tests TFM 19041
- CsvCell quote syntax doğru
- test hedefi **124/124**

### Sıradaki zorunlu kapı

1. GitHub Desktop → **Fetch origin → Pull origin**
2. `DERLE_CALISTIR.bat`
3. Beklenen:
   - restore warning: **0**
   - Core warning/error: **0 / 0**
   - WPF warning/error: **0 / 0**
4. Build yeşil olursa:
   - istatistik UI smoke
   - PDF / Yazdır / CSV / Excel / PNG çıktı smoke
   - `TESTLERI_CALISTIR.bat`
   - hedef **124/124**

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


## 20.09.2026 — Aşama 5.3 İstatistik ekranı kompaktlaştırma + CSV düzeltmesi KODLANDI

Kullanıcı geri bildirimi:
- İstatistik bölümünde dikey scrollbar istenmiyor; sayfa aşağı kaydırılmadan temel içerik görünmeli.
- `FATURA TÜRÜ KARŞILAŞTIRMASI` kartı kaldırılmalı.
- `AYLIK ÖDEME TRENDİ` kartı kaldırılmalı.
- PDF / Yazdır / CSV / Excel / PNG için ayrı ayrı düğmeler yerine tek çıktı seçim düğmesi kullanılmalı.
- CSV çıktısı bozuk/karmaşık görünmemeli.

### UI değişiklikleri

`DogruFatura.Wpf/MainWindow.xaml`:
- İstatistik görünümünün kendi `ScrollViewer` katmanı kaldırıldı.
- Alt bölümdeki `AYLIK ÖDEME TRENDİ` ve `FATURA TÜRÜ KARŞILAŞTIRMASI` kartları kaldırıldı.
- Fatura türü dağılımı kartı daha kompakt boyutlandırıldı.
- Beş ayrı çıktı düğmesi kaldırıldı.
- Tek `ÇIKTI AL ▼` düğmesi eklendi.
- Açılır menü seçenekleri:
  - PDF RAPORU
  - YAZDIR
  - CSV
  - EXCEL
  - PNG
- `YENİLE` düğmesi ayrı tutuldu; çıktı seçeneği değildir.

### Kod sadeleştirmesi

`DogruFatura.Wpf/MainWindow.Statistics.cs`:
- kaldırılan iki grafik için refresh çağrıları kaldırıldı.
- `RefreshStatisticsMonthlyTrend` kaldırıldı.
- `RefreshStatisticsTypeComparison` kaldırıldı.
- Fatura türü dağılımı / donut hesaplaması korunuyor.

`DogruFatura.Wpf/MainWindow.StatisticsExports.cs`:
- `StatisticsExportMenu_Click` eklendi; tek düğmenin ContextMenu'sunu açıyor.
- mevcut PDF / Yazdır / CSV / Excel / PNG event handler'ları yeniden kullanılıyor.
- PDF raporundaki ikinci trend/karşılaştırma grafik sayfası kaldırıldı.
- PDF ana özet + fatura türü dağılımı korunuyor.

### CSV düzeltmesi

Önceki CSV:
- rapor bilgileri,
- filtre bilgileri,
- özet değerleri,
- boş satır,
- ardından detay tablosu

şeklinde farklı kolon sayılarına sahip satırlar içeriyordu; ayrıca para değerleri `InvariantCulture` ile noktalı ondalık olarak yazılıyordu.

Yeni CSV:
- yalnız tek, düzenli dikdörtgen tablo üretir.
- başlık:
  `Kurum;Yıl;Fatura Türü;Fatura Adedi;Fatura Tutarı;KDV;Damga Vergisi;Ödeme`
- metin alanlarında mevcut CSV quote/formül güvenliği korunur.
- parasal değerler açık `tr-TR` kültürüyle virgüllü ondalık biçimde yazılır.
- ayraç `;` olarak korunur.
- UTF-8 BOM korunur.
- `sep=;` özel satırı kaldırıldı.

### Commitler

- İstatistik UI / scroll / çıktı menüsü: `53b4047fd28e4b7a01b0a31981a52e3b9b846d68`
- Alt grafik kod temizliği: `83c6d08e887a633eb1b515d81123d9cab62e214e`
- CSV + çıktı/PDF sadeleştirmesi: `05ec8fc42ff10e19b8957d85890db1a5efe805fc`

### Statik doğrulama

Geçti:
- İstatistik XAML içinde `ScrollViewer` kalmadı.
- `StatisticsMonthlyTrendChart` referansı kalmadı.
- `StatisticsTypeComparisonChart` referansı kalmadı.
- kaldırılan iki refresh metodu ve çağrısı kalmadı.
- tek çıktı menüsü ve menu handler eşleşiyor.
- CSV tek tablo başlığıyla üretiliyor.
- CSV para değerleri `tr-TR`.
- CSV UTF-8 BOM korunuyor.

### Zorunlu yerel doğrulama

1. GitHub Desktop → **Fetch origin → Pull origin**
2. `DERLE_CALISTIR.bat`
3. Hedef:
   - restore warning: **0**
   - Core warning/error: **0 / 0**
   - WPF warning/error: **0 / 0**
4. İstatistik smoke:
   - sayfa açıldığında dikey scrollbar görünmemeli,
   - tüm temel kartlar aşağı kaydırmadan görünmeli,
   - Aylık Ödeme Trendi görünmemeli,
   - Fatura Türü Karşılaştırması görünmemeli,
   - `ÇIKTI AL ▼` menüsü açılmalı,
   - PDF / Yazdır / CSV / Excel / PNG seçenekleri çalışmalı.
5. CSV smoke:
   - Excel ile açıldığında kolonlar düzenli ayrılmalı,
   - Türkçe karakterler doğru görünmeli,
   - tutarlar sayı gibi ve virgüllü ondalık görünmeli,
   - başlık dışında farklı kolon sayılı rapor/özet satırları bulunmamalı.
6. `TESTLERI_CALISTIR.bat`
   - yeni test metodu eklenmedi,
   - hedef **124/124**.

Bu kapı yeşil olmadan Aşama 5.3 kapanmış sayılmamalı.


## 20.09.2026 — İstatistik ekranı 5 sütun filtre/işlem düzeni + dağılım sığdırma KODLANDI

Kullanıcı geri bildirimi:
- Fatura türü dağılımı kartının altı kesiliyordu.
- Filtre/işlem kartı yatayda 5 işlevsel bölüme ayrılmalı.
- Üst başlıktaki çıktı/yenile düğmeleri filtre kartına taşınmalı.
- Çıktı menüsü yazıları daha küçük ve daha sade olmalı.

### Yeni 5 sütun düzeni

1. sütun:
- Yıl
- Kurum

2. sütun:
- Başlangıç Tarihi
- Bitiş Tarihi

3. sütun:
- Fatura Türü
- Hızlı Rapor

4. sütun:
- ÇIKTI AL
- YENİLE

5. sütun:
- TEMİZLE
- UYGULA

Sütun oranları içerik ihtiyacına göre:
- 2.05*
- 1.25*
- 1.40*
- 0.86*
- 0.78*

Aralarda 10 px boşluk kolonları kullanıldı.

### Çıktı menüsü

- Üst başlıktaki ayrı çıktı/yenile alanı tamamen kaldırıldı.
- `StatisticsExportMenuButton` filtre kartının 4. sütununa taşındı.
- ContextMenu yazıları `12.5` punto ve normal font ağırlığına indirildi.
- Menü seçenekleri:
  - PDF Raporu
  - Yazdır
  - CSV
  - Excel
  - PNG

### Fatura türü dağılımı sığdırma

- donut grafik: `340x340 -> 290x290`
- sol grafik kolon genişliği: `430 -> 380`
- dış kart padding: `14 -> 10`
- sol iç kart padding: `14 -> 10`
- sağ detay padding: `16 -> 12`
- merkez daire: `144x144 -> 124x124`
- dağılım başlık/açıklama boşlukları küçültüldü.
- kart alt margin kaldırıldı.
- İstatistik görünümünde ScrollViewer yine yok.

### Commit

- İstatistik 5 sütun düzeni + dağılım sığdırma:
  `5fb534e88fa45e5a85bd38e8b526232659b386f0`

### Statik doğrulama

Geçti:
- Yıl/Kurum/Tarih/Tür/Hızlı Rapor kontrollerinin her biri tek kez tanımlı.
- `StatisticsExportMenuButton` tek kez tanımlı.
- üstte eski çıktı/yenile WrapPanel kalmadı.
- 5 sütun oranları XAML'de mevcut.
- çıktı menüsü kompakt font kullanıyor.
- donut `290x290`.
- İstatistik görünümünde ScrollViewer yok.
- `StatisticsExportMenu_Click` handler mevcut.

### Zorunlu yerel doğrulama

1. GitHub Desktop -> **Fetch origin -> Pull origin**
2. `DERLE_CALISTIR.bat`
   - hedef: **0 warning / 0 error**
3. İstatistik smoke:
   - filtre kartı yatayda 5 bölüm görünmeli,
   - Yıl/Kurum alt alta,
   - Başlangıç/Bitiş Tarihi alt alta,
   - Fatura Türü/Hızlı Rapor alt alta,
   - ÇIKTI AL/YENİLE alt alta,
   - TEMİZLE/UYGULA alt alta,
   - çıktı menüsü yazıları kaba/büyük görünmemeli,
   - Fatura Türü Dağılımı alt taraftan kesilmemeli,
   - sayfada scrollbar oluşmamalı.
4. `TESTLERI_CALISTIR.bat`
   - yeni test metodu eklenmedi,
   - hedef **124/124**.

Bu görsel revizyon yerel build/smoke kapısı yeşil olmadan tamamlandı kabul edilmemeli.


## 20.09.2026 — Fatura Türü Dağılımı görsel geri dönüşü KODLANDI

Kullanıcı geri bildirimi:
- 5 sütunlu filtre düzeni doğru yönde ilerledi.
- Ancak donut kartının 290x290'a küçültülmesiyle halka görseli aşırı inceldi ve eski görünüm kayboldu.
- İstenen: yeni 5 sütunlu üst düzen korunsun, Fatura Türü Dağılımı eski görsel boyutlarına dönsün.

### Uygulanan geri dönüş

`DogruFatura.Wpf/MainWindow.xaml` içinde yalnız dağılım kartının son küçültmeleri geri alındı:

- sol grafik kolon genişliği: `380 -> 430`
- ara kolon: `12 -> 14`
- dış kart padding: `10 -> 14`
- sol iç kart padding: `10 -> 14`
- grafik: `290x290 -> 340x340`
- merkez daire: `124x124 -> 144x144`
- merkez radius: `62 -> 72`
- merkez metin alanı: `104 -> 120`
- merkez başlık fontu: `12 -> 13`
- toplam ödeme fontu: `20 -> 23`
- sağ detay padding: `12 -> 16`
- sağ başlık/açıklama font ve boşlukları eski değerlerine döndürüldü.

### Korunan yeni düzen

- 5 sütunlu filtre/işlem kartı aynen korunuyor.
- ÇIKTI AL / YENİLE filtre kartı içinde kalıyor.
- TEMİZLE / UYGULA filtre kartı içinde kalıyor.
- üst başlıkta eski çıktı düğmeleri geri gelmedi.
- İstatistik sayfasında ScrollViewer geri eklenmedi.
- CSV düzeltmeleri korunuyor.

### Kod tarafı

`MainWindow.Statistics.cs` içindeki mevcut donut ayarları korunuyor:
- `InnerRadius = 128`
- `OuterRadiusOffset = 18`
- `HoverPushout = 8`
- renkli dilimler, yüzde etiketleri ve tooltip davranışı değişmedi.

### Commit

- Dağılım görselini eski boyutlarına geri getir:
  `60fd565595af957c1caf16d542417afd3b3e5564`

### Zorunlu yerel doğrulama

1. GitHub Desktop -> **Fetch origin -> Pull origin**
2. `DERLE_CALISTIR.bat`
   - hedef: **0 warning / 0 error**
3. İstatistik smoke:
   - 5 sütunlu filtre/işlem düzeni korunmalı,
   - donut renkli/dolu eski görseline dönmeli,
   - merkez toplam ödeme alanı okunaklı olmalı,
   - dağılım detay listesi görünmeli,
   - sayfada scrollbar oluşmamalı,
   - kart alttan kesilmemeli.
4. `TESTLERI_CALISTIR.bat`
   - hedef **124/124**.

Bu görsel geri dönüş yerel build/smoke doğrulaması sonrası tamamlanmış sayılmalı.


## 20.09.2026 — İkincil istatistik kartları ekrandan kaldırıldı, dağılıma alan açıldı

Kullanıcı görsel geri bildirimi:
- Ortalama Fatura
- Önceki Dönem
- PDF Sağlığı
- Kurum Profili

kartları ana İstatistik ekranında yer kaplıyor ve Fatura Türü Dağılımı kartının alt bölümünün kesilmesine neden oluyordu.
Bu dört bilginin ekranda değil, rapor/çıktılarda kullanılması tercih edildi.

### UI değişikliği

`DogruFatura.Wpf/MainWindow.xaml`:

- Grid.Row=3 içindeki dört ikincil bilgi kartı:
  - `Visibility="Collapsed"`
  - `Margin="0"`

olarak ayarlandı.
- Kartların içindeki named TextBlock kontrolleri silinmedi:
  - `StatisticsAverageText`
  - `StatisticsPreviousPeriodText`
  - `StatisticsPdfHealthText`
  - `StatisticsInstitutionProfileText`
- Böylece code-behind hesaplamaları ve çıktı üretimi bu değerleri kullanmaya devam ederken kartlar ekranda sıfır yer kaplıyor.
- Fatura Türü Dağılımı kartına `Margin="0,4,0,0"` ve `VerticalAlignment="Stretch"` verildi.
- Donut tekrar küçültülmedi; doğrulanmış görsel ölçüler korunuyor:
  - grafik `340x340`
  - merkez daire `144x144`
  - `InnerRadius = 128`

### Çıktı davranışı korunuyor

Mevcut çıktı kodunda:
- Ortalama fatura değeri rapor sonucundan kullanılmaya devam ediyor.
- PDF raporunda:
  - PDF sağlığı,
  - önceki dönem,
  - kurum profili
  bilgileri kullanılmaya devam ediyor.
- Ekranda gizlenmeleri çıktı değerlerini silmiyor.

### Commit

- İkincil kartları kaldır / dağılıma alan aç:
  `e209b5e160406f8e6c31bb18f48b39937dc8ea3c`

### Zorunlu yerel doğrulama

1. GitHub Desktop -> **Fetch origin -> Pull origin**
2. `DERLE_CALISTIR.bat`
   - hedef: **0 warning / 0 error**
3. İstatistik smoke:
   - dört ikincil kart ekranda görünmemeli,
   - aralarında boş satır kalmamalı,
   - Fatura Türü Dağılımı daha yukarıdan başlamalı,
   - donut 340x340 renkli/dolu görünmeli,
   - üstten ve alttan kesilmemeli,
   - dağılım detay bölümü tam görünmeli,
   - scrollbar oluşmamalı,
   - 5 sütunlu filtre/işlem düzeni korunmalı.
4. Çıktı smoke:
   - PDF raporunda önceki dönem / PDF sağlığı / kurum profili bilgileri korunmalı.
5. `TESTLERI_CALISTIR.bat`
   - yeni test metodu eklenmedi,
   - hedef **124/124**.

Bu UI paketi yerel build/smoke doğrulaması yeşil olduktan sonra tamamlanmış sayılmalı.


## 20.09.2026 — Donut yüzde etiketleri ve hover katmanı düzeltildi

Kullanıcı geri bildirimi:
- Fatura Türü Dağılımı yüzde etiketleri kenarlarda kesik görünüyordu.
- Dilim üzerine gelindiğinde açılan detay kutusu merkezdeki beyaz toplam ödeme kartının arkasında kalıyordu.

### XAML düzeltmesi

`DogruFatura.Wpf/MainWindow.xaml`:
- PieChart görsel alanı `340x340 -> 328x328` yapıldı; kartın genel alanı korunurken etiketlere güvenli kenar boşluğu açıldı.
- PieChart için `Panel.ZIndex="2"` eklendi.
- merkez toplam ödeme Border'ı için `Panel.ZIndex="1"` eklendi.
- Böylece chart tooltip/hover katmanı merkez kartının üzerinde render edilir.

### Grafik seri ayarları

`DogruFatura.Wpf/MainWindow.Statistics.cs`:
- `InnerRadius: 128 -> 122`
- `OuterRadiusOffset: 18 -> 0`
- `HoverPushout: 8 -> 6`
- `DataLabelsSize: 18 -> 14`
- `DataLabelsPosition = PolarLabelsPosition.Middle` korundu.
- yüzde gösterim eşiği `%6` olarak korundu.

Amaç:
- yüzde etiketlerini dilimlerin güvenli orta bölgesinde tutmak,
- kenar kırpılmasını önlemek,
- hover sırasında gereksiz dış taşmayı azaltmak,
- tooltip'in merkez kartının arkasında kalmasını engellemek.

### Commitler

- XAML tooltip / katman / güvenli chart alanı:
  `f6f8e8313e911e30643f101129f77dc234f0570c`
- Donut label/geometry ayarları:
  `a5c669dd576810bb754992edd4e1c8853258470a`

### Statik doğrulama

Geçti:
- chart `328x328`
- chart ZIndex `2`
- merkez kart ZIndex `1`
- `InnerRadius = 122`
- `OuterRadiusOffset = 0`
- `HoverPushout = 6`
- `DataLabelsSize = 14`
- orta pozisyon label davranışı korunuyor.

### Zorunlu yerel doğrulama

1. GitHub Desktop -> **Fetch origin -> Pull origin**
2. `DERLE_CALISTIR.bat`
   - hedef: **0 warning / 0 error**
3. İstatistik smoke:
   - % etiketleri tam okunmalı,
   - kenarlarda kırpılmamalı,
   - hover detay kutusu merkez toplam ödeme alanının üstünde görünmeli,
   - donut renkleri ve sağ dağılım detayları korunmalı.
4. `TESTLERI_CALISTIR.bat`
   - hedef **124/124**.
