# DiscordLogger

Sunucu olaylarını Discord webhook'larına iletir ve istenirse günlük dosya logu tutar. 10 bağımsız kategori, 25+ olay türü; her kategori için ayrı kanal (webhook) tanımlanabilir.

## Özellikler

- **10 bağımsız log kategorisi** — harita, giriş/çıkış, komut, sohbet, öldürme, raunt, hasar, el bombası, C4 ve aktivite
- **Tıklanabilir oyuncu profilleri** — oyuncu adları Discord'da doğrudan Steam profiline link verir; botlar `(BOT)` etiketiyle düz metin gösterilir
- **Detaylı kill logu** — silah, isabet bölgesi, HP/zırh hasarı, mesafe, kafadan / dürbünsüz / smoke arkası / kör atış / havada / duvar delme etiketleri, asist (+flash asisti)
- **Detaylı raunt özeti** — kazanan, bitiş sebebi, **MVP**, oyuncu sayısı
- **Günlük dosya logu** — `log_to_file` açıldığında tüm aktif kategoriler `logs/DiscordLogger-YYYY-MM-DD.log` dosyasına da yazılır
- **Discord mesajlarında saat/süre yok** — Discord mesaj saatini zaten gösterdiği için mesajlara saat öneki eklenmez; oynama süresi ve raunt süresi yalnızca dosya loguna yazılır
- **Sıfır gereksiz yük** — webhook'u boş olan kategorinin event handler'ları hiç kayıt edilmez
- Mesajlar 3 saniyelik arabellekte toplanıp tek seferde gönderilir (rate-limit dostu, 2000 karakter sınırına uyar)
- Komut ve sohbet için **kara liste**; ısınma (warmup) rauntları loglanmaz
- Tüm mesaj şablonları `lang/` dosyalarından özelleştirilebilir (emoji dahil)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Kurulum

1. Derlenmiş `DiscordLogger` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/DiscordLogger/
   ```
2. Discord'da log kanalları için webhook URL'leri oluşturun.
3. İlk yüklemede oluşan config dosyasına webhook URL'lerini girin.
4. `css_plugins reload DiscordLogger` ile eklentiyi yeniden yükleyin.

## Komutlar

Config değişikliği sonrası `css_plugins reload DiscordLogger` yeterlidir.

## Kategoriler ve Olaylar

| Config anahtarı | Kapsanan olaylar |
| --- | --- |
| `webhook_map` | Harita değişimi |
| `webhook_connect` | Giriş, çıkış, isim değiştirme (`player_changename`) |
| `webhook_command` | Tüm komutlar (kara liste hariç) |
| `webhook_chat` | Sohbet mesajları (kara liste hariç) |
| `webhook_kill` | `player_death` — tüm detaylarıyla |
| `webhook_round` | Raunt başlangıcı ve bitişi (`round_mvp` dahil) |
| `webhook_damage` | `player_hurt` (kendine/dünyadan gelen hasar hariç) |
| `webhook_grenade` | `grenade_thrown`, `hegrenade_detonate`, `flashbang_detonate`, `player_blind`, `smokegrenade_detonate`, `smokegrenade_expired`, `molotov_detonate`, `decoy_detonate` |
| `webhook_bomb` | `bomb_planted`, `bomb_defused`, `bomb_exploded`, `bomb_dropped`, `bomb_pickup` |
| `webhook_activity` | `player_ping`, `weapon_zoom`, `item_purchase` |

> Boş bırakılan webhook'un kategorisi (dosya logu da kapalıysa) **tamamen devre dışıdır** — event handler'ları kayıt edilmez, hiçbir işlem yapılmaz.

## Yapılandırma

```
csgo/addons/counterstrikesharp/configs/plugins/DiscordLogger/DiscordLogger.json
```

| Ayar | Tip | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `webhook_map` … `webhook_activity` | string | `""` | Kategori webhook URL'leri (tablodaki 10 anahtar) |
| `log_to_file` | bool | `false` | Günlük dosya logu (`logs/DiscordLogger-YYYY-MM-DD.log`) |
| `command_blacklist` | liste | `["css_wp", "css_knife", ...]` | Loglanmayacak komutlar |
| `chat_blacklist` | liste | `["!wp", "!knife", ...]` | Loglanmayacak sohbet kalıpları |

### Örnek Config

```json
{
  "webhook_map": "https://discord.com/api/webhooks/....",
  "webhook_connect": "https://discord.com/api/webhooks/....",
  "webhook_command": "https://discord.com/api/webhooks/....",
  "webhook_chat": "https://discord.com/api/webhooks/....",
  "webhook_kill": "https://discord.com/api/webhooks/....",
  "webhook_round": "https://discord.com/api/webhooks/....",
  "webhook_damage": "https://discord.com/api/webhooks/....",
  "webhook_grenade": "https://discord.com/api/webhooks/....",
  "webhook_bomb": "https://discord.com/api/webhooks/....",
  "webhook_activity": "https://discord.com/api/webhooks/....",
  "log_to_file": true,
  "command_blacklist": ["css_wp", "css_knife"],
  "chat_blacklist": ["!wp", "!knife"]
}
```

## Mesaj Formatı

Oyuncu adları tıklanabilir Steam profil linkidir, alanlar **|** ile ayrılır:

```
🟢 ByDexter sunucuya bağlandı                          (isim → profil linki)
☠️ Kurban ⟵ Katil | Silah: ak47 | Bölge: kafa | Hasar: 108 HP / 12 zırh | Mesafe: 23.4m | [kafadan, duvar x1] | Yardım: Oyuncu (flash)
🩸 Kurban ⟵ Saldırgan | deagle | sol bacak | -25 HP / -0 zırh | Kalan: 0 HP, 88 zırh
😵 Oyuncu kör oldu | Atan: Rakip | Süre: 3.2 sn
⚡ Flash patladı | Atan: Oyuncu | Konum: (512, -128, 64)
🏁 12. raunt bitti | Kazanan: CT | Sebep: CT düşmanları öldürdü | MVP: Oyuncu | Oyuncu: 18
📍 Oyuncu ping attı | Konum: (1024, 256, 32)
✏️ Oyuncu isim değiştirdi: EskiAd → YeniAd
```

Dosya logunda ise linkler `Ad (profil-url)` biçimine düzleştirilir ve süre bilgileri eklenir:

```
[2026-07-04 17:42:10] [Connect] 🔴 ByDexter (https://steamcommunity.com/profiles/7656...) sunucudan ayrıldı | 2 saat 15 dakika oynadı
[2026-07-04 17:45:03] [Round] 🏁 12. raunt bitti | Kazanan: CT | ... | Süre: 1 dakika 35 saniye
```

## Notlar

- **İntihar ve kendine hasar** yalnızca Kill kanalında `intihar etti` olarak görünür; Damage kanalına kendine/dünyadan gelen hasar yazılmaz.
- Bot ↔ bot ölümleri doğru şekilde katil/kurban olarak loglanır (bot ayrımı SteamID yerine slot ile yapılır).
- Süresi 0 olan sahte `player_blind` olayları loglanmaz.
- Flash'tan etkilenen oyuncular `player_blind` olayıyla `webhook_grenade` kanalına düşer.
- Raunt bitiş sebebi bilinmeyen bir koda denk gelirse oyunun ham bildirimi (`#SFUI_Notice_...` kırpılmış hâli) gösterilir.
- Dosya logu, Discord gönderimiyle aynı 3 saniyelik döngüde arka planda yazılır; oyun akışını bloklamaz.
