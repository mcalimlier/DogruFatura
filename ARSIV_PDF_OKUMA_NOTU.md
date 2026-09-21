# Arşiv PDF Okuma Notu

DoğruFatura arşiv içe aktarma özelliğinde amaç, eski PDF'leri tek bir şablona göre değil içerdiği anlamlı alanlara göre okuyabilmektir.

## Temel yaklaşım

- Önce PDF'nin normal metin katmanı okunur.
- Fatura tablosu sütun başlıklarından tanınır.
- Eski/bozuk metin katmanında OCR fallback kullanılır.
- 90° dönük sayfalar da denenir.
- Okunan parasal değerler DoğruFatura vergi hesap motoruyla doğrulanmadan arşive kabul edilmez.

## Tanınan başlık örnekleri

- Okul Adı / Kurum Adı
- Hizmet Numarası / Hizmet No / Abone No / Tesisat No
- Fatura No / Fatura Numarası / Belge No
- Fatura Tarihi / Belge Tarihi
- Son Ödeme Tarihi / Vade Tarihi
- Ödenecek Tutar / Fatura Tutarı / Brüt Tutar
- KDV Tutarı / KDV
- Damga Vergisi / Damga Tutarı
- Ödeme Tutarı / Net Tutar / Net Ödeme

## Test örnekleri

Gerçek arşiv örnekleri:
- 2025 HYS PDF: metin katmanı okunabilir, iki fatura satırı içeriyor.
- 2026 HYS PDF: fatura listesi 90° dönük ve eski/bozuk font kodlamalı; OCR fallback gerektiriyor.

## Kural

Yeni bir eski PDF çeşidi çıktığında mevcut okuyucuyu tek belgeye özel sabit koordinatlarla yamamak yerine, yeni başlık eşanlamlıları ve semantik okuma kuralları eklenmelidir.


## Son test notu — 21.09.2026

Sentetik legacy PDF testinde bazı sayfa geometrilerinde sıra numarası okul adına taşarak `1 Tatar Ortaokulu` şeklinde okunabildi. Okuyucu artık kurum hücresinin başındaki saf sıra numarasını temizliyor. Bu düzeltme gerçek kurum adındaki diğer rakamlara dokunmuyor.

Durum: düzeltme kodlandı, 128/128 tekrar testi bekleniyor.


## 21.09.2026 — Son düzeltme

- Uzun fatura numarası dar başlık geometrisi nedeniyle merkez sınırını aşarsa sütun örtüşmesine göre ikinci okuma denenir.
- Damga vergisi ve ödeme tutarı içeride tam hassasiyetle korunur; PDF doğrulaması 2 ondalık para gösterimi üzerinden yapılır.
- Durum: kodlandı, 128/128 tekrar testi bekleniyor.


## 21.09.2026 — Otomatik test kapısı geçti

`TESTLERI_CALISTIR.bat` sonucu:

- Toplam: **128**
- Başarılı: **128**
- Başarısız: **0**
- Atlandı: **0**

Semantik legacy PDF okuyucu otomatik test seviyesinde yeşildir.

Sıradaki doğrulama sentetik PDF değil, gerçek arşiv belgeleridir:
- gerçek 2025 HYS PDF,
- gerçek 2026 dönük/bozuk metin katmanlı HYS PDF,
- önizleme penceresi kapatma X simgesi.

Bu gerçek smoke tamamlanmadan final kabul yapılmaz.


## 21.09.2026 — Gerçek 2023 MYS ve 2026 OCR düzeltmesi

Gerçek belgeler iki yeni tarihsel fark gösterdi:

- 2023 döneminde belge numarası `HYS` değil `MYS` olabiliyor.
- 2023 ödeme listesinde `Tüketici No`, `Toplam Tutar`, `Ele Geçen Miktar` gibi eski başlıklar bulunuyor ve son ödeme tarihi sütunu hiç bulunmayabiliyor.
- 2026 örneğinde PDF metin katmanı bozuk; OCR gerekiyor.
- OCR model dosyası Setup/Debug çıktısında `tessdata\tur.traineddata` altında olmasına rağmen motor uygulama köküne bakıyordu. Motor veri yolu `tessdata` klasörüne düzeltildi.

