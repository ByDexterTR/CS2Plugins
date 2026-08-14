# AntiCapsLock

*Bu dosyanın [İngilizcesi / English](README.md).*

Sohbette aşırı büyük harf kullanımını engeller; mesajın büyük harf oranı eşiği aşarsa mesaj otomatik küçültülür veya silinir.

## Özellikler

- İki mod: mesajı küçük harfe çevirme veya mesajı silip oyuncuyu uyarma
- Eşik oranı config'ten ayarlanır (`0.0` - `1.0` arası; `0.5` = mesajın %50'si)
- Oran yalnızca harfler üzerinden hesaplanır; rakam, noktalama ve renk kodları sayılmaz
- Kısa mesajlar için minimum harf sayısı sınırı (`OK`, `AY` gibi mesajlar tetiklemez)
- `!` ve `/` ile başlayan komut mesajları yok sayılır
- Muafiyet flag'i config'ten ayarlanabilir (boş bırakılırsa herkes etkilenir)
- Küçültme, Türkçe karakter kurallarına göre yapılır (`lowercase_culture`)
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Kurulum

1. Derlenmiş `AntiCapsLock` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/AntiCapsLock/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load AntiCapsLock` komutunu çalıştırın.

## Yapılandırma

`configs/plugins/AntiCapsLock/AntiCapsLock.json` (ilk yüklemede otomatik oluşur):

| Ayar | Açıklama | Varsayılan |
| --- | --- | --- |
| `mode_capslock` | `1` = mesajdaki karakterleri küçült, `2` = oyuncuyu uyar ve mesajı sil | `1` |
| `threshold_capslock` | Devreye girme eşiği; mesajdaki harflerin büyük harf oranı (`0.0` - `1.0`) | `0.5` |
| `minlength_capslock` | Kontrolün uygulanması için mesajdaki en az harf sayısı | `4` |
| `lowercase_culture` | Küçültmede kullanılacak dil kuralı (boş = dilden bağımsız) | `tr-TR` |
| `capsignore_flag` | Kontrolden muaf admin flag'i (boş = herkes etkilenir) | `` |

Uyarı mesajı ve sohbet ön eki `lang/tr.json` / `lang/en.json` üzerinden düzenlenebilir.

## Kullanım Örneği

`threshold_capslock: 0.5` ile:

```
mode_capslock: 1
"MERHABA ARKADAŞLAR" → "merhaba arkadaşlar"

mode_capslock: 2
"MERHABA ARKADAŞLAR" → mesaj silinir + "Çok fazla büyük harf kullandın, mesajın silindi!"

"Merhaba arkadaşlar" → her iki modda da dokunulmaz (oran %50'nin altında)
```
