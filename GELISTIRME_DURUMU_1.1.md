# DoğruFatura 1.1 — Geliştirme Durumu

Bu dosya DoğruFatura 1.1 sağlamlaştırma çalışmalarının kalıcı ilerleme kaydıdır. Her tamamlanan geliştirme parçasından sonra güncellenir. Başka bir sohbete geçildiğinde bu dosyanın tamamı yeni sohbete yapıştırılarak kaldığımız yer milimetrik biçimde aktarılabilir.

## Çalışma dalı

`hardening/1.1-data-safety`

> Bu dosya şu anda `main` dalında değil, yukarıdaki çalışma dalındadır. GitHub Desktop'ta görmek için **Fetch origin** yaptıktan sonra **Current branch** menüsünden `hardening/1.1-data-safety` dalına geçin.

## Genel hedef

DoğruFatura'nın mevcut işlevlerini ve kullanıcı akışını koruyarak veri güvenliği, arşiv bütünlüğü, mimari ayrışma, mali izlenebilirlik, CI kalite kapıları ve kullanıcı deneyimini güçlendirmek.

## Durum özeti

-  1.1 geliştirme dalı oluşturuldu.
-  Kalıcı ilerleme/durum dosyası oluşturuldu.
-  Aşama 1 — Veri Güvenliği (kod/test kapsamı tamamlandı; CI altyapı doğrulaması beklemede)
  -  1.1 Bozuk veri kurtarma davranışı
  -  1.2 Veri şema sürümleme
  -  1.3 Yedekten geri yükleme semantik doğrulaması
  -  1.4 Yedek ve PDF bütünlük doğrulaması
  -  1.5 Tek örnek / yazma güvenliği
-  Ek kullanıcı arayüzü düzeltmeleri
  -  Sağ üst küçült / büyüt-geri yükle / kapat simgeleri aynı optik ölçüye getirildi
  -  Karşılama ekranı yaklaşık 5,5 saniye görünür kalacak şekilde ayarlandı
  -  Karşılama ilerleme çubuğu ana pencere yükünden bağımsız akıcı hale getirildi
-  Yerel build doğrulaması
  -  İlk build'deki `SingleInstanceGuard.cs` / `IOException` derleme hatası düzeltildi
  -  `StartupSplashHost.cs` içindeki `CS4014` async uyarısı temizlendi
  -  `DogruFatura.Core` yerel Windows/.NET 8+ ortamında başarıyla derlendi
  -  `DogruFatura.Wpf` yerel Windows/.NET 8+ ortamında başarıyla derlendi
  -  `DERLE_CALISTIR.bat` programı başlatma adımına ulaştı
  -  Görsel/işlevsel manuel smoke kontrolü: splash, sağ üst simgeler, ikinci örnek engeli
-  Aşama 2 — Mimari Temizlik (başladı)
  -  Merkezi `InvoiceRulesEngine` oluşturuldu
  -  `InvoiceValidationService` merkezi motoru kullanıyor
  -  İlk Aşama 2 parçası yerel build ile doğrulandı
  -  Kontrol Merkezi entegrasyonu kodlandı; yerel build doğrulaması bekleniyor
-  Aşama 3 — Mali Denetim ve İzlenebilirlik
-  Aşama 4 — Kalite / CI
-  Aşama 5 — Kullanıcı Deneyimi

## Aşama 1 — Veri Güvenliği

### 1.1 Bozuk veri kurtarma davranışı

-  Bozuk ana veri dosyasının otomatik olarak boş/varsayılan dosyayla ezilmesi kaldırıldı.
-  Sorunlu dosya zaman damgalı koruma kopyasıyla saklanıyor.
-  Uygulama kullanılabilir geçici veriyle açılırken kurtarma durumu kullanıcıya açıkça bildiriliyor.
-  İşlem arşivinde sağlam kayıtlar kayıt bazında kurtarılıyor; hatalı/çakışan kayıtlar dışarıda bırakılıyor.
-  Aktif fatura ve kurum verisinde mümkün olan sağlam kayıtlar kayıt bazında kurtarılıyor.
-  Yetkili profili bozulduğunda varsayılan profil yalnız oturum içinde kullanılıyor; ana dosya otomatik ezilmiyor.
-  `RecoveryModeTests.cs` eklendi.
-  GitHub Actions doğrulaması — altyapı engeli nedeniyle beklemede.

