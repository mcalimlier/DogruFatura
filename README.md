# DoğruFatura 1.0 — Aşama 10 R2

DoğruFatura, DoğruTemin 4.1.0'ın onaylanmış WPF pencere, navigasyon ve görsel dilini temel alan bağımsız bir `.NET 8 / WPF` uygulamasıdır.

## Aşama 10 R2'de eklenenler

- Bütün kullanıcı klasörleri tek kökte toplandı: `Masaüstü\DoğruFatura`. Eski `Belgeler\DoğruFatura` içindeki `Veri`, `Yedekler` ve `Loglar` ilk açılışta güvenli biçimde yeni köke taşınır.
- İşlem Arşivindeki düğme `ARŞİV EKLE` olarak değiştirildi. `48) HYS-2026-34546180.pdf` gibi başında sıra numarası bulunan ve HYS sayısında ayırıcı tire kullanılan eski dosya adları da kabul edilir; kayıt standart HYS biçimine normalize edilir.
- `Hakkında / Güvenlik` ekranı gerçek bir güvenlik merkezine dönüştürüldü. Sürüm, çalışma ortamı, veri/PDF/yedek klasörleri ve bütünlük sonucu tek yerde gösterilir.
- `KONTROL ET`; dört kalıcı JSON dosyasını, aktif/arşiv PDF imzalarını ve bütün çalışma klasörlerinin yazılabilirliğini denetler.
- `GÜVENLİK YEDEĞİ`; aşağıdaki kayıtları tek bir doğrulanabilir ZIP içinde birlikte korur:
  - `Masaüstü\DoğruFatura\Veri` altındaki JSON kayıtları ve sürüm işareti
  - `Masaüstü\DoğruFatura\Aktif Çıktılar` PDF belgeleri
  - `Masaüstü\DoğruFatura\Arşivlenenler` PDF belgeleri
- Yedekler `Masaüstü\DoğruFatura\Yedekler\Güvenlik` altında tarih damgasıyla saklanır. Yarım kalmış ZIP bırakmamak için önce geçici dosyada hazırlanıp atomik biçimde tamamlanır.
- Yeni uygulama sürümü ilk açıldığında mevcut veri ve PDF belgeleri için `Güncelleme_Öncesi` yedeği otomatik oluşturulur.
- `YEDEKTEN GERİ YÜKLE`; ZIP kimliğini, dosya yollarını, açılmış boyut sınırını, JSON yapısını ve PDF imzalarını doğrular. Mevcut durum ayrıca `Geri_Yukleme_Oncesi` klasörüne yedeklenir; veri ve belgeler kontrollü klasör değişimiyle geri alınır.
- Başarılı geri yüklemeden sonra program kendini yeniden başlatır. Tek örnek çalışma kilidi bu güvenli yeniden başlatma için kontrollü bırakılır.
- Beklenmeyen WPF, AppDomain ve arka plan görev hataları ayrı tarih damgalı loglara yazılır. Sistem/sürüm bilgileri eklenir ve son 50 kayıt korunur.
- `SETUP_HAZIRLA.bat` ve `Kurulum\DogruFatura_NSIS.nsi` eklendi. Windows 10/11 x64 için self-contained kurulum üretir; son kullanıcı bilgisayarında .NET, SDK, Visual Studio veya Excel gerekmez.
- Kurulum eski uygulama dosyalarını temizler ancak `Masaüstü\DoğruFatura` kullanıcı verilerini ve PDF belgelerini güncelleme/kaldırma sırasında korur. Daha yeni kurulu sürümün yanlışlıkla eski sürüme düşürülmesi engellenir.

## Aşama 9 R4'ten korunanlar

