# DoğruFatura 1.1 — Geliştirme Durumu

Bu dosya DoğruFatura 1.1 sağlamlaştırma çalışmalarının kalıcı ilerleme kaydıdır. Her tamamlanan geliştirme parçasından sonra güncellenir. Başka bir sohbete geçildiğinde bu dosyanın tamamı yeni sohbete yapıştırılarak kaldığımız yer milimetrik biçimde aktarılabilir.

## Çalışma dalı

`hardening/1.1-data-safety`

> Bu dosya şu anda `main` dalında değil, yukarıdaki çalışma dalındadır. GitHub Desktop'ta görmek için **Fetch origin** yaptıktan sonra **Current branch** menüsünden `hardening/1.1-data-safety` dalına geçin.

## Genel hedef

DoğruFatura'nın mevcut işlevlerini ve kullanıcı akışını koruyarak veri güvenliği, arşiv bütünlüğü, mimari ayrışma, mali izlenebilirlik, CI kalite kapıları ve kullanıcı deneyimini güçlendirmek.

## Durum özeti

- [x] 1.1 geliştirme dalı oluşturuldu.
- [x] Kalıcı ilerleme/durum dosyası oluşturuldu.
- [x] Aşama 1 — Veri Güvenliği (kod/test kapsamı tamamlandı; CI altyapı doğrulaması beklemede)
  - [x] 1.1 Bozuk veri kurtarma davranışı
  - [x] 1.2 Veri şema sürümleme
  - [x] 1.3 Yedekten geri yükleme semantik doğrulaması
  - [x] 1.4 Yedek ve PDF bütünlük doğrulaması
  - [x] 1.5 Tek örnek / yazma güvenliği
- [x] Ek kullanıcı arayüzü düzeltmeleri
  - [x] Sağ üst küçült / büyüt-geri yükle / kapat simgeleri aynı optik ölçüye getirildi
  - [x] Karşılama ekranı yaklaşık 5,5 saniye görünür kalacak şekilde ayarlandı
  - [x] Karşılama ilerleme çubuğu ana pencere yükünden bağımsız akıcı hale getirildi
- [ ] Aşama 2 — Mimari Temizlik
- [ ] Aşama 3 — Mali Denetim ve İzlenebilirlik
- [ ] Aşama 4 — Kalite / CI
- [ ] Aşama 5 — Kullanıcı Deneyimi

## Aşama 1 — Veri Güvenliği

### 1.1 Bozuk veri kurtarma davranışı

- [x] Bozuk ana veri dosyasının otomatik olarak boş/varsayılan dosyayla ezilmesi kaldırıldı.
- [x] Sorunlu dosya zaman damgalı koruma kopyasıyla saklanıyor.
- [x] Uygulama kullanılabilir geçici veriyle açılırken kurtarma durumu kullanıcıya açıkça bildiriliyor.
- [x] İşlem arşivinde sağlam kayıtlar kayıt bazında kurtarılıyor; hatalı/çakışan kayıtlar dışarıda bırakılıyor.
- [x] Aktif fatura ve kurum verisinde mümkün olan sağlam kayıtlar kayıt bazında kurtarılıyor.
- [x] Yetkili profili bozulduğunda varsayılan profil yalnız oturum içinde kullanılıyor; ana dosya otomatik ezilmiyor.
- [x] `RecoveryModeTests.cs` eklendi.
- [ ] GitHub Actions doğrulaması — altyapı engeli nedeniyle beklemede.

### 1.2 Veri şema sürümleme

- [x] `SchemaVersion`, `DataType`, `Data` zarfı eklendi.
- [x] Migration zinciri `DataFileSchemaService` altında merkezileştirildi.
- [x] 1.0.x çıplak JSON dosyaları Schema 0 olarak geriye uyumlu okunuyor.
- [x] Schema 0 -> 1 geçişinde iş verisi değiştirilmeden yeni zarfa taşınıyor.
- [x] Otomatik yükseltme öncesi `*_Schema0_Onceki_*.json` geri dönüş kopyası oluşturuluyor.
- [x] Geri dönüş kopyası oluşturulamazsa otomatik dönüşüm yapılmıyor.
- [x] Gelecek şemalar ve yanlış `DataType` güvenli biçimde reddediliyor.
- [x] Dört ana veri deposu yeni şema sistemine geçirildi.
- [x] `DataSchemaMigrationTests.cs` eklendi.
- [ ] GitHub Actions doğrulaması — altyapı engeli nedeniyle beklemede.