### 1.2 Veri şema sürümleme

-  `SchemaVersion`, `DataType`, `Data` zarfı eklendi.
-  Migration zinciri `DataFileSchemaService` altında merkezileştirildi.
-  1.0.x çıplak JSON dosyaları Schema 0 olarak geriye uyumlu okunuyor.
-  Schema 0 -> 1 geçişinde iş verisi değiştirilmeden yeni zarfa taşınıyor.
-  Otomatik yükseltme öncesi `*_Schema0_Onceki_*.json` geri dönüş kopyası oluşturuluyor.
-  Geri dönüş kopyası oluşturulamazsa otomatik dönüşüm yapılmıyor.
-  Gelecek şemalar ve yanlış `DataType` güvenli biçimde reddediliyor.
-  Dört ana veri deposu yeni şema sistemine geçirildi.
-  `DataSchemaMigrationTests.cs` eklendi.
-  GitHub Actions doğrulaması — altyapı engeli nedeniyle beklemede.

### 1.3 Yedekten geri yükleme semantik doğrulaması

-  Merkezi `DataDirectoryValidationService.cs` eklendi.
-  Okul/kurum, aktif fatura, yetkili profili ve işlem arşivi restore öncesi iş kurallarıyla doğrulanıyor.
-  Aktif faturalarda boş/mükerrer `Guid`, mükerrer fatura ve karma fatura türü reddediliyor.
-  Arşivlerde mükerrer kimlik, arşiv numarası ve HYS reddediliyor.
-  Restore dört zorunlu veri dosyasının tamamını gerektiriyor.
-  Geçersiz yedek canlı klasörlere dokunulmadan reddediliyor.
-  `CheckIntegrity()` aynı semantik doğrulayıcıya geçirildi.
-  Şema-1 arşiv biçimiyle eski kök-dizi kontrolü arasındaki uyumsuzluk giderildi.
-  Restore güvenlik entegrasyon testleri genişletildi.
-  GitHub Actions doğrulaması — altyapı engeli nedeniyle beklemede.

### 1.4 Yedek ve PDF bütünlük doğrulaması

-  `FileIntegrityService.cs` eklendi.
-  Yeni yedek biçimi `DogruFaturaBackup/2` olarak tanımlandı.
-  Manifestte her dosyanın göreli yolu, byte boyutu ve SHA-256 özeti saklanıyor.
-  Restore öncesi ZIP içeriği manifestle birebir karşılaştırılıyor.
-  Eksik/fazladan dosya, mükerrer ZIP yolu, boyut uyuşmazlığı ve SHA-256 uyuşmazlığı restore'u engelliyor.
-  Eski `DogruFaturaBackup/1` yedekleri geriye uyumlu destekleniyor; kriptografik manifest olmadığı açıkça ayrılıyor.
-  `ArchivedInvoiceList.PdfSha256` eklendi ve doğrulanıyor.
-  PDF eşitleme legacy kayıtlara başlangıç hash'i yazıyor.
-  Kayıtlı hash ile mevcut PDF uyuşmazsa hash otomatik yenilenmiyor, bütünlük hatası oluşuyor.
-  Tarihsel PDF içe aktarımında hash kaydediliyor.
-  `CheckIntegrity()` ve restore arşiv PDF hashlerini doğruluyor.
-  `ArchivePdfIntegrityTests.cs` ve `BackupManifestIntegrityTests.cs` eklendi.
-  GitHub Actions doğrulaması — altyapı engeli nedeniyle beklemede.

### 1.5 Tek örnek / yazma güvenliği

-  Mevcut mutex fail-open davranışı kaldırıldı.
-  `SingleInstanceGuard.cs` ile tek örnek kararı test edilebilir servis/sonuç modeline ayrıldı.
-  Sonuçlar `Acquired`, `AlreadyRunning`, `Unavailable` olarak açıkça ayrılıyor.
-  İkinci uygulama örneği mevcutsa uygulama bilgi mesajıyla açılmadan kapanıyor.
-  Mutex altyapısı/izin hatası oluşursa uygulama veri yazma moduna geçmeden güvenli biçimde kapanıyor.
-  Başarılı mutex ediniminde sahiplik `SingleInstanceLease` ile yaşam döngüsü boyunca tutuluyor.
-  Normal kapanışta lease güvenli biçimde bırakılıyor.
-  Yedekten geri yükleme sonrası yeniden başlatma akışı mutex'i kontrollü biçimde serbest bırakıyor.
-  Lease `Dispose()` idempotent.
-  `SingleInstanceGuardTests.cs` ile edinim, zaten açık örnek ve mutex altyapı hatası testleri eklendi.
-  Fail-open yerine fail-closed güvenlik ilkesi uygulanıyor.
-  Yerel ilk build'de görülen `IOException` / `CS0103` hatası `using System.IO;` eklenerek düzeltildi.
-  Düzeltme sonrası WPF projesi yerel Windows/.NET 8 ortamında başarıyla derlendi.
-  GitHub Actions doğrulaması — Actions job'ları gerçek build başlamadan `steps=null` ile failure oluyor.

