# Postprocessing

Oyunculara kişiye özel post processing efekti (bloom, blur, renk düzeltme, pozlama) uygular. Efektler `post_processing_volume` entity'siyle verilir ve `CheckTransmit` ile yalnızca ilgili oyuncuya gönderilir; diğer oyuncular ekranlarında hiçbir değişiklik görmez.

## Özellikler

- Oyunda bulunan **106 hazır efekt** ile gelir: CS2'nin 81 ortak `.vpost` dosyası, 24 harita özel dosya ve bir FOV (zoom) efekti
- Efektler oyuncu bazında çalışır; aynı anda farklı oyuncularda farklı efekt olabilir
- Kategorili WASD menüsü (Efektler / Renk / Genel / Arayüz / Haritalar / HaritaOzel), `css_pp <efekt>` ile doğrudan seçim
- `map` alanıyla efekt belirli haritalara kilitlenir; harita özel efektler yalnızca o haritada precache edilir ve menüde görünür
- `flag` alanıyla efekt belirli yetkilere kilitlenebilir
- Config'ten sınırsız efekt eklenebilir; her efekt kendi `.vpost` dosyası, pozlama ayarları ve isteğe bağlı FOV değeriyle gelir
- Yetkililer için `css_ppver` ile başka oyunculara efekt verme
- Efekt tercihi SteamID bazında kaydedilir; oyuncu tekrar bağlandığında geri yüklenir
- Haritanın kendi post processing volume'leri efekt aktifken oyuncudan gizlenir (çakışma olmaz)
- FOV değeri tanımlanan efektler zoom efekti olarak da kullanılabilir
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Kurulum

1. Derlenmiş `Postprocessing` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/Postprocessing/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load Postprocessing` komutunu çalıştırın.
3. İlk yüklemede config dosyası otomatik oluşturulur.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_pp` / `css_postprocessing` | Kategorili efekt menüsünü açar | `pp_flag` |
| `css_pp <efekt>` | Efekti doğrudan uygular | `pp_flag` + efektin `flag` değeri |
| `css_pp off` | Efekti kapatır | `pp_flag` |
| `css_ppver <oyuncu> <efekt\|off>` | Hedef oyuncuya efekt verir veya kapatır | `pp_give_flag` |

## Yapılandırma

```
csgo/addons/counterstrikesharp/configs/plugins/Postprocessing/Postprocessing.json
```

| Ayar | Tip | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `pp_cmd` | string | `"css_pp,css_postprocessing"` | Virgülle ayrılmış menü komutları |
| `pp_flag` | string | `""` | Menü komutu yetkisi (boş = herkes) |
| `pp_give_cmd` | string | `"css_ppver"` | Virgülle ayrılmış yetkili komutları |
| `pp_give_flag` | string | `"@css/generic"` | Yetkili komutu yetkisi |
| `pp_remember` | bool | `true` | Oyuncunun efekt tercihini SteamID bazında kaydeder |
| `pp_hide_map_effects` | bool | `true` | Efekt aktifken haritanın kendi post processing efektini gizler |
| `pp_presets` | liste | 106 efekt | Efekt tanımları |

### Efekt Alanları

| Alan | Tip | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `name` | string | – | Menüde görünen ve komutta yazılan efekt adı |
| `file` | string | – | `.vpost` dosya yolu (boş bırakılırsa yalnızca FOV uygulanır) |
| `category` | string | `""` | Menüdeki kategori adı (boş = "Diğer" kategorisi) |
| `map` | string | `""` | Efektin çalışacağı harita adları (virgülle ayrılır, boş = tüm haritalar) |
| `flag` | string | `""` | Efekt için gereken yetki (boş = herkes) |
| `fade` | float | `0.25` | Efekte geçiş süresi (saniye) |
| `exposure` | bool | `true` | Pozlama (exposure) kontrolü açık mı |
| `min_exposure` | float | `0.5` | Minimum pozlama |
| `max_exposure` | float | `2.0` | Maksimum pozlama |
| `exposure_speed_up` | float | `1.0` | Pozlama artış hızı |
| `exposure_speed_down` | float | `1.0` | Pozlama azalma hızı |
| `fov` | int | `0` | Efektle birlikte uygulanan FOV (0 = değiştirme, 40 = zoom) |

### Örnek Config

```json
{
  "pp_cmd": "css_pp,css_postprocessing",
  "pp_flag": "",
  "pp_give_cmd": "css_ppver",
  "pp_give_flag": "@css/generic",
  "pp_remember": true,
  "pp_hide_map_effects": true,
  "pp_presets": [
    {
      "name": "bloomtest",
      "file": "lighting/postprocessing/correction/bloomtest.vpost",
      "category": "Renk",
      "map": "",
      "flag": "",
      "fade": 0.25,
      "exposure": true,
      "min_exposure": 0.5,
      "max_exposure": 2.0,
      "exposure_speed_up": 1.0,
      "exposure_speed_down": 1.0,
      "fov": 0
    },
    {
      "name": "zoom",
      "file": "",
      "category": "Genel",
      "map": "",
      "flag": "",
      "fade": 0.25,
      "exposure": true,
      "min_exposure": 0.5,
      "max_exposure": 2.0,
      "exposure_speed_up": 1.0,
      "exposure_speed_down": 1.0,
      "fov": 40
    },
    {
      "name": "de_fachwerk3_drunk",
      "file": "postprocess/de_fachwerk3_drunk.vpost",
      "category": "HaritaOzel",
      "map": "de_fachwerk",
      "flag": "",
      "fade": 0.25,
      "exposure": true,
      "min_exposure": 0.5,
      "max_exposure": 2.0,
      "exposure_speed_up": 1.0,
      "exposure_speed_down": 1.0,
      "fov": 0
    }
  ]
}
```

