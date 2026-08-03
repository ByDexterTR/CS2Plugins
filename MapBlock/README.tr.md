# MapBlock

Oyuncu sayısı düşükken haritanın belirli bölgelerini çit modelleriyle otomatik kapatır. Sunucu kalabalıklaştığında engeller kendiliğinden kalkar.

## Özellikler

- Harita bazlı kalıcı çit yerleşimleri (`MapBlock.json`)
- Oyuncu sayısına göre otomatik açma/kapama: eşik altında çitler kurulur, eşiğe ulaşılınca kaldırılır
- İki sayım modu: yalnızca CT veya T+CT
- Her raunt başında durum yeniden değerlendirilir
- Örnek yerleşim dosyası (`MapBlock.example.json`) ilk çalıştırmada otomatik kopyalanır
- JSON'u elle düzenledikten sonra sunucuyu yeniden başlatmadan yeniden yükleme komutu
- Çit modelleri otomatik precache edilir
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Kurulum

1. Derlenmiş `MapBlock` klasörünü sunucuya kopyalayın (**`MapBlock.example.json` dahil**):
   ```
   csgo/addons/counterstrikesharp/plugins/MapBlock/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load MapBlock` komutunu çalıştırın.
3. İlk yüklemede `MapBlock.example.json` → `MapBlock.json` olarak kopyalanır; kendi yerleşimlerinizi bu dosyaya ekleyin.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_mapblock_reload` | `MapBlock.json` dosyasını yeniden yükler ve yerleşimleri uygular | `@css/root` |

## Yapılandırma

Config dosyası:

```
csgo/addons/counterstrikesharp/configs/plugins/MapBlock/MapBlock.json
```

| Ayar | Tip | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `mapblock_mode` | int | `1` | `0`: kapalı, `1`: CT sayısına bak, `2`: T+CT sayısına bak |
| `mapblock_count` | int | `5` | Eşik; sayı bu değerin **altındayken** çitler kurulur (`0` = her zaman kur) |

### Yerleşim Dosyası (eklenti klasöründeki `MapBlock.json`)

Harita adı → yerleşim listesi biçimindedir:

```json
{
  "jb_harita_adi": [
    {
      "Model": "models/props/de_nuke/hr_nuke/chainlink_fence_001/chainlink_fence_001_128_capped.vmdl",
      "Origin": [512.0, -128.0, 64.0],
      "Angles": [0.0, 90.0, 0.0]
    }
  ]
}
```

| Alan | Açıklama |
| --- | --- |
| `Model` | Precache edilen çit modellerinden biri (64/128/256 boyutları) |
| `Origin` | `[x, y, z]` dünya koordinatı |
| `Angles` | `[pitch, yaw, roll]` açı değerleri |

## Kullanım Örneği

- `mapblock_mode: 1`, `mapblock_count: 5` → sunucuda 5'ten az CT varken çitler kurulur; 5. CT geldiğinde bir sonraki raunt başında çitler kaldırılır.
- Yerleşim koordinatlarını belirlemek için [Cit](../Cit) eklentisiyle çit yerleştirip konumu `MapBlock.json`'a taşıyabilirsiniz.

## Notlar

- Çitler `bydexter_mapblock` adıyla etiketlenir; temizleme yalnızca bu propları hedefler.
- Harita adı eşleşmesi büyük/küçük harf duyarsızdır.