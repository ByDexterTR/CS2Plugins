# Meslekmenu

T oyuncularının raunt başına bir kez "meslek" seçmesini sağlar. Her meslek farklı bir avantaj verir. Jailbreak sunucuları için tasarlanmıştır.

## Özellikler

- 5 meslek: **Doktor**, **Flash**, **Bombacı**, **Rambo**, **Zeus**
- Her meslek config'ten ayrı ayrı açılıp kapatılabilir ve özelleştirilebilir
- Raunt başına **tek seçim hakkı** (raunt başında sıfırlanır)
- Yalnızca hayattaki T oyuncuları kullanabilir
- Argümansız kullanımda aktif mesleklerin listesi ve detayları gösterilir
- Doktor/Zeus ile ilgili sunucu cvar'ları raunt başında otomatik ayarlanır
- Türkçe / İngilizce dil desteği (`lang/`)

## Meslekler

| Meslek | Avantaj |
| --- | --- |
| `doktor` | Healthshot verir (iyileşme miktarı config'ten, `healthshot_health` cvar'ı ile) |
| `flash` | Belirli süre boyunca hız çarpanı (varsayılan 3x, 5 sn) |
| `bombaci` | Rastgele bir bomba verir (smoke / HE / flash / molotof — hangileri verilebileceği config'ten) |
| `rambo` | Yüksek HP + zırh (+ opsiyonel kask); `rambo_fix` açıksa 100 HP altındakiler seçemez |
| `zeus` | Taser verir (şarj süresi ve düşürme ayarı cvar'lar ile) |

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Kurulum

1. Derlenmiş `Meslekmenu` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/Meslekmenu/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load Meslekmenu` komutunu çalıştırın.
3. İlk yüklemede config dosyası otomatik oluşturulur.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_meslek` | Aktif meslekleri ve detaylarını listeler | — (herkes) |
| `css_meslek <meslek>` | Belirtilen mesleği seçer | — (canlı T, raunt başına 1 kez) |

Kabul edilen meslek adları: `doktor`/`doctor`, `flash`, `bombaci`/`bombacı`/`bomber`, `rambo`, `zeus`

## Yapılandırma

```
csgo/addons/counterstrikesharp/configs/plugins/Meslekmenu/Meslekmenu.json
```

| Ayar | Tip | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `doktor_enabled` | bool | `true` | Doktor mesleği aktif mi |
| `doktor_regen` | int | `50` | Healthshot iyileştirme miktarı (`healthshot_health`) |
| `doktor_drop_healthshot` | bool | `true` | Ölünce healthshot düşsün mü (`mp_death_drop_healthshot`) |
| `flash_enabled` | bool | `true` | Flash mesleği aktif mi |
| `flash_speed` | float | `3.0` | Hız çarpanı |
| `flash_duration` | int | `5` | Hızın süresi (saniye) |
| `bombaci_enabled` | bool | `true` | Bombacı mesleği aktif mi |
| `bombaci_give_smoke` | bool | `true` | Smoke verilebilsin mi |
| `bombaci_give_grenade` | bool | `true` | HE verilebilsin mi |
| `bombaci_give_flashbang` | bool | `true` | Flashbang verilebilsin mi |
| `bombaci_give_molotov` | bool | `true` | Molotof verilebilsin mi |
| `rambo_enabled` | bool | `true` | Rambo mesleği aktif mi |
| `rambo_hp` | int | `150` | Rambo HP değeri |
| `rambo_armor` | int | `100` | Rambo zırh değeri |
| `rambo_helmet` | bool | `true` | Kevlar verilsin mi |
| `rambo_fix` | bool | `true` | 100 HP altındakilerin Rambo seçmesini engelle |
| `zeus_enabled` | bool | `true` | Zeus mesleği aktif mi |
| `zeus_recharge_taser` | int | `30` | Taser şarj süresi (`mp_taser_recharge_time`) |
| `zeus_drop_taser` | bool | `true` | Ölünce taser düşsün mü (`mp_death_drop_taser`) |

### Örnek Config

```json
{
  "doktor_enabled": true,
  "doktor_regen": 50,
  "doktor_drop_healthshot": true,
  "flash_enabled": true,
  "flash_speed": 3.0,
  "flash_duration": 5,
  "bombaci_enabled": true,
  "bombaci_give_smoke": true,
  "bombaci_give_grenade": true,
  "bombaci_give_flashbang": true,
  "bombaci_give_molotov": true,
  "rambo_enabled": true,
  "rambo_hp": 150,
  "rambo_armor": 100,
  "rambo_helmet": true,
  "rambo_fix": true,
  "zeus_enabled": true,
  "zeus_recharge_taser": 30,
  "zeus_drop_taser": true
}
```

## Kullanım Örneği

```
!meslek           → aktif mesleklerin listesi
!meslek rambo     → 150 HP + 100 zırh + kask
!meslek bombaci   → rastgele bomba
```