- Program açıldığında işlem arşivi ile Masaüstündeki PDF klasörleri otomatik olarak karşılaştırılır.
- İşlem Arşivine `EŞİTLE` düğmesi eklendi. Kullanıcı aynı denetimi istediği anda yeniden çalıştırabilir.
- HYS ile kapatılmış her geçerli arşivin PDF'si `Arşivlenenler` klasöründe `HYS-....pdf` adıyla bulunur. Eksikse kayıttaki fatura ve yetkili bilgileri kullanılarak yeniden oluşturulur.
- Açık arşivlerin eksik PDF'leri `Aktif Çıktılar` klasöründe yeniden oluşturulur.
- Yeniden oluşturulan PDF'nin dosya adı işlem arşivi kaydına otomatik bağlanır. Böylece programdaki arşiv sayısı ile kayıtlı belgelerin eşleşmesi korunur.
- Yeni bir açık arşiv oluşturulduğunda, HYS ile kapatıldığında veya kapanışı geri alındığında da eşitleme çalışır.
- Güvenlik amacıyla var olan PDF'lerin üzerine yazılmaz, dosya silinmez ve arşiv kaydı bulunmayan PDF otomatik içe aktarılmaz. Eski PDF'ler kontrollü `ARŞİV EKLE` işlemiyle alınmaya devam eder.

## Aşama 9 R3'ten korunanlar

- Ana Ekrandaki `En Yüksek Ödemeli Kurumlar` kartı kaldırıldı; mali özet, fatura türü dağılımı ve son altı aylık eğilim daha temiz bir düzende bırakıldı.
- Program açılırken Masaüstünde `DoğruFatura` klasörü ile aşağıdaki iki alt klasör hazırlanır:
  - `Aktif Çıktılar`: PDF veya Yazdır işlemi yapılan, henüz HYS ile kesin kapatılmamış listeler.
  - `Arşivlenenler`: HYS ile kapatılan veya geçmiş PDF aktarımıyla alınan listeler.
- PDF ve Yazdır işlemlerinde belgenin bir kopyası otomatik olarak `Aktif Çıktılar` klasöründe korunur.
- Açık arşive HYS kodu girilip kapanış yapıldığında ilgili PDF `Aktif Çıktılar`dan `Arşivlenenler`e taşınır ve dosya adı `HYS-....pdf` olur.
- Kapanış geri alınırsa PDF yeniden `Aktif Çıktılar`a taşınır; fatura ve HYS bilgileri korunur.
- İşlem Arşivindeki `ARŞİV EKLE` düğmesiyle birden fazla geçmiş PDF seçilebilir; aktarım öncesinde her dosyanın sonucu ayrı gösterilir.
- Dosya adındaki HYS kodu, fatura türü, belge tarihi, fatura satırları, kurum/hizmet/fatura numaraları, tarihler, tutarlar ve imza bilgileri okunur.
- Okunan tutarlar vergi motoruyla yeniden doğrulanır. Elektrik PDF'sinde çıktıda görünmeyen kesinti tazminatı varsa damga vergisi ve ödeme tutarından güvenli biçimde geri hesaplanır.
- Aynı HYS kodunun arşive ikinci kez alınması ve mevcut arşiv PDF'sinin üzerine yazılması engellenir.
- Hatalı dosyalar aktarılmaz; geçerli dosyalar tek işlemde kapalı HYS arşivi olarak kaydedilir.

Yüklenen `HYS-2026146253.pdf` örneğiyle okuyucu doğrulanmıştır. Örnekte bir Elektrik faturası, `02.09.2026` belge tarihi, kurum/hizmet bilgileri, `9.921,79 TL` ödeme ve çıktıda gizli `250,00 TL` kesinti tazminatı doğru okunmuştur. Örnek PDF kaynak paketine eklenmemiştir.

## HYS ve Ana Ekran kuralı

- HYS numarası `HYS-202614586` biçiminde saklanır. Yalnız rakamlardan oluşur ve sayısal kısmı sıfırla başlayamaz.
- Aynı HYS kodu iki ayrı arşiv kaydında kullanılamaz.
- Yalnız HYS numarası girilmiş ve kapatılmış arşivler Ana Ekran mali istatistiklerine katılır.
- Aktif liste ve açık arşivler istatistiklere dahil edilmez.
- Nihai çıktısı alınan liste ödenmiş kabul edildiğinden yaklaşan/gecikmiş ödeme takibi yapılmaz.

## Kilitli hesap kuralları

Normal Elektrik, Telefon ve İnternet faturaları:

```text
KDV Matrahı   = Fatura Tutarı - KDV
Damga Vergisi = KDV Matrahı × 0,00948
Ödeme Tutarı  = Fatura Tutarı - Damga Vergisi
```

Su faturaları:

```text
KDV Matrahı   = Fatura Tutarı - KDV
Damga Vergisi = 0,00 TL
Ödeme Tutarı  = Fatura Tutarı
```

Kesinti tazminatı bulunan Elektrik faturaları:

```text
KDV Matrahı   = (Fatura Tutarı + Kesinti Tazminat Tutarı) - KDV
Damga Vergisi = KDV Matrahı × 0,00948
Ödeme Tutarı  = Fatura Tutarı - Damga Vergisi
```

Kesinti tazminatı varsayılan olarak `0,00 TL` ve kapalıdır. Yalnız Elektrik faturasında kullanıcı tarafından açılabilir. Hesapta ve kalıcı kayıtta korunur, nihai listede/PDF'de ayrı sütun olarak gösterilmez. Nihai çıktıda bütün tutarlar iki ondalık basamakla gösterilir.

## Korunan temel

- DoğruTemin ailesiyle uyumlu lacivert, sarı ve beyaz WPF arayüz
- Tek örnek çalışma koruması ve açılış hesap motoru öz testi
- Kalıcı okul/kurum, aktif fatura, yetkili/imza ve işlem arşivi verileri
- VKN ile hizmet/tüketici numaralarını baştaki sıfırlar korunarak metin saklama
- VKN ve fatura numarasını gerektiğinde boş bırakabilme
- Gerçek fatura ekleme, düzenleme, onaylı silme ve sıra yönetimi
- Aktif ödeme listesinde yalnız tek fatura türüne izin verme
- Mükerrer fatura ve mükerrer HYS engelleri
- Kontrol Merkezi ile kurum/fatura/hizmet/tarih/tutar denetimleri
- Yatay A4 nihai çıktı, temiz canlı önizleme, PDF ve Windows yazdırma
- Çok sayfalı çıktıda başlık tekrarı; son sayfada toplamlar ve imzalar
- Rapor şeridinde yalnız `PDF`, `YAZDIR` ve iki yönlü `TAM SAYFA / GERİ DÖN`
- Bütün görünen düğmelerde `42` birim standart yükseklik ve Segoe UI tipografi sistemi
- Excel veya Office çalışma bağımlılığının olmaması

PDF üretimi `PDFsharp-WPF 6.2.4`, metin tabanlı geçmiş PDF okuma `PdfPig 0.1.12` ile yapılır. İlk kaynak derlemesinde NuGet paketleri internetten indirilir; kurulum paketinde çalışma bileşenleri uygulamayla birlikte yer alacaktır.

## Geçmiş PDF aktarım koşulları

- Dosya adında geçerli bir HYS kodu bulunmalıdır. `HYS-2026146253.pdf`, `48) HYS-2026146253.pdf` ve `48) HYS-2026-146253.pdf` gibi eski sıra numaralı/adlandırılmış kayıtlar kabul edilir; sıra numarası ile HYS içindeki ayırıcı tireler otomatik göz ardı edilir.
- PDF, DoğruFatura'nın standart resmî ödeme listesi düzeninde ve metin seçilebilir yapıda olmalıdır.
- Tablodaki sıra numaraları kesintisiz okunmalı; bütün tutarlar güncel hesap motoruyla uyuşmalıdır.
- Görüntü/tarama biçimindeki PDF'ler bu revizyonda OCR ile okunmaz ve güvenli biçimde hatalı olarak işaretlenir.
- Aktarım başarılı olduğunda kaynak dosya silinmez; bir kopyası `Arşivlenenler`de HYS adıyla korunur.

## Çalıştırma ve Aşama 10 R2 testi

Windows 10/11 üzerinde .NET 8 veya daha yeni bir SDK kuruluysa `DERLE_CALISTIR.bat` dosyasını çalıştırın. `.NET 10 SDK` da kabul edilir.