Okuyucu artık HYS ve tarihsel MYS belge numaralarını ayırt ederek korur. Son ödeme tarihi sütunu belgede hiç yoksa fatura tarihi teknik fallback olarak kullanılır ve bu durum önizleme açıklamasında açıkça belirtilir; mevcut bir sütunun okunamaması ise hata olarak kalır.

Durum: kodlandı; **131/131** otomatik test ve gerçek 2023/2026 smoke bekleniyor.


## 21.09.2026 — OCR sırasında görünür ilerleme

131/131 otomatik test başarılı olduktan sonra gerçek 2023 ve 2026 belgelerinde OCR işlemi kullanıcıya uzun süre tek bir sabit mesaj gösterdiği için işlem iptal edildi.

Düzeltme:
- OCR artık iç adımlarını ekranda canlı olarak bildirir.
- Normal metin katmanı okunabilir Ödeme Emri sayfaları OCR'da tekrar işlenmez.
- Yalnız OCR gereken bozuk/dönük sayfalar taranır.
- Arşive gerçek yazma başladığında ayrıca `PDF arşive kaydediliyor` durumu gösterilir.
- Yeni testte oluşan tek CA1307 analyzer uyarısı da temizlendi.

Durum: yeni kod için **131/131 + 0 warning** tekrar doğrulaması ve ardından gerçek 2023/2026 smoke bekleniyor.


## 21.09.2026 — 2023 para OCR ve tarama penceresi

2023 MYS gerçek PDF'de belge ve satır tanındı; kalan sorun para hücrelerinin eski tarama/OCR biçimiydi.

Eklenen toleranslar:
- düşmüş ondalık ayıracı için son iki rakam kuruş adayı,
- sık OCR harf/rakam karışmalarının para hücresinde kontrollü normalizasyonu,
- temizlenmiş tabloda SparseText OCR denemesi,
- satır veri kapsamasına göre daha iyi OCR adayının seçilmesi,
- Tutar + KDV ve en az bir basılı sonuç alanı hesap motoruyla doğrulanıyorsa diğer sonuç alanının güvenli hesaplanması.

Kullanıcı deneyimi:
- Teknik OCR adımları artık kullanıcıya gösterilmez.
- Küçük `ARŞİV PDF TARANIYOR` penceresi açılır.
- İndeterminate progress bar ve İPTAL düğmesi bulunur.
- Dosya sayısı/dosya adı sade biçimde gösterilir.

Beklenen otomatik test toplamı: **132**.
Gerçek 2023 smoke bekleniyor.


### 2023 regresyon kapsamı

Regresyon fixture'ı artık gerçek 2023 MYS listesindeki üç satırın tutarlarını ve toplam 2.484,91 ödeme değerini kapsıyor. Normal para biçimi ile OCR-benzeri bozuk para biçimi ayrı theory vakaları olarak doğrulanacak.


## 21.09.2026 — Ayıraçsız OCR para ölçeği düzeltmesi

132 testlik kapıda OCR-benzeri 2023 MYS varyantı yanlışlıkla `2.484,91` yerine `182.439,19` toplam üretti.

Neden:
- `161730` gibi OCR metni önce `161.730,00` olarak deneniyordu.
- Aynı ölçek hatası tüm para sütunlarında oluşunca oran doğrulaması yanlış büyük kombinasyonu geçirebiliyordu.

Yeni kural:
- Ayıraçsız, en az 3 haneli eski OCR para metninde önce son iki rakam kuruş kabul edilir.
- Örnek: `161730 -> 1.617,30`.
- Ham tam sayı yorumu yalnız fallback olarak kalır.
- Açık nokta/virgül içeren normal metin etkilenmez.

