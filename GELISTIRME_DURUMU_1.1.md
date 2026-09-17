# DoğruFatura 1.1 — Geliştirme Durumu

Bu dosya DoğruFatura 1.1 sağlamlaştırma çalışmalarının kalıcı ilerleme kaydıdır. Her tamamlanan geliştirme parçasından sonra güncellenir. Başka bir sohbete geçildiğinde bu dosyanın tamamı yeni sohbete yapıştırılarak kaldığımız yer milimetrik biçimde aktarılabilir.

## Çalışma dalı

`hardening/1.1-data-safety`

> Bu dosya şu anda `main` dalında değil, yukarıdaki çalışma dalındadır. GitHub Desktop'ta görmek için **Fetch origin** yaptıktan sonra **Current branch** menüsünden `hardening/1.1-data-safety` dalına geçin.

## Genel hedef

DoğruFatura'nın mevcut işlevlerini ve kullanıcı akışını koruyarak veri güvenliği, arşiv bütünlüğü, mimari ayrışma, mali izlenebilirlik ve CI kalite kapılarını güçlendirmek.

## Durum özeti

- [x] 1.1 geliştirme dalı oluşturuldu.
- [x] Kalıcı ilerleme/durum dosyası oluşturuldu.
- [ ] Aşama 1 — Veri Güvenliği
  - [x] 1.1 Bozuk veri kurtarma davranışı
  - [x] 1.2 Veri şema sürümleme
  - [x] 1.3 Yedekten geri yükleme semantik doğrulaması
  - [x] 1.4 Yedek ve PDF bütünlük doğrulaması
  - [ ] 1.5 Tek örnek / yazma güvenliği
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
- [x] Yeni kurtarma davranışı için otomatik testler eklendi (`RecoveryModeTests.cs`).
- [ ] GitHub Actions ile derleme/test doğrulaması — **altyapı engeli nedeniyle beklemede**. Workflow job'ları hiçbir step başlamadan failure oluyor; gerçek derleme/test sonucu üretilemiyor.

### 1.2 Veri şema sürümleme

- [x] Kalıcı JSON verileri için açık şema sürümü tanımlandı (`SchemaVersion`).
- [x] Veri dosyasının türünü doğrulayan `DataType` alanı eklendi.
- [x] İş verisi `Data` alanı altında sürümlü bir zarf içine alındı.
- [x] Şema sürümü ve migration zinciri `DataFileSchemaService` altında merkezileştirildi.
- [x] DoğruFatura 1.0.x dosyaları açıkça `Schema 0` olarak tanımlandı ve geriye uyumlu okunuyor.
- [x] Schema 0 -> Schema 1 geçişinde iş verisi değiştirilmeden yeni zarfa taşınıyor.
- [x] Otomatik yükseltmeden önce eski dosyanın fiziksel geri dönüş kopyası oluşturuluyor (`*_Schema0_Onceki_*.json`).
- [x] Geri dönüş kopyası oluşturulamazsa otomatik şema dönüşümü yapılmıyor.
- [x] Daha yeni/bilinmeyen şema sürümleri güvenli biçimde reddediliyor ve kaynak dosya ezilmiyor.
- [x] Yanlış `DataType` içeren dosyalar güvenli biçimde reddediliyor.
- [x] `InvoiceDataStore`, `SchoolDataStore`, `AuthorityProfileDataStore` ve `ArchivedInvoiceListDataStore` yeni şema sistemine geçirildi.
- [x] Eski dosyaların yükseltilmesi, yeni format round-trip'i, gelecek şema reddi ve veri türü doğrulaması için otomatik testler eklendi (`DataSchemaMigrationTests.cs`).
- [ ] GitHub Actions ile derleme/test doğrulaması — **runner/Actions altyapısı nedeniyle beklemede**.

### 1.3 Yedekten geri yükleme semantik doğrulaması