### 1.3 Yedekten geri yükleme semantik doğrulaması

- [x] Merkezi `DataDirectoryValidationService.cs` eklendi.
- [x] Okul/kurum, aktif fatura, yetkili profili ve işlem arşivi restore öncesi iş kurallarıyla doğrulanıyor.
- [x] Aktif faturalarda boş/mükerrer `Guid`, mükerrer fatura ve karma fatura türü reddediliyor.
- [x] Arşivlerde mükerrer kimlik, arşiv numarası ve HYS reddediliyor.
- [x] Restore dört zorunlu veri dosyasının tamamını gerektiriyor.
- [x] Geçersiz yedek canlı klasörlere dokunulmadan reddediliyor.
- [x] `CheckIntegrity()` aynı semantik doğrulayıcıya geçirildi.
- [x] Şema-1 arşiv biçimiyle eski kök-dizi kontrolü arasındaki uyumsuzluk giderildi.
- [x] Restore güvenlik entegrasyon testleri genişletildi.
- [ ] GitHub Actions doğrulaması — altyapı engeli nedeniyle beklemede.

### 1.4 Yedek ve PDF bütünlük doğrulaması

- [x] `FileIntegrityService.cs` eklendi.
- [x] Yeni yedek biçimi `DogruFaturaBackup/2` olarak tanımlandı.
- [x] Manifestte her dosyanın göreli yolu, byte boyutu ve SHA-256 özeti saklanıyor.
- [x] Restore öncesi ZIP içeriği manifestle birebir karşılaştırılıyor.
- [x] Eksik/fazladan dosya, mükerrer ZIP yolu, boyut uyuşmazlığı ve SHA-256 uyuşmazlığı restore'u engelliyor.
- [x] Eski `DogruFaturaBackup/1` yedekleri geriye uyumlu destekleniyor; kriptografik manifest olmadığı açıkça ayrılıyor.
- [x] `ArchivedInvoiceList.PdfSha256` eklendi ve doğrulanıyor.
- [x] PDF eşitleme legacy kayıtlara başlangıç hash'i yazıyor.
- [x] Kayıtlı hash ile mevcut PDF uyuşmazsa hash otomatik yenilenmiyor, bütünlük hatası oluşuyor.
- [x] Tarihsel PDF içe aktarımında hash kaydediliyor.
- [x] `CheckIntegrity()` ve restore arşiv PDF hashlerini doğruluyor.
- [x] `ArchivePdfIntegrityTests.cs` ve `BackupManifestIntegrityTests.cs` eklendi.
- [ ] GitHub Actions doğrulaması — altyapı engeli nedeniyle beklemede.

### 1.5 Tek örnek / yazma güvenliği

- [x] Mevcut mutex fail-open davranışı kaldırıldı.
- [x] `SingleInstanceGuard.cs` ile tek örnek kararı test edilebilir servis/sonuç modeline ayrıldı.
- [x] Sonuçlar `Acquired`, `AlreadyRunning`, `Unavailable` olarak açıkça ayrılıyor.
- [x] İkinci uygulama örneği mevcutsa uygulama bilgi mesajıyla açılmadan kapanıyor.
- [x] Mutex altyapısı/izin hatası oluşursa uygulama veri yazma moduna geçmeden güvenli biçimde kapanıyor.
- [x] Başarılı mutex ediniminde sahiplik `SingleInstanceLease` ile yaşam döngüsü boyunca tutuluyor.
- [x] Normal kapanışta lease güvenli biçimde bırakılıyor.
- [x] Yedekten geri yükleme sonrası yeniden başlatma akışı mutex'i kontrollü biçimde serbest bırakıyor.
- [x] Lease `Dispose()` idempotent.
- [x] `SingleInstanceGuardTests.cs` ile edinim, zaten açık örnek ve mutex altyapı hatası testleri eklendi.
- [x] Fail-open yerine fail-closed güvenlik ilkesi uygulanıyor.
- [ ] GitHub Actions doğrulaması — run #155 dahil Actions job'ları gerçek build başlamadan `steps=null` ile failure oluyor.

