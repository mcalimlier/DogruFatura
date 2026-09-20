# DoğruFatura 1.1.0 RC1 — Release Notları

**Durum:** Release Candidate 1  
**Hedef final sürüm:** 1.1.0  
**Platform:** Windows 10/11 x64, .NET 8 WPF

Bu paket 1.1 final öncesi son kullanıcı ve dağıtım doğrulaması içindir. Kullanıcı verileri üzerinde çalışan işlevler geriye uyum ve veri güvenliği önceliğiyle geliştirilmiştir.

## Başlıca yenilikler

### İstatistik ve çıktılar

- İşlem arşivinden kurum, yıl, tarih aralığı ve fatura türü bazında mali istatistik.
- Fatura Türü Dağılımı donut grafiği ve dağılım detayı.
- Tek **ÇIKTI AL** menüsünden PDF, Yazdır, CSV, Excel ve PNG.
- CSV çıktısında UTF-8, `;` ayraç ve Türkçe ondalık sayı biçimi.
- İstatistik ekranı tek görünümde daha kompakt hale getirildi; düşük öncelikli kartlar ekrandan çıkarıldı.

### Fatura doğrulama

- Fatura numarası dolu kayıtlarda mevcut kesin mükerrer engeli korunur.
- Fatura numarası boş kayıtlarda aynı kurum/hizmet, tür, tarihler ve tutarlarla benzer kayıt bulunursa olası mükerrer uyarısı gösterilir.
- Kullanıcı gerçek bir ayrı fatura olduğuna karar verirse açık onayla kayda devam edebilir.

### Tarihsel PDF arşiv içe aktarma

- Çoklu PDF işleme akışı asenkron çalışır.
- İlerleme bilgisi gösterilir.
- Kullanıcı aktarımı iptal edebilir.
- Mevcut HYS dosya adı ve PDF içerik doğrulamaları korunur.
- Eski metadata'sız DoğruFatura PDF'leri desteklenmeye devam eder.

### PDF standardizasyonu

- Yeni ortak metadata sözleşmesi: `DogruFaturaPdf/1`.
- Ödeme listesi ve istatistik PDF'leri ortak Title/Author/Subject/Creator/Keywords üretir.
- Metadata makine-okunur belge türü ve uygulama sürümü taşır.
- İstatistik filtrelerindeki kurum gibi kullanıcı verileri teknik PDF metadata'sına yazılmaz.

### Veri güvenliği ve destek

- Tanılama Paketi JSON/PDF kullanıcı içeriğini taşımaz.
- Loglarda kullanıcı/makine yolu, kurum/yetkili etiketleri, VKN/hizmet/fatura/HYS numarası, e-posta, GUID, IPv4 ve uzun sayısal kimlik örüntüleri maskelenir.
- Hakkında / Güvenlik ekranı sadeleştirildi; günlük işlemlerde yalnız bütünlük kontrolü, yedek oluşturma ve geri yükleme görünür.
- Tanılama Paketi **Gelişmiş / Destek** altında tutulur.
- Gereksiz klasör açma kısayolları kaldırıldı.

### Veri modeli ve bütünlük

- Kurum kayıtlarında kararlı `SchoolId` kimliği ve legacy uyumluluğu.
- Fatura kayıtlarında vergi kural sürümü/effective date/damga oranı metadata'sı.
- Arşiv snapshot/bütünlük kontrollerinde kapalı arşiv güvenliği.
- JSON veri şema migrasyonu ve yedek/geri yükleme korumaları.

## Kalite tabanı

RC1 hazırlığı öncesi son doğrulanan otomatik test tabanı: **126/126**.

Release adayı için tekrar doğrulanması gereken kapılar:
- Release build: 0 warning / 0 error
- Otomatik testler: 126/126
- PDF oluşturma ve tarihsel PDF regresyonu
- İstatistik görünüm ve çıktı smoke testi
- Güvenlik yedeği / geri yükleme
- Tanılama ZIP gizlilik kontrolü
- Setup temiz kurulum / güncelleme / kaldırma ve kullanıcı veri koruma smoke testi

## Bilinen yayın koşulu

Bu sürüm `1.1.0-rc1` olarak işaretlidir. Final `1.1.0` için:
1. `QUALITY_CHECKLIST_1.1_RC1.md` kritik hata olmadan tamamlanmalı.
2. CI build-and-test ve setup-smoke yeşil olmalı.
3. Son Windows kullanıcı smoke turu kabul edilmeli.
4. Sonrasında `VersionSuffix` boşaltılarak final Setup hazırlanmalıdır.
