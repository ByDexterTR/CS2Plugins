# JBTeams

Hayattaki T oyuncularını renkli takımlara bölen etkinlik eklentisi. Aynı takımdaki oyuncular birbirine hasar veremez; son takım ayakta kalana kadar mücadele sürer.

## Özellikler

- 2–5 takım desteği: Kırmızı, Yeşil, Mavi, Sarı, Magenta
- Oyuncular rastgele karıştırılıp **eşit sayıda** dağıtılır (eşit bölünemiyorsa uyarı verir)
- Aynı takım üyeleri arasındaki hasar otomatik sıfırlanır (friendly fire koruması)
- Her oyuncu takım rengine boyanır; ölen oyuncunun rengi sıfırlanır
- Tek takım kaldığında kazanan duyurulur ve sistem kapanır
- `!takim 0` veya `!takim 1` ile takımlar manuel kapatılabilir
- Raunt başında/sonunda otomatik sıfırlama
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Kurulum

1. Derlenmiş `JBTeams` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/JBTeams/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load JBTeams` komutunu çalıştırın.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_takim <2-5>` | Canlı T'leri belirtilen sayıda takıma böler | `@css/generic` **veya** `@jailbreak/warden` |
| `css_takim <0-1>` | Aktif takım sistemini kapatır | `@css/generic` **veya** `@jailbreak/warden` |

## Yapılandırma

Config dosyası yoktur. Takım renkleri ve isimleri kaynak kodda tanımlıdır; mesajlar `lang/tr.json` / `lang/en.json` üzerinden düzenlenebilir.

## Kullanım Örneği

```
!takim 2
```

> 8 canlı T → 4 kişilik Kırmızı ve Yeşil takımlar oluşturulur.
> Kırmızı takımdan biri Yeşil'in tamamını elediğinde: `Kırmızı kazandı.`

## Notlar

- Canlı T sayısı takım sayısına tam bölünemiyorsa sistem başlamaz (ör. 7 oyuncu / 2 takım).
- Yalnızca **T takımındaki canlı oyuncular** dahil edilir.