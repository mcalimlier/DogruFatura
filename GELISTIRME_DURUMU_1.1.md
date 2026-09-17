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
- [ ] Ek kullanıcı arayüzü düzeltmeleri
  - [ ] Sağ üst küçült / büyüt-geri yükle / kapat simgelerini görsel olarak eşitle
  - [ ] Karşılama ekranını yaklaşık 5–6 saniye görünür tut
  - [ ] Karşılama ilerleme çubuğunu kesintisiz ve akıcı ilerlet
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
- [x] Yeni `SingleInstanceGuard.cs` ile tek örnek kararı test edilebilir servis/sonuç modeline ayrıldı.
- [x] Sonuçlar `Acquired`, `AlreadyRunning`, `Unavailable` olarak açıkça ayrılıyor.
- [x] İkinci uygulama örneği mevcutsa uygulama bilgi mesajıyla açılmadan kapanıyor.
- [x] Mutex altyapısı/izin hatası oluşursa uygulama artık açılmıyor; veri yazma moduna geçmeden güvenli biçimde kapanıyor.
- [x] Başarılı mutex ediniminde sahiplik `SingleInstanceLease` ile yaşam döngüsü boyunca tutuluyor.
- [x] Normal kapanışta lease güvenli biçimde bırakılıyor.
- [x] Yedekten geri yükleme sonrası yeniden başlatma akışı mutex'i kontrollü biçimde serbest bırakmaya devam ediyor.
- [x] Lease `Dispose()` idempotent olacak biçimde tasarlandı.
- [x] `SingleInstanceGuardTests.cs` ile edinim, zaten açık örnek ve mutex altyapı hatası senaryoları eklendi.
- [x] Fail-open yerine fail-closed güvenlik ilkesi uygulanıyor.
- [ ] GitHub Actions doğrulaması — run #155 dahil Actions job'ı `steps=null` ile gerçek build başlamadan failure oluyor; `setup-smoke` skipped.

## Ek kullanıcı arayüzü düzeltmeleri — devam ediyor

Kullanıcının 17.09.2026 tarihli isteği:

- Sağ üstteki **küçült / büyüt-geri yükle / kapat** simgelerinin algılanan büyüklükleri eşitlenecek.
- Karşılama ekranı yaklaşık **5–6 saniye** görünür kalacak.
- İlerleme çubuğu “dur-yükle-dur-yükle” hissi vermeden baştan sona **akıcı** ilerleyecek.

Durum: 1.5 tamamlandıktan sonra bu üç UI işi uygulanıyor.

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

**Aşama 1.5 — Tek örnek / yazma güvenliği**

Son kod commit'i: `731c64efea0ac1ba05ba803bcaa3cedc09007edd`

Bu parçada:

- `DogruFatura.Wpf/SingleInstanceGuard.cs` eklendi.
- `DogruFatura.Wpf/App.xaml.cs` fail-closed tek örnek akışına geçirildi.
- `DogruFatura.Tests/SingleInstanceGuardTests.cs` eklendi.
- Mutex oluşturulamaması artık “program yine açılsın” şeklinde ele alınmıyor.
- Aynı kullanıcı oturumunda ikinci süreç veya güvenli kilit altyapısı sorunu, JSON/PDF yazma başlamadan engelleniyor.

### Doğrulama notu

17.09.2026 GitHub Actions run #155, commit `731c64e...` için `build-and-test` job'ı yine `steps=null` ile failure oldu; `setup-smoke` skipped. Bu nedenle 1.5 kod/test kapsamı tamamlandı ancak GitHub Actions üzerinde gerçek build/test yürütülemedi.

## Sıradaki iş

**Ek kullanıcı arayüzü düzeltmeleri:**

