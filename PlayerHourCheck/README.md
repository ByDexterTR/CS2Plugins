# PlayerHourCheck

Sunucuya bağlanan oyuncuların CS2 oynama saatini kontrol eder; yetersiz saati olan veya profili gizli oyunculara kademeli ceza (kick/ban) uygular.

## Özellikler

- 3 aşamalı oynama saati sorgusu: **Steam Web API** → **DecAPI** → **ByDexter API** (ilk başarılı sonuç kullanılır)
- **JSON (varsayılan) veya MySQL** depolama; MySQL bağlantısı başarısız olursa JSON'a düşer
- Sonuçlar veritabanında önbelleğe alınır — eksik saat dolmadan tekrar API sorgusu yapılmaz
- Profili gizli oyunculara yapılandırılabilir sayıda **uyarı**, ardından ceza
- İhlal sayısına göre **kademeli ceza sistemi** (ör. 1. ihlal kick, 3. ihlal 1 saat ban, 5. ihlal 1 gün ban)
- Yetki bayrağı veya SteamID ile muafiyet listesi
- Config'i ve tüm oyuncuları yeniden kontrol eden reload komutu
- Renk kodlu mesaj desteği (`{Gold}`, `{Red}` vb.)
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371
- Ceza uygulamak için `css_kick` ve `css_ban` komutlarını sağlayan bir admin eklentisi (ör. CS2-SimpleAdmin)
- (Önerilen) [Steam Web API anahtarı](https://steamcommunity.com/dev/apikey)
- (MySQL kullanılacaksa) MySQL 8+ sunucusu

## Kurulum

1. Derlenmiş `PlayerHourCheck` klasörünü **tüm bağımlılık DLL'leriyle birlikte** sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/PlayerHourCheck/
   ```
2. İlk yüklemede oluşan config dosyasını düzenleyin (en azından `phc_required_playtime` ve tercihen `phc_steam_api_key`).
3. `css_plugins reload PlayerHourCheck` ile yeniden yükleyin.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_phc_reload` | Config'i diskten yeniden yükler ve tüm oyuncuları yeniden kontrol eder | `@css/root` |

## Yapılandırma

```
csgo/addons/counterstrikesharp/configs/plugins/PlayerHourCheck/PlayerHourCheck.json
```

| Ayar | Tip | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `phc_db` | nesne | json | Depolama ayarları (aşağıda) |
| `phc_steam_api_key` | string | `""` | Steam Web API anahtarı (boşsa doğrudan DecAPI'ye geçilir) |
| `phc_required_playtime` | int | `100` | Gereken minimum CS2 saati |
| `phc_warn_enabled` | int | `1` | Gizli profil uyarı sistemi (1: açık, 0: direkt ceza) |
| `phc_warn_times` | int | `3` | Cezadan önceki uyarı sayısı |
| `phc_warn_timer` | int | `30` | Uyarılar arası bekleme (saniye) |
| `phc_warn_reason_private` | string | — | Gizli profil uyarı mesajı (`{0}`: mevcut, `{1}`: toplam uyarı) |
| `phc_kick_reason_private` | string | — | Gizli profil kick sebebi |
| `phc_kick_reason_playtime` | string | — | Yetersiz saat kick sebebi |
| `phc_penalty` | nesne | aşağıda | İhlal sayısı → ceza eşlemesi |
| `phc_ignore_flags` | liste | `["@bydexter/ignoreplaytime", "@css/root"]` | Muaf yetki bayrakları |
| `phc_ignore_steamids` | liste | — | Muaf SteamID64 listesi |

### `phc_db`

```json
"phc_db": {
  "provider": "json",
  "host": "localhost",
  "name": "cs2_playerhourcheck",
  "port": "3306",
  "user": "root",
  "password": ""
}
```

- `provider`: `"json"` (varsayılan, eklenti klasöründe `players.json`) veya `"mysql"`
- MySQL seçiliyse veritabanı ve tablo otomatik oluşturulur.

### `phc_penalty`

Anahtar = ihlal sayısı, değer = ceza. `type`: `"kick"` veya `"ban"`, `time`: ban süresi (dakika), `reason` içinde `{PlayerPlaytime}` ve `{RequiredPlaytime}` yer tutucuları kullanılabilir:

```json
"phc_penalty": {
  "1": { "type": "kick", "time": 0,    "reason": "Yetersiz oyun saati ({PlayerPlaytime}/{RequiredPlaytime} saat)" },
  "3": { "type": "ban",  "time": 60,   "reason": "Yetersiz oyun saati ({PlayerPlaytime}/{RequiredPlaytime} saat)" },
  "5": { "type": "ban",  "time": 1440, "reason": "Yetersiz oyun saati ({PlayerPlaytime}/{RequiredPlaytime} saat)" }
}
```

> Aradaki ihlallerde bir alt eşiğin cezası uygulanır (ör. 4. ihlal → "3" kaydı).

## Çalışma Mantığı

1. Oyuncu bağlanır → muafiyet kontrolü → veritabanı kaydına bakılır.
2. Kayıtlı saat yeterliyse hiçbir şey yapılmaz; eksikse ve "eksik saat kadar zaman" henüz geçmemişse doğrudan ceza uygulanır (API sorgusu yapılmaz).
3. Aksi hâlde ~2 saniye sonra API'lerden güncel saat çekilir:
   - Saat yeterli → kayıt güncellenir, ihlal sayacı sıfırlanır.
   - Saat yetersiz → ihlal sayacı artar, kademeli ceza uygulanır.
   - Profil gizli → uyarı döngüsü başlar; uyarılar bitince ceza uygulanır.

## Notlar

- Cezalar `css_kick` / `css_ban` konsol komutlarıyla uygulanır; bu komutlar sunucuda tanımlı değilse ceza gerçekleşmez.
- Tüm veritabanı işlemleri arka planda yürütülür, oyun akışı bloklanmaz.