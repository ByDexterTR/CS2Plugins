# Sesler

Oyuncuların duymak istemediği oyun seslerini kategori bazında kapatmasını sağlar. Tercihler oyuncu bazında veritabanında saklanır ve tekrar girişte otomatik uygulanır.

## Özellikler

- 5 ses kategorisi: **Bıçak**, **Silah**, **Ayak sesi**, **Oyuncu sesleri**, **MVP müziği**
- Her kategori için 4 mod: **Açık**, **Düşmanı Sustur**, **Takımı Sustur**, **Kapalı** (MVP için yalnızca Açık/Kapalı)
- CenterHtml menü ile kolay yönetim; aktif seçenek ► işareti ve renkle vurgulanır
- **JSON (varsayılan) veya MySQL** depolama; MySQL bağlantısı başarısız olursa JSON'a düşer, tablo otomatik oluşturulur
- Ses engelleme sunucu tarafında UserMessage alıcı filtrelemesiyle yapılır — diğer oyuncular sesleri normal duyar
- MVP susturması `StopSoundEvents.StopAllMusic` ile yalnızca ilgili oyuncuda çalışır
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371
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
| `Database.name` | string | `"bydexter_sesler"` | Veritabanı adı (yoksa oluşturulur) |
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

- Ses hash listeleri oyun güncellemeleriyle değişebilir; yeni sesler duyulmaya başlarsa hash listelerinin güncellenmesi gerekir.
- Veritabanı işlemleri arka planda yürütülür, oyun akışını bloklamaz.