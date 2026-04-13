# SQL Server Kurulum

Ilk kurulum icin sirasiyla:

1. `001_initial_setup.sql`
2. `002_listing_offers.sql`
3. `003_grant_admin_role.sql`
4. `004_schema_versioning.sql`

Bu script:

- `TrampBazaar` veritabanini yoksa olusturur
- MVP ve sonraki fazlari tasiyacak temel tablolari acar
- roller, izinler, satis modlari, ana kategoriler ve paketler icin baslangic verisi ekler

`002_listing_offers.sql`:

- ilan bazli teklif verme modulu icin `ListingOffers` tablosunu ekler
- ilk MVP teklif akisinin API ve mobil ekranlarina zemin hazirlar

Notlar:

- `001_initial_setup.sql` mevcut tablolari yeniden olusturur. Canli ortamda degil, ilk kurulum veya gelistirme ortaminda calistirin.
- `002_listing_offers.sql` artimsal script'tir; mevcut veritabani ustune guvenle calistirilabilir.
- `003_grant_admin_role.sql` sadece gerekli admin rol atamasini yapar.
- `004_schema_versioning.sql` sonraki artimsal rollout'lar icin `dbo.SchemaVersions` tablosunu olusturur ve mevcut scriptleri baseline olarak isaretler.
- SQL Server Management Studio uzerinden yeni query acip tum dosyayi calistirmaniz yeterli.
- Uretimde bundan sonraki tum scriptler additive ve idempotent yazilmali; uygulama oncesi `dbo.SchemaVersions` kontrol edilerek rollout yapilmalidir.
