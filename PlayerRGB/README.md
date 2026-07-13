# PlayerRGB

Oyuncu modelini akıcı bir RGB (gökkuşağı) döngüsüyle renklendirir. Komutla açılıp kapatılır ve tercih kalıcı olarak saklanır.

## Özellikler

- Model rengi her tick'te kırmızı → yeşil → mavi döngüsüyle yumuşak geçiş yapar
- Tercih `PlayerRGB.json` dosyasında saklanır — oyuncu sunucuya tekrar girdiğinde otomatik açılır
- Kapatıldığında model rengi anında normale döner
- RGB aktif oyuncu yokken tick maliyeti sıfıra yakındır
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Kurulum

1. Derlenmiş `PlayerRGB` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/PlayerRGB/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load PlayerRGB` komutunu çalıştırın.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_rgb` | RGB efektini açar/kapatır | `@css/cheats` |

## Yapılandırma

Config dosyası yoktur. Açık durumdaki oyuncuların SteamID64 listesi eklenti klasöründeki `PlayerRGB.json` dosyasında tutulur:

```json
[
  "76561198000000000",
  "76561198000000001"
]
```

## Notlar

- Efekt yalnızca hayattaki oyunculara uygulanır.
- Renk döngüsü sunucu genelinde ortaktır (tüm RGB'li oyuncular aynı anda aynı rengi alır).
- Dosya yazma işlemleri arka planda yapılır.