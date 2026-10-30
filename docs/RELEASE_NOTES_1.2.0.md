## DoğruFatura v1.2.0 — Kararlı Sürüm

Fatura girişi, resmî ödeme listeleri ve HYS arşiv yönetimi; yenilenen lacivert ve altın sarısı arayüzle bir arada.

### Yenilikler
- Aktif faturalar, ödeme toplamı, vadesi geçenler ve kurum sayısı için bilgi kartları.
- 1024×768, 1366×768 ve 1920×1080 ekran yerleşimleri.
- Su faturalarında ödeme yapılan kuruluş seçimi: belediye için KDV ve damga sıfır, tutar aynen ödenir; şirketler için standart hesaplama uygulanır.
- Kurum bazında varsayılan su ödeme seçimi ve faturayla saklanan hesaplama tercihi.
- Kaydet ve Yeni Fatura sonrasında kurum seçimi dâhil giriş kartının tamamının sıfırlanması. Kaydedilen faturalar listede kalır.
- Ayrı kurum ekleme/düzenleme pencereleri, PDF oluşturma, yazdırma ve çıktı klasörünü açma.
- HYS belge numarasıyla paket kapatma, güvenli geri alma ve damga/KDV/tutar özetleri.
- Geliştirilmiş kurum eşleştirmeleri ve küçük yüzdeleri de okunabilen istatistik grafikleri.
- Aktif faturalarla çıkış onayı, yedekleme ve veri bütünlüğü kontrolleri.

### İndirme ve İlk Kullanım
Ekli **TEMIZ_VERISIZ_DAGITIM** kurulum paketi okul/kurum, imzacı, fatura ve arşiv kaydı içermez. İlk açılışta kendi kurumlarınızı ve imzacı bilgilerinizi tanımlayın. Mevcut kullanıcı kayıtları kurulum ve kaldırma sırasında korunur.

Windows 10/11 **64 bit** içindir. .NET, Excel veya Visual Studio kurulu olması gerekmez. En düşük desteklenen ekran yerleşimi 1024×768'dir.

Kurulum dijital olarak imzalı değildir; Windows yayıncı uyarısı gösterebilir. Dosyanın SHA-256 değeri ekli doğrulama dosyasındadır.

### Doğrulama
210 otomatik test başarıyla tamamlandı. İki kurulumun içerikleri, izole ilk açılışı, masaüstü/kaldırma simgeleri ve kullanıcı verilerini koruma davranışı ayrıca doğrulandı.
