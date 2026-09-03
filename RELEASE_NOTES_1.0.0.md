# DoğruFatura 1.0.0 — Yayın Notları

DoğruFatura 1.0.0; fatura ödeme listelerinin hazırlanması, hesap kontrolü, PDF çıktısı, HYS arşiv yönetimi ve kullanıcı verisi güvenliği için hazırlanmış Windows masaüstü sürümüdür.

## Öne çıkanlar

- Elektrik, telefon, internet ve su faturaları için merkezi hesaplama ve doğrulama kuralları.
- Kurum/hizmet numarası yönetimi, mükerrer fatura engelleme ve Kontrol Merkezi.
- A4 yatay PDF önizleme, kaydetme ve yazdırma; sağ/sol kenar 0,75 cm ortak çıktı düzeni.
- Aktif PDF → HYS arşivi → kapanışı geri alma belge akışı.
- Eski DoğruFatura PDF'lerini `ARŞİV EKLE` ile içe aktarma; kaynak dosya korunur.
- Masaüstü `DoğruFatura` altında merkezi Veri, Yedekler, Loglar, Aktif Çıktılar ve Arşivlenenler yapısı.
- Bütünlük kontrolü, manuel güvenlik yedeği, güncelleme öncesi yedek ve doğrulanmış geri yükleme.
- Beklenmeyen kapanma algılama ve sonraki açılışta koruyucu güvenlik yedeği.
- Fatura/PDF içermeyen gizlilik odaklı Tanılama Paketi.
- Klavye kısayolları, geliştirilmiş TAB sırası, yüksek kontrast uyumu ve ekran/DPI korumaları.
- Büyük listelerde DataGrid sanallaştırma ve boş durum açıklamaları.

## Kalite güvencesi

1.0.0 yayın adayı GitHub Actions üzerinde Windows Release derlemesi, otomatik birim/entegrasyon/PDF regresyon testleri ve gerçek sessiz Setup smoke testi ile denetlenir. Setup smoke testi temiz kurulum, mevcut kurulum üstüne güncelleme, eski program artığı temizleme, kullanıcı verisini koruma ve kaldırma sonrası kullanıcı verisinin kalması senaryolarını kapsar.

## Dağıtım

Final paket `DogruFatura_Setup_1.0.0_x64.exe` olarak hazırlanır ve yanında SHA-256 doğrulama dosyası bulunur. Kod imzalama sertifikası sağlandığında uygulama EXE ve Setup Authenticode/SHA-256 ile otomatik imzalanabilir.

## Kullanıcı verisi

Kurulum veya kaldırma işlemleri `Masaüstü\DoğruFatura` altındaki kullanıcı verilerini, yedekleri, logları ve PDF belgelerini silmez.

> Bu dosya RC1 boyunca yayın notu taslağıdır. `VersionSuffix` kaldırılıp v1.0.0 onaylanmadan final sürüm olarak değerlendirilmez.
