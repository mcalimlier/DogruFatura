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
  - [ ] 1.4 Yedek ve PDF bütünlük doğrulaması
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
- [ ] GitHub Actions ile derleme/test doğrulaması — **altyapı engeli nedeniyle beklemede**. Son denemede `build-and-test` job'ı `steps=null` ile failure oldu ve `setup-smoke` skipped oldu; gerçek kod çalıştırılmadı.

### 1.4 Yedek ve PDF bütünlük doğrulaması

- [ ] Yedek manifestine her dosya için SHA-256 özeti ekle.
- [ ] Yedek manifestine her dosyanın beklenen byte boyutunu ekle.
- [ ] Restore sırasında manifestte kayıtlı hash ve boyutları açılan dosyalarla karşılaştır.
- [ ] Manifestte olmayan/fazladan dosya veya hash uyuşmazlığında restore'u reddet.
- [ ] `ArchivedInvoiceList` içine arşiv PDF'sinin SHA-256 bütünlük bilgisini ekle.
- [ ] Kapalı arşiv PDF'si mevcutsa hash doğrulaması yap; dosya değiştirilmişse kullanıcıyı uyar ve otomatik olarak “sağlam” kabul etme.
- [ ] PDF bütünlük senaryolarını otomatik testlerle koru.

### 1.5 Tek örnek / yazma güvenliği

- [ ] Mutex oluşturulamadığında fail-open davranışını kaldır.
- [ ] Gerekirse salt okunur güvenli çalışma modu tasarla.

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

**Aşama 1.3 — Yedekten geri yükleme semantik doğrulaması**

Son kod commit'i: `d82140b8fa31b5e10406d830010df2119cf129c4`

Bu parçada:

- `DogruFatura.Core/Services/DataDirectoryValidationService.cs` eklendi.
- `DogruFatura.Wpf/ApplicationSafetyService.cs` restore ve bütünlük kontrolü merkezi semantik doğrulamaya bağlandı.
- `DogruFatura.Tests/DataSafetyIntegrationTests.cs` geçerli restore senaryosu ve yeni güvenlik/red senaryolarıyla genişletildi.
- Geçersiz yedek canlı veri üzerinde hiçbir swap/copy işlemi başlamadan reddediliyor.
- Yeni şema-1 arşiv formatıyla eski bütünlük kontrolü arasındaki uyumsuzluk giderildi.

### Doğrulama notu

17.09.2026 tarihindeki son branch workflow denemesinde GitHub Actions `build-and-test` job'ı yine step oluşturmadan failure oldu (`steps=null`); `setup-smoke` skipped oldu. Bu nedenle 1.3 için kod ve otomatik test kapsamı hazırlanmış durumda ancak CI üzerinde gerçek derleme/test yürütülemedi. Bu sorun kod hatası olarak sınıflandırılmıyor; Actions/runner tarafındaki engel çözülene kadar “CI doğrulandı” denmeyecek.

## Sıradaki iş

**Aşama 1.4 — Yedek ve PDF bütünlük doğrulaması.**

Uygulanacak sıra:

1. `BackupManifest` yapısını sürümleyerek her yedek dosyası için göreli yol, byte boyutu ve SHA-256 özeti saklamak.
2. Restore sırasında çıkarılan bütün dosyaların manifestle birebir eşleşmesini ve hash/byte boyutlarını doğrulamak.
3. Eski `DogruFaturaBackup/1` yedekleri için güvenli geriye uyumluluk stratejisini belirlemek; yeni yedekleri daha güçlü manifest formatında üretmek.
4. `ArchivedInvoiceList` modeline arşiv PDF bütünlük alanı eklemek.
5. PDF üretimi/kapatma/senkronizasyon akışlarında SHA-256 kaydetmek ve mevcut PDF'yi doğrulamak.
6. Hash uyuşmazlığı, eksik dosya ve değiştirilmiş PDF senaryolarını otomatik testlerle korumak.
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

## Yeni sohbete aktarım özeti

Yeni bir ChatGPT sohbetine geçilirse bu dosyanın tamamını yapıştırın ve şu talimatı ekleyin:

> “Bu MD, DoğruFatura 1.1 geliştirmesinde önceki sohbetten kalan tek doğru durum kaydıdır. GitHub reposu `mcalimlier/DogruFatura`, çalışma branch'i `hardening/1.1-data-safety`. MD'deki tamamlanan işleri yeniden yapma. `Sıradaki iş` bölümünden devam et ve her tamamlanan parçadan sonra bu MD'yi hem repoda güncelle hem de sohbet içinde tam metin olarak ver.”

Kritik bağlam:

- Repo: `mcalimlier/DogruFatura`
- Branch: `hardening/1.1-data-safety`
- `main` başlangıç noktası: `72963ec0a829f2e4b487393160c7ca93290b470f`
- Son tamamlanan kod parçası: **Aşama 1.3**
- Son kod commit'i: `d82140b8fa31b5e10406d830010df2119cf129c4`
- Sıradaki parça: **Aşama 1.4 — Yedek ve PDF bütünlük doğrulaması**
- CI durumu: GitHub Actions job'ları runner/step başlamadan failure oluyor; gerçek derleme/test sonucu henüz alınamıyor.
- `main` ile henüz merge yapılmayacak.

## Notlar

- Mevcut kullanıcı verisini otomatik silen veya geri döndürülemez biçimde dönüştüren değişiklik yapılmayacak.
- Otomatik şema yükseltmelerinde geri dönüş kopyası olmadan kaynak dosya dönüştürülmeyecek.
- Restore işleminde semantik doğrulama tamamlanmadan canlı veri klasörlerine dokunulmayacak.
- Her parça bağımsız ve geri alınabilir commit olarak tutulacak.
- Her tamamlanan parçadan sonra bu dosya mutlaka güncellenecek.
- Her tamamlanan parçadan sonra bu dosyanın **tam güncel içeriği sohbet içinde de kullanıcıya verilecek**.
- CI runner sorunu çözülmeden hiçbir aşama için “CI doğrulandı” ifadesi kullanılmayacak.
