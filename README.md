# CS2 Plugins

Counter-Strike 2 için geliştirilmiş sunucu eklentileri koleksiyonu.

## 📥 Kurulum

Kurmak istediğiniz eklentiyi derleyebilirsiniz veya `.Compiled` klasöründen derlenmiş halini alıp direkt sunucunuza yükleyebilirsiniz.

> **Not:** Bazı eklentiler harici kütüphaneler kullanır. Eklenti açıklamalarını kontrol edin.

---

### 1v1Slay
> Oyuncular 1v1 kaldıklarında otomatik geri sayım ve slay sistemi

**Ayarlar:**
| Ayar | Açıklama | Varsayılan |
|------|----------|------------|
| `chat_prefix` | Sohbet mesajlarında kullanılacak önek | `[ByDexter]` |
| `min_players` | Sistemin aktif olması için minimum oyuncu sayısı | `3` |
| `countdown_time` | Geri sayım süresi (saniye) | `30` |
| `enable_announcements` | Tüm bildirimleri aktif/pasif yapar (HUD + Chat) | `true` |

---

### Cekilis
> Rastgele oyuncu seçme aracı

**Komutlar:**
- `css_cek all` - Tüm oyunculardan seç
- `css_cek dead` - Ölü oyunculardan seç
- `css_cek live` - Canlı oyunculardan seç
- `css_cek T` / `Tdead` / `Tlive` - Terörist takımından seç
- `css_cek CT` / `CTdead` / `CTlive` - CT takımından seç

**Yetki:** `@css/chat`

---

### ChatCleaner
> Sohbet temizleme sistemi

**Komutlar:**
- `css_cc` - Tüm sohbeti temizle (admin)
- `css_selfcc` - Kendi sohbetini temizle

**Yetkiler:**
- `css_cc` için `@css/chat`
- `css_selfcc` için yetki gerekmez

---

### Cit
> Warden için harita bariyerleri

**Komut:** `css_cit`

**Yetki:** `@css/root` veya `@jailbreak/warden`