Aynı değişiklik iki analyzer uyarısını da temizler: `CA1870` ve `CA1861`.

Durum: **132/132 + 0 warning tekrar testi bekleniyor.**


## 21.09.2026 — Gerçek OCR satır rekonstrüksiyonu

Gerçek 2023/2026 smoke, yalnız karakter düzeltmesinin yeterli olmadığını gösterdi. Sparse OCR aynı fiziksel fatura satırındaki hücreleri farklı text-line gruplarına ayırabiliyor.

Yeni yaklaşım:
- Fatura Tarihi sütunundaki tarih satır ankrajıdır.
- Aynı yatay Y bandındaki hücreler tekrar tek semantik fatura satırında birleştirilir.
- OCR tarih karakterleri kontrollü normalize edilir.
- OCR aday seçiminde gerçek para/vergi tutarlılığı kullanılır.
- 2026'da gerçek Son Ödeme Tarihi varsa okunması tercih edilir; uydurma fallback yapılmaz.

Tarama penceresindeki İPTAL yazısı için pencere/düğme yüksekliği artırıldı ve düğme yerel template ile merkezlendi.

Durum: **132/132 + 0 warning ve gerçek 2023/2026 tekrar smoke bekleniyor.**


## 21.09.2026 — Test derleme düzeltmesi

Son test koşusunda `CS0103` ile yanlış fixture değişken adı ve `CA1859` analyzer uyarısı yakalandı.

- `useAlternativeHeaders` -> mevcut `alternative` parametresi
- `BuildInvoiceRows` -> `LineData[]` dönüş tipi

Durum: **132/132 + 0 warning tekrar testi bekleniyor.**


## 21.09.2026 — OCR komşu satır ayrımı

132 testlik kapıda OCR-benzeri 2023 MYS varyantında 3 beklenen faturadan 2'si okunuyordu.

Neden:
- Aynı satırdaki OCR hücrelerini toplamak için kullanılan Y tolerans bantları komşu fatura satırlarında üst üste gelebiliyordu.

Yeni kural:
- Her kelime yalnızca en yakın `Fatura Tarihi` satır ankrajına atanır.
- OCR'ın küçük dikey kaymaları tolere edilir.
- Komşu fatura satırlarının hücreleri birbirine karıştırılmaz.

Durum: **132/132 + 0 warning tekrar testi bekleniyor.**


## 21.09.2026 — Sparse OCR veri parçası / başlık ayrımı

132 testlik kapıda OCR-benzeri 2023 MYS varyantında 3 beklenen faturadan 2'si okunuyordu.

Neden:
- İlk fatura satırının tarih içermeyen OCR parçası, başlığa çok yakın olduğu için başlığın ikinci satırı sanılabiliyordu.
- Böylece header sınırı ilk fatura verisinin içine kadar kayıyordu.

Yeni kural:
- uzun yalnız-rakam token,
- uzun harf+rakam token,
- veya en az iki sayısal token

içeren yakın satırlar başlık devamı değil veri parçası kabul edilir.

Durum: **132/132 + 0 warning tekrar testi bekleniyor.**


## 21.09.2026 — Tek eksik OCR başlığı geometrik fallback

Gerçek 2023 MYS taramasında bütün tablo sütunları görselde mevcut olmasına rağmen OCR bazı modlarda sağdaki para başlıklarından birini atlayabiliyor.

Yeni güvenlik kuralı:
- 8 zorunlu semantik sütunun 7'si güvenilir bulunursa yalnız **tek** eksik sütun geometriden türetilebilir.
- İki komşu sütun arasındaki eksik merkez ara noktadan bulunur.
- Son `Ele Geçen Miktar` sütunu eksikse son bilinen merkez ile sayfa sağ kenarı arasından türetilir.
- 2 veya daha fazla temel sütun eksikse belge kabul edilmez.

Ayrıca `Ele Miktar` OCR alias'ı eklendi.

Beklenen test toplamı: **133**.
Durum: **133/133 + gerçek 2023 smoke bekleniyor.**
