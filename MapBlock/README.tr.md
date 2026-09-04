# MapBlock

*Bu dosyanın [İngilizcesi / English](README.md).*

Oyuncu sayısı düşükken haritanın belirli bölgelerini çit modelleriyle otomatik kapatır, takımlar dolduğunda engelleri kendiliğinden kaldırır. Engelleri oyun içi menüden yerleştirirsiniz.

## Özellikler

- Oyun içi menüden engel oluşturma ve silme; her değişiklik anında kaydedilir
- Harita başına ayrı yerleşim dosyası (`maps/` klasörü)
- Oyuncu sayısına göre otomatik açma/kapama, her raunt başında yeniden değerlendirilir
- Warmup sırasında engeller kurulmaz
- İki sayım modu: yalnızca CT veya iki takım birden
- Bot ve HLTV/GOTV yayını sayıma katılmaz
- Engeller kurulduğu her raunt sohbete duyuru
- Düzenleme modu ile sunucu kalabalıkken bile engelleri görüp düzenleyebilirsiniz
- Menüde çıkacak modeller config'ten belirlenir
- Elle düzenleme sonrası sunucuyu yeniden başlatmadan yeniden yükleme komutu
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.373
- `gamedata` dosyası: `addons/counterstrikesharp/gamedata/NativeTrace.gamedata.json`

## Kurulum

1. Derlenmiş `MapBlock` klasörünü sunucuya kopyalayın (**`MapBlock.example.json` dahil**):
   ```
   csgo/addons/counterstrikesharp/plugins/MapBlock/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load MapBlock` komutunu çalıştırın.
