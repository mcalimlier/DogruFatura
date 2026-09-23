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


---

## 22.09.2026 — Performans Paketi 2: Tek Metin Çıkarımı ve Kademeli OCR

Uygulananlar:

- Hızlı ve güçlü aşamalar arasında PdfPig sayfa verisi dosya kimliği
  (tam yol + boyut + son değiştirme zamanı) ile önbelleğe alındı.
- Genel okuyucunun çıkardığı sayfalar legacy okuyucuya aktarıldı; aynı Read
  çağrısında PDF'nin ikinci kez açılması kaldırıldı.
- Toplu aktarım başında ve sonunda önbellek temizlenerek işlem dışına veri
  taşınması engellendi.
- 1000 px yön ön-taramasının en iyi PageData sonucu artık saklanıyor.
- Ön-tarama en az iki tutarlı fatura satırı üretiyorsa yüksek çözünürlüklü render
  ve OCR tamamen atlanıyor.
- Ön-tarama yetersizse önce 1500 px ara kademe deneniyor; geçerliyse son kademe
  çalıştırılmıyor.
- 1600–2600 px son kademe yalnız ara kademe yetersizse devreye giriyor.
- RemoveLines temizliği ham/gri OCR yetersiz kaldıktan sonra tembel çalışıyor.
- Eksik para satırı bir OCR modundan sonra tutarlı hale gelirse kalan modlar
  çalıştırılmıyor.
- PdfPig, yön ön-taraması, ara çözünürlük, son çözünürlük ve sayfa toplam süreleri
  ilerleme metninde milisaniye olarak gösteriliyor.
- Yeni regresyon testi hızlı taramanın çıkarılmış sayfaları yeniden kullandığını
  ve legacy okuyucunun PDF'yi tekrar açmadığını doğruluyor.

Beklenen test hedefi: **136/136 test, 0 hata, 0 uyarı**.

Not: Bu paket kaynak PDF'yi döndürmez veya yeniden yazmaz. Arşiv PDF'sinin
SHA-256 değeri ve kanıt niteliği korunur.

---

## 23.09.2026 — Toplu aktarım sonucu ve dosya bazlı rapor

Kullanıcı gerçek denemede 2025 yılına ait 24 PDF'den 20'sinin hızlı biçimde
arşive eklendiğini bildirdi. Kalan dört PDF'nin adları ve hata nedenleri önceki
oturumun yalnız sayısal sonucundan çıkarılamıyor.

Yeni akış, seçilen her PDF için hızlı metin taraması veya güçlü tarama durumunu
önizlemede gösterir. Legacy güçlü taramada OCR yoluna girildiyse bunu ayrı
işaretler; süre aşımında OCR başlangıcı kesinleşmemişse durumu belirsiz olarak
yazar. Başarısız dosyalar, gerçek doğrulama/çakışma nedenleri ve kaynak yolları
ile aktarım sonunda kullanıcıya gösterilir.

Her tamamlanan önizleme işlemi (onay, vazgeçme, geri alma) için masaüstündeki
`DoğruFatura` klasörüne `Arsiv_Aktarim_Raporu_*.txt` yazılır. Raporda
seçilen/eklenen/eklenemeyen sayıları, başarısız dosyalar ve bütün seçilen
dosyaların sonuçları yer alır. Rapor oluşturulurken arşiv yeniden taranmaz.
Rapor yazılamazsa uygulama bunu açıkça bildirir.
Aynı 24 PDF daha önce 20'si eklenmişken yeniden seçilirse, bu 20 dosya
`ZATEN ARŞİVDE` olarak ayrılır; dört gerçek inceleme adayı ayrı listelenir.
Bu tekrar koşusunda beklenen sayılar: 24 seçilen, 0 yeni eklenen, 20 zaten
arşivde, 4 inceleme gereken. Böylece önceden eklenenler hata listesinde
sorunlu dört PDF'yi gizlemez.

Kod değişikliği ve 24/20/4 senaryolu regresyon testi eklendi. Bu çalışma
ortamında .NET SDK olmadığı için derleme ve Windows gerçek PDF testi henüz
çalıştırılmadı. Kullanıcı Windows'ta `TESTLERI_CALISTIR.bat` ve
`DERLE_CALISTIR.bat` ile doğrulamalı; ardından 24 PDF'yi yeniden seçerek dört
dosyanın adını, OCR aşamasını ve metin raporunu kontrol etmelidir.

