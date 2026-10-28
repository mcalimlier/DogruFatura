# DoğruFatura çalışma masası uygulama planı

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans. Kullanıcı önceki değerlendirmedeki bütün önerilerin uygulanmasını istedi ve Excel yaklaşımının seçimini geliştirmeye bıraktı.

**Goal:** Günlük çalışma masası, okunabilir uyarlanabilir arayüz, hızlı giriş, hibrit tablo, taslaklar, arşiv doğrulaması ve izlenebilirlik.
**Architecture:** Mevcut .NET 8/WPF hesap ve belge motorları korunur. Yeni iş kuralları Core servislerinde, ekran ekleri ayrı WPF bileşenlerinde; eski veri dosyaları yerinde kalır.
**Tech Stack:** C#, WPF, xUnit, mevcut PDF/Excel kütüphaneleri.
**Spec:** Kullanıcının onayladığı önceki değerlendirme ve aşağıdaki kapsam.

## Global Constraints
- Vergi oranları ve resmî belge içeriği değiştirilmez; Excel kurulumu gerekmez.
- Kapalı HYS arşivi banka ödeme teyidi olarak sunulmaz.
- Taslak ve görünüm dosyaları mevcut Veri kökünde, güvenlik yedeğinin kapsamında tutulur.
- Toplu yapıştırmada önizleme ve tam doğrulama olmadan kayıt yapılmaz; tanımsız kurum, karışık tür ve mükerrer kayıt engellenir.
- Her mevcut veri yazma hatasında bellekteki değişiklik geri alınır.

## Review Focus
- Türkçe arama, baştaki sıfırlar, geçersiz tarihler ve tutarlar.
- Boş liste, karışık fatura türü, mükerrer ve boş numaralı olası mükerrer.
- Küçük pencere/yüksek DPI: metin küçültülmeden kaydırma ve yeniden yerleşim.
- Bozuk taslak/ayar dosyasında mevcut içeriğin üzerine yazmama.
- PDF incelemesinde doğrulanmamış sonucu arşive geçirmeme.

## Tasks
- [x] Başlangıç Release derlemesi ve mevcut test tabanını doğrula.
- [x] Çalışma özeti, Türkçe filtre, toplu TSV doğrulama ve taslak deposu davranış testlerini önce yaz; başarısızlığı gözle, uygula ve geçir.
- [x] Ana ekran eylem kartları, süreç şeridi, seri kayıt ve hesap ayrıntılarını bağla.
- [x] Liste arama/tarih/tutar filtresi, seçili/filtreli toplam, sütun tercihleri, geri alma ve hibrit Excel yapıştırmasını bağla.
- [x] Çoklu taslak geçişini güvenli kalıcı kayıtlarla bağla.
- [x] Sabit ölçeklemeyi kaldır; dar pencere düzeni ve yoğunluk tercihini uygula.
- [x] HYS kapanış önizlemesi, geçmiş, PDF kaynak/satır incelemesi ve istatistik kayıt detaylarını ekle.
- [x] Yedek zamanı, ikinci konum ve geri yükleme önizlemesini geliştir.
- [ ] Release build/test, izole örnek veriyle WPF görsel kontrolü, bağımsız gözle kod incelemesi ve çalıştırılabilir paket.

## Ledger
- 2026-10-27: Kaynak GitHub main'den source/ klasörüne klonlandı. codex/dogrufatura-workspace dalı; kullanıcı dosyası olmayan bağımsız checkout.
- Referanslar: Ek Ders projesi Excel kurmadan canlı hibrit tablo/JSON; OEHS doğrulamalı resmî belge, dönem takibi ve koruyucu yedek yaklaşımı.
- Başlangıç: 155/155 test. Yeni çekirdek davranışları, gerekçeli PDF toplam doğrulaması ve gerçek WPF kayıt/taslak/ayar akışı ile toplam 166 test.
- WPF beklemesi araştırıldı: çalışma zamanı logo URI'si test giriş derlemesinde aranıyordu. Kaynak derlemesi açıkça belirtildi; pencere daraltma ve yeniden büyütme regresyonu eklendi.
- Gözden geçirme: taslak yazma hatası mevcut listeyi koruyor; arşiv değişiminde geri alma temizleniyor; istatistik ayrıntıları aynı filtre ve mükerrer kimlik elemesini kullanıyor; PDF düzeltmesi kural ve toplam kontrolünü atlayamıyor.
- Ruling: İkinci ajan aracı bu oturumda yok; bütün değişikliklerin incelemesi burada, test ve ekran kanıtlarıyla yapıldı. Ana dal birleştirilmez; önizleme ayrı geliştirme dalında sunulur.