- [x] JSON parse kontrolünün ötesinde model ve iş kuralı doğrulaması eklendi.
- [x] Merkezi `DataDirectoryValidationService.cs` oluşturuldu.
- [x] `Okullar.json` içindeki kurum kayıtları `SchoolValidationService` kurallarıyla doğrulanıyor; eksik alan ve mükerrer kurumlar reddediliyor.
- [x] `AktifFaturalar.json` kayıtları `InvoiceValidationService`, mükerrer fatura kontrolü ve tek fatura türü politikasıyla doğrulanıyor.
- [x] Aktif faturalarda boş/mükerrer `Guid` kimlikleri reddediliyor.
- [x] `YetkiliProfili.json` eksik veya boş yetkili alanlarında reddediliyor.
- [x] `IslemArsivi.json` kayıtları `ArchivedInvoiceListValidationService` ile doğrulanıyor; mükerrer arşiv kimliği, arşiv numarası ve HYS numarası reddediliyor.
- [x] Restore işlemi artık dört zorunlu veri dosyasının (`Okullar.json`, `AktifFaturalar.json`, `YetkiliProfili.json`, `IslemArsivi.json`) tamamını gerektiriyor.
- [x] Geçersiz veya eksik yedek, geri yükleme öncesi güvenlik yedeği oluşturulmadan ve canlı veri klasörlerine dokunulmadan reddediliyor.
- [x] `ApplicationSafetyService.CheckIntegrity()` aynı merkezi semantik doğrulayıcıya geçirildi.
- [x] Şema-1 `IslemArsivi.json` dosyasının eski “kök mutlaka dizi olmalı” kontrolü nedeniyle yanlış bozuk sayılması engellendi.
- [x] Başarılı restore round-trip testi geçerli semantik verilerle güncellendi.
- [x] Semantik olarak bozuk yedeğin canlı veriyi değiştirmediği entegrasyon testi eklendi.
- [x] Zorunlu veri dosyası eksik yedeğin reddedildiği entegrasyon testi eklendi.
- [x] Şema-1 tam veri kümesinin `CheckIntegrity()` tarafından sağlıklı kabul edildiği test eklendi.
- [x] Semantik olarak bozuk canlı verinin bütünlük kontrolünde raporlandığı test eklendi.
- [ ] GitHub Actions ile derleme/test doğrulaması — **altyapı engeli nedeniyle beklemede**.

### 1.4 Yedek ve PDF bütünlük doğrulaması

- [x] Genel amaçlı `FileIntegrityService.cs` eklendi; dosya SHA-256 üretimi ve doğrulaması tek noktaya alındı.
- [x] Yeni yedek biçimi `DogruFaturaBackup/2` olarak tanımlandı.
- [x] Yedek manifestine yedekteki her dosya için göreli yol (`RelativePath`) eklendi.
- [x] Yedek manifestine her dosyanın beklenen byte boyutu (`ByteLength`) eklendi.
- [x] Yedek manifestine her dosyanın SHA-256 özeti (`Sha256`) eklendi.
- [x] Restore başlamadan önce ZIP içindeki gerçek dosya listesi manifestle birebir karşılaştırılıyor.
- [x] Manifestte olmayan fazladan dosya reddediliyor.
- [x] Manifestte olup ZIP'te bulunmayan dosya reddediliyor.
- [x] Aynı ZIP yolunun birden fazla kez kullanılması reddediliyor.
- [x] Dosya byte boyutu manifestle eşleşmezse restore reddediliyor.
- [x] Dosya SHA-256 özeti manifestle eşleşmezse restore reddediliyor.
- [x] Hash/manifest doğrulaması semantik doğrulamadan ve canlı klasör swap işleminden önce yapılıyor.
- [x] Eski `DogruFaturaBackup/1` yedekleri geriye uyumlu olarak desteklenmeye devam ediyor; bu yedeklerde SHA-256 manifesti olmadığı için semantik doğrulama ve PDF imza kontrolü uygulanıyor ve sonuç mesajında eski v1 biçimi olduğu belirtiliyor.
- [x] `ArchivedInvoiceList` modeline `PdfSha256` alanı eklendi.
- [x] `ArchivedInvoiceListValidationService`, dolu `PdfSha256` değerinin 64 haneli geçerli hex SHA-256 olmasını doğruluyor.
- [x] PDF eşitleme sırasında eski kayıtta hash yoksa mevcut PDF için başlangıç SHA-256 değeri kaydediliyor.
- [x] Eksik PDF yeniden oluşturulursa yeni dosyanın SHA-256 özeti kaydediliyor.
- [x] Kayıtlı hash bulunan mevcut PDF'nin içeriği değişmişse eşitleme hash'i otomatik güncellemiyor; bütünlük hatası üretiyor.
- [x] Tarihsel PDF içe aktarımında arşive alınan PDF'nin SHA-256 özeti anında kaydediliyor.
- [x] `ApplicationSafetyService.CheckIntegrity()` kapalı arşivlerde hash kayıtlı PDF'lerin varlığını ve SHA-256 bütünlüğünü kontrol ediyor.
- [x] Yedekten restore sırasında `IslemArsivi.json` içindeki kayıtlı kapalı PDF hashleri, yedek içindeki `Belgeler/Arşivlenenler` PDF'leriyle karşılaştırılıyor.
- [x] ZIP içindeki dosyanın sonradan değiştirilmesinin restore'u engellediği entegrasyon testi eklendi.
- [x] Kapalı arşiv PDF'si sonradan değiştirilirse `CheckIntegrity()` tarafından SHA-256 hatası olarak yakalandığı test eklendi.
- [x] PDF eşitlemenin hash bulunmayan legacy kayda hash eklediği test eklendi.
- [x] PDF eşitlemenin hash uyuşmazlığında kayıtlı hash'i değiştirmediği test eklendi.
- [x] V2 manifestin her dosya için boyut ve SHA-256 içerdiğini ZIP içinden doğrulayan test eklendi (`BackupManifestIntegrityTests.cs`).
- [ ] GitHub Actions ile derleme/test doğrulaması — **altyapı engeli nedeniyle beklemede**. Run #151 dahil son workflow'larda `build-and-test` job'u `steps=null` ile failure oluyor; `setup-smoke` skipped kalıyor. Gerçek kod çalıştırılmadı.