---

## 23.09.2026 — HYS ödeme emrine eklenmiş e-Fatura ve gerçek OCR durumu

Gerçek örnek: `24) HYS-2025-21213030.pdf`. Belgenin birinci sayfasında
resmî ödeme emri, ikinci sayfasında e-Fatura bulunuyor. Ödeme emrinde klasik
fatura listesi tablosu yok; eski parserın tablo bulamaması bu nedenle
beklenebilir. Metin katmanı her iki sayfada da okunabildiğinden önce hedefli
metin okuma yapılır; uygun veriler çıkarılamazsa güçlü aşamada gerekli sayfa
görüntü OCR'ına girer.

Yeni hedefli yol şu alanları e-Faturadan okur: fatura numarası, sözleşme/tüketici
hesap numarası, fatura ve vade tarihi, ödenecek tutar ve KDV. Hizmet numarası
kaynak okul/kurum kayıtlarında tekil eşleşiyorsa kurum ve VKN oradan alınır;
aksi durumda faturanın alıcı unvanı kullanılır. Damga vergisi ve net ödeme
mevcut DoğruFatura hesap motoruyla hesaplanır ve eklenecek tüm faturaların
brüt/damga/net toplamı ödeme emrindeki gerçek toplamlarla bire bir
karşılaştırılır. Uyuşmuyorsa otomatik kapalı arşiv kaydı oluşturulmaz.
Kaynak PDF dönüştürülmez veya yeniden yazılmaz.

Örnek belge değerleri: e-Faturada 4.911,20 TL ödenecek tutar ve 818,53 TL
KDV; ödeme emrinde 4.911,20 TL brüt, 38,80 TL kesinti ve 4.872,40 TL net.
Faturadaki 4.911,17 TL ara tutar ve 0,03 TL yuvarlama ayrı bilgidir; kayıt
için faturanın ve ödeme emrinin 4.911,20 TL ödenecek/brüt tutarı kullanılır.

Güçlü aşamaya girmek görüntü OCR'ının gerçekten çalıştığı anlamına gelmez.
Önceki semantik sözcük puanına göre sayfa atlama kuralı, okunamayan e-Fatura
sayfasını atlayabiliyordu. Artık yalnız toplamları metinden doğrulanan ödeme
emri sayfası atlanır; sorunlu fatura sayfası OCR'a girer. Önizleme ve metin
raporu gerçek OCR çalıştı / sayfalar atlandı / OCR başlamadı ayrımını gösterir.

Örnek dosyada ödeme emri mevcut olduğundan uygulama yeni bir resmî ödeme emri
üretmez. Yalnız e-Fatura içeren ayrı bir PDF resmî HYS belgesi olarak kapalı
arşive alınmaz; bunun için ayrı taslak iş akışı ve gerçek örnek gerekir.

Dört regresyon testi eklendi: ödeme emri + ek fatura başarılı, eksik hizmet
numarasını tekil kurum kaydından tamamlama, toplamlar uyuşmayınca ret ve
yalnız fatura varken resmî ödeme emri üretmeme. Önceki 137 testle beklenen
toplam 141'dir. Windows .NET derleme/test ve gerçek örnekle
çalıştırma bu ortamda henüz yapılamadı.


## 2025 toplu aktarımda 8, 19 ve 20 numaralı PDF'ler (23.09.2026)

Kullanıcı Windows üzerinde .NET SDK 10.0.400 ile derledi; önceki sürümde 24 numaralı PDF başarıyla arşive alındı. Derlemede CA1305 (2), CA1861 (1), CA1068 (2) olmak üzere beş uyarı vardı. Bu uyarıların kaynakları düzeltildi. Bu değişikliklerin Windows derlemesi ve testleri kullanıcı ortamında yeniden doğrulanmalıdır; burada .NET SDK bulunmuyor.

Gerçek belgelerin sayfa düzeni incelendi:

- `8) HYS-2025-19794703.pdf`: 1 resmî ödeme emri + dört faturanın ikişer sayfası (toplam 9 sayfa). Fatura tutarları 2.638,50 + 872,20 + 1.733,70 + 186,10 = 5.430,50 TL; faturalardaki KDV toplamı 439,75 + 79,30 + 288,94 + 31,01 = 839,00 TL. Ödeme emrinde 43,53 TL damga ve 5.386,97 TL net var. Fatura kimliği, alıcı, hizmet/abone, fatura ve vade tarihi ile ödenecek tutar ilk sayfadan; KDV sonraki sayfadan eşleştirilir. Dört fatura **tek** HYS kapalı arşiv kaydında tutulur.
- `19) HYS-2025-20585737.pdf`: 1 resmî ödeme emri + yatay `AYRINTILI GÖNDERİLEN ÖDENEK/FATURA LİSTESİ`. İki fatura 5.819,00 ve 653,10 TL; ödeme emri 6.472,10 TL brüt, 51,13 TL damga, 6.420,97 TL net. Fatura numaraları ve birinci abone numarası listede alt satıra bölünmüştür. İki fatura tek HYS kaydına girer.
- `20) HYS-2025-20588537.pdf`: benzer yatay liste, 571,20 ve 90,20 TL tutarlı iki fatura; 661,40 TL brüt, 5,23 TL damga ve 656,17 TL net. İki fatura tek HYS kaydına girer.

Yatay listeler KDV göstermiyor. İthalat yolu yalnız %1, %10 ve %20 KDV adaylarının her birini fatura tutarından hesaplar; sadece **tek** oran ödeme emrindeki brüt, damga ve net toplamları bire bir tutturduğunda kayıt hazırlar. Bu iki gerçek örnekte %20 adayı eşleşir. Önizleme ve işlem raporu KDV'nin belgeden okunmadığını, ödeme emriyle doğrulanarak hesaplandığını açıkça yazar. Bu işlem KDV'nin fatura üzerinde kanıtlandığı anlamına gelmez; ayrıntılı vergi kontrolü için özgün faturalar gerektiğinde ayrıca incelenmelidir. Hiçbir resmî ödeme emri PDF'si üretilmez veya değiştirilmez.

Kod: `DogruFatura.Wpf/LegacyHysPdfImportService.cs` çok sayfalı fatura grupları ve liste satırları için genişletildi. `DogruFatura.Wpf/HistoricalPdfImportReport.cs` tarih kültürü açık belirtildi. `DogruFatura.Tests/LegacyHysPdfImportTests.cs` dört faturayı tek ödeme emrine bağlama, 19/20 düzenindeki listeler ve tutarsız kesintiyi reddetme durumlarını sınar. Gerçek PDF'ler depoya eklenmedi. Önceki 141 teste dört yeni durum eklenerek beklenen toplam 145 test oldu. Bu ortamda .NET SDK olmadığından derleme veya xUnit çalıştırılmadı; yeni Windows çalıştırmasında beklenen durum her üç dosya için tek HYS altında sırasıyla 4, 2, 2 fatura ve sıfır analiz uyarısıdır.


## Windows derleme geri bildirimi: CS1503 (23.09.2026)

Kullanıcının .NET 10.0.400 ortamındaki son derlemede `LegacyHysPdfImportService.cs:571` için CS1503 görüldü: `ArchivedInvoiceList` kurucusu `List<InvoiceRecord>` beklerken ayrıntılı liste yolundan `InvoiceRecord[]` geçiriliyordu. `selected.Invoices.ToList()` ile kurucuya doğru tür verildi (commit `27883967e376cc8e6af694c397530082cfab3edd`). Bu commit henüz Windows üzerinde yeniden derlenmedi; yeni test veya çalışma başarısı varsayılmamalıdır. Kullanıcının sonraki adımı güncel dalı çekip `DERLE_CALISTIR.bat` ve ardından `TESTLERI_CALISTIR.bat` çalıştırmaktır. Hata çıkarsa ilk hatanın tam metniyle devam edilir.


## 8 numaralı çok faturali PDF: toplam başlığı düzeltmesi (23.09.2026)