## Ek kullanıcı arayüzü düzeltmeleri — tamamlandı

### Sağ üst pencere düğmeleri

-  Eski durumda küçültme `—` karakteri 23.5 pt, kapatma `×` karakteri 27.5 pt, büyütme ise Segoe MDL2 Assets 16.5 pt kullanıyordu.
-  `WindowControlPolishService.cs` eklendi.
-  Küçült, büyüt/geri yükle ve kapat kontrolleri aynı `Segoe MDL2 Assets` simge ailesine geçirildi.
-  Küçült için `E921`, büyüt için mevcut `E922/E923`, kapat için `E8BB` Windows simgeleri kullanılıyor.
-  Üç kontrolün optik simge boyutu 16.5, buton ölçüsü 44×42 ve içerik hizası merkez olarak standardize edildi.
-  Servis `UiPolishService` üzerinden MainWindow yüklendiğinde uygulanıyor.
-  Görsel eşitlik kullanıcı tarafından çalışan EXE üzerinde manuel olarak teyit edilecek.

### Karşılama ekranı ve akıcı ilerleme

-  `StartupProgressProfile` minimum görünür süreyi 5500 ms olarak tanımlıyor.
-  Normal ilerleme 0 → %94 arasında ilk 5000 ms boyunca kesintisiz ilerliyor.
-  Çok uzun açılışlarda %94 → %98 arasında yavaş devam eden kuyruk animasyonu bulunuyor; %100 yalnız gerçek hazırlık tamamlanınca kullanılıyor.
-  `StartupSplashHost.cs` eklendi.
-  Splash penceresi ana MainWindow thread'inden bağımsız STA/Dispatcher thread'inde çalışıyor.
-  `MainWindow()` veri/arayüz hazırlığı ana thread'i meşgul etse bile splash spinner ve progress animasyonu ayrı Dispatcher'da akmaya devam ediyor.
-  MainWindow erken hazır olsa dahi splash yaklaşık 5,5 saniye dolmadan kapanmıyor.
-  MainWindow geç hazır olursa splash %98'e kadar kontrollü biçimde ilerlemeye devam ediyor ve hazır olunca son geçişi yapıyor.
-  Tamamlanma aşaması mevcut animasyon genişliğinden %100'e 450 ms EaseOut geçişle tamamlanıyor.
-  Yüzde metni yaklaşık 33 ms cadence ile zaman tabanlı güncelleniyor.
-  Spinner yaklaşık 0,9 saniyelik sürekli dönüşle çalışıyor.
-  `StartupProgressProfileTests.cs` yeni 5–6 saniye ve monoton progress davranışına göre güncellendi.
-  Bilinçli 5,5 saniyelik splash süresi `StartupPerformanceService` performans metriğine dahil edilmiyor; performans ölçümü MainWindow gerçek hazırlığı bittiğinde tamamlanıyor.
-  Yerel ilk build'deki `StartupSplashHost.cs` CS4014 uyarısı Dispatcher çağrısını gerçek await zincirine çevirerek temizlendi.
-  Splash süresi ve akıcılığı kullanıcı tarafından çalışan EXE üzerinde manuel olarak teyit edilecek.
-  GitHub Actions doğrulaması — `build-and-test` job'ı gerçek step başlatmadan failure oluyor.

## Yerel build doğrulama kaydı

### 17.09.2026 — İlk gerçek kullanıcı-PC derlemesi

Kullanıcı `GitHub Desktop → Show in Explorer → DERLE_CALISTIR.bat` yoluyla gerçek Windows/.NET derlemesi çalıştırdı.

