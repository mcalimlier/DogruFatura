# DoğruFatura 1.1 — Geliştirme Durumu

Bu dosya DoğruFatura 1.1 sağlamlaştırma çalışmalarının kalıcı ilerleme kaydıdır. Her tamamlanan geliştirme parçasından sonra güncellenir.

## Çalışma dalı

`hardening/1.1-data-safety`

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

- [ ] Bozuk ana veri dosyasının otomatik olarak boş/varsayılan dosyayla ezilmesini kaldır.
- [ ] Bozuk dosyayı güvenli karantinaya al.
- [ ] Uygulamayı kullanılabilir veriyle açarken kullanıcıya kurtarma durumunu açıkça bildir.
- [ ] Arşivde mümkünse sağlam kayıtları kurtar, yalnız hatalı kayıtları dışarıda bırak.
- [ ] Yeni davranışı otomatik testlerle koru.

### 1.2 Veri şema sürümleme

- [ ] Kalıcı JSON verileri için açık şema sürümü tanımla.
- [ ] Sürüm geçişlerini merkezi migration zincirine taşı.
- [ ] Eski sürüm dosyaları için geriye uyumluluk testleri ekle.

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

**Başlangıç kurulumu:** geliştirme dalı ve bu takip dosyası oluşturuldu.

## Sıradaki iş

**Aşama 1.1 — Bozuk veri kurtarma davranışı.** Önce `InvoiceDataStore`, `SchoolDataStore`, `AuthorityProfileDataStore` ve özellikle `ArchivedInvoiceListDataStore` üzerinde güvenli kurtarma davranışı uygulanacak; ardından ilgili otomatik testler eklenecek.

## Notlar

- Mevcut kullanıcı verisini otomatik silen veya geri döndürülemez biçimde dönüştüren değişiklik yapılmayacak.
- Her parça bağımsız ve geri alınabilir commit olarak tutulacak.
- Her tamamlanan parçadan sonra bu dosya mutlaka güncellenecek.
