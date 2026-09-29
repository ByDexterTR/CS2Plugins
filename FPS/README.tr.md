# FPS

*Bu dosyanın [İngilizcesi / English](README.md).*

Oyunculara, görmeleri gerekmeyen şeyleri kaldıran bir FPS modu sunar: duvar arkasındaki oyuncular, cesetler, kendi ayakları, kan, mermi izleri ve haritadaki küçük çöp prop'lar. Her şey varsayılan olarak açıktır. `!fps` ile her oyuncu, sunucu ayarına göre ya kendi ayarlarını menüden değiştirir ya da FPS modunu açıp kapatır. Seçimler kaydedilir ve oyuncu sunucuya tekrar girdiğinde uygulanır.

## Özellikler

- `css_fps` (sohbette `!fps`) her zaman kullanılabilir: WASD ayar menüsünü açar, `player_config` `false` ise FPS modunu açıp kapatır
- Oyuncu seçimleri SteamID ile `players.json` dosyasına kaydedilir, her girişte uygulanır
- **Duvar arkası:** görüş hattınızda olmayan oyuncular size gönderilmez. Herkes, sadece takım arkadaşları veya sadece rakipler seçilebilir
- **Duvar arkası ses:** sizden gizlenen oyuncuların atış ve silah sesleri susturulur. Herkes, sadece takım arkadaşları veya sadece rakipler seçilebilir
- Gizlenen takım arkadaşları duvar arkasından görünmez (model, isim ve çizgi yok), ama onlar ve takımınızın gördüğü rakipler radarda her zamanki gibi hareket eder
- **Killfeed:** killfeed'de sadece sizin öldürdükleriniz (ve sizin ölümünüz) görünür
- **Cesetler:** ölen oyuncuların cesetleri kısa bir süre sonra kaybolur
- **Ayaklar:** aşağı baktığınızda kendi ayaklarınızı görmezsiniz
- **Kan ve mermi izi:** oyunculardaki kan ve vuruş efektleri gösterilmez; kan izleri ve mermi delikleri belirlenen aralıkla silinir
- **Çöp prop:** şişe, teneke, çömlek gibi küçük fırlatılabilir prop'lar gösterilmez. Liste harita bazında `maps` klasöründe tutulur
- Ceset, kan ve çöp prop herkes için zorunlu yapılabilir: prop'lar haritadan silinir, ceset ve kan tüm oyunculardan kaldırılır
- Her özellik `settings.json` üzerinden kapatılabilir. Kapalı bir özellik hiç çalışmaz
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp)

## Kurulum

1. Derlenmiş `FPS` klasörünü `maps` klasörüyle birlikte sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/FPS/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load FPS` komutunu çalıştırın.
3. `settings.json` yoksa eklenti klasöründe otomatik oluşturulur. Dosya okunamazsa `settings.old.json` olarak saklanır ve varsayılan ayarlarla yeniden yazılır.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_fps` | `player_config: true`: FPS ayarları menüsünü açar. `player_config: false`: FPS modunu açar/kapatır. Seçim kaydedilir | `fps_flag` (varsayılan: herkes) |

Menüde **W/S** kaydırır, **E** seçili ayarı değiştirir, **R** menüyü kapatır.

## Yapılandırma

```
csgo/addons/counterstrikesharp/plugins/FPS/settings.json
```

| Ayar | Tip | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `fps_cmd` | string | `"css_fps"` | Virgülle ayrılmış komut adları |
| `fps_flag` | string | `""` | Gerekli yetki; boş string = herkes kullanabilir |
| `player_config` | bool | `true` | `true`: oyuncular her ayarı menüden kendisi değiştirir, `false`: oyuncular sadece FPS modunu açıp kapatabilir ve aşağıdaki ayarları alır |
| `hide_unseen` | int | `3` | Duvar arkasındaki oyuncuları gizlemenin varsayılanı. `0`: kapalı (menüde yok), `1`: takım arkadaşları, `2`: rakip takım, `3`: herkes |
| `mute_unseen` | int | `3` | Gizlenen oyuncuların silahlarını susturmanın varsayılanı. `0`: kapalı (menüde yok), `1`: takım arkadaşları, `2`: rakip takım, `3`: herkes. `hide_unseen` `0` iken etkisi yoktur |
| `own_killfeed` | bool | `true` | `true`: varsayılan açık, menüden değiştirilebilir, `false`: kapalı, menüde yok |
| `hide_corpses` | int | `1` | `0`: kapalı, `1`: varsayılan açık ve menüden değiştirilebilir, `2`: tüm oyunculardan gizlenir, menüde yok |
| `corpse_delay` | float | `0.5` | Ölümden sonra cesedin kaybolma süresi (saniye) |
| `hide_legs` | bool | `true` | `true`: varsayılan açık, menüden değiştirilebilir, `false`: kapalı, menüde yok |
| `hide_blood` | int | `1` | `0`: kapalı, `1`: varsayılan açık ve menüden değiştirilebilir, `2`: tüm oyunculara uygulanır, menüde yok |
| `blood_delay` | float | `0.5` | Kan izleri ve mermi deliklerinin silinme aralığı (saniye, en az `0.1`) |
| `hide_props` | int | `1` | `0`: kapalı, `1`: varsayılan açık ve menüden değiştirilebilir, `2`: çöp prop'lar herkes için haritadan silinir, menüde yok |

### Örnek Config

```json
{
  "fps_cmd": "css_fps,css_fpsboost",
  "fps_flag": "",
  "player_config": true,
  "hide_unseen": 2,
  "mute_unseen": 0,
  "own_killfeed": true,
  "hide_corpses": 1,
  "corpse_delay": 0.5,
  "hide_legs": true,
  "hide_blood": 2,
  "blood_delay": 0.5,
  "hide_props": 2
}
```