İlk deneme:

-  `DogruFatura.Core` başarıyla derlendi.
-  Core tarafındaki 16 kayıt yalnız analyzer uyarısıydı; derlemeyi durdurmadı.
-  `DogruFatura.Wpf` ilk denemede 1 hata ve 42 uyarıyla durdu.
-  Tek gerçek hata: `SingleInstanceGuard.cs` içinde `IOException` adının çözülememesi (`CS0103`).
-  Hatanın nedeni: `System.IO` namespace'inin eksik olması.
-  Düzeltme commit'i: `26f3aa160d61a87ea8e6f4c7071e53ed9f8356f8`.
-  Aynı build çıktısındaki `StartupSplashHost.cs` `CS4014` uyarısı da ayrıca temizlendi.
-  Async uyarı düzeltme commit'i: `abff2d44ac6160c9a21e961df610d1c41388d00d`.

İkinci deneme — düzeltmeler sonrası:

-  Genel restore/build adımı başarıyla tamamlandı.
-  `DogruFatura.Core` 16 analyzer uyarısıyla başarıyla derlendi.
-  `DogruFatura.Wpf` 41 analyzer uyarısıyla başarıyla derlendi.
-  Toplam sonuç: 57 uyarı, **0 derleme hatası**, başarılı build.
-  `DERLE_CALISTIR.bat` `[3/3] DogruFatura baslatiliyor...` aşamasına ulaştı.
-  Yerel derleme doğrulaması tamamlandı.

### 17.09.2026 — Aşama 2 ilk parça yerel doğrulaması

-  `InvoiceRulesEngine` ve `InvoiceValidationService` entegrasyonu sonrası yeniden derleme yapıldı.
-  Kullanıcı ortamındaki SDK sürümü: `.NET SDK 10.0.400`.
-  `DogruFatura.Core` 18 analyzer uyarısıyla başarıyla derlendi.
-  `DogruFatura.Wpf` 41 analyzer uyarısıyla başarıyla derlendi.
-  Toplam sonuç: 59 uyarı, **0 derleme hatası**, başarılı build.
-  BAT dosyası `[3/3] DogruFatura baslatiliyor...` aşamasına ulaştı.
-  Önceki 57 uyarıya eklenen iki yeni kayıt yalnız analyzer önerisiydi: `InvoiceValidationService.cs` CA1826 ve `InvoiceRulesEngine.cs` CA1859.
-  Bu iki yeni uyarı sonraki Aşama 2 commit'inde temizlenecek; mevcut 57 legacy analyzer uyarısı Aşama 4 kapsamında ele alınacak.

Not: CA1014/CA1822/CA1859/CA1305/CA1307/CA1309/CA1512/CA1861/CA1806 gibi kayıtlar analyzer uyarılarıdır; build'i durdurmaz. Bunlar Aşama 4 kalite/uyarı temizliğinde sistematik olarak ele alınacaktır. Aşama 2'de eklenen yeni kodun warning borcunu artırmaması ayrıca korunacaktır.

## Aşama 2 — Mimari Temizlik

### 2.1 Merkezi fatura kural motoru — başladı

-  `InvoiceRulesEngine.cs` eklendi.
-  Desteklenen fatura türleri merkezi motora taşındı.
-  Kimlik, kurum, VKN, fatura türü, hizmet numarası, tarih ve vergi hesap bütünlüğü kuralları tek değerlendirme modelinde toplandı.
-  `InvoiceValidationService` mevcut kullanıcı hata sırasını/metinlerini koruyarak `InvoiceRulesEngine` sonucunu kullanmaya geçirildi.
-  `InvoiceRulesEngineTests.cs` eklendi.
-  İlk motor/validation entegrasyonu yerel build ile 0 hata sonucu doğrulandı.
-  İlk build'de yeni koddan gelen CA1826 ve CA1859 analyzer önerileri saptandı; sonraki committe temizleniyor.
-  Kontrol Merkezi'nin aynı motoru kullanması için entegrasyon kodlandı; yerel build/test doğrulaması bekleniyor.

### Aşama 2 kalan işleri

-  MainWindow sorumluluklarını ViewModel / application service katmanlarına ayır.
-  Dependency Injection kullan.
-  Merkezi InvoiceRulesEngine'i validation ve Kontrol Merkezi arasında tam ortak kaynak haline getir.
-  InvoiceType / InstitutionType / ArchiveStatus için enum veya value object kullan.
-  SchoolRecord için değişmez `SchoolId` ekle ve fatura-kurum bağını ID üzerinden kur.