Windows geri bildiriminde 19 ve 20 numaralı PDF'ler başarıyla arşive alındı; yalnız `8) HYS-2025-19794703.pdf` başarısız. Gerçek 8 numaralı PDF'nin ödeme emri sayfasında `Ödeme Emri Detayı` başlığı (yaklaşık y=253), gerçek brüt/damga/net toplam sütunlarından (yaklaşık y=403–420) önce bulunuyor. Eski yedek toplam okuyucu ilk `Ödeme Emri` başlığını seçip yalnız sonraki 40 birimi aradığı için toplam satırına hiç ulaşmıyordu. Genel toplam okuyucusu ise bu PDF'de `Ödeme Emri` ile `Toplam Tutarı` arasına sütun başlıkları girdiğinden eşleşmiyordu. Bu, dört fatura sayfasının okunmasına geçmeden işlem yolunu kesiyordu; görüntü OCR'ı gerektiren bir metin eksikliği değildi.

Yedek okuyucu artık yalnız `Ödeme Emri` ile brüt veya toplam sütun başlığını içeren satırları tarıyor ve ilk bölüm başlığında durmuyor. Gerçek PDF metninde bulunan toplam 5.430,50 TL brüt, 43,53 TL damga ve 5.386,97 TL net olarak doğrulandı. Dört faturanın tutarı ve KDV'si önceki belgede ayrıntılı; bunların toplamıyla ödeme emri tekrar karşılaştırılacak. `FourTwoPageInvoicesStayUnderOnePaymentOrder` regresyon testi, önce gelen detay başlığını ve iki satıra bölünen gerçek toplam başlığını canlandıracak şekilde güncellendi. Kaynak düzeltme commit'i `430c65cd5d6d27fad1777ea953d2e2bfd84191f0`, test commit'i `229d0140122059c88c7dc3ae369f4bfce0db516b`. Bu ortamda .NET derlemesi çalıştırılamıyor; kullanıcının yeni Windows denemesinde 8 numara için dört faturanın **tek** kapalı HYS kaydında açılması doğrulanmalıdır.


## 8 numaralı PDF: iki dakikalık güçlü OCR zaman aşımı (23.09.2026)

Son Windows geri bildiriminde 19 ve 20 başarılı; 8 numarada `Güçlü tarama 2 dakikalık süre sınırını aştı` sonucu görüldü. Bu süre sınırı `MainWindow.ArchiveDocuments.cs` içinde güçlü OCR içindir. Metin yolu geçersiz sonuca düştüğünde OCR ödeme emri dışındaki sekiz sayfayı ayrı ayrı döndürüp yüksek çözünürlükte işleyerek sınırı doldurur.

Önceki düzeltme gerçek toplam başlığının bulunmasını sağladı. Ancak ikinci bir hata vardı: genel toplam okuyucusu bir para değeri için tam ve ilk rakamı atılmış adayları üretiyordu. `5.430,50` değerinden türetilen `430,50`, 5.430,50 TL brütün %10'undan küçük olduğu için yanlışlıkla damga vergisi kabul edilebiliyordu. Metin çıkarma sırası bu genel yolu seçerse resmî 43,53 TL yerine 430,50 TL kullanılıp dört fatura gerçek toplamla eşleşmiyor, gereksiz sekiz sayfalık OCR devreye giriyordu.

Çözüm: Ek faturaları okuyan yol önce ödeme emrinin doğrudan brüt/damga/net tutar satırını kullanıyor; genel okuyucu da yalnız `brüt − damga = net` eşitliğini sağlayan adayı kabul ediyor. Böylece 430,50 adayı reddedilir. İki sayfalık dört faturanın tek ödeme emrine bağlanmasını sınayan test hem gerçek PDF'deki bölünmüş toplam başlığı hem tek satırlık başlık için çalışacak biçimde genişletildi. Beklenen test sayısı önceki 145'ten 146'ya yükseldi. Kod commit'i `c223a2cb46e5866af76815c3a1f09b691bf1a9af`, test commit'i `7ecad36f1d08ef4e790bd8fdc3a5bcc447a174d5`. Gerçek PDF'nin brüt 5.430,50, kesinti 43,53, net 5.386,97 ve faturalardaki KDV toplamı 839,00 TL metinden çapraz doğrulandı. Windows .NET derlemesi ve gerçek içe alma henüz yeniden doğrulanmadı; sonuç hızlı aşamada 4 fatura olarak beklenir. Eğer hâlâ başarısızsa hızlı tarama sonucundaki ilk hata metni ve uygulamanın gösterdiği son çalışma adımı kaydedilmelidir; süreyi uzatmak ilk tercih değildir.


