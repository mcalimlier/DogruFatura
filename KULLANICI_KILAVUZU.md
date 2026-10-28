# DoğruFatura — Kullanıcı Kılavuzu

## Günlük çalışma

1. **Okul / Kurum:** kartın içindeki Yeni düğmesiyle ayrı kayıt penceresini açın. Tablodan kayıt seçince Düzenle etkinleşir ve seçili bilgileri ayrı pencerede açar. Kaydet kalıcı kaydı yapar ve başarı bildirimi gösterir; Vazgeç veya pencereyi kapatmak değişiklik yapmaz.
2. **Yetkili / imza:** gerçekleştirme görevlisi ve harcama yetkilisini kaydedin.
3. **Fatura Girişi:** kurum, tür, tarihler, tutar ve KDV bilgilerini girin. Okul/Kurum alanında kurum adını yazarak seçim yapabilirsiniz. Elektrik kesinti tazminatı ve su damga istisnası hesaplama kartında dikkate alınır.
4. **Fatura Listesi:** faturaları düzenleyin, sıralayın, silin veya son silmeyi geri alın. PDF dosyayı kaydeder; Yazdır yazıcı seçimini açar; Klasör ilgili çıktı klasörünü açar. Önizleme alanı bulunmaz.
5. **Paketi Kaydet ve Gönder:** PDF, faturalar ve o anki imza bilgileri korunur. PDF yazılamazsa aktif faturalar kaldırılmaz. Başarılı kayıt sonrasında aktif liste boşalır; paket arşivde HYS bekleyenler arasında görünür.
6. **İşlem Arşivi:** HYS Bekleyenler tablosundaki ilgili satırın HYS İle Kapat düğmesine basın. Paket bilgilerini ve faturaları inceleyip HYS belge numarasını Kaydet ile onaylayın. Vazgeç değişiklik yapmaz. Paket HYS İle Kapananlar sekmesine geçer. HYS kapanışı banka ödeme teyidi değildir.

## Ana ekran

Yalnızca bilgi verir: aktif fatura sayısı, liste türü, ödeme toplamı, çıktı sayfa sayısı, HYS bekleyen paket adedi, vadesi geçen fatura adedi/tutarı ve farklı kurum sayısı. İşlem veya yönlendirme düğmesi yoktur.

## Paketler ve geçmiş PDF'ler

Arşiv tablosu sıralanabilir sütunlar, satır seçimi, başlıklarla kopyalama ve metin/tür/tarih filtreleri sunar. Paket bilgileri, faturaları ve işlem geçmişi satırın HYS düğmesiyle açılan pencerededir. HYS İle Kapananlar bölümündeki Kapanışı Geri Al penceresinde Düzenle, aktif liste boşsa paketi düzenlemek için aktif listeye taşır ve kapananlardan kaldırır. Aktif liste doluysa bilgilendirme gösterilir ve veri değiştirilmez. PDF ve Sil işlemleri de satırdadır. Önceki PDF'ler Arşiv Ekle ile aktarılır; PDF Kontrol belgeleri denetler. Kaynak PDF'ler korunur.

Üst şeritte sürüm kısa biçimde ve ayrı sürüm kanalı rozetiyle gösterilir. Sürüm ve bilgi değerleri fareyle seçilip Ctrl+C ile kopyalanabilir; diğer bilgi metinlerinde sağ tık menüsündeki Metni Kopyala kullanılabilir.

## İstatistik

HYS ile kapanmış uygun arşivler üzerinden yıl, tarih, kurum ve fatura türüne göre analiz yapılır. Hazır rapor seçenekleri, dağılım grafikleri, PDF, Excel ve yazdırma bulunur. Filtrelerde Tümü veya boş tarih alanları kullanılabilir.

## Güvenlik

Kurulum çeşitleri: **Tam Verili – Kişisel Kurulum**, okul/kurum, imzacılar, aktif faturalar, işlem arşivi ve PDF belgelerini içerir. **Temiz Verisiz – Dağıtım Kurulumu** yeni kullanıcıda boş başlar. Kişisel veriler yalnız ilk açılışta doğrulanarak aktarılır; mevcut kayıtlar varsa başlangıç paketi bunların üzerine yazılmaz.

İki sürüm aynı bilgisayarda yan yana kullanılabilir. Kişisel kayıtlar Masaüstü/DoğruFatura, dağıtım kayıtları Masaüstü/DoğruFatura - Dağıtım klasöründe tutulur. Dağıtım eski kişisel kayıtları otomatik içe aktarmaz. Masaüstünde kişisel sürümün adı DoğruFatura - Kişisel, dağıtımın adı DoğruFatura'dır. Eksik kurum veya imzacı tanımı varsa açılışta kısa bir bilgilendirme gösterilir; program normal çalışmaya devam eder.

Aktif fatura listesi doluyken program kapatılırsa çıkış onayı istenir. Uyarı, faturaların diske kaydedildiğini ve sonraki açılışta aktif listede kalacağını belirtir. Formlarda ayrıca kaydedilmemiş değişiklik varsa bunlar da bildirilir. Varsayılan seçenek Hayır'dır; onay verilmezse program açık kalır.

Kontrol Et veri/PDF bütünlüğünü denetler. Yedek Oluştur tam güvenlik yedeği alır. Geri Yükle seçilen yedeği doğrular ve mevcut durumu ayrıca korur. Son yedek bilgisi gösterilir.

Kullanıcı kayıtları uygulama klasöründen ayrı, Masaüstü altındaki DoğruFatura klasöründe saklanır. Veri, Aktif Çıktılar, Arşivlenenler, Yedekler ve Loglar klasörleri güncelleme veya kaldırmada bilinçli olarak silinmez.

## Hakkında / Ayarlar

Programın amacı, sürümü, geliştirici ve çalışma ortamı bilgileri gösterilir. Ekran profilleri 1024×768 (4:3), 1366×768 ve 1920×1080'dir. Seçimden sonra yanındaki Uygula düğmesine basın. Uygulanan seçim saklanır; düğme yeni bir profil seçilene kadar pasif kalır. Pencere Windows ölçeklendirmesi ve kullanılabilir masaüstü alanına sığar; dar alanda paneller alt alta geçer, tablolar kendi içinde kayar.

Gelişmiş / destek altındaki tanılama paketi fatura JSON/PDF içeriklerini dışarıda bırakır; sınırlı hata günlüklerindeki yaygın kişisel veri örüntülerini maskeler.
