# DoğruFatura 1.2.0-preview.1 — Çalışma masası

Bu geliştirme sürümü mevcut C# hesaplama ve PDF kurallarını korur. Windows x64 paketindeki `DogruFatura.exe` çalıştırılır; ek .NET veya Excel kurulumu gerekmez. ZIP'in tamamını tek klasöre çıkarın. Bu taşınabilir önizleme paketi kurulum sihirbazı değildir.

## Günlük iş

- Ana ekrandaki kartlar aktif faturaları, bugünden sonraki yedi gün içindeki vadeleri, gecikmiş faturaları, HYS bekleyen listeleri ve kontrol bulgularını açar.
- Çalışmaya devam et mevcut listeye yönlendirir. Geçmiş mali özet açılır bölümde kalır.
- Süreç şeridi: Faturalar → Kontrol → Çıktı → HYS kapanışı. HYS kapanışı banka ödeme teyidi değildir.
- Kurum adının bir bölümüyle arama, son kullanılan kurumlar, Kaydet ve yeni fatura, Kaydet ve listeye git ve isteğe bağlı tarih koruma seri girişi kolaylaştırır.
- Hesap ayrıntısı kullanılan kuralı ve ara hesapları gösterir. Elektrik kesinti alanları yalnız ilgili türde görünür.

## Hibrit Excel girişi

Fatura listesinde **Excel'den yapıştır** seçilir. Başlıkları kopyala ile sütun sırası alınır:

Kurum | Tür | Fatura No | Fatura Tarihi | Son Ödeme | Tutar | KDV | Kesinti Tazminatı (isteğe bağlı)

Excel'den kopyalanan hücreler sekmeyle ayrılır. Para örneği `1.200,00`, tarih örneği `27.10.2026`. Kurum adı mevcut kayıtta bulunmalı; hizmet numarası kurumdan alınır. Başındaki sıfırlar korunur. Matrah, damga ve ödeme C# motorunda hesaplanır. Tek bir hatalı/mükerrer/karışık tür satırı varsa toplu kayıt yapılmaz. Boş fatura numaralı olası mükerrerler tekli girişte karşılaştırılmalıdır.

## Liste ve taslaklar

- Arama, vade aralığı ve fatura tutarı filtresi; görünen ve seçili kayıtların ayrı toplamları.
- Ctrl+F arama; Ctrl+tıklama veya Shift ile çoklu seçim. Düzenle ve sil tek seçili odak kaydına uygulanır.
- Sütunlar menüsü, sıralama, genişlik ve sütun yeri tercihleri; kurum sütunu her zaman görünür.
- Son silinen fatura aynı oturumda düğmeden veya metin alanı dışında Ctrl+Z ile geri alınır. Tür/mükerrer çakışması varsa geri alma durdurulur.
- Rahat/Sıkı görünüm ve tarih koruma tercihi kalıcıdır.
- Taslaklar aynı türde ayrı ödeme listelerini saklar. Yeni listeye veya başka taslağa geçmeden önce mevcut kayıtlar yeni bir taslak kopyasına yazılır. Açılan taslağın önceki kopyası rafta kalır; bunlar otomatik silinmez.

## PDF ve HYS

- PDF aktarımında Doğrulandı / İnceleme gerekiyor / Zaten arşivde durumları görünür.
- Kaynağı incele metin katmanını ve çıkarılmış satırları birlikte gösterir; özgün PDF ayrıca açılabilir.
- Düzelterek doğrula: kullanıcı satırları hibrit girişte düzeltir, sonra özgün belgedeki brüt/damga/net toplamları ve düzeltme gerekçesini girer. Fatura kuralları ve üç toplam eşleşmeden arşiv oluşmaz. HYS mükerrer kontrolü korunur; özgün PDF değiştirilmez.
- Kullanıcı açıklaması arşiv kaydı ve içe aktarma denetim olayında saklanır. Sayısal kayıtlar kapalı arşiv mührüyle, özgün belge PDF SHA-256 ile korunur; kullanıcı açıklaması normal arşivin eski mühür sözleşmesine dahil değildir.
- HYS kapanışında liste numarası, adet, tür, toplam ve HYS numarası tekrar gösterilir.
- Arşivde İşlem geçmişi; istatistikte toplamı oluşturan faturalar ve kurum bazında önceki dönem farkları açılabilir. Tutar artışı tüketim artışı olarak yorumlanmaz.

## Görünüm ve veri koruma

İçerik artık tümüyle küçültülmez. Dar pencerede menü simgelere iner, fatura formu tek sütuna geçer. Geniş tablo ve eski bileşik paneller okunur boyutlarda kaydırılır. Rahat/Sıkı görünüm tablo satır yoğunluğunu değiştirir.

Veri kökü mevcut Masaüstü/DoğruFatura klasörüdür. `Veri/FaturaTaslaklari.json` ve `Veri/GorunumTercihleri.json` tam yedeklere dahil edilir. Okunamayan taslak veya görünüm dosyası sessizce boş dosyayla değiştirilmez. Güvenlik ekranı önceki oturumlardaki son yedeği de gösterir; ikinci disk/ağ konumuna isteğe bağlı yedek kopyası oluşturulabilir. Geri yüklemeden önce içerik sayıları gösterilir, bütünlük kontrolü ve koruyucu yedek mevcut motorla devam eder.

## Geliştirici doğrulaması

`dotnet test DogruFatura.sln -c Release` bütün testleri çalıştırır. WPF bütün sayfa testi geçici klasörde örnek veri oluşturur; gerçek kullanıcı kayıtlarına yazmaz. `DOGRUFATURA_SCREENSHOTS` mutlak klasör yolu verilirse sayfa PNG'leri üretilir. Paket: `scripts/Publish-WorkspacePreview.ps1`.
