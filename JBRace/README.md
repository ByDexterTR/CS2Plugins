# JBRace

Jailbreak sunucuları için yarış etkinliği. Warden başlangıç ve bitiş noktası belirler, T oyuncuları bitişe ilk ulaşan olmak için yarışır.

## Özellikler

- CenterHtml menü ile tam yarış yönetimi: başlat, iptal, başlangıç/bitiş noktası, kazanan sayısı, işaretçileri temizle
- Bitiş noktasında dönen **coin modeli** ve gökyüzüne uzanan **yeşil ışın** işaretçisi
- 3 saniyelik HUD geri sayımı; yarışçılar başlangıç noktasına ışınlanıp dondurulur
- Yarışan T'ler kırmızı, bitişe ulaşanlar yeşil renge boyanır
- Hedeflenen kazanan sayısına ulaşıldığında **kazanamayan T'ler slay edilir** ve yarış sona erer
- Kazanan sayısı sohbete yazılarak belirlenir (menüden istendiğinde)
- Raunt başında yarış otomatik sıfırlanır
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.369+

## Kurulum

1. Derlenmiş `JBRace` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/JBRace/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load JBRace` komutunu çalıştırın.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_race` | Yarış menüsünü açar | `@css/generic` **veya** `@jailbreak/warden` |

### Menü Seçenekleri

| Seçenek | İşlev |
| --- | --- |
| Yarışı Başlat / İptal Et | Yarışı başlatır (başlangıç+bitiş ayarlanmış olmalı) veya aktif yarışı iptal eder |
| Başlangıç Noktası | Bulunduğunuz konumu ve bakış yönünü başlangıç olarak kaydeder |
| Bitiş Noktası | Bulunduğunuz konumu bitiş olarak kaydeder, işaretçiyi diker |
| Kazanan Sayısı (N) | Sohbete sayı yazmanızı ister; ilk N kişi kazanır |
| İşaretçileri Temizle | Bitiş modelini/ışınını kaldırır |

## Yapılandırma

Config dosyası yoktur. Bitiş yarıçapı (64 birim) ve geri sayım süresi (3 sn) kaynak kodda tanımlıdır.

## Kullanım Örneği

1. Warden bitiş çizgisine gider → `!race` → "Bitiş Noktası".
2. Yarışın başlayacağı yere gider → "Başlangıç Noktası".
3. "Kazanan Sayısı" → sohbete `3` yazar.
4. "Yarışı Başlat" → tüm canlı T'ler başlangıca ışınlanır, 3-2-1 geri sayımı sonrası yarış başlar.
5. Bitişe ilk 3 ulaşan kazanır; kalan T'ler otomatik slay edilir.

## Notlar

- Yalnızca **hayattaki T oyuncuları** yarışa katılır.
- Coin modeli (`models/coop/challenge_coin.vmdl`) sunucu tarafından otomatik precache edilir.