## Ek kullanıcı arayüzü düzeltmeleri — tamamlandı

### Sağ üst pencere düğmeleri

- [x] Eski durumda küçültme `—` karakteri 23.5 pt, kapatma `×` karakteri 27.5 pt, büyütme ise Segoe MDL2 Assets 16.5 pt kullanıyordu.
- [x] `WindowControlPolishService.cs` eklendi.
- [x] Küçült, büyüt/geri yükle ve kapat kontrolleri aynı `Segoe MDL2 Assets` simge ailesine geçirildi.
- [x] Küçült için `E921`, büyüt için mevcut `E922/E923`, kapat için `E8BB` Windows simgeleri kullanılıyor.
- [x] Üç kontrolün optik simge boyutu 16.5, buton ölçüsü 44×42 ve içerik hizası merkez olarak standardize edildi.
- [x] Servis `UiPolishService` üzerinden MainWindow yüklendiğinde uygulanıyor.

### Karşılama ekranı ve akıcı ilerleme

- [x] `StartupProgressProfile` minimum görünür süreyi 5500 ms olarak tanımlıyor.
- [x] Normal ilerleme 0 → %94 arasında ilk 5000 ms boyunca kesintisiz ilerliyor.
- [x] Çok uzun açılışlarda %94 → %98 arasında yavaş devam eden kuyruk animasyonu bulunuyor; %100 yalnız gerçek hazırlık tamamlanınca kullanılıyor.
- [x] `StartupSplashHost.cs` eklendi.
- [x] Splash penceresi ana MainWindow thread'inden bağımsız STA/Dispatcher thread'inde çalışıyor.
- [x] `MainWindow()` veri/arayüz hazırlığı ana thread'i meşgul etse bile splash spinner ve progress animasyonu ayrı Dispatcher'da akmaya devam ediyor.
- [x] MainWindow erken hazır olsa dahi splash yaklaşık 5,5 saniye dolmadan kapanmıyor.
- [x] MainWindow geç hazır olursa splash %98'e kadar kontrollü biçimde ilerlemeye devam ediyor ve hazır olunca son geçişi yapıyor.
- [x] Tamamlanma aşaması mevcut animasyon genişliğinden %100'e 450 ms EaseOut geçişle tamamlanıyor.
- [x] Yüzde metni yaklaşık 33 ms cadence ile zaman tabanlı güncelleniyor.
- [x] Spinner yaklaşık 0,9 saniyelik sürekli dönüşle çalışıyor.
- [x] `StartupProgressProfileTests.cs` yeni 5–6 saniye ve monoton progress davranışına göre güncellendi.
- [x] Bilinçli 5,5 saniyelik splash süresi `StartupPerformanceService` performans metriğine dahil edilmiyor; performans ölçümü MainWindow gerçek hazırlığı bittiğinde tamamlanıyor.
- [ ] GitHub Actions doğrulaması — run #165 dahil `build-and-test` job'ı `steps=null`; gerçek build/test çalışmadı.

## Aşama 2 — Mimari Temizlik

- [ ] MainWindow sorumluluklarını ViewModel / application service katmanlarına ayır.
- [ ] Dependency Injection kullan.
- [ ] Merkezi InvoiceRulesEngine oluştur; validation ve Kontrol Merkezi aynı kuralları kullansın.
- [ ] InvoiceType / InstitutionType / ArchiveStatus için enum veya value object kullan.
- [ ] SchoolRecord için değişmez `SchoolId` ekle ve fatura-kurum bağını ID üzerinden kur.

## Aşama 3 — Mali Denetim ve İzlenebilirlik

