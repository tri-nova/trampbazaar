# SQL Server Kurulum

Ilk kurulum icin sirasiyla:

1. `001_initial_setup.sql`
2. `002_listing_offers.sql`

Bu script:

- `TrampBazaar` veritabanini yoksa olusturur
- MVP ve sonraki fazlari tasiyacak temel tablolari acar
- roller, izinler, satis modlari, ana kategoriler ve paketler icin baslangic verisi ekler

`002_listing_offers.sql`:

- ilan bazli teklif verme modulu icin `ListingOffers` tablosunu ekler
- ilk MVP teklif akisinin API ve mobil ekranlarina zemin hazirlar

Notlar:

- Script mevcut tablolari yeniden olusturur. Canli ortamda degil, ilk kurulum veya gelistirme ortaminda calistirin.
- `002_listing_offers.sql` artimsal script'tir; mevcut veritabani ustune guvenle calistirilabilir.
- SQL Server Management Studio uzerinden yeni query acip tum dosyayi calistirmaniz yeterli.
- Uretimde migration tabanli ilerlemeye gectigimizde bu klasore yeni `00X_*.sql` scriptleri ekleyebiliriz.
