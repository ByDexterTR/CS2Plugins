# Silahsil

Yerdeki sahipsiz silahları tek komutla temizler. Jailbreak'te hücre açılışı öncesi silah temizliği için kullanılır.

## Özellikler

- Haritadaki tüm sahipsiz (`weapon_*`) entity'lerini kaldırır
- Oyuncuların elindeki/üzerindeki silahlara dokunmaz
- Kaldırılan silah sayısı komutu kullanan oyuncuya bildirilir
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.369+

## Kurulum

1. Derlenmiş `Silahsil` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/Silahsil/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load Silahsil` komutunu çalıştırın.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_silahsil` | Yerdeki tüm silahları siler | `@css/slay` |

## Yapılandırma

Config dosyası yoktur. Mesajlar `lang/tr.json` / `lang/en.json` üzerinden düzenlenebilir.

## Kullanım Örneği

```
!silahsil → [ByDexter] AdminAdı yerdeki 12 silahı sildi.
```