- [ ] Vergi hesaplama kural sürümü / yürürlük tarihi modeli ekle.
- [ ] Append-only audit trail ekle.
- [ ] Çok dosyalı arşiv işlemleri için transaction journal ekle.
- [ ] Kapalı HYS arşivini değişmez belge modeliyle güçlendir.

## Aşama 4 — Kalite / CI

- [ ] WPF katmanında warning-as-error kapısını yeniden etkinleştir.
- [ ] Coverage için minimum kalite eşiği ekle.
- [ ] Güvenlik/bağımlılık otomasyonunu güçlendir.
- [ ] Sürüm/release belgelerini `VersionPrefix` üzerinden otomatikleştir.

## Aşama 5 — Kullanıcı Deneyimi

- [ ] PDF içe aktarmayı async + ilerleme + iptal destekli yap.
- [ ] Boş fatura numarasında olası mükerrer uyarısı ekle.
- [ ] Tanılama paketi için içerik önizlemesi / daha güçlü kişisel veri temizleme ekle.
- [ ] Yeni PDF'lere makine-okunabilir DoğruFatura metadata formatı ekle.

## Tamamlanan son parça

**Aşama 1.5 + kullanıcı tarafından istenen pencere/splash arayüz düzeltmeleri**

Son kod commit'i: `b506457ec04e391c9a256116011a5945e5fa8c78`

Bu turda eklenen/değişen ana dosyalar:

- `DogruFatura.Wpf/SingleInstanceGuard.cs`
- `DogruFatura.Wpf/App.xaml.cs`
- `DogruFatura.Tests/SingleInstanceGuardTests.cs`
- `DogruFatura.Wpf/WindowControlPolishService.cs`
- `DogruFatura.Wpf/UiPolishService.cs`
- `DogruFatura.Wpf/StartupSplashHost.cs`
- `DogruFatura.Wpf/SplashWindow.xaml.cs`
- `DogruFatura.Wpf/StartupProgressProfile.cs`
- `DogruFatura.Tests/StartupProgressProfileTests.cs`

### Doğrulama notu

17.09.2026 GitHub Actions run #165, commit `b506457...` için `build-and-test` job'ı yine `steps=null` ile failure oldu ve `setup-smoke` skipped kaldı. Bu nedenle kod/test kapsamı hazırlanmış durumda fakat GitHub Actions altyapısı gerçek derleme/test çalıştırmadı.

Bu aşamadan sonra kullanıcı yerel bilgisayarda branch'i çekip Visual Studio ile build ederek gerçek WPF görünümünü kontrol etmelidir. Özellikle sağ üst simgelerin optik eşitliği ve splash akıcılığı görsel olarak yerel çalıştırmada kontrol edilebilir.

## Sıradaki iş

**Aşama 2 — Mimari Temizlik**

Önerilen uygulama sırası:

1. MainWindow'daki servis oluşturma ve veri yükleme sorumluluklarını application service katmanına çıkarmak.
2. Dependency Injection composition root oluşturmak.
3. Fatura doğrulama/Kontrol Merkezi kurallarını ortak `InvoiceRulesEngine` altında birleştirmek.
4. String tabanlı `InvoiceType` / `InstitutionType` değerlerini güvenli value object/enum modeline geçirmek.
5. `SchoolRecord` için kalıcı `SchoolId` ekleyip fatura-kurum bağını isim yerine ID ile güçlendirmek.
6. Her alt parçayı bağımsız commit/test ile yapmak.
7. Her tamamlanan alt parçada bu MD'yi hem repoda hem sohbet içinde tam metin olarak güncellemek.

> Not: Aşama 1 kod/test kapsamı tamamlandı. `main` ile merge kararı vermeden önce yerel build ve kısa manuel smoke test yapılması tercih edilir; GitHub Actions runner sorunu nedeniyle uzaktaki CI henüz güvenilir doğrulama sağlayamıyor.

## GitHub Desktop'ta çalışmaları görme

1. GitHub Desktop'ta `DogruFatura` deposunu seçin.
2. **Fetch origin** düğmesine basın.
3. **Current branch** menüsünden `hardening/1.1-data-safety` dalını seçin.
4. **Pull origin** görünüyorsa basın.
5. **History** sekmesinde commit'leri görebilirsiniz.
6. **Repository > Show in Explorer** ile dosyaları açabilirsiniz.
7. `main` farkı için **Branch > Compare to Branch... > main** kullanın.
8. Yerel build/smoke kontrolü yapılana kadar branch'i `main` ile birleştirmeyin.

