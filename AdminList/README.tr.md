# AdminList

Oyuncuların `css_admins` komutuyla o an çevrimiçi olan yetkilileri grup etiketleriyle görmesini sağlar. Gruplar config'ten tanımlanır; her grubun etiketi, etiket rengi, isim rengi ve yetki bayrağı vardır.

```
Çevrimiçi Yetkililer:
[OWNER] ByDexter
[DEV] Claude
[MOD] Grok
[VIP] Gemini
```

## Özellikler

- Sunucu sahibi config'ten sınırsız grup tanımlayabilir (`tag`, `tag_color`, `name_color`, `flag`)
- Gruplar yukarıdan aşağıya öncelik sırasıyla değerlendirilir; oyuncu eşleştiği ilk gruptan sayılır (ör. `@css/root` sahibi yalnızca en üstteki grupta görünür, alt gruplarda tekrarlanmaz)
- Etiket ve isim renkleri grup bazında ayrı ayrı ayarlanabilir
- `css_adminsreload` / `css_reloadadmins` ile config sunucu yeniden başlatılmadan yeniden yüklenir
- Botlar ve GOTV listeye girmez
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Kurulum

1. Derlenmiş `AdminList` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/AdminList/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load AdminList` komutunu çalıştırın.
3. İlk yüklemede config dosyası otomatik oluşturulur.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_admins` | Çevrimiçi yetkilileri grup etiketleriyle listeler | Yok |
| `css_adminsreload` / `css_reloadadmins` | Config'i yeniden yükler | `@css/root` |

## Yapılandırma

```
csgo/addons/counterstrikesharp/configs/plugins/AdminList/AdminList.json
```

| Ayar | Tip | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `admins_cmd` | string | `"css_admins"` | Virgülle ayrılmış liste komutu adları |
| `reload_cmd` | string | `"css_adminsreload,css_reloadadmins"` | Virgülle ayrılmış reload komutu adları |
| `reload_flag` | string | `"@css/root"` | Reload komutu için gereken yetki |
| `groups` | array | 4 örnek grup | Öncelik sırasına göre grup tanımları |

### Grup Alanları

| Alan | Açıklama |
| --- | --- |
| `tag` | Chat'te gösterilen etiket (`[TAG]`) |
| `tag_color` | Etiket rengi |
| `name_color` | Oyuncu ismi rengi |
| `flag` | Grubun yetki bayrağı (ör. `@css/mod`) |

Geçerli renkler: `default`, `white`, `darkred`, `green`, `lightgreen`, `lime`, `red`, `grey`, `yellow`, `bluegrey`, `blue`, `darkblue`, `purple`, `orchid`, `lightred`, `gold`

### Örnek Config

```json
{
  "admins_cmd": "css_admins",
  "reload_cmd": "css_adminsreload,css_reloadadmins",
  "reload_flag": "@css/root",
  "groups": [
    { "tag": "OWNER", "tag_color": "darkred", "name_color": "gold", "flag": "@css/owner" },
    { "tag": "DEV", "tag_color": "purple", "name_color": "lightred", "flag": "@css/dev" },
    { "tag": "MOD", "tag_color": "blue", "name_color": "bluegrey", "flag": "@css/mod" },
    { "tag": "VIP", "tag_color": "gold", "name_color": "yellow", "flag": "@css/vip" }
  ]
}
```

## Notlar

- Grup sıralaması önceliktir: oyuncu birden fazla grubun bayrağına sahipse yalnızca listede en üstte olan grupta gösterilir. `@css/root` sahipleri tüm `@css/*` bayraklarını geçtiği için en üst gruba `@css/root` veya `@css/owner` gibi bir bayrak koymak doğru sıralamayı sağlar.
- Bayraklar CounterStrikeSharp'ın `admins.json` dosyasındaki yetkilerle eşleşir; `@css/owner`, `@css/dev`, `@css/mod` gibi özel bayraklar da kullanılabilir.
- Komut adı değişiklikleri (`admins_cmd`, `reload_cmd`) eklenti yeniden başlatıldığında etkinleşir; reload komutu yalnızca grup ve yetki ayarlarını anında günceller.
