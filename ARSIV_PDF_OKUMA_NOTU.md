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
