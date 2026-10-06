# FPS

*Bu dosyanın [İngilizcesi / English](README.md).*

Oyunculara, görmeleri gerekmeyen şeyleri kaldıran bir FPS modu sunar: duvar arkasındaki oyuncular, cesetler, kendi ayakları, kan, mermi izleri ve haritadaki küçük çöp prop'lar. Her şey varsayılan olarak açıktır. `!fps` ile her oyuncu, sunucu ayarına göre ya kendi ayarlarını menüden değiştirir ya da FPS modunu açıp kapatır. Seçimler kaydedilir ve oyuncu sunucuya tekrar girdiğinde uygulanır.

## Özellikler

- `css_fps` (sohbette `!fps`) her zaman kullanılabilir: WASD ayar menüsünü açar, `player_configable` `false` ise FPS modunu açıp kapatır
- Oyuncu seçimleri SteamID ile `players.json` dosyasına kaydedilir, her girişte uygulanır
- **Duvar arkası:** görüş hattınızda olmayan oyuncular size gönderilmez. Herkes, sadece takım arkadaşları veya sadece rakipler seçilebilir
- **Duvar arkası ses:** sizden gizlenen oyuncuların atış ve silah sesleri susturulur. Herkes, sadece takım arkadaşları veya sadece rakipler seçilebilir
- Gizlenen takım arkadaşları duvar arkasından görünmez (model, isim ve çizgi yok), ama onlar ve takımınızın gördüğü rakipler radarda her zamanki gibi hareket eder
- **Killfeed:** killfeed'de sadece sizin öldürdükleriniz (ve sizin ölümünüz) görünür
- **Cesetler:** ölen oyuncuların cesetleri kısa bir süre sonra kaybolur
- **Ayaklar:** aşağı baktığınızda kendi ayaklarınızı görmezsiniz
- **Kan:** oyunculardaki kan ve vuruş efektleri gösterilmez ya da kan izleri belirlenen süre sonra silinir
- **Mermi izi:** duvarlardaki mermi delikleri ve lekeler, oluştuktan belirlenen süre sonra silinir
- **Çöp prop:** şişe, teneke, çömlek gibi küçük fırlatılabilir prop'lar gösterilmez. Liste harita bazında `maps` klasöründe tutulur
- Her özellik `settings.json` üzerinden kapatılabilir, yeni oyuncular için varsayılanı ayarlanabilir. Kapalı bir özellik hiç çalışmaz
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
4. Eski sürümden kalan `settings.json` seçimleriniz korunarak yeni yapıya otomatik çevrilir. Eski dosya `settings.v1.json` olarak saklanır.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_fps` | `player_configable: true`: FPS ayarları menüsünü açar. `player_configable: false`: FPS modunu açar/kapatır. Seçim kaydedilir | `fps_flag` (varsayılan: herkes) |

Menüde **W/S** kaydırır, **E** seçili ayarı değiştirir, **R** menüyü kapatır.

## Yapılandırma

```
csgo/addons/counterstrikesharp/plugins/FPS/settings.json
```

| Ayar | Tip | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `ConfigVersion` | int | `2` | Ayar dosyasının yapı sürümü, değiştirmeyin |
| `fps_cmd` | string | `"css_fps"` | Virgülle ayrılmış komut adları |
| `fps_flag` | string | `""` | Gerekli yetki; boş string = herkes kullanabilir |
| `player_configable` | bool | `true` | `true`: oyuncular her ayarı menüden kendisi değiştirir, `false`: sunucu ayarları sabittir, oyuncular sadece FPS modunu açıp kapatabilir |
| `hide_unseen_enable` | bool | `true` | Duvar arkasındaki oyuncuları gizleme sunucuda kullanılsın |
| `hide_unseen_default` | int | `3` | Duvar arkasındaki oyuncuları gizlemenin varsayılanı. `0`: kapalı, `1`: takım arkadaşları, `2`: rakip takım, `3`: herkes |
| `mute_unseen_enable` | bool | `true` | Gizlenen oyuncuların silah seslerini kapatma sunucuda kullanılsın. `hide_unseen_enable` `false` iken etkisi yoktur |
| `mute_unseen_default` | int | `3` | Gizlenen oyuncuları susturmanın varsayılanı. `0`: kapalı, `1`: takım arkadaşları, `2`: rakip takım, `3`: herkes |
| `own_killfeed_enable` | bool | `true` | Killfeed'de sadece kendi öldürdüklerini görme sunucuda kullanılsın |
| `own_killfeed_default` | int | `1` | `0`: varsayılan kapalı, `1`: varsayılan açık |
| `hide_ragdoll_enable` | bool | `true` | Cesetleri gizleme sunucuda kullanılsın |
| `hide_ragdoll_default` | int | `1` | `0`: varsayılan kapalı, `1`: varsayılan açık |
| `hide_ragdoll_delay` | float | `0.5` | Ölümden sonra cesedin kaybolma süresi (saniye) |
| `hide_legs_enable` | bool | `true` | Kendi ayaklarını gizleme sunucuda kullanılsın |
| `hide_legs_default` | int | `1` | `0`: varsayılan kapalı, `1`: varsayılan açık |
| `hide_blood_enable` | bool | `true` | Kanı gizleme sunucuda kullanılsın |
| `hide_blood_default` | int | `1` | `0`: varsayılan kapalı, `1`: varsayılan açık |
| `hide_blood_delay` | float | `0` | `0`: kan hiç gösterilmez. `0`'dan büyük: kan gösterilir, kan izleri bu kadar saniye sonra silinir (en az `0.1`) |
| `hide_bullethole_enable` | bool | `true` | Mermi izlerini silme sunucuda kullanılsın |
| `hide_bullethole_default` | int | `1` | `0`: varsayılan kapalı, `1`: varsayılan açık |
| `hide_bullethole_delay` | float | `1.0` | Atıştan kaç saniye sonra mermi izlerinin silineceği (en az `0.1`) |
| `hide_props_enable` | bool | `true` | Çöp prop'ları gizleme sunucuda kullanılsın |
| `hide_props_default` | int | `1` | `0`: varsayılan kapalı, `1`: varsayılan açık |

`_enable` değeri `false` olan ayar menüde görünmez ve hiç çalışmaz. `player_configable: false` iken her oyuncu `_default` değerlerini alır ve sadece FPS modunu açıp kapatabilir.

### Örnek Config

```json
{
  "ConfigVersion": 2,
  "fps_cmd": "css_fps,css_fpsboost",
  "fps_flag": "",
  "player_configable": true,
  "hide_unseen_enable": true,
  "hide_unseen_default": 2,
  "mute_unseen_enable": false,
  "mute_unseen_default": 3,
  "own_killfeed_enable": true,
  "own_killfeed_default": 0,
  "hide_ragdoll_enable": true,
  "hide_ragdoll_default": 1,
  "hide_ragdoll_delay": 1.0,
  "hide_legs_enable": true,
  "hide_legs_default": 1,
  "hide_blood_enable": true,
  "hide_blood_default": 0,
  "hide_blood_delay": 0,
  "hide_bullethole_enable": true,
  "hide_bullethole_default": 1,
  "hide_bullethole_delay": 2.0,
  "hide_props_enable": true,
  "hide_props_default": 1
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
    "hide_props": 0,
    "hide_blood": 1
  },
  "76561198111111111": {
    "fps": 0
  }
}
```

Sadece oyuncunun sunucu varsayılanından farklı seçtiği ayarlar tutulur; `0` o özelliğin oyuncuda kapalı, `1` açık olduğu anlamına gelir. `"fps": 0`, oyuncunun `player_configable` `false` iken FPS modunu kapattığını gösterir. Her şeyi varsayılana döndüren oyuncu dosyadan silinir. Elle de düzenlenebilir, değişiklik eklenti yeniden yüklendiğinde okunur.

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
- Oyuncuların ayrı alanlara ışınlandığı arena ve çok bölümlü haritalar da kapsanır: tablo; spawn, ışınlanma noktası veya nav işaretinden ulaşılabilen her alanı içerir.
- Tablo, spawn'lardan merdiven, basamak ve zıplamayla ulaşılabilen her yeri kapsar. Haritada bot navigasyonu (nav) varsa, ulaşılması zor yerleri (kutu, çatı, tren üstü) tamamlamak ve harita dışı odaları ayıklamak için de kullanılır. Nav'ı olmayan haritalar spawn'lar, merdivenler ve çarpışma geometrisinden kapsanır.
- Yeniden hazırlatmak için haritanın `.vis` dosyasını silin.

## Notlar

- **Duvar arkası** sadece hayattayken çalışır. Ölüyken veya izlerken tüm oyuncular görünür kalır.
- Gizlenen oyuncu görüş hattı açıldığı anda, köşeden çıkmak üzereyken de görünür. Kutuya zıplayan ya da merdiven veya rampadan çıkan oyuncu, kafası siperi aşmadan biraz önce gösterilir. Bir oyuncu sizi görebiliyorsa siz de onu görürsünüz. Oyuncunun bastığı hareket tuşlarına da bakılır: yana adım atarak köşeden çıkmaya başlayan oyuncu bir an erken gösterilir, duran oyuncu ise önceden gösterilmez. Pingi yüksek oyunculara daha erken gösterilir, görünen bir oyuncu da hemen gizlenmez. Çarpışma, mermi ve hasar etkilenmez.
- Çok kalabalık bir sunucuda, sunucu yoğunken gizli oyuncular herkese gösterilmek yerine biraz daha seyrek kontrol edilir.
- Size çok yakın olan oyuncular hiç gizlenmez.
- `mp_teammates_are_enemies` `1` iken (ör. deathmatch), takım/rakip seçimlerinde herkes rakip sayılır.
- Bir oyuncuyu izlerken onun cesedi gizlenmez, böylece kamera takılı kalmaz.
- **Duvar arkası ses** sadece o an sizden gizlenen oyuncuları susturur; ayak sesleri ve diğer sesler etkilenmez.
- **Mermi izi** sadece oyun sırasında oluşan izleri siler; haritanın kendi decal'leri kalır. Oyun izleri tek tek değil, oyuncunun ekranındaki hepsini birden silebiliyor. Süre ilk yeni iz ya da lekeyle başlar; dolduğunda o oyuncunun ekranındaki tüm izler ve lekeler, az önce oluşanlar dahil, birlikte gider. Kan lekeleri ve mermi izleri her zaman birlikte silinir.
- **Ayaklar** kendi modelinizi diğer oyuncuların fark edemeyeceği kadar az saydam yapar. Oyuncu saydamlığını değiştiren başka bir eklenti ayaklarınızı yeniden görünür yapabilir.
- **Killfeed:** GOTV killfeed'in tamamını almaya devam eder. Killfeed'i değiştiren başka bir eklenti (ör. DM) varsa killfeed iki kez görünebilir; o sunucuda `own_killfeed_enable: false` yapın.
- `settings.json` ve komut adı değişiklikleri sunucu/eklenti yeniden başlatıldığında etkinleşir.
