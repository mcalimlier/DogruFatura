# DoğruFatura 1.2.0

Güncel panel düzeni: salt okunur aktif liste vitrini, kartlarla ayrılan fatura girişi ve hesaplama, Fatura Listesi içinde ayrı PDF/Yazdır/Klasör işlemleri, PDF ve imza bilgileriyle paket kaydı, HYS bekleyen/kapalı paket sekmeleri ve Excel benzeri arşiv tablosu. Güvenlik ile Hakkında/Ayarlar ayrıdır. Ekran profilleri 1024×768, 1366×768, 1920×1080'dir ve Uygula ile etkinleştirilir. Ayrı Raporlar/Kontrol Merkezi sayfaları, önizleme, oturum uyarısı ve Temizle düğmeleri kaldırılmıştır. Güncel kullanım: [Kullanıcı kılavuzu](KULLANICI_KILAVUZU.md).

DoğruFatura; fatura ödeme listelerinin hazırlanması, mali hesap kontrolü, PDF/çıktı üretimi, HYS arşiv yönetimi, istatistik analizi ve kullanıcı verisi güvenliği için geliştirilmiş .NET 8 / WPF Windows masaüstü uygulamasıdır.

## 1.2.0 Kararlı Sürüm

- Su faturalarında belediyeye ödeme için KDV ve damga sıfır, şirketlere ödeme için standart hesaplama.
- Kaydet ve Yeni Fatura sonrasında kurum, fatura türü, tarihler ve tutarlar dâhil giriş kartının tamamı sıfırlanır; kaydedilen faturalar listede korunur.
- Lacivert/altın kartlar, uyarlanabilir üç ekran profili ve HYS paket yönetimi.
- Herkese açık kurulum okul/kurum, imzacı, aktif fatura ve arşiv kaydı içermez. İlk açılışta kurum ve imzacı tanımları istenir.
- Kişisel tam verili kurulum ayrı hazırlanır ve herkese açık release'e yüklenmez.

Önceki sürüm notları:

## 1.1.0 Final

RC1 kullanıcı doğrulaması tamamlandı ve sürüm numarası 1.1.0 final durumuna geçirildi. Dağıtım paketi yalnız son final build/test/setup kapısı yeşil olduğunda yayınlanmalıdır.

### 1.1 öne çıkanlar

- Tarihsel DoğruFatura PDF arşiv içe aktarmada ilerleme ve iptal desteği
- Arşiv tabanlı İstatistik ekranı; kurum, yıl, tarih ve fatura türü filtreleri
- Fatura türü dağılım grafiği ve PDF / yazdır / CSV / Excel / PNG çıktıları
- Düzenli Türkçe Excel uyumlu CSV biçimi
- Boş fatura numaralarında olası mükerrer kayıt uyarısı
- Güçlendirilmiş Tanılama Paketi kişisel veri maskelemesi
- DoğruFatura PDF'leri için ortak `DogruFaturaPdf/1` metadata sözleşmesi
- Veri güvenliği ekranında sadeleştirilmiş yedek / geri yükleme / bütünlük akışı
- Veri şema migrasyonu, vergi kural metadata'sı ve arşiv bütünlük sertleştirmeleri

## Temel güvenlik yaklaşımı

- Kullanıcı verileri Program Files altında tutulmaz.
- Kalıcı Veri, Yedekler, Loglar ve PDF arşivi Masaüstü `DoğruFatura` kökü altında korunur.
- Güncelleme ve kaldırma işlemleri kullanıcı veri klasörünü bilinçli olarak silmez.
- Tanılama ZIP'i fatura JSON/PDF içeriklerini taşımaz; loglardaki yaygın kişisel veri örüntülerini maskeler.
- Yedekten geri yüklemeden önce mevcut durum için koruyucu yedek alınır.

## Kalite ve dağıtım

GitHub Actions; Release build, otomatik testler, NuGet güvenlik taraması, PDF regresyon kontrolleri ve Windows Setup smoke testini yürütür. Final dağıtım paketi sürüm eki olmadan `1.1.0` olarak üretilir ve SHA-256 ile doğrulanır.

Ayrıntılar:
- `RELEASE_NOTES_1.1.0.md`
- `QUALITY_CHECKLIST_1.1_FINAL.md`
- `KULLANICI_KILAVUZU.md`
- `CODE_SIGNING.md`
