# DoğruFatura 1.1 — Hızlı Kullanım Kılavuzu

## 1. Programı açma

DoğruFatura Windows 10/11 64-bit için hazırlanmıştır. Dağıtım Setup'ı self-contained olduğundan hedef bilgisayarda .NET SDK, Visual Studio veya Excel kurulması gerekmez.

Kullanıcı kayıtları uygulama klasöründe değil, Masaüstü altındaki `DoğruFatura` klasöründe tutulur. Program güncellense veya kaldırılsa bile kullanıcı verileri korunur.

## 2. Temel klasörler

- `Masaüstü\DoğruFatura\Veri` — kurum, fatura, yetkili ve arşiv kayıtları
- `Masaüstü\DoğruFatura\Aktif Çıktılar` — henüz HYS ile kapatılmamış PDF çıktıları
- `Masaüstü\DoğruFatura\Arşivlenenler` — HYS ile kapatılmış PDF belgeleri
- `Masaüstü\DoğruFatura\Yedekler` — güvenlik ve güncelleme öncesi yedekler
- `Masaüstü\DoğruFatura\Loglar` — beklenmeyen hata kayıtları

## 3. Günlük kullanım sırası

1. **OKUL / KURUM** bölümünde kurum kayıtlarını kontrol edin.
2. **YETKİLİ / İMZA** bölümünde gerçekleştirme görevlisi ve harcama yetkilisini doğrulayın.
3. **FATURA GİRİŞİ** bölümünde kurum, fatura türü, fatura numarası, tarihler ve tutarları girin.
4. Hesaplama alanlarını kontrol edin ve faturayı aktif listeye ekleyin.
5. **FATURA LİSTESİ** bölümünde kayıtları son kez kontrol edin.
6. **RAPORLAR / ÇIKTI** bölümünden PDF oluşturun veya yazdırın.
7. İşlem tamamlandığında **İŞLEM ARŞİVİ** üzerinden listeyi arşivleyip HYS Ödeme Emri Belge No ile kapatın.

Fatura numarası boş olan yeni kayıt, aynı kurum/hizmet, tür, tarihler ve tutarlara sahip başka bir boş-numaralı kayıtla eşleşirse olası mükerrer uyarısı gösterilir. Gerçekten ayrı bir fatura ise kullanıcı onayıyla kayda devam edilebilir.

## 4. Geçmiş PDF'yi arşive ekleme

**İŞLEM ARŞİVİ → ARŞİV EKLE** ile eski DoğruFatura PDF belgeleri eklenebilir. Çoklu dosya aktarımında ilerleme gösterilir ve işlem iptal edilebilir. Dosya adında sıra numarası bulunabilir; örneğin `48) HYS-2026-34546180.pdf` kabul edilir ve HYS numarası standart biçime dönüştürülür. Kaynak PDF silinmez.

## 5. İstatistik

**İSTATİSTİK** ekranında arşiv kayıtları:
- yıl ve kurum,
- başlangıç / bitiş tarihi,
- fatura türü

ile filtrelenebilir.

Fatura Türü Dağılımı ödeme tutarlarının türlere göre payını gösterir. **ÇIKTI AL** menüsünden PDF, Yazdır, CSV, Excel veya PNG seçilebilir. CSV çıktısı Türkçe Excel kullanımına uygun `;` ayraçlı, UTF-8 ve virgüllü ondalık biçimindedir.

## 6. Güvenlik yedeği ve geri yükleme

**HAKKINDA / GÜVENLİK** bölümünde:
- **KONTROL ET** — veri/PDF bütünlüğünü denetler.
- **YEDEK OLUŞTUR** — kullanıcı JSON kayıtları ve PDF belgeleri için tam güvenlik yedeği oluşturur.
- **GERİ YÜKLE** — seçilen yedeği doğrulayıp geri yükler; önce mevcut durumu ayrıca yedekler.

## 7. Tanılama paketi

**HAKKINDA / GÜVENLİK → GELİŞMİŞ / DESTEK → TANILAMA PAKETİ** destek amaçlı ZIP oluşturur.

Tanılama paketi:
- fatura JSON kayıtlarını içermez,
- PDF belgelerini içermez,
- yalnız çalışma ortamı özeti ile sınırlı hata günlüklerini içerir,
- loglardaki kullanıcı/makine yolları ve yaygın kişisel veri örüntülerini maskeler.

## 8. Beklenmeyen kapanma

Program normal kapanmadığını algılarsa bir sonraki açılışta mevcut durumu korumak için otomatik güvenlik yedeği almaya çalışır ve kullanıcıyı bilgilendirir. Ardından **HAKKINDA / GÜVENLİK → KONTROL ET** kullanılabilir.

## 9. Klavye kısayolları

- `Ctrl + F` — açık ekranda arama alanına git
- `Ctrl + P` — rapor ekranında yazdır
- `Enter` — uygun formda ana kaydet/ekle işlemi
- `Esc` — form düzenleme/temizleme işleminden vazgeç
- `Alt + A` — Ana Ekran
- `Alt + F` — Fatura Girişi
- `Alt + Y` — Yetkili / İmza
- `Alt + L` — Fatura Listesi
- `Alt + R` — Raporlar / Çıktı
- `Alt + O` — Okul / Kurum
- `Alt + I` — İşlem Arşivi
- `Alt + K` — Kontrol Merkezi
- `Alt + H` — Hakkında / Güvenlik

## 10. Güncelleme ve kaldırma

Setup yeni sürüm kurulurken Program Files altındaki program dosyalarını yeniler. Masaüstü `DoğruFatura` klasöründeki kullanıcı verileri korunur. Program kaldırıldığında da kullanıcı veri klasörü bilinçli olarak silinmez.

## 11. Sorun yaşandığında

Önce **KONTROL MERKEZİ** ve **HAKKINDA / GÜVENLİK → KONTROL ET** kullanılmalıdır. Sorun devam ederse **GELİŞMİŞ / DESTEK → TANILAMA PAKETİ** oluşturularak destek incelemesi için kullanılabilir.
