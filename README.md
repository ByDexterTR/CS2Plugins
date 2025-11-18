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
> Oyuncu ses kontrolü (bıçak, ayak/yürüme, oyuncu/hasar seslerini açma/kapama)

**Komut:**
- `css_ses` - Ses ayarları menüsünü açar

**Yetki:** Yok

**Özellikler:**
- Oyuncular kendi sesleri için bıçak, ayak/yürüme ve oyuncu/hasar seslerini kapatabilir

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