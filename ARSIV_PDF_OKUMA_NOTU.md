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