3. İlk yüklemede örnek yerleşimler `maps/` klasörüne harita harita bölünür; sonrasını menü kendisi yazar.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_mapblock` | Engel menüsünü açar (hayatta olmak gerekir) | `mapblock_flag` |
| `css_engel` | Aynı menüyü açar | `mapblock_flag` |
| `css_mapblock_reload` | Bulunduğunuz haritanın yerleşim dosyasını yeniden yükler ve uygular | `mapblock_reload_flag` |

### Menü Seçenekleri

| Seçenek | İşlev |
| --- | --- |
| Oluştur | Seçili modeli baktığınız noktaya yerleştirir ve kaydeder |
| Model Değiştir | `mapblock_models` içindeki modeller arasında döngüsel geçiş |
| Baktığın Engeli Sil | Nişan aldığınız engeli kaldırır (maks. 256 birim mesafe) ve kayıttan siler |
| Haritadaki Tüm Engelleri Sil | Bulunduğunuz haritanın bütün engellerini temizler |
| Düzenleme Modu | Oyuncu sayısına bakmadan engelleri açık tutar |

Engeller modelin `offset` değeri kadar yana kaydırılarak kurulur, bu yüzden modelin sağ kenarının oturmasını istediğiniz noktaya bakın. Düzenleme modunu kapattığınızda eklenti anında otomatik kurala döner.

## Yapılandırma

```
csgo/addons/counterstrikesharp/configs/plugins/MapBlock/MapBlock.json
```

| Ayar | Tip | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `mapblock_mode` | int | `2` | `0`: kapalı, `1`: CT sayısına bakar, `2`: iki takıma birden bakar |
| `mapblock_count` | int | `4` | Eşik; sayı bu değerin **altındayken** engeller kurulur (`0` = her zaman kur) |
| `mapblock_announce` | bool | `true` | Engellerin kurulduğu her raunt sohbete duyuru yazar |
| `mapblock_cmd` | string | `"css_mapblock,css_engel"` | Menüyü açan komutlar, virgülle ayrılır |
| `mapblock_flag` | string | `"@css/root"` | Menüyü kullanabilecek flagler, virgülle ayrılır |
| `mapblock_reload_cmd` | string | `"css_mapblock_reload"` | Yeniden yükleme komutları, virgülle ayrılır |
| `mapblock_reload_flag` | string | `"@css/root"` | Yeniden yükleme komutunu kullanabilecek flagler, virgülle ayrılır |
| `mapblock_models` | object | 6 model | Menüde çıkacak modeller |

`mapblock_mode: 2` iki takımın küçüğüne bakar: `mapblock_count: 4` iken 3v3, 4v3 ve 3v4 durumlarında engeller durur, takımlar 4v4 olduğunda bir sonraki raunt başında kalkar. Warmup süresince mod ne olursa olsun engel kurulmaz.

### `mapblock_announce`

Duyuru metni moda göre değişir:

| Ayarlar | Sohbete düşen mesaj |
| --- | --- |
| `mapblock_mode: 1`, `mapblock_count: 4` | `CT 4 kişiden az olduğu için harita küçültüldü.` |
| `mapblock_mode: 2`, `mapblock_count: 5` | `5v5 olmadığı için harita küçültüldü.` |

Duyuru engellerin kurulduğu her raunt başında yazılır; kalktıklarında bir şey yazılmaz. Haritanın hiç kayıtlı engeli yoksa, `mapblock_count` `0` ise veya düzenleme modu açıksa duyuru yapılmaz. Metinleri `lang/` klasöründen değiştirebilirsiniz.

### `mapblock_models`

Menüde görünen ad anahtar, değer de model yolu ile yerleştirme kaymasıdır:

```json
"mapblock_models": {
  "Cit 128": {
    "model": "models/props/de_nuke/hr_nuke/chainlink_fence_001/chainlink_fence_001_128_capped.vmdl",
    "offset": 64.0
  }
}
```

| Alan | Açıklama |
| --- | --- |
| `model` | Model yolu |
| `offset` | Model kurulurken yana ne kadar kaydırılacağı; modelin genişliğinin yarısını yazın |

Buraya eklediğiniz her model kurduğunuz anda doğru görünür.

### Yerleşim Dosyaları

Eklenti klasöründeki `maps/` klasöründe, `lang/` ile yan yana durur. Her haritanın kendi dosyası vardır ve dosya adı harita adıdır — `maps/de_mirage.json`:

```json
[
  {
    "Model": "models/props/de_nuke/hr_nuke/chainlink_fence_001/chainlink_fence_001_128_capped.vmdl",
    "Origin": [512.0, -128.0, 64.0],
    "Angles": [0.0, 90.0, 0.0]
  }
]
```

| Alan | Açıklama |
| --- | --- |
| `Model` | Model yolu |
| `Origin` | `[x, y, z]` dünya koordinatı |
| `Angles` | `[pitch, yaw, roll]` açı değerleri |

Bu dosyaları menü sizin yerinize yazar; yalnızca koordinatları elle düzenlemek isterseniz açmanız gerekir. Alan adları büyük/küçük harf duyarsızdır, sondaki fazla virgüller sorun çıkarmaz. Elle düzenledikten sonra `css_mapblock_reload` çalıştırın.

## Notlar

- **Eklentiyi güncellemeden önce `maps/` klasörünü yedekleyin.** Klasör eklenti klasörünün içindedir, o klasörü komple değiştiren bir kurulum yerleşimlerinizi siler.
- Eski sürümden kalma tek dosyalık yerleşim (`MapBlock.placements.json` veya eklenti klasöründeki `MapBlock.json`) ilk yüklemede `maps/` klasörüne bölünür, eski dosya `.bak` olarak saklanır.
- Dosya adları küçük harfe çevrilir, harita adı eşleşmesi büyük/küçük harf duyarsızdır. Hiç engeli olmayan haritanın dosyası da olmaz.
- Engeller yerinde sabit durur, itilmez ve hasar almaz. Temizlik yalnızca bu eklentiyle koyulan engelleri kaldırır, haritanın kendi nesnelerine dokunmaz.
- Kullanılan modeller `de_nuke` chainlink fence modelleridir; tüm resmi haritalarda kullanılabilir.
