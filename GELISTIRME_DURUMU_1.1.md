# DoğruFatura 1.1 — Geliştirme Durumu

Bu dosya DoğruFatura 1.1 sağlamlaştırma çalışmalarının kalıcı ilerleme kaydıdır. Her tamamlanan geliştirme parçasından sonra güncellenir.

## Çalışma dalı

`hardening/1.1-data-safety`

> Bu dosya şu anda `main` dalında değil, yukarıdaki çalışma dalındadır. GitHub Desktop'ta görmek için **Fetch origin** yaptıktan sonra **Current branch** menüsünden `hardening/1.1-data-safety` dalına geçin.

## Genel hedef

DoğruFatura'nın mevcut işlevlerini ve kullanıcı akışını koruyarak veri güvenliği, arşiv bütünlüğü, mimari ayrışma, mali izlenebilirlik ve CI kalite kapılarını güçlendirmek.

## Durum özeti

- [x] 1.1 geliştirme dalı oluşturuldu.
- [x] Kalıcı ilerleme/durum dosyası oluşturuldu.
- [ ] Aşama 1 — Veri Güvenliği
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
- [ ] GitHub Actions ile derleme/test doğrulaması — **altyapı engeli nedeniyle beklemede**. Workflow job'ları runner atanamadan (`runner_id=0`) ve hiçbir step başlamadan birkaç saniyede failure oluyor; bu nedenle şu an kod/test sonucu üretilemiyor.

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
- [ ] GitHub Actions ile derleme/test doğrulaması — **runner altyapısı nedeniyle beklemede**. Son denemede de `windows-latest` job'u hiçbir step başlamadan `runner_id=0` ile kapandı.

### 1.3 Yedekten geri yükleme semantik doğrulaması

- [ ] JSON parse kontrolüne ek olarak model/iş kuralı doğrulaması yap.
- [ ] Kurum, aktif fatura, yetkili profili ve işlem arşivini geri yükleme öncesinde doğrula.
- [ ] Geçersiz yedeğin mevcut veriyi değiştirmesini engelle.

### 1.4 Yedek ve PDF bütünlük doğrulaması

- [ ] Yedek manifestine dosya SHA-256 özetleri ve boyutlarını ekle.
- [ ] Restore sırasında manifest hash doğrulaması yap.
- [ ] Kapalı arşiv PDF'leri için SHA-256 parmak izi sakla ve doğrula.

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

**Aşama 1.2 — Veri şema sürümleme:**

- Yeni `DataFileSchemaService.cs` ile kalıcı JSON formatının sürümü tek merkezden yönetiliyor.
- Yeni dosya biçimi `SchemaVersion`, `DataType` ve `Data` alanlarını kullanıyor.
- Eski 1.0.x dosyaları Schema 0 olarak tanınıyor ve doğrulama başarılıysa Schema 1'e yükseltiliyor.
- Her otomatik yükseltmeden önce eski dosyanın birebir geri dönüş kopyası saklanıyor.
- Gelecek sürüm şemaları veya yanlış veri türleri otomatik dönüştürülmüyor ve kaynak dosya korunuyor.
- `DataSchemaMigrationTests.cs` ile geriye uyumluluk ve güvenli yükseltme senaryoları eklendi.

### Doğrulama notu

17.09.2026 tarihindeki son branch workflow denemesinde `build-and-test` job'u yine runner atanmadan (`runner_id=0`) ve `steps=[]` durumunda birkaç saniye içinde failure oldu. Bu nedenle Aşama 1.2 kod/test kapsamı tamamlandı ancak GitHub Actions CI doğrulaması henüz tamamlanmış sayılmıyor.

## Sıradaki iş

**Aşama 1.3 — Yedekten geri yükleme semantik doğrulaması.** `ApplicationSafetyService` yalnızca JSON sözdizimini kontrol etmek yerine yedekteki `Okullar`, `AktifFaturalar`, `YetkiliProfili` ve `IslemArsivi` verilerini model ve iş kurallarıyla doğrulayacak. Geçersiz yedek mevcut canlı verinin üzerine hiçbir şekilde geçirilmeden reddedilecek ve bu davranış entegrasyon testleriyle korunacak.

## GitHub Desktop'ta çalışmaları görme

1. GitHub Desktop'ta `DogruFatura` deposunu seçin.
2. Üst bölümde **Fetch origin** düğmesine basın.
3. **Current branch** menüsünü açın.
4. `hardening/1.1-data-safety` branch'ini seçin. İlk kez görünüyorsa **Other Branches / Remote Branches** bölümünden seçin.
5. Branch açıldıktan sonra **History** sekmesinde yaptığım commit'leri görebilirsiniz.
6. Dosyaları diskte görmek için **Repository > Show in Explorer** seçeneğini kullanın. Kök dizinde `GELISTIRME_DURUMU_1.1.md` görünecektir.
7. Kodun mevcut `main` sürümünden farkını görmek için GitHub Desktop'ta **Branch > Compare to Branch... > main** yolunu kullanabilirsiniz.
8. Bu branch'i şimdilik `main` ile birleştirmeyin; veri güvenliği aşamasının tamamını bitirip doğrulama yaptıktan sonra birlikte merge edeceğiz.

## Notlar

- Mevcut kullanıcı verisini otomatik silen veya geri döndürülemez biçimde dönüştüren değişiklik yapılmayacak.
- Otomatik şema yükseltmelerinde geri dönüş kopyası olmadan kaynak dosya dönüştürülmeyecek.
- Her parça bağımsız ve geri alınabilir commit olarak tutulacak.
- Her tamamlanan parçadan sonra bu dosya mutlaka güncellenecek.
- CI runner sorunu çözülmeden hiçbir aşama için "CI doğrulandı" ifadesi kullanılmayacak.
