# DoğruFatura 1.0.0 — Hızlı Kullanım Kılavuzu

## 1. Programı açma

DoğruFatura Windows 10/11 64-bit için hazırlanmıştır. Final Setup self-contained olduğundan hedef bilgisayarda .NET SDK, Visual Studio veya Excel kurulması gerekmez.

Program kullanıcı kayıtlarını uygulama klasöründe değil, Masaüstü altındaki `DoğruFatura` klasöründe tutar. Bu nedenle program güncellense veya kaldırılsa bile kullanıcı verileri korunur.

## 2. Temel klasörler

- `Masaüstü\DoğruFatura\Veri` — kurum, fatura, yetkili ve arşiv kayıtları
- `Masaüstü\DoğruFatura\Aktif Çıktılar` — henüz HYS ile kapatılmamış PDF çıktıları
- `Masaüstü\DoğruFatura\Arşivlenenler` — HYS ile kapatılmış PDF belgeleri
- `Masaüstü\DoğruFatura\Yedekler` — güvenlik ve güncelleme öncesi yedekler
- `Masaüstü\DoğruFatura\Loglar` — beklenmeyen hata kayıtları

## 3. İlk kullanım sırası

1. **OKUL / KURUM** bölümünde kurum kayıtlarını kontrol edin.
2. **YETKİLİ / İMZA** bölümünde gerçekleştirme görevlisi ve harcama yetkilisini doğrulayın.
3. **FATURA GİRİŞİ** bölümünde kurum, fatura türü, fatura numarası, tarihler ve tutarları girin.
4. Hesaplama alanlarını kontrol edin ve faturayı aktif listeye ekleyin.
5. **FATURA LİSTESİ** bölümünde kayıtları son kez kontrol edin.
6. **RAPORLAR / ÇIKTI** bölümünden PDF oluşturun veya yazdırın.
7. İşlem tamamlandığında **İŞLEM ARŞİVİ** üzerinden listeyi arşivleyip HYS Ödeme Emri Belge No ile kapatın.

## 4. Geçmiş PDF'yi arşive ekleme

**İŞLEM ARŞİVİ → ARŞİV EKLE** ile eski DoğruFatura PDF belgeleri eklenebilir. Dosya adında sıra numarası bulunabilir; örneğin `48) HYS-2026-34546180.pdf` kabul edilir ve HYS numarası standart biçime dönüştürülür. Kaynak PDF silinmez.

## 5. Güvenlik yedeği

**HAKKINDA / GÜVENLİK → GÜVENLİK YEDEĞİ** ile kullanıcı verileri ve kayıtlı PDF belgeleri ZIP yedeğine alınır.

**YEDEKTEN GERİ YÜKLE** işlemi seçilen yedeği doğrular. Geri yüklemeden önce mevcut durum ayrıca güvenlik amacıyla yedeklenir.

## 6. Tanılama paketi

**HAKKINDA / GÜVENLİK → TANILAMA PAKETİ** destek amaçlı bir ZIP oluşturur. Bu paket fatura JSON kayıtlarını ve PDF belgelerini içermez; yalnız çalışma ortamı özeti ile sınırlı sayıdaki hata günlüğünü içerir.

## 7. Beklenmeyen kapanma

Program normal kapanmadığını algılarsa bir sonraki açılışta mevcut durumu korumak için otomatik güvenlik yedeği almaya çalışır ve kullanıcıyı bilgilendirir. Ardından **HAKKINDA / GÜVENLİK** ekranındaki bütünlük kontrolü kullanılabilir.

## 8. Klavye kısayolları

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

## 9. Güncelleme ve kaldırma

Setup yeni sürüm kurulurken yalnız Program Files altındaki program dosyalarını yeniler. Masaüstü `DoğruFatura` klasöründeki kullanıcı verileri korunur. Program kaldırıldığında da bu kullanıcı klasörü bilinçli olarak silinmez.

## 10. Sorun yaşandığında

Önce **KONTROL MERKEZİ** ve **HAKKINDA / GÜVENLİK → KONTROL ET** kullanılmalıdır. Sorun devam ederse **TANILAMA PAKETİ** oluşturularak destek incelemesi için kullanılabilir.
