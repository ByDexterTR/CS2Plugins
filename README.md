# CS2 Plugins

**Diller:** 
- 🇹🇷 [Türkçe](README.md) 
- 🇬🇧 [English](README.en.md)

## 📥 Kurulum

Kurmak istediğiniz eklentiyi derleyebilirsiniz veya `.Compiled` klasöründen derlenmiş halini alıp direkt sunucunuza yükleyebilirsiniz.

> **Not:** Bazı eklentiler harici kütüphaneler kullanır. Eklenti açıklamalarını kontrol edin.

---

## 1v1Slay

**Açıklama:** Oyuncular 1v1 kaldıklarında otomatik geri sayım ve slay sistemi

| Komut | Açıklama | Yetki |
|-------|----------|-------|
| Otomatik | Sistem otomatik çalışır | Yok |

**Ayarlar:**
| Ayar | Açıklama | Varsayılan |
|------|----------|------------|
| `chat_prefix` | Sohbet mesajlarında kullanılacak önek | `[ByDexter]` |
| `min_players` | Sistemin aktif olması için minimum oyuncu sayısı | `3` |
| `countdown_time` | Geri sayım süresi (saniye) | `30` |
| `enable_announcements` | Tüm bildirimleri aktif/pasif yapar | `true` |

---

## Cekilis

**Açıklama:** Rastgele oyuncu seçme aracı

| Komut | Açıklama | Yetki |
|-------|----------|-------|
| `css_cek all` | Tüm oyunculardan seç | `@css/chat` |
| `css_cek dead` | Ölü oyunculardan seç | `@css/chat` |
| `css_cek live` | Canlı oyunculardan seç | `@css/chat` |
| `css_cek T` | Terörist oyunculardan seç | `@css/chat` |
| `css_cek CT` | CT oyuncularından seç | `@css/chat` |

---

## ChatCleaner

**Açıklama:** Sohbet temizleme sistemi

| Komut | Açıklama | Yetki |
|-------|----------|-------|
| `css_cc` | Tüm sohbeti temizle | `@css/chat` |
| `css_selfcc` | Kendi sohbetini temizle | Yok |

---

## Cit

**Açıklama:** Warden için harita bariyerleri

| Komut | Açıklama | Yetki |
|-------|----------|-------|
| `css_cit` | Barrier menüsünü açar | `@css/root` veya `@jailbreak/warden` |