1. `MainWindow.xaml` sağ üst pencere kontrol simgelerini aynı görsel ölçekte standardize etmek.
2. `SplashWindow` / `StartupProgressProfile` açılış süresini yaklaşık 5.5 saniyeye sabitlemek.
3. Progress animasyonunu gerçek yükleme aşamalarından bağımsız, tek kesintisiz zaman çizgisi üzerinde akıcı ilerletmek.
4. MainWindow hazırlığı erken biterse splash minimum süre dolana kadar akıcı animasyona devam etmek; hazırlık uzarsa ilerlemeyi %98 civarında tutup hazır olduğunda tamamlamak.
5. UI davranışlarını mümkün olduğunca test edilebilir profil/hesaplama koduyla korumak.
6. İşler bitince bu MD'yi tekrar güncellemek ve tam metnini sohbet içinde vermek.

## GitHub Desktop'ta çalışmaları görme

1. GitHub Desktop'ta `DogruFatura` deposunu seçin.
2. **Fetch origin** düğmesine basın.
3. **Current branch** menüsünden `hardening/1.1-data-safety` dalını seçin.
4. **Pull origin** görünüyorsa basın.
5. **History** sekmesinde commit'leri görebilirsiniz.
6. **Repository > Show in Explorer** ile dosyaları açabilirsiniz.
7. `main` farkı için **Branch > Compare to Branch... > main** kullanın.
8. Branch'i şimdilik `main` ile birleştirmeyin; UI düzeltmeleri ve toplu yerel kontrol sonrası merge kararı verilecek.

## Programı yerelde çalıştırma

Çalıştırılacak uygulama: `DogruFatura.exe`

Debug yolu:

`DogruFatura.Wpf\bin\Debug\net8.0-windows\DogruFatura.exe`

Release yolu:

`DogruFatura.Wpf\bin\Release\net8.0-windows\DogruFatura.exe`

EXE yoksa Visual Studio'da solution açılıp **Build > Build Solution** yapılmalıdır.

## Yeni sohbete aktarım özeti

Yeni bir sohbete geçilirse bu dosyanın tamamını yapıştırın ve şu talimatı ekleyin:

> “Bu MD, DoğruFatura 1.1 geliştirmesinde önceki sohbetten kalan tek doğru durum kaydıdır. GitHub reposu `mcalimlier/DogruFatura`, çalışma branch'i `hardening/1.1-data-safety`. MD'deki tamamlanan işleri yeniden yapma. `Sıradaki iş` bölümünden devam et ve her tamamlanan parçadan sonra bu MD'yi hem repoda güncelle hem de sohbet içinde tam metin olarak ver.”

Kritik bağlam:

- Repo: `mcalimlier/DogruFatura`
- Branch: `hardening/1.1-data-safety`
- `main` başlangıç commit'i: `72963ec0a829f2e4b487393160c7ca93290b470f`
- Son tamamlanan ana parça: **Aşama 1.5**
- Son kod commit'i: `731c64efea0ac1ba05ba803bcaa3cedc09007edd`
- Aşama 1'in 1.1–1.5 kod/test kapsamı tamamlandı.
- Şu an yürütülen iş: sağ üst pencere simgeleri + splash süresi/akıcı progress.
- CI durumu: GitHub Actions job'ları step başlamadan failure oluyor; gerçek build/test sonucu alınamıyor.
- `main` ile henüz merge yapılmayacak.

## Notlar

- Kullanıcı verisini otomatik silen veya geri döndürülemez dönüştüren değişiklik yapılmayacak.
- Restore hash/manifest ve semantik doğrulama bitmeden canlı verilere dokunmayacak.
- PDF hash uyuşmazlığı otomatik olarak “yeni doğru hash” şeklinde kabul edilmeyecek.
- Tek örnek güvenliği fail-closed çalışacak; kilit güvenli kurulamıyorsa yazma modunda uygulama açılmayacak.
- Her tamamlanan parçadan sonra bu dosya mutlaka repoda güncellenecek.
- Her tamamlanan parçadan sonra dosyanın tam güncel içeriği sohbet içinde de kullanıcıya verilecek.
- CI runner sorunu çözülmeden hiçbir aşama için “CI doğrulandı” ifadesi kullanılmayacak.
