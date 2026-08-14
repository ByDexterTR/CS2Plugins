# ShowPlayerClips

*Bu dosyanın [İngilizcesi / English](README.md).*

Haritadaki görünmez tool fırçalarını renkli çizgilerle gösterir: clip, player clip, merdiven, grenade clip, trigger ve diğerleri. Herkes tek komutla kendisi için açar.

## Özellikler

- Oyuncunun çevresindeki görünmez hacimler renkli çizgilerle çizilir
- Her türün kendi rengi var, renkler tool dokusunun renginden alındı
- Hangi türlerin çizileceği configten seçilir
- Çizgileri sadece açan oyuncular görür; diğer oyuncular ve GOTV göremez
- Çizgiler yüzeyin bir tık dışına çizilir, duvarda ve zeminde okunur kalır
- Trigger hacimleri (ışınlanma, itme, hasar, satın alma alanı, bomba noktası) kutu olarak çizilir
- Atölye (workshop) haritalarında da çalışır
- Komut erişimi flag ile sınırlanabilir
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Kurulum

1. Derlenmiş `ShowPlayerClips` klasörünü olduğu gibi sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/ShowPlayerClips/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load ShowPlayerClips` komutunu çalıştırın.
3. İlk yüklemede config dosyası otomatik oluşturulur.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_showclips` / `css_clips` | Çizgileri sizin için açar/kapatır | `showclips_flag` |

## Yapılandırma

```
csgo/addons/counterstrikesharp/configs/plugins/ShowPlayerClips/ShowPlayerClips.json
```

| Ayar | Tip | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `showclips_cmd` | string | `"css_showclips,css_clips"` | Virgülle ayrılmış komut adları |
| `showclips_flag` | string | `"@css/generic"` | Komut için gereken flag (boş bırakılırsa herkes kullanabilir) |
| `showclips_types` | string | `"clip,playerclip,trigger,ladder"` | Çizilecek türlerin virgülle ayrılmış listesi |
| `showclips_colors` | object | aşağıya bakın | Tür başına renk (`#RRGGBB` veya `R G B`) |
| `showclips_radius` | float | `4096` | Bu mesafeden uzaktaki çizgiler çizilmez (minimum 128) |
| `showclips_max_beams` | int | `1000` | Aynı anda çizilecek maksimum çizgi sayısı (16 - 4096) |
| `showclips_width` | float | `0.5` | Çizgi kalınlığı (minimum 0.1) |
| `showclips_offset` | float | `1` | Çizgilerin yüzeyden ne kadar dışarıda duracağı (unit) |
| `showclips_refresh` | float | `0.4` | Çizgilerin saniye cinsinden yenilenme aralığı (minimum 0.1) |
| `showclips_move_step` | float | `24` | Oyuncu bu kadar yol gittikten sonra çizgiler yeniden hesaplanır |

### Örnek Config

```json
{
  "showclips_cmd": "css_showclips,css_clips",
  "showclips_flag": "@css/generic",
  "showclips_types": "clip,playerclip,trigger,ladder",
  "showclips_colors": {
    "clip": "#CD3920",
    "playerclip": "#C00078",
    "npcclip": "#8820CD",
    "grenadeclip": "#B6FC16",
    "ladder": "#F84A00",
    "blockbullets": "#F88005",
    "passbullets": "#25B9F5",
    "blocklos": "#0000F8",
    "blocksound": "#B5E51E",
    "blocklight": "#95C04A",
    "sky": "#B2E1FD",
    "water": "#00E8CA",
    "navclip": "#C508A7",
    "navspaceclip": "#527097",
    "teleportclip": "#2E9DA6",
    "controlclip": "#CD20A8",
    "otherclip": "#7821D3",
    "blockbomb": "#31D3AE",
    "trigger": "#F89A00",
    "ignorenpc": "#BA6D9C"
  },
  "showclips_radius": 4096,
  "showclips_max_beams": 1000,
  "showclips_width": 0.5,
  "showclips_offset": 1,
  "showclips_refresh": 0.4,
  "showclips_move_step": 24
}
```

## Türler

| Tür | Tool dokusu | Ne olduğu |
| --- | --- | --- |
| `clip` | `toolsclip` | Hem oyuncuyu hem botu durduran görünmez duvar |
| `playerclip` | `toolsplayerclip` | Yalnızca oyuncuyu durduran görünmez duvar |
| `npcclip` | `toolsnpcclip` | Yalnızca botu durduran görünmez duvar |
| `grenadeclip` | `toolsgrenadeclip` | El bombalarını durduran hacim |
| `ladder` | `toolsinvisibleladder` | Görünmez merdiven |
| `trigger` | `toolstrigger` | Trigger hacimleri: ışınlanma, itme, hasar, satın alma alanı, bomba noktası |
| `blockbullets` | `toolsblockbullets` | Mermiyi durdurur, oyuncu içinden geçer |
| `passbullets` | — | Katı ama merminin içinden geçtiği yüzey |
| `blocklos` | `toolsblock_los` | Bot görüşünü kapatan hacim |
| `blocksound` | `toolsblocksound` | Sesi kesen hacim |
| `blocklight` | `toolsblocklight` | Işığı kesen hacim |
| `blockbomb` | `toolsblockbomb` | Bombanın kurulamadığı hacim |
| `navclip`, `navspaceclip` | `toolsnavclip`, `toolsnavspaceclip` | Bot navigasyonuna kapalı alanlar |
| `teleportclip` | `toolsteleportclip` | Işınlanmayı engelleyen hacim |
| `controlclip`, `otherclip` | `toolscontrolclip`, `toolsotherclip` | Özel clip türleri |
| `ignorenpc` | `toolsignorenpc` | Botların yok saydığı geometri |
| `sky` | `toolsskybox` | Gökyüzü sınırı |
| `water` | — | Su hacmi |

Her haritada her tür bulunmaz. Haritanın içerdiği türler, harita açılırken sunucu konsoluna yazılır.

## Notlar

- Bir harita ilk açıldığında çizgiler arka planda hazırlanır. O sırada komutu kullanırsanız "hazırlanıyor" mesajı gelir; birkaç saniye sonra tekrar deneyin.
- Hazırlanan veri eklenti klasöründeki `cache` klasöründe tutulur. Harita dosyası veya `showclips_types` listesi değişince kendiliğinden yenilenir, klasör istendiği zaman silinebilir.
- Çok geniş alan kaplayan bir tür eklemek (`blockbullets` gibi) büyük haritalarda ilk açılışı uzatır.
- Trigger hacimleri, trigger'ın etrafındaki kutu olarak çizilir; şekli sıra dışı olan triggerlarda kutu gerçek trigger'dan büyük görünebilir.
- Çizgiler tam duvara oturduğu yerlerde bozuk görünüyorsa `showclips_offset` değerini artırın (örneğin `2`) veya `showclips_width` değerini kalınlaştırın.
- Komut adı değişikliği (`showclips_cmd`) sunucu/eklenti yeniden başlatıldığında etkinleşir.