**Gereksinimler:** [CS2TraceRay](https://github.com/schwarper/CS2TraceRay)

---

### CommandMaker
> JSON tabanlı dinamik komut oluşturma sistemi

**Komutlar:**
- `css_cm_reload` - Komutları yeniden yükle (yetki: `@css/root`)

**Yetki:** Her komut için JSON'da tanımlanabilir (multi-flag destekler)

**Ayarlar:**
| Ayar | Açıklama | Varsayılan |
|------|----------|------------|
| `ConfigPath` | Komut tanımlarının bulunduğu JSON dosyası | `commands.json` |

**Komut Türleri:**
- `default` - Basit komutlar (chat/center mesajı gösterir)
- `target` - Hedef oyuncu gerektiren komutlar (örn: `!hp <oyuncu> <değer>`)
- `playertarget` - Opsiyonel hedef (kendine veya başkasına, örn: `!god [oyuncu]`)
- `execute` - Sunucu komutu çalıştırır

**Özellikler:**

*Validasyon:*
- `arg1`, `arg2`, `arg3` - 3 argümana kadar destek
- `arg*_number_min/max` - Sayısal sınırlar
- `arg*_word_length` - Kelime uzunluğu sınırı
- `arg*`: `"number"`, `"word"`, `"optional"`

*Aksiyon Sistemi (18 aksiyon):*
- `sethealth` - Can ayarla
- `setmaxhealth` - Maksimum can ayarla
- `setarmor` - Zırh ayarla
- `sethelmet` - Miğfer ver
- `setfreeze` - Dondur/çöz
- `setnoclip` - Noclip aç/kapat
- `setspeed` - Hız çarpanı ayarla
- `setgravity` - Yerçekimi ayarla
- `setgodmode` - Ölümsüzlük aç/kapat
- `setmovetype` - Hareket tipi ayarla
- `giveweapon` - Silah ver
- `stripweapons` - Tüm silahları kaldır
- `setclip` - Şarjör mermisi ayarla
- `setammo` - Yedek mermi ayarla
- `teleport` - Işınla (x y z)
- `setplayercolor` - Oyuncu rengi (r g b)
- `setmodel` - Model değiştir
- `playsound` - Ses oynat
- `setname` - İsim değiştir
- `slapdamage` - Tokat ve hasar
- `setmoney` - Para ayarla
- `changeteam` - Takım değiştir (0-3)
- `respawn` - Yeniden canlandır
- `kill` - Öldür

*Mesaj Sistemi:*
- `chat` - Komutu kullanan oyuncuya chat mesajı
- `center` - Komutu kullanan oyuncuya merkez ekran mesajı (HTML destekli)
- `centertime` - Merkez mesajın ekranda kalma süresi (saniye, varsayılan: 5.0)
- `serverchat` - Tüm oyunculara chat mesajı
- `servercenter` - Tüm oyunculara merkez ekran mesajı (HTML destekli)

*Yer Tutucular (17+):*
- `[PLAYER]` / `[PLAYERNAME]` - Komutu kullanan oyuncu
- `[TARGET]` - Hedef oyuncu
- `[PLAYER/TARGET]` - Hedef oyuncu (playertarget için)
- `[ARG1]`, `[ARG2]`, `[ARG3]` - Argümanlar
- `[TARGETHEALTH]` - Hedef oyuncunun canı
- `[TARGETTEAM]` - Hedef oyuncunun takımı
- `[PLAYERCOORDINATE]` - Oyuncu koordinatları (x y z)
- `[TARGETCOORDINATE]` - Hedef koordinatları (x y z)
- `[SERVERIP]`, `[SERVERPORT]` - Sunucu IP ve port
- `[HOSTNAME]` - Sunucu ismi
- `[MAPNAME]` - Harita ismi
- `[PLAYERCOUNT]` - Oyuncu sayısı
- `[ALIVECOUNT]` - Canlı oyuncu sayısı
- `[RANDOMPLAYER]` - Rastgele oyuncu
- `[TIME]` - Saat (HH:mm:ss)
- `[DEFAULT]`, `[RED]`, `[GOLD]`, `[GREEN]`, `[BLUE]`, `[ORCHID]`, vb. - Renk kodları

*Diğer:*
- `announce` - Komut kullanımını sunucuya duyurur (true/false)
- `flag` - Yetki (noktalı virgül ile çoklu yetki: `@css/slay;@css/cheats`)

**Örnek Komut:**
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
  "chat": "[GOLD][TARGET][DEFAULT] oyuncusunun canı [GOLD][ARG1][DEFAULT] yapıldı.",
  "center": "<font color='green'>Can: [ARG1]</font>",
  "centertime": 3.0,
  "announce": false
}
```

---

### CTBan
> CT takımı yasaklama sistemi

**Komutlar:**
- `css_ctban <oyuncu> <süre>` - CT yasağı ver
- `css_ctunban <oyuncu>` - CT yasağını kaldır
- `css_ctaddban <oyuncu> <süre>` - CT yasağına ek süre ekle
- `css_ctbanlist` - Yasaklı oyuncuları listele

**Yetkiler:**
- `@css/ban` (ctban, ctunban, ctaddban için)
- Yetki gerekmez (ctbanlist için)

**Ayarlar:**
| Ayar | Açıklama |
|------|----------|
| `chat_prefix` | Sohbet mesajlarında kullanılacak önek |

---

### CTKit
> CT takımı için silah kiti menüsü (Jailbreak)

**Komut:**
- `css_kit` - CT silah menüsünü açar

**Ayarlar:**
| Ayar | Açıklama | Varsayılan |
|------|----------|------------|
| `chat_prefix` | Sohbet mesajlarında kullanılacak önek | `[ByDexter]` |
| `default_primary_weapon` | Varsayılan ana silah | `weapon_ak47` |
| `default_secondary_weapon` | Varsayılan yan silah | `weapon_deagle` |
| `primary_weapons` | Ana silah seçenekleri | AK47, M4A4, M4A1-S, AWP, MAG7 |
| `secondary_weapons` | Yan silah seçenekleri | DEAGLE, CZ75A, TEC9, ÇİFT BERETTA, USP-S, GLOCK, REVOLVER |

**Özellikler:**
- CT oyuncuları raunt başında seçtiği silahlarla doğar

---

### CTKov
> CT takımındaki gardiyanları (komutçu hariç) tüm CT'leri T takımına gönderir

**Komut:**
- `css_ctkov` - CT gardiyanları hariç tüm CT'leri T'ye atar

**Yetki:** `@css/generic` veya `@jailbreak/warden`

**Ayarlar:**
| Ayar | Açıklama | Varsayılan |
|------|----------|------------|
| `chat_prefix` | Sohbet mesajlarında kullanılacak önek | `[ByDexter]` |

---

### CTPerk
> CT takımı için perk (özellik) sistemi (Jailbreak)

**Komut:**
- `css_ctperk` - CT perk seçim menüsünü açar

**Yetki:** `@css/generic` veya `@jailbreak/warden`

**Ayarlar:**
| Ayar | Açıklama | Varsayılan |
|------|----------|------------|
| `chat_prefix` | Sohbet mesajlarında kullanılacak önek | `[ByDexter]` |
| `perk_hparmor_hp` | HP & Armor perk HP miktarı | `200` |
| `perk_hparmor_armor` | HP & Armor perk zırh miktarı | `100` |
| `perk_lifesteal_ratio` | Lifesteal perk oranı | `0.25` |
| `perk_damagereducation_ratio` | Hasar azaltma perk oranı | `0.25` |
| `perk_damageboost_ratio` | Hasar artırma perk oranı | `1.50` |
| `enabled_perk_*` | Perkleri aktif/pasif yapar | `true` |
| `selection_rights` | T sayısına göre seçim hakkı | Özelleştirilebilir |

**Özellikler:**
- CT'ler raunt başında perk seçebilir (HP/Zırh, Lifesteal, Sınırsız Mermi, Hasar Azaltma, Hasar Artırma)

---

### CTRev
> CT takımına canlandırma (revive) menüsü ve otomatik canlandırma sistemi

**Komutlar:**
- `css_ctr`, `css_ctrev`, `css_ctrevmenu` - CT revive menüsünü açar
- `css_hak0`, `css_haksifir`, `css_haksifirla` - Canlandırma haklarını sıfırlar

**Yetki:** 
- `@css/generic` veya `@jailbreak/warden` (revive menüsü)
- `@css/generic` (hak sıfırlama)

**Ayarlar:**
| Ayar | Açıklama | Varsayılan |
|------|----------|------------|
| `chat_prefix` | Sohbet mesajlarında kullanılacak önek | `[ByDexter]` |
| `cooldown` | Canlanma bekleme süresi (sn) | `15` |
| `revive_count` | Raunt başına maksimum canlandırma hakkı | `3` |

**Özellikler:**
- Otomatik canlandırma modu ve manuel canlandırma seçeneği

---

### CTSpawnKill
> CT doğumunda geçici ölümsüzlük (spawn kill önleme)

**Yetki:** Yok

**Ayarlar:**
| Ayar | Açıklama | Varsayılan |
|------|----------|------------|
| `chat_prefix` | Sohbet mesajlarında kullanılacak önek | `[ByDexter]` |
| `spawn_protect_seconds` | Spawn koruma süresi (saniye) | `5` |

**Özellikler:**
- CT'ler doğduğunda belirtilen süre boyunca hasar almaz ve turuncu renkle işaretlenir

---

### DiscordLogger
> Discord webhook entegrasyonu ile sunucu logları

**Ayarlar:**
| Ayar | Açıklama |
|------|----------|
| `webhook_map` | Harita değişikliği logları için webhook URL |
| `webhook_connect` | Oyuncu bağlantı logları için webhook URL |
| `webhook_command` | Komut logları için webhook URL |
| `webhook_chat` | Chat logları için webhook URL |
| `webhook_kill` | Öldürme logları için webhook URL |
| `webhook_round` | Round logları için webhook URL |

> **Öneri:** Her log kategorisi için ayrı webhook kullanın.

---

### JBDoors
> Haritadaki tüm kapıları hızlıca açıp/kapatma

**Komut:**
- `css_kapiac`
- `css_kapikapat`

**Yetki:** `@css/generic` veya `@jailbreak/warden`

> Not: `func_door`, `func_movelinear`, `func_door_rotating`, `prop_door_rotating` ve `func_breakable` üzerinde çalışır.

---

### JBTeams
> Jailbreak için takım sistemi (T oyuncularını belirtilen sayıda renklere göre takımlara böler ve takım içi dost hasarını engeller)

**Komutlar:**
- `css_takim <0-5>` - `0/1` kapatır, `2-5` arası takım sayısını ayarlar

**Yetki:** `@css/generic`

**Ayarlar:**
| Ayar | Açıklama |
|------|----------|
| `chat_prefix` | Sohbet mesajlarında kullanılacak önek |

---

### JBRace
> Jailbreak için yarış (race) sistemi

**Komut:**
- `css_race` - Yarış menüsünü açar

**Yetki:** `@css/generic` veya `@jailbreak/warden`

**Ayarlar:**
| Ayar | Açıklama | Varsayılan |
|------|----------|------------|
| `chat_prefix` | Sohbet mesajlarında kullanılacak önek | `[ByDexter]` |

**Özellikler:**
- Belirlenen noktalar arasında yarış başlatılır, kazananlar otomatik belirlenir

---

### MapBlock
> Oyuncu sayısına göre dinamik harita engelleri

**Ayarlar:**
| Ayar | Açıklama | Değerler |
|------|----------|----------|
| `mapblock_mode` | Çalışma modu | `0`: Kapalı, `1`: CT sayısı, `2`: Toplam oyuncu |
| `mapblock_count` | Tetiklenecek oyuncu sayısı | Sayısal değer |

---

### Meslekmenu
> Terörist takımı için meslek seçim sistemi

**Komutlar:**
- `css_meslek` - Meslek menüsünü aç
- `css_meslek doktor` - Doktor mesleğini seç
- `css_meslek flash` - Flash mesleğini seç
- `css_meslek bombacı` - Bombacı mesleğini seç
- `css_meslek rambo` - Rambo mesleğini seç
- `css_meslek zeus` - Zeus mesleğini seç

**Yetki:** Yok

---

### PlayerHourCheck
> Steam oyun saati kontrolü ve kademeli ceza sistemi

**Özellikler:**
- Oyuncuların CS2 oyun saatini kontrol eder
- Yetersiz saati olan oyunculara kademeli ceza sistemi uygular
- Gizli profillere uyarı ve ceza sistemi
- MySQL veritabanı desteği
- Steam API, DecAPI ve ByDexter API desteği

**Ayarlar:**
| Ayar | Açıklama | Varsayılan |
|------|----------|------------|
| `phc_db` | MySQL veritabanı bağlantı bilgileri | `localhost:3306` |
| `phc_chat_prefix` | Sohbet mesajlarında kullanılacak önek | `{Orchid}[ByDexter]` |
| `phc_steam_api_key` | Steam API anahtarı (opsiyonel) | `""` |
| `phc_required_playtime` | Gereken minimum oyun saati | `100` |
| `phc_warn_times` | Gizli profil için uyarı sayısı | `3` |
| `phc_warn_enabled` | Uyarı sistemi aktif/pasif | `1` |
| `phc_warn_timer` | Uyarılar arası bekleme süresi (saniye) | `30` |
| `phc_warn_reason_private` | Gizli profil uyarı mesajı | Özelleştirilebilir |
| `phc_kick_reason_private` | Gizli profil kick mesajı | Özelleştirilebilir |
| `phc_penalty` | Kademeli ceza ayarları | Özelleştirilebilir |
| `phc_ignore_flags` | Kontrolden muaf yetkiler | `@bydexter/ignoreplaytime`, `@css/root` |
| `phc_ignore_steamids` | Kontrolden muaf SteamID'ler | Özelleştirilebilir |

**Ceza Sistemi:**

Ceza anahtarları esnek şekilde tanımlanabilir. İhlal sayısına en yakın (küçük veya eşit) ceza uygulanır.

| Config | Açıklama |
|--------|----------|
| `"1"` | 1-2. ihlal için uygulanır |
| `"3"` | 3-4. ihlal için uygulanır |
| `"5"` | 5+ ihlal için uygulanır |

**Örnek Config:**
```json
"phc_penalty": {
  "1": { "type": "kick", "time": 0, "reason": "Yetersiz oyun saati ({PlayerPlaytime}/{RequiredPlaytime} saat)" },
  "3": { "type": "ban", "time": 60, "reason": "Yetersiz oyun saati - 1 saat ban" },
  "5": { "type": "ban", "time": 1440, "reason": "Yetersiz oyun saati - 1 gün ban" }
}
```

**Placeholder'lar:**
- `{RequiredPlaytime}` - Gereken oyun saati
- `{PlayerPlaytime}` - Oyuncunun mevcut saati
- `{Default}` `{White}` `{Red}` `{LightRed}` `{DarkRed}` `{BlueGrey}` `{Blue}` `{DarkBlue}` `{Purple}` `{Orchid}` `{Yellow}` `{Gold}` `{Orange}` `{LightGreen}` `{Green}` `{Lime}` `{Grey}` `{Gray}` `{Grey2}` - Renk kodları

**Gereksinimler:**
- MySQL veritabanı
- MySqlConnector.dll (plugin ile birlikte gelir)

---

### PlayerRGB
> Oyuncu modeli RGB renklendirme

**Komut:** `css_rgb`

**Yetki:** `@css/cheats`

---

### Redbull
> Oyuncuya geçici hız ve renk efekti uygular

**Komut:**
- `css_redbull` - Redbull efektini etkinleştir

**Ayarlar:**
| Ayar | Açıklama | Varsayılan |
|------|----------|------------|
| `chat_prefix` | Sohbet mesajlarında kullanılacak önek | `[ByDexter]` |
| `speed` | Hız çarpanı (`1.0` normal) | `2.0` |
| `duration` | Etki süresi (saniye) | `10` |
| `filter_team` | Kullanım kısıtı (`T`, `CT`, `Both`) | `T` |
| `player_color` | Efekt rengi (RGB) | `[248,123,27]` |
| `round_limiter` | Raunt başına kullanım limiti (`0` sınırsız) | `2` |
| `cooldown` | Tekrar kullanım bekleme süresi (saniye) | `15` |

---

### Sesler
> Oyuncu ses kontrolü (bıçak, silah, ayak/yürüme, oyuncu/hasar seslerini açma/kapama)

**Komut:**
- `css_ses`, `css_sesler` - Ses ayarları menüsünü açar

**Yetki:** Yok

**Ayarlar:**
| Ayar | Açıklama | Değerler |
|------|----------|----------|
| `Database.provider` | Veritabanı türü | `sqlite` |
| `Database.host` | MySQL sunucu adresi | `localhost` |
| `Database.name` | Veritabanı adı | `bydexter_sesler` |
| `Database.port` | MySQL port | `3306` |
| `Database.user` | MySQL kullanıcı adı | `root` |
| `Database.password` | MySQL şifre | `""` |

**Özellikler:**
- Diğer oyuncuların bıçak, silah, ayak/yürüme, oyuncu/hasar ve MVP müziği seslerini kapatabilir

**Gereksinimler:**
- SQLite: `e_sqlite3.dll` (plugin ile birlikte gelir)
- MySQL: `MySqlConnector.dll` (plugin ile birlikte gelir)

**Veritabanı:**
Plugin ilk çalıştırıldığında otomatik olarak `player_preferences` tablosunu oluşturur:
- `steamid` - Oyuncu Steam ID
- `knife` - Bıçak sesi tercihi (0-3)
- `weapon` - Silah sesi tercihi (0-3)
- `foot` - Ayak/Yürüme sesi tercihi (0-3)
- `player` - Oyuncu/Hasar sesi tercihi (0-3)
- `mvp` - MVP müziği tercihi (0 veya 3)

---

### Silahsil
> Yere düşen silahları temizleme

**Komut:** `css_silahsil`

**Yetki:** `@css/slay`

---

### Sustum
> Jailbreak için hızlı yazma yarışı sistemi

**Komutlar:**
- `css_ctsustum` - CT'ler arası yarış
- `css_tsustum` - T'ler arası yarış
- `css_dsustum` - Ölüler arası yarış
- `css_olusustum` - Tüm oyuncular arası yarış
- `css_sustum0` / `css_ctsustum0` / `css_tsustum0` / `css_dsustum0` / `css_olusustum0` - Sustum yarışmasını durdurur

**Yetki:** `@css/generic` veya `@jailbreak/warden`

**Ayarlar:**
| Ayar | Açıklama |
|------|----------|
| `chat_prefix` | Sohbet etiketi |
| `sustum.json` | Yarış için kullanılacak kelime havuzu |

---