## Programı yerelde çalıştırma

Çalıştırılacak uygulama: `DogruFatura.exe`

Debug yolu:

`DogruFatura.Wpf\bin\Debug\net8.0-windows\DogruFatura.exe`

Release yolu:

`DogruFatura.Wpf\bin\Release\net8.0-windows\DogruFatura.exe`

EXE yoksa Visual Studio'da solution açılıp **Build > Build Solution** yapılmalıdır.

Bu turdaki değişiklikleri yerelde görmek için:

1. GitHub Desktop → **Fetch origin**.
2. Branch `hardening/1.1-data-safety` seçili olsun.
3. **Pull origin** yapın.
4. Visual Studio'da solution'ı açın.
5. **Build > Rebuild Solution** yapın.
6. Build başarılıysa `DogruFatura.Wpf\bin\Debug\net8.0-windows\DogruFatura.exe` çalıştırın.
7. Açılışta splash yaklaşık 5,5 saniye görünmeli ve bar kesintisiz akmalıdır.
8. Ana pencere açılınca sağ üst üç kontrolün simgeleri aynı optik ölçüde görünmelidir.
9. EXE açıkken aynı EXE'yi ikinci kez çalıştırın; ikinci örnek açılmamalı ve “DoğruFatura zaten açık” mesajı görünmelidir.

## Yeni sohbete aktarım özeti

Yeni bir sohbete geçilirse bu dosyanın tamamını yapıştırın ve şu talimatı ekleyin:

> “Bu MD, DoğruFatura 1.1 geliştirmesinde önceki sohbetten kalan tek doğru durum kaydıdır. GitHub reposu `mcalimlier/DogruFatura`, çalışma branch'i `hardening/1.1-data-safety`. MD'deki tamamlanan işleri yeniden yapma. `Sıradaki iş` bölümünden devam et ve her tamamlanan parçadan sonra bu MD'yi hem repoda güncelle hem de sohbet içinde tam metin olarak ver.”

Kritik bağlam:

- Repo: `mcalimlier/DogruFatura`
- Branch: `hardening/1.1-data-safety`
- `main` başlangıç commit'i: `72963ec0a829f2e4b487393160c7ca93290b470f`
- Aşama 1'in 1.1–1.5 kod/test kapsamı tamamlandı.
- Kullanıcının istediği sağ üst simge eşitlemesi tamamlandı.
- Kullanıcının istediği yaklaşık 5,5 saniyelik akıcı splash tamamlandı.
- Son kod commit'i: `b506457ec04e391c9a256116011a5945e5fa8c78`
- Sıradaki ana parça: **Aşama 2 — Mimari Temizlik**.
- CI durumu: GitHub Actions job'ları runner/step başlamadan failure oluyor; gerçek build/test sonucu henüz alınamıyor.
- `main` ile henüz merge yapılmayacak; önce yerel build/smoke kontrolü öneriliyor.

## Notlar

- Kullanıcı verisini otomatik silen veya geri döndürülemez dönüştüren değişiklik yapılmayacak.
- Restore hash/manifest ve semantik doğrulama bitmeden canlı verilere dokunmayacak.
- PDF hash uyuşmazlığı otomatik olarak “yeni doğru hash” şeklinde kabul edilmeyecek.
- Tek örnek güvenliği fail-closed çalışacak; kilit güvenli kurulamıyorsa yazma modunda uygulama açılmayacak.
- Splash görsel süresi performans ölçümünden ayrı tutulacak.
- Her tamamlanan parçadan sonra bu dosya mutlaka repoda güncellenecek.
- Her tamamlanan parçadan sonra dosyanın tam güncel içeriği sohbet içinde de kullanıcıya verilecek.
- CI runner sorunu çözülmeden hiçbir aşama için “CI doğrulandı” ifadesi kullanılmayacak.
