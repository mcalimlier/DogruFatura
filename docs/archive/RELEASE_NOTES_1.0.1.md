# DoğruFatura 1.0.1

DoğruFatura 1.0.1, 1.0.0 final sürümünün bakım ve sağlamlaştırma güncellemesidir.

## Öne çıkan iyileştirmeler

- Karşılama ekranındaki yükleme çubuğu daha akıcı hale getirildi ve ilerleme profili otomatik testlerle koruma altına alındı.
- Büyük `MainWindow.xaml.cs` monoliti görev bazlı partial sınıflara ayrılarak bakım kolaylığı ve değişiklik güvenliği artırıldı.
- Fatura girişi, liste, raporlar, kurum/yetkililer, kontrol merkezi, arşiv, navigasyon, güvenlik ve pencere davranışları ayrı kod bölümlerine taşındı.
- Kullanıcıya gösterilen hata mesajlarında teknik ayrıntı sızıntısını azaltan güvenlik iyileştirmeleri korundu.
- CI kalite kapıları; Release derleme, otomatik testler, coverage, bağımlılık güvenlik taraması ve setup smoke testleriyle doğrulanmaktadır.

## Kurulum ve veri güvenliği

Kurulum paketi temiz kurulum, mevcut kurulum üstüne güncelleme, kaldırma ve kullanıcı verilerinin korunması senaryolarıyla test edilir. Masaüstü `DoğruFatura` altındaki kullanıcı verileri ve arşivler kurulum/kaldırma sırasında korunur.

## Sürüm

- Uygulama sürümü: `1.0.1`
- Hedef platform: Windows x64
