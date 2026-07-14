# Slowmode

Sohbete genel yavaş mod uygular; açıkken oyuncular mesajlar arasında belirlenen saniye kadar beklemek zorunda kalır.

## Özellikler

- `!slowmode <saniye>` ile tüm sunucuya yavaş mod (sınırlar config'ten)
- `!slowmode off` veya `!slowmode 0` ile kapatma (`0` her zaman geçerli, min sınırından etkilenmez)
- Süre dolmadan yazan oyuncunun mesajı engellenir, kalan süre kendisine bildirilir
- Muafiyet flag'i config'ten ayarlanabilir (varsayılan `@css/chat`, boş bırakılırsa kimse muaf olmaz)
- Açılış/kapanış tüm sunucuya duyurulur
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Kurulum

1. Derlenmiş `Slowmode` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/Slowmode/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load Slowmode` komutunu çalıştırın.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_slowmode <saniye>` | Yavaş modu belirtilen saniye aralığıyla açar | `@css/chat` |
| `css_slowmode off` | Yavaş modu kapatır | `@css/chat` |

## Yapılandırma

`configs/plugins/Slowmode/Slowmode.json` (ilk yüklemede otomatik oluşur):

| Ayar | Açıklama | Varsayılan |
| --- | --- | --- |
| `slowignore_flag` | Yavaş moddan etkilenmeyecek admin flag'i (boş = herkes etkilenir) | `@css/chat` |
| `slow_min` | Komutla girilebilecek en düşük saniye (en az 1) | `1` |
| `slow_max` | Komutla girilebilecek en yüksek saniye | `300` |

Mesajlar ve sohbet ön eki `lang/tr.json` / `lang/en.json` üzerinden düzenlenebilir.

## Kullanım Örneği

```
!slowmode 10  → Yavaş mod açıldı! Mesajlar arasında 10 saniye beklemelisin.
!slowmode off → Yavaş mod kapatıldı.
```
