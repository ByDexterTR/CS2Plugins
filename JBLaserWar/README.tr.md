# JBLaserWar

*Bu dosyanın [İngilizcesi / English](README.md).*

Roundu bir lazer savaşına çevirir. Mermiler kimseye zarar vermez; her atışta silahtan bir lazer çıkar, duvarlardan seker ve değdiği kişiyi öldürür. Her şey tek bir menüden yönetilir, herkes tek veya takımlar halinde oynanır.

## Özellikler

- Oyun açıkken oyuncular birbirine mermiyle hasar veremez
- Her atışta silahtan lazer çıkar, duvarlardan seker
- Lazer anında belirmez, haritada ilerler
- Lazer silahtan çıkarken ve her sekmede ses çalar
- Herkes tek oynanır veya oyuncular 2, 3, 4 rastgele takıma bölünür
- Takımların kendi ismi, rengi ve oyuncu modeli olur
- Tek atış veya çift atış ölüm; ölmeyen oyuncunun HP'si yarıya düşer ve ekranı kızarır
- Herkese aynı silah verilir, her doğuşta yeniden gelir
- Yerden aldığı silahla ateş eden oyuncu ölür ve sohbete yazılır
- Son kalan oyuncu veya takım kazanan ilan edilir
- Oyunu başlatan ve durduran sohbete yazılır
- Oyun bitince silahlar, HP ve model eski haline döner
- Menüdeki seçimler kaydedilir, yeniden başlatınca aynen gelir
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371
- `gamedata` dosyası: `addons/counterstrikesharp/gamedata/NativeTrace.gamedata.json`

## Kurulum

1. Derlenmiş `JBLaserWar` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/JBLaserWar/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load JBLaserWar` komutunu çalıştırın.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_lw` | LaserWar menüsünü açar | `laserwar_flag` |
| `css_laserwar` | `css_lw` ile aynı | `laserwar_flag` |

### Menü

| Seçenek | İşlev |
| --- | --- |
| Oyunu Başlat / Oyunu Durdur | Lazer savaşını başlatır veya bitirir |
| Ayarlar | Ayarlar menüsünü açar |

### Ayarlar

| Seçenek | İşlev |
| --- | --- |
| Takım Sayısı | Herkes Tek → 2 → 3 → 4 |
| Silah | `laserwar_weapons` listesindeki silahlar arasında gezer |
| Hasar | Tek Atış → Çift Atış |
| Lazer Sekme | 1 → 2 → 3 → 4 |
| Sınırsız Mermi | Silahların mermisinin bitip bitmeyeceği |
| Gravity | `laserwar_gravity` listesindeki değerler arasında gezer |
| Lazer Sesi | Lazerin çıkış ve sekme sesleri |

Takımlar oyun başlarken rastgele dağıtılır; takım sayısı oyun sürerken değiştirilirse bir sonraki oyunda geçerli olur. Oyuncu sayısı takımlara eşit bölünmüyorsa oyun başlamaz.

Menüdeki her seçim yapıldığı anda kaydedilir.

## Yapılandırma

`addons/counterstrikesharp/configs/plugins/JBLaserWar/JBLaserWar.json`

| Ayar | Varsayılan | Açıklama |
| --- | --- | --- |
| `laserwar_cmd` | `css_lw,css_laserwar` | Menü komutları |
| `laserwar_flag` | `@css/generic,@jailbreak/warden` | Menü yetkisi |
| `laserwar_weapons` | 3 silah | Menüdeki silahlar; ilki varsayılandır |
| `laserwar_gravity` | `0.3, 0.5, 0.8, 1.0` | Menüdeki gravity değerleri; ilki varsayılandır, `1.0` normal yerçekimi |
| `laserwar_max_distance` | `4096` | Lazerin sekmeden önce gidebileceği mesafe |
| `laserwar_hit_radius` | `20` | Lazerin isabet sayılması için oyuncuya ne kadar yakın geçeceği |
| `laserwar_killfeed_icon` | `spray0` | Killfeed'de görünen ikon |

Silah listesinde `weapon_` öneki yazılmasa da olur, otomatik tamamlanır.

### `laserwar_beam`

| Ayar | Varsayılan | Açıklama |
| --- | --- | --- |
| `width` | `0.5` | Lazer kalınlığı |
| `speed` | `3000` | Lazerin ilerleme hızı |
| `length` | `260` | Görünen lazerin uzunluğu |
| `max_active` | `128` | Aynı anda havada olabilecek lazer sayısı |

### `laserwar_teams`

Dört takım tanımlıdır, menüden seçilen takım sayısı kadarı kullanılır.

| Ayar | Varsayılan | Açıklama |
| --- | --- | --- |
| `name` | Sith, Jedi, Mandalor, Klon | Sohbette görünen takım ismi |
| `color` | `#FF3C28`, `#28C8FF`, `#5CE05C`, `#FFD24A` | Takımın lazer ve oyuncu rengi (`#RRGGBB` veya `R G B`) |
| `model` | dört T ajanı | Takımın oyuncu modeli; boş bırakılırsa model değişmez |

### `laserwar_sound`

| Ayar | Varsayılan | Açıklama |
| --- | --- | --- |
| `fire` | `Weapon_Taser.ChargeReady_Zap` | Lazer silahtan çıkarken çalan ses |
| `fire_volume` | `1.0` | Ateş sesinin yüksekliği |
| `bounce` | `FX_RicochetSound.Ricochet_Legacy` | Her sekmede çalan ses |
| `bounce_volume` | `0.8` | Sekme sesinin yüksekliği |

### `laserwar_flash`

Lazer yiyip hayatta kalan oyuncunun ekranını kaplayan kırmızılık.

| Ayar | Varsayılan | Açıklama |
| --- | --- | --- |
| `r` / `g` / `b` | `255` / `0` / `0` | Efektin rengi |
| `a` | `90` | Efektin şiddeti; `0` kapatır |
| `duration` | `150` | Efektin ekranı kaplama süresi (ms) |
| `hold_time` | `500` | Ekranda kalma süresi (ms) |

## Notlar

- Oyuncunun kendi lazeri kendisine zarar vermez, dibindeki duvara ateş edebilir.
- Oyuna sadece T tarafındaki oyuncular katılır.
- Oyun bitince silahlar ve HP hayatta kalanlara geri verilir.
- Oyun tek round sürer; sonrasında menüden tekrar başlatmak gerekir.