## Tercih Dosyası

```
csgo/addons/counterstrikesharp/plugins/FPS/players.json
```

```json
{
  "76561198000000000": {
    "hide_unseen": 2,
    "hide_props": 0
  },
  "76561198111111111": {
    "fps": 0
  }
}
```

Sadece oyuncunun sunucu varsayılanından farklı seçtiği ayarlar tutulur; `0` o özelliğin oyuncuda kapalı olduğu anlamına gelir. `"fps": 0`, oyuncunun `player_config` `false` iken FPS modunu kapattığını gösterir. Her şeyi varsayılana döndüren oyuncu dosyadan silinir. Elle de düzenlenebilir, değişiklik eklenti yeniden yüklendiğinde okunur.

## Çöp Prop Listeleri

```
csgo/addons/counterstrikesharp/plugins/FPS/maps/<harita>.json
```

```json
[
  {
    "model": "models/props/de_inferno/claypot03.vmdl",
    "count": 17
  },
  {
    "model": "models/props_junk/garbage_sodacup01a.vmdl",
    "count": 7
  }
]
```

de_ancient, de_ancient_night, de_anubis, de_cache, de_dust2, de_inferno, de_mirage, de_nuke, de_overpass, de_train ve de_vertigo için hazır liste gelir. `model` gizlenecek prop'un modeli, `count` haritada kaç tane olduğudur.

- Listesi olmayan haritada eklenti listeyi ilk rauntta çıkarır ve bu klasöre kaydeder.
- Bir prop'un görünmesini istiyorsanız satırını silin. Boş liste (`[]`) o haritada hiçbir şeyi gizlemez.
- Sadece oyuncuların içinden geçip tekmelediği küçük prop'lar gizlenir. Oyuncuyu engelleyen bir prop listede olsa bile gizlenmez.
- Liste, harita yüklendiğinde haritayla karşılaştırılır. Bir CS2 güncellemesinden sonra artık olmayan ya da oyuncuyu engeller hale gelen bir model, değişen bir adet veya listede olmayan yeni bir çöp prop sunucu konsolunda uyarı olarak yazılır. Listeyi yeniden çıkarmak için haritanın dosyasını silin.

## Görünürlük Verisi

```
csgo/addons/counterstrikesharp/plugins/FPS/maps/<harita>.vis
```

**Duvar arkası**, haritanın kendi geometrisinden hazırlanan harita bazlı bir görünürlük tablosu kullanır. Gizlenen oyuncuların çoğu ek görüş kontrolü yapılmadan bu tablodan belirlenir, böylece tüm oyuncular özelliği kullanırken bile sunucu çok az iş yapar.

- Çöp prop listeleriyle aynı haritalar için hazır tablo gelir.
- Tablosu olmayan haritada (atölye haritaları dahil) tablo, harita yüklenirken arka planda hazırlanır ve bu klasöre kaydedilir. Haritaya göre birkaç saniye ile yarım dakika arası sürer, işlemci çekirdeklerinin yarısını düşük öncelikle kullanır. Hazır olana kadar her oyuncu görüş kontrolüyle denetlenir.
- Atölye haritalarının tablosu `<harita>.<atölye id>.vis` adıyla kaydedilir, aynı isimli atölye haritaları tabloyu paylaşmaz. Yüklü haritanın adını taşıyan birden fazla harita dosyası varsa eklenti her birini çalışan haritayla karşılaştırır ve eşleşeni kullanır.
- Bir CS2 güncellemesi haritayı değiştirirse tablo haritayla uyuşmaz ve otomatik yeniden hazırlanır.
- Yeniden hazırlatmak için haritanın `.vis` dosyasını silin.

## Notlar

- **Duvar arkası** sadece hayattayken çalışır. Ölüyken veya izlerken tüm oyuncular görünür kalır.
- Gizlenen oyuncu görüş hattı açıldığı anda, köşeden çıkmak üzereyken de görünür. Pingi yüksek oyunculara daha erken gösterilir, görünen bir oyuncu da hemen gizlenmez. Çarpışma, mermi ve hasar etkilenmez.
- Size çok yakın olan oyuncular hiç gizlenmez.
- `mp_teammates_are_enemies` `1` iken (ör. deathmatch), takım/rakip seçimlerinde herkes rakip sayılır.
- Bir oyuncuyu izlerken onun cesedi gizlenmez, böylece kamera takılı kalmaz.
- **Duvar arkası ses** sadece o an sizden gizlenen oyuncuları susturur; ayak sesleri ve diğer sesler etkilenmez.
- **Kan ve mermi izi** sadece oyun sırasında oluşan izleri siler; haritanın kendi decal'leri kalır.
- `hide_props: 2` iken prop'lar her raunt silinir, kimse onları alamaz veya tekmeleyemez.
- `hide_corpses: 2` iken GOTV de cesetleri görmez.
- **Ayaklar** kendi modelinizi diğer oyuncuların fark edemeyeceği kadar az saydam yapar. Oyuncu saydamlığını değiştiren başka bir eklenti ayaklarınızı yeniden görünür yapabilir.
- **Killfeed:** GOTV killfeed'in tamamını almaya devam eder. Killfeed'i değiştiren başka bir eklenti (ör. DM) varsa killfeed iki kez görünebilir; o sunucuda `own_killfeed: false` yapın.
- `settings.json` ve komut adı değişiklikleri sunucu/eklenti yeniden başlatıldığında etkinleşir.