### 1.5 Tek örnek / yazma güvenliği

- [ ] Mutex oluşturulamadığında fail-open davranışını kaldır.
- [ ] İkinci örnek açılma / yazma yarışını güvenli biçimde engelle.
- [ ] Gerekirse salt okunur güvenli çalışma modu tasarla.
- [ ] Tek örnek davranışını test edilebilir bir servis/sonuç modeline ayır.
- [ ] Başarısız mutex senaryolarını otomatik testlerle koru.

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

**Aşama 1.4 — Yedek ve PDF bütünlük doğrulaması**

Son kod commit'i: `98a241726adba07e529d694039c002f6b1c17033`

Bu parçada öne çıkan değişiklikler:

- `DogruFatura.Core/Services/FileIntegrityService.cs` eklendi.
- `DogruFatura.Core/Models/ArchivedInvoiceList.cs` içine `PdfSha256` eklendi.
- `DogruFatura.Core/Services/ArchivedInvoiceListValidationService.cs` hash doğrulamasıyla genişletildi.
- `DogruFatura.Wpf/ApplicationSafetyService.cs` yedek formatı v2, dosya boyutu/hash manifesti ve restore öncesi kriptografik bütünlük kontrolüyle güçlendirildi.
- `DogruFatura.Wpf/ArchivePdfSynchronizationService.cs` mevcut PDF'nin kayıtlı hashle uyuşmadığı durumda otomatik hash yenilemeyi reddedecek biçimde güçlendirildi.
- `DogruFatura.Wpf/MainWindow.ArchiveDocuments.cs` tarihsel PDF içe aktarımında hash kaydediyor.
- `DogruFatura.Tests/DataSafetyIntegrationTests.cs` ZIP değiştirme ve arşiv PDF değiştirme senaryolarıyla genişletildi.
- `DogruFatura.Tests/ArchivePdfIntegrityTests.cs` eklendi.
- `DogruFatura.Tests/BackupManifestIntegrityTests.cs` eklendi.

### Doğrulama notu

17.09.2026 tarihindeki GitHub Actions run #151 (`98a2417...`) yine gerçek bir build adımına başlayamadan kapandı. `build-and-test` job kaydında `steps=null`; `setup-smoke` skipped. Dolayısıyla kod ve otomatik test kapsamı hazır ancak GitHub Actions üzerinde gerçek derleme/test henüz yürütülemedi. Actions/runner engeli çözülene kadar hiçbir aşama için “CI doğrulandı” denmeyecek.

## Sıradaki iş

**Aşama 1.5 — Tek örnek / yazma güvenliği.**

Uygulanacak sıra:

1. `App.xaml.cs` içindeki mevcut named Mutex davranışını yeniden incelemek.
2. Mutex oluşturma/açma sırasında exception oluştuğunda mevcut fail-open davranışını kaldırmak.
3. İkinci uygulama örneğinin aynı JSON/PDF dosyalarına eşzamanlı yazmasını kesin olarak engellemek.
4. Uygun durumda kullanıcıya “DoğruFatura zaten açık” veya güvenli başlatma hatası göstermek; veri yazma moduna geçmemek.
5. Tek örnek kararını test edilebilir bir servis/sınıfa ayırmak.
6. Mutex edinildi, zaten başka örnek var ve mutex altyapısı başarısız senaryoları için otomatik testler eklemek.
7. Bölüm tamamlanınca bu MD dosyasını yeniden güncellemek ve tamamını sohbet içinde kullanıcıya vermek.

## GitHub Desktop'ta çalışmaları görme