**Gereksinimler:** [CS2TraceRay](https://github.com/schwarper/CS2TraceRay)

---

## CommandMaker

**Açıklama:** JSON tabanlı dinamik komut oluşturma sistemi

| Komut | Açıklama | Yetki |
|-------|----------|-------|
| `css_cm_reload` | Komutları yeniden yükle | `@css/root` |

**Komut Türleri:**
| Tür | Açıklama |
|-----|----------|
| `default` | Basit komutlar, argümansız |
| `target` | Hedef oyuncu gerekli |
| `playertarget` | İsteğe bağlı hedef oyuncu |
| `execute` | Sunucu komutu çalıştırır |

**Validasyon Seçenekleri:**
| Validasyon | Açıklama | Örnek |
|------------|----------|-------|
| `arg1: "number"` | Sayısal argüman | `Arg1NumberMin: 1, Arg1NumberMax: 500` |
| `arg1: "word"` | Kelime argümanı | `Arg1WordLength: 20` |
| `flag` | Yetki kontrolü | `@css/slay;@css/cheats` (multi-flag) |

**Aksiyon Sistemi:**
| Aksiyon | Açıklama | Parametreler |
|--------|----------|-------------|
| `sethealth` | Can ayarla | `[TARGET] [ARG1]` |
| `setarmor` | Zırh ayarla | `[TARGET] [ARG1]` |
| `setmoney` | Para ayarla | `[TARGET] [ARG1]` |
| `setmaxhealth` | Max can ayarla | `[TARGET] [ARG1]` |
| `setfreeze` | Dondur | `[TARGET]` |
| `giveweapon` | Silah ver | `[TARGET] [ARG1]` |
| `stripweapons` | Silahları al | `[TARGET]` |
| `setnoclip` | No-clip modunu aç/kapat | `[TARGET] [ARG1]` |
| `setgodmode` | Tanrı modunu aç/kapat | `[TARGET] [ARG1]` |
| `kill` | Öldür | `[TARGET]` |
| `setname` | İsim değiştir | `[TARGET] [ARG1]` |
| `setmodel` | Model değiştir | `[TARGET] [ARG1]` |
| `changeteam` | Takım değiştir | `[TARGET] [ARG1]` |
| `respawn` | Diriltme | `[TARGET]` |
| `setspeed` | Hız çarpanı | `[TARGET] [ARG1]` |
| `setgravity` | Yerçekimi | `[TARGET] [ARG1]` |
| `teleport` | Teleport | `[TARGET] [ARG1] [ARG2] [ARG3]` (X Y Z) |
| `setplayercolor` | Oyuncu rengi | `[TARGET] [ARG1]` |
| `slapdamage` | Hasar ver | `[TARGET] [ARG1]` |
| `sethelmet` | Kask ayarla | `[TARGET] [ARG1]` |
| `setclip` | Yüksükleme miktarı | `[TARGET] [ARG1]` |
| `setammo` | Mühimmat ayarla | `[TARGET] [ARG1]` |
| `playsound` | Ses çal | `[TARGET] [ARG1]` |
| `execute` | Sunucu komutu | `say [ARG1]` |

**Mesaj Sistemi:**
| Mesaj | Açıklama | Özellikler |
|-------|----------|----------|
| `chat` | Oyuncu sohbetine yazı gönder | Renk kodları: `[GOLD]`, `[DEFAULT]`, vs |
| `center` | Center mesajı (HUD) | `centertime` ile süre ayarlanır |
| `serverchat` | Sunucu geneli sohbetine yazı | Tüm oyuncuların görmesi için |
| `servercenter` | Sunucu geneli center mesajı | Tüm oyuncuları bilgilendir |

**Yer Tutucular (Placeholder'lar):**
| Placeholder | Açıklama | Örnek |
|-------------|----------|-------|
| `[TARGET]` | Hedef oyuncu ismi | `Oyuncu1` |
| `[ARG1]` | 1. argüman | `100` |
| `[ARG2]` | 2. argüman | `200` |
| `[ARG3]` | 3. argüman | `300` |
| `[PLAYER]` | Komutu çalıştıran oyuncu | `Admin1` |
| `[GOLD]` | Altın renk kodu | Mesajlarda renklendirme |
| `[DEFAULT]` | Varsayılan renk | Mesajlarda renklendirme |
| `[RED]` | Kırmızı renk | Uyarı mesajları için |
| `[ORCHID]` | Mor renk | Özel mesajlar için |

**Örnek Komut (HP Ayarı):**
```json
{
  "command": "css_hp;css_health",
  "type": "target",
  "args": 1,
  "arg1": "number",
  "arg1_number_min": 1,
  "arg1_number_max": 500,
  "flag": "@css/slay;@css/cheats",
  "sethealth": "[TARGET] [ARG1]",
  "chat": "[GOLD][TARGET] [DEFAULT]adlı oyuncunun canı [GOLD][ARG1] [DEFAULT]olarak ayarlandı.",
  "center": "<font color='green'>Can: [ARG1]</font>",
  "centertime": 3.0,
  "announce": false
}
```

**Ayarlar:**
| Ayar | Açıklama | Varsayılan |
|------|----------|------------|
| `ConfigPath` | Komut tanımlarının bulunduğu JSON dosyası | `commands.json` |

---

## CTBan

**Açıklama:** CT takımı yasaklama sistemi

| Komut | Açıklama | Yetki |
|-------|----------|-------|
| `css_ctban <oyuncu> <süre>` | CT yasağı ver | `@css/ban` |
| `css_ctunban <oyuncu>` | CT yasağını kaldır | `@css/ban` |
| `css_ctaddban <oyuncu> <süre>` | CT yasağına ek süre ekle | `@css/ban` |
| `css_ctbanlist` | Yasaklı oyuncuları listele | Yok |

**Ayarlar:**
| Ayar | Açıklama |
|------|----------|
| `chat_prefix` | Sohbet mesajlarında kullanılacak önek |

---

## CTKit

**Açıklama:** CT takımı için silah kiti menüsü

| Komut | Açıklama | Yetki |
|-------|----------|-------|
| `css_kit` | CT silah menüsünü açar | Yok |

**Ayarlar:**
| Ayar | Açıklama | Varsayılan |
|------|----------|------------|
| `chat_prefix` | Sohbet mesajlarında kullanılacak önek | `[ByDexter]` |
| `default_primary_weapon` | Varsayılan ana silah | `weapon_ak47` |
| `default_secondary_weapon` | Varsayılan yan silah | `weapon_deagle` |

---

## CTKov

**Açıklama:** CT gardiyanları hariç tüm CT'leri T takımına gönderir

| Komut | Açıklama | Yetki |
|-------|----------|-------|
| `css_ctkov` | CT'leri T'ye atar | `@css/generic` veya `@jailbreak/warden` |

**Ayarlar:**
| Ayar | Açıklama | Varsayılan |
|------|----------|------------|
| `chat_prefix` | Sohbet mesajlarında kullanılacak önek | `[ByDexter]` |

---

## CTPerk

**Açıklama:** CT takımı için perk (özellik) sistemi

| Komut | Açıklama | Yetki |
|-------|----------|-------|
| `css_ctperk` | CT perk seçim menüsünü açar | `@css/generic` veya `@jailbreak/warden` |

**Ayarlar:**
| Ayar | Açıklama | Varsayılan |
|------|----------|------------|
| `chat_prefix` | Sohbet mesajlarında kullanılacak önek | `[ByDexter]` |
| `perk_hparmor_hp` | HP & Armor perk HP miktarı | `200` |
| `perk_hparmor_armor` | HP & Armor perk zırh miktarı | `100` |
| `perk_lifesteal_ratio` | Lifesteal perk oranı | `0.25` |
| `perk_damagereducation_ratio` | Hasar azaltma perk oranı | `0.25` |
| `perk_damageboost_ratio` | Hasar artırma perk oranı | `1.50` |

---

## CTRev

**Açıklama:** CT takımına canlandırma (revive) sistemi

| Komut | Açıklama | Yetki |
|-------|----------|-------|
| `css_ctr` / `css_ctrev` | Revive menüsünü açar | `@css/generic` veya `@jailbreak/warden` |
| `css_hak0` / `css_haksifir` | Canlandırma haklarını sıfırla | `@css/generic` |

**Ayarlar:**
| Ayar | Açıklama | Varsayılan |
|------|----------|------------|
| `chat_prefix` | Sohbet mesajlarında kullanılacak önek | `[ByDexter]` |
| `cooldown` | Canlanma bekleme süresi (sn) | `15` |
| `revive_count` | Raunt başına maksimum canlandırma hakkı | `3` |

---

## CTSpawnKill

**Açıklama:** CT doğumunda geçici ölümsüzlük (spawn kill önleme)

| Komut | Açıklama | Yetki |
|-------|----------|-------|
| Otomatik | Sistem otomatik çalışır | Yok |

**Ayarlar:**
| Ayar | Açıklama | Varsayılan |
|------|----------|------------|
| `chat_prefix` | Sohbet mesajlarında kullanılacak önek | `[ByDexter]` |
| `spawn_protect_seconds` | Spawn koruma süresi (saniye) | `5` |

---

## DiscordLogger

**Açıklama:** Discord webhook entegrasyonu ile sunucu logları

| Komut | Açıklama | Yetki |
|-------|----------|-------|
| Otomatik | Sistem otomatik çalışır | Yok |

**Ayarlar:**
| Ayar | Açıklama |
|------|----------|
| `webhook_map` | Harita değişikliği logları için webhook URL |
| `webhook_connect` | Oyuncu bağlantı logları için webhook URL |
| `webhook_command` | Komut logları için webhook URL |
| `webhook_chat` | Chat logları için webhook URL |
| `webhook_kill` | Öldürme logları için webhook URL |
| `webhook_round` | Round logları için webhook URL |

---

## JBDoors

**Açıklama:** Haritadaki tüm kapıları hızlıca açıp/kapatma

| Komut | Açıklama | Yetki |
|-------|----------|-------|
| `css_kapiac` | Tüm kapıları aç | `@css/generic` veya `@jailbreak/warden` |
| `css_kapikapat` | Tüm kapıları kapat | `@css/generic` veya `@jailbreak/warden` |

---

## JBRace

**Açıklama:** Jailbreak için yarış sistemi

| Komut | Açıklama | Yetki |
|-------|----------|-------|
| `css_race` | Yarış menüsünü açar | `@css/generic` veya `@jailbreak/warden` |

**Ayarlar:**
| Ayar | Açıklama | Varsayılan |
|------|----------|------------|
| `chat_prefix` | Sohbet mesajlarında kullanılacak önek | `[ByDexter]` |

---

## JBTeams

**Açıklama:** Jailbreak için takım sistemi

| Komut | Açıklama | Yetki |
|-------|----------|-------|
| `css_takim <0-5>` | Takım sayısını ayarla | `@css/generic` |

**Ayarlar:**
| Ayar | Açıklama |
|------|----------|
| `chat_prefix` | Sohbet mesajlarında kullanılacak önek |

---

## MapBlock

**Açıklama:** Oyuncu sayısına göre dinamik harita engelleri

| Komut | Açıklama | Yetki |
|-------|----------|-------|
| Otomatik | Sistem otomatik çalışır | Yok |

**Ayarlar:**
| Ayar | Açıklama | Değerler |
|------|----------|----------|
| `mapblock_mode` | Çalışma modu | `0`: Kapalı, `1`: CT sayısı, `2`: Toplam oyuncu |
| `mapblock_count` | Tetiklenecek oyuncu sayısı | Sayısal değer |

---

## Meslekmenu

**Açıklama:** Terörist takımı için meslek seçim sistemi

| Komut | Açıklama | Yetki |
|-------|----------|-------|
| `css_meslek` | Meslek menüsünü aç | Yok |
| `css_meslek doktor` / `css_meslek doctor` | Doktor seç | Yok |
| `css_meslek flash` | Flash seç | Yok |
| `css_meslek bombacı` / `css_meslek bomber` | Bombacı seç | Yok |
| `css_meslek rambo` | Rambo seç | Yok |
| `css_meslek zeus` | Zeus seç | Yok |

---

## PlayerHourCheck

**Açıklama:** Steam oyun saati kontrolü ve kademeli ceza sistemi

| Komut | Açıklama | Yetki |
|-------|----------|-------|
| Otomatik | Sistem otomatik çalışır | Yok |

**Placeholder'lar:**
| Placeholder | Açıklama | Örnek |
|-------------|----------|-------|
| `{PlayerPlaytime}` | Oyuncunun oyun saati | `45` (saat cinsinden) |
| `{RequiredPlaytime}` | Gerekli minimum oyun saati | `100` |
| `{PenaltyCount}` | Ceza sayısı | `2` |
| `{Orchid}` | Mor renk kodu | Chat mesajlarında |
| `{Gold}` | Altın renk kodu | Uyarı mesajlarında |
| `{Red}` | Kırmızı renk kodu | Tehdit mesajlarında |

**Ceza Sistemi:**
- Oyuncu saatini kontrol eder
- Yetersizse kademeli ceza uygular
- Konfigürasyonda belirlenen sayıda uyarı sonrası ceza

**Örnek Config:**
```json
{
  "phc_db": {
    "provider": "sqlite",
    "host": "localhost",
    "name": "cs2_playerhourcheck",
    "port": "3306",
    "user": "root",
    "password": ""
  },
  "phc_chat_prefix": "{Orchid}[ByDexter]",
  "phc_steam_api_key": "SteamWebAPI-AnahtarınızBuraya",
  "phc_required_playtime": 100,
  "phc_warn_times": 3,
  "phc_warn_enabled": 1,
  "phc_warn_timer": 30,
  "phc_warn_reason_private": "{Gold}Oyun detaylarınızı açmazsanız atılacaksınız. {Red}[{0}/{1}]",
  "phc_penalty": {
    "1": {
      "type": "kick",
      "time": 0,
      "reason": "Yetersiz oyun saati ({PlayerPlaytime}/{RequiredPlaytime} saat)"
    },
    "3": {
      "type": "ban",
      "time": 60,
      "reason": "Yetersiz oyun saati ({PlayerPlaytime}/{RequiredPlaytime} saat)"
    },
    "5": {
      "type": "ban",
      "time": 1440,
      "reason": "Yetersiz oyun saati ({PlayerPlaytime}/{RequiredPlaytime} saat)"
    }
  },
  "phc_ignore_flags": ["@bydexter/ignoreplaytime", "@css/root"],
  "phc_ignore_steamids": ["76561198843494248"]
}
```

**Ayarlar:**
| Ayar | Açıklama | Varsayılan |
|------|----------|------------|
| `phc_required_playtime` | Gereken minimum oyun saati | `100` |
| `phc_warn_enabled` | Uyarı sistemi aktif/pasif | `1` |
| `phc_warn_times` | Gizli profil için uyarı sayısı | `3` |
| `phc_warn_timer` | Uyarılar arası bekleme süresi (sn) | `30` |

**Gereksinimler:** MySQL veritabanı, Steam API anahtarı (opsiyonel)

**Veritabanı:** MySQL / SQLite

---

## PlayerRGB

**Açıklama:** Oyuncu modeli RGB renklendirme

| Komut | Açıklama | Yetki |
|-------|----------|-------|
| `css_rgb` | RGB menüsünü açar | `@css/cheats` |

---

## Redbull

**Açıklama:** Oyuncuya geçici hız ve renk efekti uygular

| Komut | Açıklama | Yetki |
|-------|----------|-------|
| `css_redbull` | Redbull efektini etkinleştir | Yok |

**Ayarlar:**
| Ayar | Açıklama | Varsayılan |
|------|----------|------------|
| `chat_prefix` | Sohbet mesajlarında kullanılacak önek | `[ByDexter]` |
| `speed` | Hız çarpanı | `2.0` |
| `duration` | Etki süresi (saniye) | `10` |
| `round_limiter` | Raunt başına kullanım limiti | `2` |
| `cooldown` | Tekrar kullanım bekleme süresi (sn) | `15` |

---

## Sesler

**Açıklama:** Oyuncu ses kontrolü (bıçak, silah, ayak/yürüme, oyuncu/hasar seslerini kontrol etme)

| Komut | Açıklama | Yetki |
|-------|----------|-------|
| `css_ses` / `css_sesler` | Ses ayarları menüsünü açar | Yok |

**Ayarlar:**
| Ayar | Açıklama | Değerler |
|------|----------|----------|
| `Database.provider` | Veritabanı türü | `sqlite` / `mysql` |
| `Database.host` | MySQL sunucu adresi | `localhost` |
| `Database.name` | Veritabanı adı | `bydexter_sesler` |

**Veritabanı:** SQLite / MySQL

---

## Silahsil

**Açıklama:** Yere düşen silahları temizleme

| Komut | Açıklama | Yetki |
|-------|----------|-------|
| `css_silahsil` | Silahları temizle | `@css/slay` |

---

## Sustum

**Açıklama:** Jailbreak için hızlı yazma yarışı sistemi

| Komut | Açıklama | Yetki |
|-------|----------|-------|
| `css_ctsustum` | CT'ler arası yarış | `@css/generic` veya `@jailbreak/warden` |
| `css_tsustum` | T'ler arası yarış | `@css/generic` veya `@jailbreak/warden` |
| `css_dsustum` | Ölüler arası yarış | `@css/generic` veya `@jailbreak/warden` |
| `css_olusustum` | Tüm oyuncular arası yarış | `@css/generic` veya `@jailbreak/warden` |
| `css_sustum0` | Yarışmayı durdur | `@css/generic` veya `@jailbreak/warden` |

**Ayarlar:**
| Ayar | Açıklama |
|------|----------|
| `chat_prefix` | Sohbet etiketi |

---
