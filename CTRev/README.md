# CTRev

Ölen CT oyuncularını menüden veya otomatik olarak canlandırır (respawn). Jailbreak'te gardiyan dengesini korumak için raunt başına sınırlı hak sistemiyle çalışır.

## Özellikler

- Ölen CT'leri listeleyen CenterHtml menü — canlandırılabilir oyuncular yeşil, bekleme süresindekiler gri gösterilir
- Ölümden sonra **bekleme süresi (cooldown)** — süre dolmadan canlandırma yapılamaz
- Raunt başına **sınırlı canlandırma hakkı**; her raunt başında otomatik yenilenir
- **Otomatik canlandırma modu** — açıldığında bekleme süresi dolan CT'ler hak bitene kadar kendiliğinden doğar
- Hakları raunt ortasında sıfırlama komutu
- Menü açıkken saniyede bir otomatik yenilenir (kalan süreler canlı görünür)
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.369+

## Kurulum

1. Derlenmiş `CTRev` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/CTRev/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load CTRev` komutunu çalıştırın.
3. İlk yüklemede config dosyası otomatik oluşturulur.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_ctr` / `css_ctrev` / `css_ctrevmenu` | Canlandırma menüsünü açar | `@css/generic` **veya** `@jailbreak/warden` |
| `css_hak0` / `css_haksifir` / `css_haksifirla` | Canlandırma haklarını sıfırlar (yeniler) | `@css/generic` |

## Yapılandırma

```
csgo/addons/counterstrikesharp/configs/plugins/CTRev/CTRev.json
```

| Ayar | Tip | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `cooldown` | int | `15` | Ölümden sonra canlandırılabilmek için beklenecek süre (saniye) |
| `revive_count` | int | `3` | Raunt başına toplam canlandırma hakkı |

### Örnek Config

```json
{
  "cooldown": 15,
  "revive_count": 3
}
```

## Kullanım Örneği

1. Warden `!ctr` yazar → menüde kalan hak sayısı ve ölü CT listesi görünür.
2. Yeşil görünen oyuncuya tıklar → oyuncu canlanır, kalan hak tüm sunucuya duyurulur.
3. Dilerse "Otomatik Canlandırma: Kapalı" seçeneğini açar → bekleme süresi dolan CT'ler hak bitene kadar otomatik doğar.

## Notlar

- Canlandırma hakları **takım genelidir**, oyuncu başına değildir.
- Hak bittiğinde menüden veya otomatik moddan canlandırma yapılamaz; `!hak0` ile yenilenebilir.