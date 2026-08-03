# CTPerk

CT takımına raunt bazlı güçlendirmeler (perk) seçtirir. Jailbreak'te T sayısına göre CT'lere denge sağlamak için tasarlanmıştır.

## Özellikler

- 5 farklı perk:
  - **Can + Zırh** — tüm canlı CT'lere yüksek HP ve zırh (+ kask)
  - **Can Çalma (Lifesteal)** — CT'nin verdiği hasarın belirli yüzdesi can olarak geri döner
  - **Sınırsız Mermi** — şarjör yarıya düşünce otomatik dolar
  - **Hasar Azaltma** — CT'nin aldığı hasarın belirli yüzdesi geri yüklenir
  - **Hasar Artırma** — CT'lerin T'lere verdiği hasar çarpanla artar
- Raunt başındaki **T sayısına göre seçim hakkı** (ör. 9+ T → 2 hak, 20+ T → 3 hak)
- Her perk config'ten ayrı ayrı açılıp kapatılabilir, oranlar özelleştirilebilir
- Perk seçimleri tüm CT'lere duyurulur; menüde seçilenler yeşil ✔ ile işaretlenir
- Raunt başında perkler sıfırlanır, CT'lerin fazla HP/zırhı normale çekilir
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Kurulum

1. Derlenmiş `CTPerk` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/CTPerk/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load CTPerk` komutunu çalıştırın.
3. İlk yüklemede config dosyası otomatik oluşturulur.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_ctperk` | Perk seçim menüsünü açar | `@css/generic` **veya** `@jailbreak/warden` |

## Yapılandırma

```
csgo/addons/counterstrikesharp/configs/plugins/CTPerk/CTPerk.json
```

| Ayar | Tip | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `perk_hparmor_hp` | int | `200` | Can+Zırh perk'inin verdiği HP |
| `perk_hparmor_armor` | int | `100` | Can+Zırh perk'inin verdiği zırh |
| `perk_lifesteal_ratio` | float | `0.25` | Can çalma oranı (0.25 = %25) |
| `perk_damagereducation_ratio` | float | `0.25` | Hasar azaltma oranı |
| `perk_damageboost_ratio` | float | `1.50` | Hasar çarpanı (1.5 = +%50) |
| `enabled_perk_hparmor` | bool | `true` | Can+Zırh perk'i aktif mi |
| `enabled_perk_lifesteal` | bool | `true` | Can çalma perk'i aktif mi |
| `enabled_perk_infammo` | bool | `true` | Sınırsız mermi perk'i aktif mi |
| `enabled_perk_damagereducation` | bool | `true` | Hasar azaltma perk'i aktif mi |
| `enabled_perk_damageboost` | bool | `true` | Hasar artırma perk'i aktif mi |
| `selection_rights` | liste | aşağıda | T sayısına göre perk seçim hakları |

`selection_rights` — T sayısı eşiği (`t_count`) ve o eşikte tanınan hak (`hak`); en yüksek eşleşen eşik geçerlidir:

```json
"selection_rights": [
  { "t_count": 0,  "hak": 1 },
  { "t_count": 9,  "hak": 2 },
  { "t_count": 20, "hak": 3 }
]
```

### Örnek Config

```json
{
  "perk_hparmor_hp": 200,
  "perk_hparmor_armor": 100,
  "perk_lifesteal_ratio": 0.25,
  "perk_damagereducation_ratio": 0.25,
  "perk_damageboost_ratio": 1.5,
  "enabled_perk_hparmor": true,
  "enabled_perk_lifesteal": true,
  "enabled_perk_infammo": true,
  "enabled_perk_damagereducation": true,
  "enabled_perk_damageboost": true,
  "selection_rights": [
    { "t_count": 0, "hak": 1 },
    { "t_count": 9, "hak": 2 },
    { "t_count": 20, "hak": 3 }
  ]
}
```

## Kullanım Örneği

1. Raunt başlar → CT'lere "Bu raunt 2 perk hakkınız var" duyurusu yapılır.
2. Warden `!ctperk` yazar, menüden "Can Çalma (%25)" seçer.
3. Hak bitene kadar menü yeniden açılır; tüm seçimler CT'lere duyurulur.

## Notlar

- Perkler **takım geneli** çalışır; oyuncu bazlı değildir.
- Hasar azaltma perk'i hasarı kesmek yerine alınan hasarın bir kısmını anında geri yükler.
- Sınırsız mermi bıçak, taser gibi silahlarda devreye girmez.