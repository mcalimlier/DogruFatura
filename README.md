# DoğruFatura 1.0.0

DoğruFatura; fatura ödeme listelerinin hazırlanması, hesap kontrolü, PDF çıktısı, HYS arşiv yönetimi ve kullanıcı verisi güvenliği için geliştirilmiş .NET 8 / WPF Windows masaüstü uygulamasıdır.

## 1.0.0 Final

- Elektrik, telefon, internet ve su faturaları için merkezi hesaplama ve doğrulama kuralları
- Kurum/hizmet numarası yönetimi, mükerrer fatura engelleme ve Kontrol Merkezi
- A4 yatay PDF önizleme, kaydetme ve yazdırma
- Aktif PDF → HYS arşivi → kapanışı geri alma belge akışı
- Eski DoğruFatura PDF'lerini güvenli arşiv içe aktarma
- Masaüstü `DoğruFatura` altında merkezi Veri, Yedekler, Loglar, Aktif Çıktılar ve Arşivlenenler yapısı
- Bütünlük kontrolü, manuel güvenlik yedeği, güncelleme öncesi yedek ve doğrulanmış geri yükleme
- Beklenmeyen kapanma algılama ve sonraki açılışta koruyucu güvenlik yedeği
- Fatura/PDF içermeyen gizlilik odaklı Tanılama Paketi
- Klavye erişilebilirliği, yüksek kontrast, responsive/DPI koruması ve liste sanallaştırma

## Kalite ve dağıtım

GitHub Actions; Release build, otomatik testler, PDF regresyon kontrolleri, NuGet güvenlik taraması, coverage çıktısı ve Windows Setup smoke testini yürütür. Final dağıtım paketinde Authenticode kod imzası zorunludur; ayrıca SHA-256 doğrulama dosyası üretilir. Ayrıntılar `CODE_SIGNING.md` ve `RELEASE_NOTES_1.0.0.md` dosyalarındadır.
