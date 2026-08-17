# CTKov

*Bu dosyanın [İngilizcesi / English](README.md).*

Tek komutla, warden yetkisi olmayan tüm CT oyuncularını T takımına gönderir. Jailbreak'te "CT kovma" etkinliği için kullanılır.

## Özellikler

- Warden (`@jailbreak/warden`) yetkisine sahip oyuncular korunur — takımda kalır
- Botlar işleme dahil edilmez
- Taşınan gardiyan sayısı tüm sunucuya duyurulur
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Kurulum

1. Derlenmiş `CTKov` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/CTKov/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load CTKov` komutunu çalıştırın.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_ctkov` | Warden olmayan tüm CT'leri T takımına taşır | `@css/generic` **veya** `@jailbreak/warden` |

## Yapılandırma

```
csgo/addons/counterstrikesharp/configs/plugins/CTKov/CTKov.json
```

| Ayar | Tip | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `ctkov_cmd` | string | `"css_ctkov,css_kovct"` | Çalıştıran komutlar, virgülle ayrılır |
| `ctkov_flag` | string | `"@jailbreak/warden,@css/generic"` | Kullanabilecek flagler, virgülle ayrılır |

Mesajlar `lang/tr.json` / `lang/en.json` üzerinden düzenlenebilir.

## Kullanım Örneği

```
!ctkov
```

> `[ByDexter] AdminAdı tüm gardiyanları kovdu! (5 oyuncu T takımına taşındı)`