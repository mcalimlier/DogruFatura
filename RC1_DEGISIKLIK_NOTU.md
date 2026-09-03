# DoğruFatura 1.0.0 RC1

Bu dal Final Setup öncesi görsel, kullanılabilirlik ve kalite turudur. Ana iş kuralları değiştirilmemiştir.

## Uygulanan final cila maddeleri

1. Nihai rapor sağ/sol kenar boşluğu fiziksel olarak 0,75 cm yapıldı.
2. WPF önizleme ve PDF ölçüleri `FinalReportLayout` üzerinden tek merkezden yönetiliyor.
3. Kullanıcı arayüzündeki Aşama/R2 geliştirme etiketleri kaldırılıyor; RC testinde yalnız sürüm bilgisi gösteriliyor.
4. Uygulama sürümü `Directory.Build.props` üzerinden merkezi yönetiliyor.
5. Açılıştaki yapay yüzde sayacı kaldırıldı; gerçek başlangıç aşamalarını belirten sade durum ekranı kullanılıyor.
6. Üst bar sadeleştirildi; sürüm ve damga oranı kalıcı üst bardan çıkarıldı.
8. Ana pencere kullanıcı tarafından yeniden boyutlandırılabilir hale getirildi ve minimum güvenli boyut tanımlandı.
9. Tarih alanları lacivert/altın giriş alanı diliyle uyumlu hale getirildi.
10. Buton yüksekliği, uyarı rengi ve metin kontrastı standardize edildi.
11. Ctrl+P, Ctrl+F, Enter ve Esc için bağlama duyarlı klavye davranışları eklendi.
12. Tab sırası, otomasyon adları ve klavye odağını görünür kılan altın odak vurgusu eklendi.
13. Bloklaması gerekmeyen başarılı güvenlik yedeği bilgisi uygulama içi bildirime dönüştürüldü; hata, silme ve geri yükleme gibi kritik onaylar modal kalır.
14. Yeni final düzeni `AppVersionInfo`, `FinalReportLayout`, `UiPolishService`, `UiNotificationService` ve `UnsavedChangesGuard` bileşenlerine ayrıldı.
15. GitHub Actions sadeleştirildi; gereksiz yazma yetkisi kaldırıldı.
16. Vergi hesabı, su istisnası, elektrik kesinti tazminatı, HYS normalizasyonu, mükerrer fatura, sayfalama ve 0,75 cm rapor ölçüsü için otomatik test projesi eklendi.
20. Ana ekrandaki geliştirme rozeti kaldırıldı; metin ve kart görünümü sadeleştirildi.
21. Kart köşeleri, giriş/buton yükseklikleri ve odak görünümü daha tutarlı hale getirildi.
22. Kullanıcıya görünen bazı teknik ifadeler sadeleştirildi (`HESAP MOTORU` → `HESAPLAMA`, `EŞİTLE` → `PDF KONTROL` vb.).
23. Fatura, kurum ve yetkili formlarında kaydedilmemiş değişiklik varken program kapatılırsa kullanıcı uyarılır.
24. Tüm çalışmalar `release/1.0.0-rc1` dalı ve PR üzerinden doğrulanır. RC tamamlanmadan final Setup üretimi güvenlik amacıyla engellenir.

## Bilerek uygulanmayan maddeler

- 7: `Created by Murat ÇALIMLIER` yerleşimi değiştirilmedi.
- 17: Damga vergisi oranı için tarihsel/mevzuat sürümleme sistemi eklenmedi.
- 18: OCR eklenmedi.
- 19: Çok bilgisayarlı/veritabanlı eşzamanlı kullanım altyapısı eklenmedi.

## Final yayın koşulu

RC1; GitHub Windows Release derlemesini, otomatik testleri ve kullanıcı tarafındaki görsel/işlevsel kontrolü geçmeden `main` dalına alınmamalıdır. Final onayından sonra `Directory.Build.props` içindeki `VersionSuffix` boşaltılır ve `SETUP_HAZIRLA.bat` ile gerçek 1.0.0 dağıtım paketi hazırlanır.
