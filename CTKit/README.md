# CTKit

CT oyuncularının her spawn'da otomatik alacağı birincil ve ikincil silahı menüden seçmesini sağlar. Jailbreak gardiyan kiti sistemidir.

## Özellikler

- CenterHtml menü ile birincil / ikincil silah seçimi
- Seçimler oyuncu bazında hatırlanır (oyuncu ayrılana kadar)
- Seçim yapmayanlara config'teki varsayılan silahlar verilir
- Spawn'da bıçak hariç tüm silahlar temizlenip kit verilir
- Silah listeleri tamamen config üzerinden özelleştirilebilir
- Menüde "kiti sıfırla" seçeneği
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.369+

## Kurulum

1. Derlenmiş `CTKit` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/CTKit/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load CTKit` komutunu çalıştırın.
3. İlk yüklemede config dosyası otomatik oluşturulur.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_kit` | Silah kiti menüsünü açar | — (yalnızca CT takımı) |

## Yapılandırma

Config dosyası ilk yüklemede otomatik oluşturulur:

```
csgo/addons/counterstrikesharp/configs/plugins/CTKit/CTKit.json
```

| Ayar | Tip | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `default_primary_weapon` | string | `weapon_ak47` | Seçim yapılmamışsa verilecek birincil silah |
| `default_secondary_weapon` | string | `weapon_deagle` | Seçim yapılmamışsa verilecek ikincil silah |
| `primary_weapons` | liste | AK47, M4A4, M4A1-S, AWP, MAG7 | Menüde sunulacak birincil silahlar |
| `secondary_weapons` | liste | Deagle, CZ75A, Tec9, Dual Beretta, USP-S, Glock, Revolver | Menüde sunulacak ikincil silahlar |

Her silah kaydı iki alandan oluşur:

| Alan | Açıklama |
| --- | --- |
| `weapon_name` | Oyun içi entity adı (`weapon_` ön ekiyle) |
| `display_name` | Menüde gösterilecek isim |

### Örnek Config

```json
{
  "default_primary_weapon": "weapon_ak47",
  "default_secondary_weapon": "weapon_deagle",
  "primary_weapons": [
    { "weapon_name": "weapon_ak47", "display_name": "AK47" },
    { "weapon_name": "weapon_m4a4", "display_name": "M4A4" },
    { "weapon_name": "weapon_awp", "display_name": "AWP" }
  ],
  "secondary_weapons": [
    { "weapon_name": "weapon_deagle", "display_name": "DEAGLE" },
    { "weapon_name": "weapon_usp_silencer", "display_name": "USP-S" }
  ]
}
```

## Kullanım Örneği

1. CT oyuncusu `!kit` yazar → menü açılır (mevcut seçimler başlıkta görünür).
2. "Birincil Silah" → listeden AWP seçer.
3. Bir sonraki spawn'da otomatik olarak AWP + seçili tabanca verilir.

## Notlar

- Kit yalnızca **CT takımına** ve spawn anında uygulanır; T oyuncuları etkilenmez.
- Menü başlığındaki ikon repodaki [`img/pistol.png`](../img/pistol.png) dosyasından yüklenir.