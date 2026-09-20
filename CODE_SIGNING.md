# DoğruFatura Windows Kod İmzalama

DoğruFatura kod imzalamayı destekler ancak **1.1.0 dağıtımında kod imzası zorunlu değildir**.

Kullanıcı, kod imzalama sertifikası bulunmadığı için 1.1.0 sürümünü bilinçli olarak **imzasız** dağıtmayı kabul etmiştir. İmzasız pakette Windows SmartScreen / "Bilinmeyen yayıncı" uyarısı görülebilir. Bu nedenle dağıtımda Setup EXE ile birlikte SHA-256 doğrulama dosyası verilmelidir.

Depoya sertifika, PFX dosyası, özel anahtar veya parola **asla** eklenmez.

## İmzalı üretim varsa

Windows kod imzalama sertifikası mevcutsa:

1. Sertifikayı Windows sertifika deposuna güvenli biçimde yükleyin.
2. SHA-1 thumbprint değerini `DOGRUFATURA_SIGN_CERT_SHA1` ortam değişkenine verin.
3. `FINAL_EXE_OLUSTUR.bat` çalıştırın.
4. Araç yayınlanan `DogruFatura.exe` ve Setup EXE'yi SHA-256 ile imzalar.
5. `signtool verify /pa` ile imzayı doğrular.

```powershell
$env:DOGRUFATURA_SIGN_CERT_SHA1 = '<sertifika-thumbprint>'
.\FINAL_EXE_OLUSTUR.bat
```

## İmzasız final dağıtım

Sertifika yoksa:

`FINAL_EXE_OLUSTUR.bat`

otomatik olarak:

`SETUP_HAZIRLA.ps1 -AllowUnsignedDistribution`

modunu kullanır.

Bu mod:
- final sürüm için bilinçli imzasız dağıtımı açıkça işaretler,
- tek self-contained Setup EXE üretir,
- SHA-256 dosyası üretir,
- SHA-256 değerini ikinci kez doğrular,
- SmartScreen / Bilinmeyen yayıncı uyarısının beklenebileceğini ekranda belirtir.

Dağıtımda birlikte verilmesi önerilir:
- `DogruFatura_Setup_1.1.0_x64.exe`
- `DogruFatura_Setup_1.1.0_x64_SHA256.txt`
- `KURULUM_NOTU.txt`

## Smoke modu ayrıdır

`-AllowUnsignedFinal` yalnız yerel/CI smoke testi içindir.

Gerçek imzasız dağıtım için:
- `-AllowUnsignedDistribution`

kullanılır.

Bu ayrım, smoke paketinin yanlışlıkla dağıtım-ready final olarak değerlendirilmesini engeller.

## RC/test sürümleri

`VersionSuffix` doluyken normal final Setup üretilmez. `-AllowPrerelease` yalnız RC/CI kurulum testleri içindir.
