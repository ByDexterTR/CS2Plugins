# Sesler

*Bu dosyanın [İngilizcesi / English](README.md).*

Oyuncuların duymak istemediği oyun seslerini kategori bazında kapatmasını sağlar. Tercihler oyuncu bazında database'de saklanır ve tekrar girişte otomatik uygulanır.

## Özellikler

- 5 ses kategorisi: **Bıçak**, **Silah**, **Ayak sesi**, **Oyuncu sesleri**, **MVP müziği**
- Her kategori için 4 mod: **Açık**, **Düşmanı Sustur**, **Takımı Sustur**, **Kapalı** (MVP için yalnızca Açık/Kapalı)
- Ekran menüsüyle kolay yönetim; aktif seçenek ► işareti ve renkle vurgulanır
- **JSON (varsayılan) veya MySQL** depolama; MySQL bağlantısı başarısız olursa JSON'a düşer, tablo otomatik oluşturulur
- Susturma yalnızca o oyuncuya özeldir — diğer oyuncular sesleri normal duymaya devam eder
- MVP müziği susturulduğunda yalnızca susturan oyuncunun müziği kesilir
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.375
- (MySQL kullanılacaksa) MySQL 8+ sunucusu

## Kurulum

1. Derlenmiş `Sesler` klasörünü **tüm bağımlılık DLL'leriyle birlikte** sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/Sesler/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load Sesler` komutunu çalıştırın.
3. Varsayılan olarak JSON kullanılır (eklenti klasöründe `players.json`); MySQL için config'i düzenleyin.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_ses` / `css_sesler` | Ses tercihleri menüsünü açar | — (herkes) |

## Yapılandırma

```
csgo/addons/counterstrikesharp/configs/plugins/Sesler/Sesler.json
```

| Ayar | Tip | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `Database.provider` | string | `"json"` | `"json"` veya `"mysql"` |
| `Database.host` | string | `"localhost"` | MySQL sunucu adresi |
| `Database.name` | string | `"bydexter_sesler"` | Database adı (yoksa oluşturulur) |
| `Database.port` | string | `"3306"` | MySQL portu |
| `Database.user` | string | `"root"` | MySQL kullanıcısı |
| `Database.password` | string | `""` | MySQL şifresi |

### Örnek Config

```json
{
  "Database": {
    "provider": "mysql",
    "host": "127.0.0.1",
    "name": "bydexter_sesler",
    "port": "3306",
    "user": "cs2",
    "password": "gizli"
  }
}
```

## Notlar

- **Takımı Sustur** kendi seslerinizi asla susturmaz; **Kapalı** onları da susturur.
- **Düşmanı Sustur** ve **Takımı Sustur** yalnızca T veya CT takımındaki oyuncularda çalışır. İzleyicilerde sadece **Kapalı** geçerlidir.
- Bıçak bir oyuncuya isabet ettiğinde biri saldıranda, biri kurbanda olmak üzere iki ses çalar. Kurban tarafındaki isabet sesi yalnızca **Kapalı** modunda susturulur; böylece **Düşmanı Sustur** kendi isabetlerinizin sesini kesmez.
- Tercihler oyuncunun Steam ID'si doğrulandıktan sonra yüklenir. Sunucuya girdikten sonraki ilk saniyede `css_ses` yanıt vermeyebilir.
- Bir CS2 güncellemesinden sonra kapattığınız kategorideki bazı sesler yeniden duyulmaya başlarsa, eklentinin güncellenmesi gerekiyor demektir.
