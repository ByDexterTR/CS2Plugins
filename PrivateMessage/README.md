# PrivateMessage

Oyuncuların birbirlerine `!pm` / `!msg` ile özel mesaj göndermesini sağlar. Mesaj komutları chat'te diğer oyunculara gözükmez; alıcı ve gönderene ayrı bildirim sesleri çalınır.

```
Alıcının ekranı:    [ByDexter] Gönderen: Bugün nasılsın
Gönderenin ekranı:  [ByDexter] Mesaj Alıcı kullanıcısına gönderildi.
```

## Özellikler

- `!pm <oyuncu> <mesaj>` ile özel mesaj; isim tam veya kısmi eşleşmeyle bulunur
- `!pm` / `!msg` ile başlayan chat yazıları herkese gözükmez (UserMessage hook)
- Oyuncu bazında özel mesajları kapatma/açma (`!pmoff` / `!pmon`); kapatan oyuncu PM alamaz ve gönderemez
- Oyuncu bazında bildirim sesi açma/kapama (`!pmsound`)
- Alıcıya ve gönderene ayrı sesler çalınır; sesler config'ten değiştirilebilir
- Tercihler kalıcıdır — JSON (varsayılan) veya MySQL; MySQL bağlantısı koparsa otomatik JSON'a düşer
- İsteğe bağlı loglama: konsola ve `logs/` altına günlük ayrı dosyalarla yazar
- Botlar ve GOTV hedef alınamaz
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Kurulum

1. Derlenmiş `PrivateMessage` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/PrivateMessage/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load PrivateMessage` komutunu çalıştırın.
3. İlk yüklemede config dosyası otomatik oluşturulur.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_msg` / `css_pm <oyuncu> <mesaj>` | Oyuncuya özel mesaj gönderir | Yok |
| `css_msgoff` / `css_pmoff` | Özel mesajları kapatır | Yok |
| `css_msgon` / `css_pmon` | Özel mesajları açar | Yok |
| `css_msgsound` / `css_pmsound` | Özel mesaj sesini açar/kapatır | Yok |

## Yapılandırma

```
csgo/addons/counterstrikesharp/configs/plugins/PrivateMessage/PrivateMessage.json
```

| Ayar | Tip | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `msg_cmd` | string | `"css_msg,css_pm"` | Virgülle ayrılmış mesaj komutu adları |
| `msgoff_cmd` | string | `"css_msgoff,css_pmoff"` | Virgülle ayrılmış kapatma komutu adları |
| `msgon_cmd` | string | `"css_msgon,css_pmon"` | Virgülle ayrılmış açma komutu adları |
| `msgsound_cmd` | string | `"css_msgsound,css_pmsound"` | Virgülle ayrılmış ses komutu adları |
| `receive_sound` | string | `"sounds/ambient/common/water/rain_drip3.vsnd"` | Alıcıya çalınan ses |
| `send_sound` | string | `"sounds/ambient/common/water/rain_drip1.vsnd"` | Gönderene çalınan ses |
| `log_enabled` | bool | `false` | Mesajları konsola ve günlük log dosyasına yazar |
| `database` | object | JSON | Tercih depolama ayarları (aşağıda) |

### Depolama

| Alan | Varsayılan | Açıklama |
| --- | --- | --- |
| `provider` | `"json"` | `"json"` veya `"mysql"` |
| `host` | `"localhost"` | MySQL sunucusu |
| `name` | `"bydexter_pm"` | Veritabanı adı (yoksa oluşturulur) |
| `port` | `"3306"` | MySQL portu |
| `user` | `"root"` | MySQL kullanıcısı |
| `password` | `""` | MySQL şifresi |

JSON modunda tercihler `plugins/PrivateMessage/players.json` dosyasında tutulur. MySQL modunda `pm_preferences` tablosu otomatik oluşturulur; bağlantı kurulamazsa JSON'a düşülür.

### Örnek Config

```json
{
  "msg_cmd": "css_msg,css_pm",
  "msgoff_cmd": "css_msgoff,css_pmoff",
  "msgon_cmd": "css_msgon,css_pmon",
  "msgsound_cmd": "css_msgsound,css_pmsound",
  "receive_sound": "sounds/ambient/common/water/rain_drip3.vsnd",
  "send_sound": "sounds/ambient/common/water/rain_drip1.vsnd",
  "log_enabled": true,
  "database": {
    "provider": "mysql",
    "host": "localhost",
    "name": "bydexter_pm",
    "port": "3306",
    "user": "root",
    "password": ""
  }
}
```

## Loglama

`log_enabled: true` iken her mesaj `[GÖNDEREN -> ALICI]: Mesaj` biçiminde sunucu konsoluna yazılır ve gün bazında ayrı dosyalara kaydedilir:

```
plugins/PrivateMessage/logs/PrivateMessage-2026-07-16.log
```

Dosya satırları saat damgası içerir: `[21:45:03] [ByDexter -> Oyuncu]: selam`

## Notlar

- Chat'e yazılan `!pm ...` / `/pm ...` satırları (config'teki tüm komut adları dahil) UserMessage hook ile engellenir, kimseye gözükmez; komut yine de çalışır.
- Mesaj ve ses tercihleri SteamID bazında kalıcıdır (varsayılan: mesajlar açık, ses açık); her değişiklikte ve oyuncu çıkışında kaydedilir.
- İsim eşleşmesi önce tam ad, yoksa kısmi arama yapar; birden fazla eşleşmede mesaj gönderilmez.
- Komut adı değişiklikleri eklenti yeniden başlatıldığında etkinleşir.
