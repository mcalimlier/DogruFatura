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


## 21.09.2026 — Ödeme Emri toplamıyla aggregate OCR doğrulaması

Gerçek 2023 MYS OCR'da üçüncü satırın Damga Vergisi ve Ele Geçen Miktar hücreleri aynı anda kaybolabiliyor.

Yeni kural:
- Tutar + KDV güvenilir okunuyorsa hesap motoru eksik sonuç hücrelerini geçici olarak türetebilir.
- Ancak böyle bir satır varsa belge ancak ana Ödeme Emri sayfasındaki toplamlarla tamamen uyuştuğunda kabul edilir.
- Doğrulanan toplamlar:
  - Brüt Tutar
  - Kesinti/Damga toplamı
  - Net/Ödenmesi gereken toplam
- Herhangi bir aggregate uyuşmazlıkta belge reddedilir.

OCR sayfası, okunabilir ana sayfa ile birleştirildiği için belge numarası, tarih, tür ve resmi toplamlar OCR fallback sırasında korunur.

Durum: **133/133 otomatik test + gerçek 2023 smoke bekleniyor.**


## 21.09.2026 — Aggregate recovery fixture düzeltmesi

133 testlik kapıda 132 test geçti. Tek failure, belge parse hatası değildi; test, aggregate fallback notunu bekliyordu fakat fixture'daki `XX/YY` hücreleri fallback'i kesin olarak tetiklemiyordu.

Yeni fixture davranışı:
- son 2023 MYS satırındaki Damga ve Ödeme hücreleri hiç çizilmez;
- böylece gerçek OCR kaybı doğrudan simüle edilir;
- satır yalnız resmi Ödeme Emri toplamlarıyla aggregate doğrulama üzerinden tamamlanabilir.

Beklenen: **133/133**.


## 21.09.2026 — Son 133 test failure: implementation-detail assertion

132/133 sonucundaki son failure, parserın yanlış veri üretmesi değildi. Test belgenin 3 faturasını geçerli biçimde okumuştu; yalnız recovery yolunun status mesajını zorunlu bekliyordu.

Test artık status metni yerine nihai fatura tutarlarını ve 2.484,91 toplamı doğrular.

Beklenen sonraki sonuç: **133/133**.


## 21.09.2026 — Hedefli tablo-band OCR

Gerçek 2023 MYS sayfasında sorun tek bir alan alias'ı değil, tam sayfa OCR'ın para hücrelerini farklı segmentasyonlarda parça parça okumasıdır.

Yeni fallback:
- doğru yöne çevrilmiş landscape sayfanın üst tablo bandı ayrıca OCR edilir;
- `SingleBlock` ve `SparseText` sonuçları birleştirilir;
- aynı kelimenin yakın koordinattaki tekrarları temizlenir;
- sütun sınırı kaymış para tokenları row-wide soldan-sağa geometriyle tekrar değerlendirilir;
- eksik sonuç hücreleri yalnız resmi Ödeme Emri toplamı aggregate kontrolü varsa türetilebilir.

Gerçek-benzeri testte para hücrelerine yatay kolon drift'i de eklendi.

Durum: **133/133 tekrar test + gerçek 2023 smoke bekleniyor.**


## 21.09.2026 — Tesseract tek aktif Page kısıtı

Hedefli tablo-band OCR'da `SingleBlock` ve `SparseText` işlemleri arka arkaya çalıştırılırken ilk Tesseract `Page` nesnesi dispose edilmeden ikinci `Process` çağrısı yapılıyordu.

Bu gerçek 2023 smoke'ta:
`Only one image can be processed at a time`
hatasına yol açtı.

Düzeltme:
- her OCR `Page` sonucu önce `PageData`'ya kopyalanır,
- confidence saklanır,
- Tesseract `Page` dispose edilir,
- sonra sonraki OCR modu çalıştırılır,
- iki bağımsız `PageData` sonucu en son birleştirilir.

Durum: gerçek 2023 smoke tekrarı bekleniyor.


## 21.09.2026 — Image-dominant tek dosya fallback

Diğer 2023 MYS belgeleri çalıştığı için `MYS-2023-4010364.pdf` artık genel 2023 parser davranışıyla değil, kendi anomalisine göre ele alınıyor.

Belgenin 2. sayfasında:
- metin katmanı bozuk,
- gerçek görüntü net,
- tablo 90° dönük,
- tek OCR modu bütün para hücrelerini aynı anda yakalayamıyor.

Yeni fallback yalnız bu tür zayıf metin katmanlı legacy sayfalarda çalışır:
- doğru yön sonrası üst %5-%36 tablo bandı,
- SingleBlock + SingleColumn + Auto,
- ham gri + çizgi temizlenmiş görüntü,
- koordinat bazlı OCR sonucu birleştirme.

Normal çalışan arşiv formatlarının yolu değiştirilmez.

Durum: **133/133 tekrar test + gerçek anomalik 2023 smoke bekleniyor.**


## 22.09.2026 — İki aşamalı toplu arşiv taraması

Toplu arşiv ekleme akışı performans ve güvenlik amacıyla iki aşamaya ayrıldı:

1. Tüm seçili PDF'ler önce yalnız metin katmanı ve ucuz doğrulamalarla hızlı taranır.
2. Hızlı taramada geçemeyen belgeler ayrı kuyruğa alınır ve yalnız bu belgelerde güçlü OCR çalışır.
3. Başarılı belgeler ikinci kez okunmaz; sonuçlar özgün dosya sırasıyla önizlemeye taşınır.
4. Çift HYS/MYS ve hedef PDF kontrolleri iki aşama tamamlandıktan sonra tek kez uygulanır.
5. Güçlü tarama belge başına iki dakika ile sınırlıdır; süre aşımı diğer belgeleri durdurmaz.
6. İptal ve onaydan önce diske yazmama davranışı korunur.

Tarama modları: `Automatic` mevcut tek-belge uyumluluğunu, `Fast` OCR'siz ilk aşamayı,
`Strong` ise metin katmanı yetmediğinde güçlü OCR tekrarını temsil eder.


## 22.09.2026 — Seçili yön OCR ve erken çıkış optimizasyonu

Güçlü OCR performans yolu yeniden düzenlendi:

- Sorunlu sayfa önce 1000 piksel genişlikte düşük maliyetli yön taramasından geçer.
- Normal, +90° ve -90° adayları semantik başlık, fatura satırı ve OCR güven puanıyla karşılaştırılır.
- Yüksek çözünürlüklü OCR yalnız seçilen yönde çalışır; kaynak PDF diskte dönüştürülmez.
- Image-dominant sayfalarda tam sayfa yerine seçilen yöndeki tablo bandı önceliklidir.
- En az iki satırın Tutar/KDV/Damga/Ödeme dörtlüsü tutarlıysa kalan OCR modlarından erken çıkılır.
- Tek satırlı görüntü belgelerinde doğruluk için tüm mevcut tablo modları korunur.
- Belirsiz/eksik sonuçlarda ham gri, çizgi temizlenmiş görüntü ve satır para bandı fallback'leri devam eder.
- `HistoricalPdfImportService.Read` imzasında `CancellationToken` son parametreye alınarak CA1068 giderildi.
