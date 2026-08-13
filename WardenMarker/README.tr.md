# WardenMarker

Warden'a tek bir işaret halkası verir; tuşa bastığı anda halka baktığı noktaya taşınır. Halkanın ortasında duvar arkasından da görünen parlayan bir disk durur, böylece mahkumlar nereye toplanacaklarını uzaktan görür.

## Özellikler

- Warden'ın baktığı noktaya anında halka bırakma
- Tek marker; yeni yere bakıp tuşa basınca oraya taşınır
- Yerleştirme tuşu seçilebilir: Kullan (E), İncele (F), Ping (Orta Tuş) veya kapalı
- Renk, boyut ve kalınlık menüden seçilir
- Ortadaki disk ve parlama ayrı ayrı kapatılabilir, saydamlığı ayarlanabilir
- Herkesin seçtiği ayarlar kaydedilir, bir dahaki girişte aynen gelir
- Sadece CT tarafında çalışır; oyuncu T'ye geçince markerı kaybolur
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Kurulum

1. Derlenmiş `WardenMarker` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/WardenMarker/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load WardenMarker` komutunu çalıştırın.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_marker` | Marker menüsünü açar | `marker_flag` |
| `css_isaret` | `css_marker` ile aynı | `marker_flag` |

### Menü

| Seçenek | İşlev |
| --- | --- |
| Markerımı Sil | Kendi markerını kaldırır |
| Marker'ın Özellikleri | Renk / Boyut / Kalınlık |
| Disk'in Özellikleri | Disk / Parlama / Saydamlık |
| Tuş | Kullan (E) → İncele (F) → Ping (Orta Tuş) → Kapalı |

## Yapılandırma

`addons/counterstrikesharp/configs/plugins/WardenMarker/WardenMarker.json`

| Ayar | Varsayılan | Açıklama |
| --- | --- | --- |
| `marker_cmd` | `css_marker,css_isaret` | Menü komutları |
| `marker_flag` | `@jailbreak/warden` | Marker yetkisi (root otomatik erişim almaz) |
| `marker_default_key` | `use` | Varsayılan tuş: `use`, `inspect`, `ping`, `off` |
| `marker_cooldown` | `0.3` | İki taşıma arasındaki bekleme (saniye) |
| `marker_max_distance` | `8192` | Marker'ın konulabileceği en uzak mesafe |
| `marker_clear_on_roundend` | `true` | Round bitince tüm markerları siler |
| `marker_clear_on_death` | `false` | Oyuncu ölünce markerını siler |

### `marker_ring`

| Ayar | Varsayılan | Açıklama |
| --- | --- | --- |
| `colors` | 6 renk | Menüdeki renkler (`#RRGGBB` veya `R G B`) |
| `sizes` | `100, 150, 200, 250, 300` | Menüdeki halka boyutları |
| `widths` | `2, 4, 6, 8, 10` | Menüdeki halka kalınlıkları |
| `default_color` | `#00AFFF` | Varsayılan renk |
| `default_size` | `150` | Varsayılan boyut |
| `default_width` | `4` | Varsayılan kalınlık |

### `marker_disc`

| Ayar | Varsayılan | Açıklama |
| --- | --- | --- |
| `enabled` | `true` | Ortadaki disk varsayılan olarak açık mı |
| `glow` | `true` | Parlama ana anahtarı; `false` ise kimse açamaz |
| `glow_range` | `4096` | Parlamanın görüldüğü mesafe |
| `alphas` | `64, 128, 178, 255` | Menüdeki saydamlık değerleri |
| `default_alpha` | `178` | Varsayılan saydamlık |

Varsayılan değerler listede yoksa listenin ilk değeri kullanılır.

## Notlar

- Menü açıkken tuşla marker taşınmaz; E menüde seçim yapar, R menüyü kapatır.