## Varsayılan Efektler

Efekt adları `.vpost` dosya adının kendisidir, yani `css_pp de_fachwerk3_drunk` doğrudan çalışır.

| Kategori | Adet | İçerik |
| --- | --- | --- |
| `Efektler` | 11 | `lighting/postprocessing/effects/` — ölüm kamerası, dürbün, bomba sonu, satın alma bulanıklığı, ağır zırh, HLTV replay |
| `Renk` | 3 | `lighting/postprocessing/correction/` — `bloomtest`, `cc_freeze_ct`, `cc_freeze_t` |
| `Genel` | 15 | Kök `lighting/postprocessing/` ve `postprocess/` dosyaları — `ar_dizzy`, `filmic_default`, `basepostprocess`, `inspect_laptop`, `graphics_settings` ve FOV efekti `zoom` |
| `Arayuz` | 4 | `lighting/postprocessing/ui/` — envanter/kasa ikon efektleri |
| `Haritalar` | 49 | Resmî harita prefab'leri (`de_dust2_prefab`, `de_train_postprocess_v2`, `de_mirage_vanity` …) — tüm haritalarda çalışır |
| `HaritaOzel` | 24 | Harita `.vpk` dosyalarındaki özel efektler — yalnızca ilgili harita yüklüyken görünür |

`HaritaOzel` içeriği:

| Harita | Efektler |
| --- | --- |
| `de_fachwerk` | `de_fachwerk`, `de_fachwerk2`, `de_fachwerk3`, `de_fachwerk3_drunk`, `de_fachwerk4`, `de_fachwerk5`, `drawbridge`, `basic_linear_post` |
| `de_boulder` | `de_boulder_postprocess`, `de_boulder_postprocess2`, `de_boulder_postprocess3`, `de_boulder_prefab`, `de_boulder_skybox`, `bldr_01_ct_spawn`, `bldr_04_b_site`, `de_inferno_postprocess_boulder` |
| `ar_pool_day` | `ar_pool_day`, `postprocess_filmic_pool_day`, `postprocess_filmic_pool_day_cs16`, `postprocess_filmic_underwater`, `basic_linear_post` |
| `de_eldorado` | `eldorado`, `eldorado_postprocess` |
| `de_poseidon` | `poseidon`, `basic_linear_post` |
| `de_debris` | `de_debris` |
| `cs_shelter` | `basic_linear_post` |

## Kendi `.vpost` Dosyanı Bulma

Oyunun tüm post processing dosyaları `pak01_dir.vpk` içinde `lighting/postprocessing/` ve `postprocess/` altındadır. Harita özel dosyalar ilgili haritanın `.vpk` dosyasında bulunur. Listelemek için [Source2Viewer CLI](https://valveresourceformat.github.io/) kullanılır:

```powershell
# Oyunun ortak dosyaları
Source2Viewer-CLI.exe -i "csgo\pak01_dir.vpk" --vpk_dir | Select-String "vpost"

# Bir haritanın kendi dosyaları
Source2Viewer-CLI.exe -i "csgo\maps\de_fachwerk.vpk" --vpk_dir | Select-String "vpost"
```

Haritanın hangi efekti kullandığını görmek için entity dökümü alınır:

```powershell
Source2Viewer-CLI.exe -i "csgo\maps\de_fachwerk.vpk" --vpk_filepath "maps/de_fachwerk/entities" -o out -d
```

Çıkan `default_ents.vents` dosyasındaki `post_processing_volume` kaydı config'e birebir taşınabilir:

```
fadetime                       20.0
exposurespeeddown              1.0
exposurespeedup                1.0
enableexposure                 true
maxexposure                    1.1
minexposure                    0.8
master                         true
postprocessing                 resource_name:"postprocess/de_fachwerk5.vpost"
classname                      "post_processing_volume"
```

Atölye haritalarındaki `.vpost` dosyaları da aynı şekilde kullanılır; `map` alanına harita adı yazılması yeterlidir.

## Notlar

- Efekt entity'si `master` olarak spawn edilir, yani oyuncunun konumundan bağımsız olarak tüm haritada geçerlidir. Entity oyuncunun pawn'ına parent edilir, böylece PVS dışında kalıp kaybolmaz.
- `.vpost` dosyaları harita yüklenirken precache edilir. Bir dosya o haritada bulunmuyorsa konsola uyarı yazılır ve yalnızca o efekt çalışmaz; eklenti çalışmaya devam eder. Bu yüzden harita özel dosyalarda `map` alanı doldurulmalıdır.
- Oyuncu öldüğünde efekt entity'si kaldırılır, tekrar doğduğunda otomatik geri gelir. Ölüyken izlenen oyuncunun efekti görünmez.
- `fov` alanı `m_iDesiredFOV` üzerinden çalışır; dürbünlü silahlarda oyunun kendi zoom'u önceliklidir.
- Efekt adları komutlarda büyük/küçük harf duyarsızdır (`css_pp bloomtest` = `css_pp BloomTest`).
- Menü iki kademelidir: önce kategori, sonra efekt listesi. Kategori içinde `R` (Geri) üst menüye döner.
- Efekt listesi config'te değiştirildiğinde mevcut `Postprocessing.json` dosyası **otomatik güncellenmez**. Yeni varsayılan listeyi almak için config dosyasını silip sunucuyu yeniden başlatın.
- Komut adı değişikliği (`pp_cmd`, `pp_give_cmd`) sunucu/eklenti yeniden başlatıldığında etkinleşir.