1. GitHub Desktop'ta `DogruFatura` deposunu seçin.
2. Üst bölümde **Fetch origin** düğmesine basın.
3. **Current branch** menüsünü açın.
4. `hardening/1.1-data-safety` branch'ini seçin. İlk kez görünüyorsa **Other Branches / Remote Branches** bölümünden seçin.
5. Branch açıldıktan sonra **Pull origin** görünüyorsa basın.
6. **History** sekmesinde yapılan commit'leri görebilirsiniz.
7. Dosyaları diskte görmek için **Repository > Show in Explorer** seçeneğini kullanın. Kök dizinde `GELISTIRME_DURUMU_1.1.md` bulunur.
8. Kodun mevcut `main` sürümünden farkını görmek için **Branch > Compare to Branch... > main** yolunu kullanın.
9. Bu branch'i şimdilik `main` ile birleştirmeyin; Veri Güvenliği Aşaması 1.5 dahil tamamlanıp toplu kontrol yapıldıktan sonra merge edilecek.

## Programı yerelde çalıştırma

GitHub Desktop'ta **Repository > Show in Explorer** yaptıktan sonra çalıştırılacak uygulama dosyasının adı:

`DogruFatura.exe`

Debug derlemesi varsa tipik yol:

`DogruFatura.Wpf\bin\Debug\net8.0-windows\DogruFatura.exe`

Release derlemesi varsa tipik yol:

`DogruFatura.Wpf\bin\Release\net8.0-windows\DogruFatura.exe`

`bin` klasörü veya `DogruFatura.exe` yoksa GitHub Desktop kaynak kodu çekmiş ancak program henüz yerel bilgisayarda derlenmemiş demektir. Visual Studio'da solution açılıp **Build > Build Solution** yapılmalı; ardından yukarıdaki Debug veya Release yolundaki `DogruFatura.exe` çalıştırılmalıdır.

## Yeni sohbete aktarım özeti

Yeni bir ChatGPT sohbetine geçilirse bu dosyanın tamamını yapıştırın ve şu talimatı ekleyin:

> “Bu MD, DoğruFatura 1.1 geliştirmesinde önceki sohbetten kalan tek doğru durum kaydıdır. GitHub reposu `mcalimlier/DogruFatura`, çalışma branch'i `hardening/1.1-data-safety`. MD'deki tamamlanan işleri yeniden yapma. `Sıradaki iş` bölümünden devam et ve her tamamlanan parçadan sonra bu MD'yi hem repoda güncelle hem de sohbet içinde tam metin olarak ver.”

Kritik bağlam:

- Repo: `mcalimlier/DogruFatura`
- Branch: `hardening/1.1-data-safety`
- `main` başlangıç noktası: `72963ec0a829f2e4b487393160c7ca93290b470f`
- Son tamamlanan kod parçası: **Aşama 1.4**
- Son kod commit'i: `98a241726adba07e529d694039c002f6b1c17033`
- Sıradaki parça: **Aşama 1.5 — Tek örnek / yazma güvenliği**
- CI durumu: GitHub Actions job'ları runner/step başlamadan failure oluyor; gerçek derleme/test sonucu henüz alınamıyor.
- `main` ile henüz merge yapılmayacak.
- Yerel uygulama EXE adı: `DogruFatura.exe`
- Tipik Debug yolu: `DogruFatura.Wpf\bin\Debug\net8.0-windows\DogruFatura.exe`

## Notlar

- Mevcut kullanıcı verisini otomatik silen veya geri döndürülemez biçimde dönüştüren değişiklik yapılmayacak.
- Otomatik şema yükseltmelerinde geri dönüş kopyası olmadan kaynak dosya dönüştürülmeyecek.
- Restore işleminde hash/manifest ve semantik doğrulama tamamlanmadan canlı veri klasörlerine dokunulmayacak.
- Kayıtlı PDF hash'iyle uyuşmayan mevcut PDF otomatik olarak yeni hash ile “sağlam” kabul edilmeyecek.
- SHA-256 manifest bütünlüğü dosya bozulmasını/değişikliğini tespit eder; dijital imza yerine geçmez.
- Her parça bağımsız ve geri alınabilir commit olarak tutulacak.
- Her tamamlanan parçadan sonra bu dosya mutlaka güncellenecek.
- Her tamamlanan parçadan sonra bu dosyanın **tam güncel içeriği sohbet içinde de kullanıcıya verilecek**.
- CI runner sorunu çözülmeden hiçbir aşama için “CI doğrulandı” ifadesi kullanılmayacak.