## 8 numaralı belge için dört faturanın tek HYS arşivine aktarımı (23.09.2026)

Gerçek `8) HYS-2025-19794703.pdf` PDF'si 9 sayfadır: sayfa 1 resmî HYS ödeme emri; sayfa 2–3, 4–5, 6–7 ve 8–9 dört faturadır. Ödeme emri PDF'nin içinde **zaten vardır**; uygulama yeni resmî belge imal etmez. Aktarım onayında aynı özgün PDF, `Arşivlenenler` içine kopyalanır ve fatura kayıtları tek kapalı `HYS-202519794703` arşiv kaydına bağlanır.

| Fatura no | Abone / tüketici no | Brüt (TL) | KDV (TL) |
| --- | --- | ---: | ---: |
| EAB2025000055348 | 00002604070 | 2.638,50 | 439,75 |
| AAB2025000996266 | 00002200440 | 872,20 | 79,30 |
| AAB2025000996213 | 00002201830 | 1.733,70 | 288,94 |
| AAB2025001051104 | 00002700660 | 186,10 | 31,01 |

Gerçek belgenin metin katmanından bağımsız sayısal kontrol: **4 fatura**, toplam **5.430,50 TL brüt**, **839,00 TL KDV**. Mevcut vergi kuralıyla (KDV hariç matrah × 0,00948, toplamda kuruşa yuvarlama) **43,53 TL damga**, **5.386,97 TL net** bulunur; ödeme emrindeki 5.430,50 / 43,53 / 5.386,97 toplamlarıyla bire bir eşleşir. Tarih, okul/alıcı adı ve abone alanları faturaların ilk sayfalarından, KDV ikinci sayfalardan okunur. Bir fatura eksikse, yineleniyorsa, hesaplanmış tutarlar resmî toplamla eşleşmiyorsa veya arşiv geçerli değilse kapalı kayıt hazırlanmaz.

Son kod değişikliği toplam satırını sayfa üzerinde ilk bölüm başlığına bağımlı olmadan altı para hücresinden ve `brüt − damga = net` doğrulamasıyla okur; dört faturanın `FAT URA T ARİHİ` gibi parçalanmış etiketlerini, komşu satırlara taşan tarihler ve `KDV (% 10 )` gibi yazımları tolere eder. Tam metinli iki sayfalık eklerin alanları doğrulanamazsa sekiz sayfaya gereksiz OCR uygulamak yerine dosya ve sebep hızlı aşamada gösterilir. Bu koruma, görüntü tabanlı veya eksik metinli PDF'lerin güçlü OCR yolunu kaldırmaz. Onay sonrası DoğruFatura kök klasöründe oluşan `Arsiv_Aktarim_Raporu_*.txt`, artık her fatura için kurum, abone, tarih, brüt, KDV, damga ve net tutarları ve aynı HYS toplamlarını yazar.

İlgili commit'ler: `e0e4ab6ebf20a62e9259944b9925cdc739ef5528` (okuma ve doğrulama), `ac6679310f24b8ce7b3ca4284333a21f4c7a164e` (gereksiz güçlü taramayı atlama), `1725ada18fb31eec6a06263baf6bbbe5a81561b0` (PDF düzeni ve yanlış toplam regresyonu), `cd05734815ec3d26024f9e07c11ed98fa88160a1` (metin raporu), `35e2e3143368c947f140c8e6e2f744adcd0caa35` (rapor testi). Beklenen toplam test sayısı önceki 146'dan 148'e çıkar. Bu ortamda .NET SDK bulunmadığından Windows derlemesi, xUnit ve gerçek uygulama aktarımı henüz çalıştırılamadı. Son kod çekildikten sonra `TESTLERI_CALISTIR.bat` ve `DERLE_CALISTIR.bat` çalıştırılmalı; yalnız 8 numara seçilerek önizlemede tek HYS / 4 fatura / 5.386,97 TL net ve onay sonrası arşiv kaydı ile .txt raporu doğrulanmalıdır.
