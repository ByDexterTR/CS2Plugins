# HideTeammates

Oyuncuların diğer oyuncu modellerini komutla gizlemesini sağlar. Gizlenen oyuncular istemciye hiç gönderilmez (`CheckTransmit`), istenirse çıkardıkları sesler de duyulmaz. Tercihler SteamID bazında JSON'a yazılır; kayıtlı oyuncu sunucuya girdiğinde gizleme otomatik açılır.

## Özellikler

- `css_hide` / `css_gizle` ile aç/kapat; komut adları config'ten değiştirilebilir
- Yetki (flag) kontrolü — boş bırakılırsa herkes kullanabilir
- 3 gizleme modu: takım arkadaşları, rakip takım veya herkes (`mode_hide`)
- Gizlenen oyuncunun modeli ve elindeki silahlar istemciye gönderilmez
- `disable_sound` ile gizlenen oyuncuların ayak sesi, beden sesi, bıçak ve silah sesleri de susturulur (257 ses hash'i)
- Tercihler `players.json` içinde SteamID dizisi olarak saklanır; girişte otomatik uygulanır
- Ölüyken/izlerken gizleme uygulanmaz, izlenen oyuncunun görüntüsü bozulmaz
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Kurulum

1. Derlenmiş `HideTeammates` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/HideTeammates/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load HideTeammates` komutunu çalıştırın.
3. İlk yüklemede config dosyası otomatik oluşturulur.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_hide` / `css_gizle` | Gizlemeyi açar/kapatır; tercih kaydedilir | `flag_hide` (varsayılan: herkes) |

## Yapılandırma

```
csgo/addons/counterstrikesharp/configs/plugins/HideTeammates/HideTeammates.json
```

| Ayar | Tip | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `cmd_hide` | string | `"css_hide,css_gizle"` | Virgülle ayrılmış komut adları |
| `flag_hide` | string | `""` | Gerekli yetki; boş string = herkes kullanabilir |
| `mode_hide` | int | `1` | `1`: takım arkadaşları, `2`: rakip takım, `3`: herkes |
| `disable_sound` | int | `1` | `0`: sesler duyulur, `1`: gizlenen oyuncuların sesleri de susturulur |

### Örnek Config

```json
{
  "cmd_hide": "css_hide,css_gizle",
  "flag_hide": "",
  "mode_hide": 1,
  "disable_sound": 1
}
```

## Tercih Dosyası

```
csgo/addons/counterstrikesharp/plugins/HideTeammates/players.json
```

```json
[
  "76561198000000000",
  "76561198111111111"
]
```

Dosyadaki SteamID'ler gizlemesi açık oyunculardır; komutla açıp kapattıkça dosya güncellenir. Elle de düzenlenebilir, değişiklik eklenti yeniden yüklendiğinde okunur.

## Notlar

- Gizleme yalnızca **hayattayken** uygulanır; ölü/izleyici durumundayken tüm oyuncular görünür kalır, böylece izlenen oyuncunun görüntüsü bozulmaz.
- Gizlenen oyuncular yalnızca görünmez olur; çarpışma (collision) ve mermi engelleme devam eder.
- Ses susturma `208` (soundevent), `369` (weapon sound) ve `452` (weapon event) user message'ları üzerinden yapılır; ayak sesi/beden sesi/bıçak hash listesi Sesler, VIPCore `Silent` ve jRandomSkills kaynaklarının birleşimidir.
- `disable_sound` değeri sunucu/eklenti yeniden başlatıldığında etkinleşir (hook'lar yüklemede bağlanır).
- Komut adı değişikliği (`cmd_hide`) sunucu/eklenti yeniden başlatıldığında etkinleşir.
- Sesleri kategori bazında kendisi ayarlamak isteyen oyuncular için [Sesler](../Sesler) eklentisine bakın.