1. Programı ilk kez R2 ile açmadan önce varsa `Belgeler\DoğruFatura\Veri`, `Yedekler` ve `Loglar` klasörlerini not edin. Açılıştan sonra bunların `Masaüstü\DoğruFatura` altına taşındığını ve mevcut kayıtların programda aynen göründüğünü doğrulayın.
2. `Hakkında / Güvenlik` ekranını açın. Sürümün `1.0.0-ASAMA10-R2`; veri, PDF, yedek ve log yollarının tamamının `Masaüstü\DoğruFatura` altında olduğunu kontrol edin.
3. İşlem Arşivinde düğmenin `ARŞİV EKLE` olduğunu doğrulayın. `48) HYS-2026-34546180.pdf` biçimindeki gerçek eski dosyalarınızdan biriyle deneyin; baştaki sıra numarası ve HYS içindeki ayırıcı tireler nedeniyle hata verilmemelidir.
4. `KONTROL ET` düğmesine basın. Mevcut dört JSON dosyası ile PDF sayısını içeren yeşil `Veri bütünlüğü sağlıklı` sonucu görünmelidir.
5. `GÜVENLİK YEDEĞİ`ne basın. Oluşan ZIP'nin `Masaüstü\DoğruFatura\Yedekler\Güvenlik` klasöründe bulunduğunu doğrulayın.
6. Test amacıyla bir okul/kurum kaydında küçük bir değişiklik yapıp kaydedin. Ardından oluşturduğunuz yedeği `YEDEKTEN GERİ YÜKLE` ile seçin.
7. Geri yükleme öncesi mevcut durum için ayrıca emniyet kopyası oluşturulduğunu, programın yeniden başladığını ve deneme değişikliğinin yedekteki eski haline döndüğünü kontrol edin.
8. Aktif ve arşivlenmiş PDF belgelerinin de seçilen yedek tarihindeki durumuna döndüğünü; veri, PDF, yedek ve log klasörü düğmelerinin doğru Masaüstü klasörlerini açtığını kontrol edin.
9. Aşama 9 R4'te kilitlenen arşiv–PDF eşitlemesi ile önceki hesap, çıktı ve arşiv davranışlarını yeniden kontrol edin.

### Self-contained Setup hazırlama

Setup üretilecek geliştirme bilgisayarında .NET 8 veya daha yeni SDK ile NSIS 3.x kurulu olmalıdır. `SETUP_HAZIRLA.bat` çalıştırıldığında:

1. Kaynaklar ve araçlar denetlenir.
2. `win-x64` self-contained Release yayını hazırlanır.
3. Runtime ve WPF dosyalarının eksiksizliği kontrol edilir.
4. NSIS kurulum dosyası üretilip PE/MZ ve boyut kontrolünden geçirilir.
5. `DAGITIM` klasörüne Setup EXE, kurulum notu ve SHA-256 doğrulama dosyası bırakılır.

Son kullanıcı yalnız `DogruFatura_Setup_1.0.0_x64.exe` dosyasını çalıştırır; geliştirme araçlarına ihtiyaç duymaz.

## Kalıcı konumlar

DoğruFatura artık bütün kullanıcı içeriklerini tek kökte toplar:

- `Masaüstü\DoğruFatura\Veri\Okullar.json`
- `Masaüstü\DoğruFatura\Veri\AktifFaturalar.json`
- `Masaüstü\DoğruFatura\Veri\YetkiliProfili.json`
- `Masaüstü\DoğruFatura\Veri\IslemArsivi.json`
- `Masaüstü\DoğruFatura\Loglar`
- `Masaüstü\DoğruFatura\Yedekler\Güvenlik`
- `Masaüstü\DoğruFatura\Yedekler\Güncelleme_Öncesi`
- `Masaüstü\DoğruFatura\Yedekler\Geri_Yukleme_Oncesi`
- `Masaüstü\DoğruFatura\Aktif Çıktılar`
- `Masaüstü\DoğruFatura\Arşivlenenler`

Eski sürümlerde `Belgeler\DoğruFatura` altında bulunan `Veri`, `Yedekler` ve `Loglar` ilk açılışta yeni Masaüstü köküne güvenli biçimde taşınır. Aynı adlı bir hedef dosya zaten varsa üzerine yazılmaz; olası çakışan eski dosya korunur.

## Sonraki aşama

Aşama 10 R2 güvenlik yedeği ve geri yükleme akışı kullanıcı bilgisayarında doğrulandıktan sonra Windows kurulum paketi son kez üretilecek, temiz kurulum/güncelleme/kaldırma testleri tamamlanacak ve final dağıtım kilitlenecektir.

Created by Murat ÇALIMLIER