## Aşama 3 — Mali Denetim ve İzlenebilirlik

-  Vergi hesaplama kural sürümü / yürürlük tarihi modeli ekle.
-  Append-only audit trail ekle.
-  Çok dosyalı arşiv işlemleri için transaction journal ekle.
-  Kapalı HYS arşivini değişmez belge modeliyle güçlendir.

## Aşama 4 — Kalite / CI

-  WPF katmanında warning-as-error kapısını yeniden etkinleştir.
-  Mevcut analyzer uyarılarını sınıflandır ve güvenli olanları temizle.
-  Yerel build'de görülen legacy analyzer uyarılarını kontrollü biçimde azalt.
-  Coverage için minimum kalite eşiği ekle.
-  Güvenlik/bağımlılık otomasyonunu güçlendir.
-  Sürüm/release belgelerini `VersionPrefix` üzerinden otomatikleştir.

## Aşama 5 — Kullanıcı Deneyimi

-  PDF içe aktarmayı async + ilerleme + iptal destekli yap.
-  Boş fatura numarasında olası mükerrer uyarısı ekle.
-  Tanılama paketi için içerik önizlemesi / daha güçlü kişisel veri temizleme ekle.
-  Yeni PDF'lere makine-okunabilir DoğruFatura metadata formatı ekle.

## Tamamlanan son parça

**Aşama 2 ilk parça — merkezi InvoiceRulesEngine + InvoiceValidationService entegrasyonu yerel build doğrulaması başarılı**

Doğrulanan kod commit'i: `12e8a80dbd18e24a5cabc7b4b2a239f67656eab8`

Doğrulama sonucu:

-  Core ve WPF yerel kullanıcı bilgisayarında başarıyla derlendi.
-  Derleme hatası yok.
-  Toplam 59 analyzer uyarısı görüldü; bunların 2'si yeni Aşama 2 kodundan geldi ve sonraki committe temizleniyor.
-  BAT dosyası programı başlatma aşamasına ulaştı.

### Doğrulama notu

Bu sonuç GitHub Actions değil, kullanıcının gerçek Windows bilgisayarında .NET SDK 10.0.400 ile yapılan yerel build doğrulamasıdır. GitHub Actions runner sorunu ayrıca devam etmektedir; bu nedenle “yerel build doğrulandı” denebilir ancak “CI doğrulandı” denmeyecek.

## Sıradaki iş

**Aşama 2 — Kontrol Merkezi'ni `InvoiceRulesEngine` ile ortaklaştıran commit'in yerel doğrulaması.**

Doğrulama:

1. GitHub Desktop'ta `hardening/1.1-data-safety` branch'inde Fetch/Pull yap.
2. `DERLE_CALISTIR.bat` çalıştır.
3. Derleme özetinde `0 error` doğrula.
4. Yeni koddan gelen CA1826 ve CA1859 uyarılarının kaybolduğunu; toplam warning sayısının yeniden legacy tabana yaklaşmasını kontrol et.
5. Program açıldığında Kontrol Merkezi'ni aç ve mevcut kayıtların kontrol taramasının çalıştığını kontrol et.
6. Bu kontrol uygunsa merkezi kural motoru + Kontrol Merkezi entegrasyonunu tamamlandı kabul et ve sonraki mimari parçaya geç.

## GitHub Desktop'ta çalışmaları görme

1. GitHub Desktop'ta `DogruFatura` deposunu seçin.
2. **Fetch origin** düğmesine basın.
3. **Current branch** menüsünden `hardening/1.1-data-safety` dalını seçin.
4. **Pull origin** görünüyorsa basın.
5. **History** sekmesinde commit'leri görebilirsiniz.
6. **Repository > Show in Explorer** ile dosyaları açabilirsiniz.
7. `main` farkı için **Branch > Compare to Branch... > main** kullanın.
8. Manuel smoke kontrolü tamamlanana kadar branch'i `main` ile birleştirmeyin.

## Programı GitHub Desktop üzerinden yerelde derleme ve çalıştırma

Visual Studio kullanılmayacak. Standart yerel kontrol akışı:

1. GitHub Desktop → **Fetch origin**.
2. `hardening/1.1-data-safety` branch'ini seç.
3. **Pull origin**.
4. **Repository > Show in Explorer**.
5. Repo kökündeki `DERLE_CALISTIR.bat` dosyasına çift tıkla.
6. BAT dosyası .NET SDK'yı kontrol eder, restore yapar, `DogruFatura.Wpf` projesini Debug modunda derler ve başarılıysa programı otomatik çalıştırır.

Başarılı build sonrası EXE yolu:

`DogruFatura.Wpf\bin\Debug\net8.0-windows\DogruFatura.exe`

Release yolu:

`DogruFatura.Wpf\bin\Release\net8.0-windows\DogruFatura.exe`

Build hata verirse siyah pencereyi kapatmadan ilk `error` satırını ve mümkünse özet bölümünü sohbete gönderin.

## Yeni sohbete aktarım özeti

Yeni bir sohbete geçilirse bu dosyanın tamamını yapıştırın ve şu talimatı ekleyin:

> “Bu MD, DoğruFatura 1.1 geliştirmesinde önceki sohbetten kalan tek doğru durum kaydıdır. GitHub reposu `mcalimlier/DogruFatura`, çalışma branch'i `hardening/1.1-data-safety`. MD'deki tamamlanan işleri yeniden yapma. `Sıradaki iş` bölümünden devam et ve her tamamlanan parçadan sonra bu MD'yi hem repoda güncelle hem de sohbet içinde tam metin olarak ver.”

Kritik bağlam:

-  Repo: `mcalimlier/DogruFatura`
-  Branch: `hardening/1.1-data-safety`
-  `main` başlangıç commit'i: `72963ec0a829f2e4b487393160c7ca93290b470f`
-  Aşama 1'in 1.1–1.5 kod/test kapsamı tamamlandı.
-  Sağ üst simge eşitlemesi tamamlandı; manuel görsel teyit bekliyor.
-  Yaklaşık 5,5 saniyelik akıcı splash tamamlandı; manuel görsel teyit bekliyor.
-  Tek örnek güvenliği tamamlandı.
-  Yerel build doğrulaması başarılı; ilk Aşama 2 motor/validation parçası da 0 hatayla derlendi.
-  Son doğrulanan kod commit'i: `12e8a80dbd18e24a5cabc7b4b2a239f67656eab8`.
-  Son yerel build: 0 hata, 59 analyzer uyarısı; 2 yeni analyzer uyarısı sonraki committe temizleniyor.
-  Sıradaki işlem: Kontrol Merkezi'nin `InvoiceRulesEngine` entegrasyonu için yeni commit'i yerelde derleyip Kontrol Merkezi smoke kontrolü yapmak.
-  CI durumu: GitHub Actions job'ları runner/step başlamadan failure oluyor; gerçek CI build/test sonucu alınamıyor.
-  `main` ile henüz merge yapılmayacak.

## Notlar

-  Kullanıcı verisini otomatik silen veya geri döndürülemez dönüştüren değişiklik yapılmayacak.
-  Restore hash/manifest ve semantik doğrulama bitmeden canlı verilere dokunmayacak.
-  PDF hash uyuşmazlığı otomatik olarak “yeni doğru hash” şeklinde kabul edilmeyecek.
-  Tek örnek güvenliği fail-closed çalışacak; kilit güvenli kurulamıyorsa yazma modunda uygulama açılmayacak.
-  Splash görsel süresi performans ölçümünden ayrı tutulacak.
-  Kullanıcı yerel derleme için Visual Studio yerine GitHub Desktop + `DERLE_CALISTIR.bat` akışını kullanacak.
-  Yerel build doğrulaması ile GitHub Actions CI doğrulaması ayrı statüler olarak tutulacak.
-  Analyzer uyarıları Aşama 4'te kontrollü olarak ele alınacak; davranış değiştiren toplu “uyarı temizliği” yapılmayacak.
-  Aşama 2'de eklenen kod mevcut analyzer warning borcunu artırmadan ilerletilecek.
-  Her tamamlanan parçadan sonra bu dosya mutlaka repoda güncellenecek.
-  Her tamamlanan parçadan sonra dosyanın tam güncel içeriği sohbet içinde de kullanıcıya verilecek.
-  CI runner sorunu çözülmeden hiçbir aşama için “CI doğrulandı” ifadesi kullanılmayacak.
