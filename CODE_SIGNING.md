# DoğruFatura Windows Kod İmzalama

DoğruFatura Setup üretim aracı isteğe bağlı Authenticode imzasını destekler. Depoya sertifika, PFX dosyası, özel anahtar veya parola **asla** eklenmez.

## Üretim kullanımı

1. Windows kod imzalama sertifikası bilgisayardaki sertifika deposuna güvenli biçimde yüklenir.
2. Sertifikanın SHA-1 thumbprint değeri yalnız üretim ortamında `DOGRUFATURA_SIGN_CERT_SHA1` ortam değişkenine verilir.
3. `SETUP_HAZIRLA.ps1` çalıştırılır.
4. Araç önce yayınlanan `DogruFatura.exe` dosyasını, ardından oluşan Setup EXE'yi SHA-256 ile imzalar ve `signtool verify /pa` ile doğrular.
5. Zaman damgası varsayılan olarak `http://timestamp.digicert.com` üzerinden SHA-256 ile alınır; gerekirse `-TimestampUrl` parametresiyle değiştirilebilir.

Örnek:

```powershell
$env:DOGRUFATURA_SIGN_CERT_SHA1 = '<sertifika-thumbprint>'
.\SETUP_HAZIRLA.ps1
```

## Sertifika yoksa

Araç imzasız paket üretir ve konsolda açık bir uyarı verir. Sertifika olmadığı halde program sahte, test veya kendi kendine imzalanmış bir sertifikayı final ürün gibi kullanmaz.

## RC güvenlik kilidi

`Directory.Build.props` içindeki `VersionSuffix` doluyken normal `SETUP_HAZIRLA.ps1` final paket üretmez. `-AllowPrerelease` yalnız CI/RC kurulum testleri içindir.
