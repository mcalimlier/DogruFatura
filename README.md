# DoğruFatura 1.1.0 RC1

DoğruFatura; fatura ödeme listelerinin hazırlanması, mali hesap kontrolü, PDF/çıktı üretimi, HYS arşiv yönetimi, istatistik analizi ve kullanıcı verisi güvenliği için geliştirilmiş .NET 8 / WPF Windows masaüstü uygulamasıdır.

## 1.1.0 RC1

Bu dal 1.1 final öncesi release adayıdır. Final paket, son Windows smoke turu ve 126/126 otomatik test kapısı doğrulandıktan sonra hazırlanacaktır.

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

GitHub Actions; Release build, otomatik testler, NuGet güvenlik taraması, PDF regresyon kontrolleri ve Windows Setup smoke testini yürütür. RC Setup yalnız test amacıyla üretilebilir; final dağıtımda sürüm eki kaldırılır ve dağıtım bütünlüğü SHA-256 ile doğrulanır.

Ayrıntılar:
- `RELEASE_NOTES_1.1.0.md`
- `QUALITY_CHECKLIST_1.1_RC1.md`
- `KULLANICI_KILAVUZU.md`
- `CODE_SIGNING.md`
