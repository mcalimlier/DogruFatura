# DoğruFatura 1.0.0 RC1

Bu dal Final Setup öncesi görsel, kullanılabilirlik, veri güvenliği ve dağıtım kalite turudur. Ana mali iş kuralları değiştirilmemiştir.

## İlk final cila turu

1. Nihai rapor sağ/sol kenar boşluğu fiziksel olarak 0,75 cm yapıldı.
2. WPF önizleme ve PDF ölçüleri `FinalReportLayout` üzerinden tek merkezden yönetiliyor.
3. Kullanıcı arayüzündeki Aşama/R2 geliştirme etiketleri kaldırılıyor; RC testinde yalnız sürüm bilgisi gösteriliyor.
4. Uygulama sürümü `Directory.Build.props` üzerinden merkezi yönetiliyor.
5. Açılıştaki yapay yüzde sayacı kaldırıldı; gerçek başlangıç aşamalarını belirten sade durum ekranı kullanılıyor.
6. Üst bar sadeleştirildi; sürüm ve damga oranı kalıcı üst bardan çıkarıldı.
8. Ana pencere kullanıcı tarafından yeniden boyutlandırılabilir hale getirildi.
9. Tarih alanları lacivert/altın giriş alanı diliyle uyumlu hale getirildi.
10. Buton yüksekliği, uyarı rengi ve metin kontrastı standardize edildi.
11–12. Ctrl+P, Ctrl+F, Enter, Esc, TAB sırası, otomasyon adları ve görünür klavye odağı eklendi.
13. Bloklaması gerekmeyen bilgi mesajları uygulama içi bildirime taşınırken kritik onaylar modal bırakıldı.
14. Görsel/kalite kodu ana pencere iş kurallarından ayrıldı.
15–16. Windows CI sadeleştirildi ve otomatik test projesi eklendi.
20–23. Ana ekran/kart düzeni, kullanıcı metinleri ve kaydedilmemiş değişiklik koruması iyileştirildi.
24. Tüm çalışma RC1 dalı ve PR üzerinden yürütülüyor; final Setup RC sırasında varsayılan olarak engelli.

## 99 kalite hedefi sertleştirme turu

- UI sorumlulukları `UiVisualPolishService`, `UiAccessibilityService`, `UiPerformanceService`, `ResponsiveLayoutService` ve `UiTree` bileşenlerine ayrıldı. Tam MVVM dönüşümü yerine RC için düşük riskli modüler ayrım tercih edildi.
- Büyük ekranlarda Viewbox büyütmesi engellendi (`DownOnly`); küçük ekranlarda kontrollü ölçekleme korunuyor. 1366×768, Full HD ve 2K/4K yüksek DPI profilleri otomatik test ediliyor.
- Metin keskinliği için ClearType/Display, layout rounding ve metin taşıyan kontrollere gölge uygulanmaması program genelinde korunuyor.
- Büyük DataGrid listelerinde satır/sütun sanallaştırma ve recycling etkinleştirildi; boş listelere kullanıcı dostu açıklamalar eklendi.
- TAB sırası ekran akışına göre düzenlendi. Alt+A/F/Y/L/R/O/I/K/H menü hızlandırıcıları eklendi.
- Windows Yüksek Karşıtlık modu için sistem renklerine uyum eklendi. Kritik marka/metin renkleri otomatik kontrast testine bağlandı.
- 1.000 kayıt seviyesinde Kontrol Merkezi ve Ana Panel performans koruma testleri eklendi.
- Açılış süresi ölçülüyor; 5 saniyenin üstündeki açılışlar destek amacıyla loglanıyor.
- Yedek → değiştir → geri yükle, eski Belgeler klasöründen Masaüstü'ne veri taşıma ve çakışmada üzerine yazmama entegrasyon testleri eklendi.
- HYS kapatma/geri açma PDF taşıması, tarihsel PDF kopyalama ve mevcut arşiv PDF'sinin üzerine yazmama entegrasyon testleri eklendi.
- Türkçe karakter, sayı biçimi, uzun hizmet/fatura numarası, tarih sınırı ve büyük tutar testleri eklendi.
- Gerçek PDF üretimi PdfPig ile tekrar okunarak A4 yatay sayfa, sayfa adedi, 0,75 cm kenar sınırı ve tarihsel PDF içe aktarma regresyonu doğrulanıyor.
- .NET analizörleri etkinleştirildi; C# derleyici warning'leri kalite kapısı olarak hata kabul ediliyor. Analizör önerileri raporlanıyor.
- `TANILAMA PAKETİ` eklendi. Fatura JSON'ları ve PDF belgeleri pakete alınmıyor; yalnız ortam özeti ve sınırlı hata günlüğü bulunuyor.
- Normal kapanmayan oturum bir sonraki açılışta algılanıyor; mevcut veriler için koruyucu güvenlik yedeği alınmaya çalışılıyor.
- Setup üretimi tekrar üretilebilir hale getirildi. Normal RC kullanımında final Setup kilidi devam ediyor; `-AllowPrerelease` yalnız CI/RC smoke testi içindir.
- Setup üretiminden önce restore + Release build + test kalite kapısı zorunlu.
- CI'da gerçek NSIS paket üretilip sessiz temiz kurulum, tekrar/güncelleme kurulumu, eski program artığı temizleme, kullanıcı verisini koruma ve kaldırma sonrası kullanıcı verisini koruma test ediliyor.
- Setup ile birlikte SHA-256 üretiliyor. Kod imzalama sertifikası sağlanırsa `DogruFatura.exe` ve Setup `signtool` ile SHA-256 Authenticode imzalanıp doğrulanabiliyor; sertifika/özel anahtar depoya eklenmiyor.
- `KULLANICI_KILAVUZU.md`, `RELEASE_NOTES_1.0.0.md`, `CODE_SIGNING.md` ve `QUALITY_CHECKLIST_RC1.md` eklendi.

## Bilerek uygulanmayan maddeler

- 7: `Created by Murat ÇALIMLIER` yerleşimi değiştirilmedi.
- 17: Damga vergisi oranı için tarihsel/mevzuat sürümleme sistemi eklenmedi.
- 18: OCR eklenmedi.
- 19: Çok bilgisayarlı/veritabanlı eşzamanlı kullanım altyapısı eklenmedi.

## Önemli mimari not

29–30 kapsamındaki bakım iyileştirmesinde tam `MainWindow → UserControl/MVVM` göçü RC1 öncesinde yapılmadı. Bunun yerine yeni ve değişken kalite sorumlulukları ayrı servis katmanlarına çıkarıldı. Tam ekran mimarisi göçü iş kurallarını ve yüzlerce mevcut event bağlantısını aynı anda değiştireceği için 1.0.0 stabilite hedefinde gereksiz regresyon riski yaratacaktı. Bu karar bilinçli bir yayın güvenliği tercihidir.

## Final yayın koşulu

- GitHub `build-and-test` yeşil olmalı.
- GitHub `setup-smoke` yeşil olmalı.
- `QUALITY_CHECKLIST_RC1.md` gerçek Windows kullanımında kritik hata olmadan tamamlanmalı.
- Kullanıcı RC1'i onaylamalı.

Bunlardan sonra `Directory.Build.props` içindeki `VersionSuffix` boşaltılır, `v1.0.0` hazırlanır ve gerçek final Setup üretilir.
