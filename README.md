# CS2 Plugins

Counter-Strike 2 için geliştirilmiş sunucu eklentileri koleksiyonu.

## 📥 Kurulum

Kurmak istediğiniz eklentiyi derleyebilirsiniz veya `.Compiled` klasöründen derlenmiş halini alıp direkt sunucunuza yükleyebilirsiniz.

> **Not:** Bazı eklentiler harici kütüphaneler kullanır. Eklenti açıklamalarını kontrol edin.

---

## 🔌 Eklentiler

### 1v1Slay
> Oyuncular 1v1 kaldıklarında otomatik geri sayım ve slay sistemi

**Özellikler:**
- Otomatik 1v1 algılama (botlar dahil)
- HUD ve chat bildirimleri
- Yapılandırılabilir geri sayım süresi
- Minimum oyuncu kontrolü

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

### DiscordLogger
> Discord webhook entegrasyonu ile sunucu logları

**Özellikler:**
- 6 farklı log kategorisi
- Harita değişikliği, bağlantı, komut, chat, kill, round logları
- Ayrı webhook URL'leri ile kategorize edilmiş loglar

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

### MapBlock
> Oyuncu sayısına göre dinamik harita engelleri

**Özellikler:**
- Harita dosyaları ile önceden tanımlı engel noktaları
- CT sayısı veya toplam oyuncu sayısına göre otomatik aktivasyon

**Ayarlar:**
| Ayar | Açıklama | Değerler |
|------|----------|----------|
| `mapblock_mode` | Çalışma modu | `0`: Kapalı, `1`: CT sayısı, `2`: Toplam oyuncu |
| `mapblock_count` | Tetiklenecek oyuncu sayısı | Sayısal değer |

**Yetki:** Sunucu ayarına bağlı (önerilen: `@css/root`)

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

**Yetki:** Yok (tüm oyuncular kullanabilir)

**Ayarlar:**
| Ayar | Açıklama |
|------|----------|
| `chat_prefix` | Sohbet etiketi |
| `doktor_*` | Doktor meslek ayarları |
| `flash_*` | Flash meslek ayarları |
| `bombaci_*` | Bombacı meslek ayarları |
| `rambo_*` | Rambo meslek ayarları |
| `zeus_*` | Zeus meslek ayarları |

> **Not:** Meslekler sadece canlı T oyuncuları tarafından turda bir kez seçilebilir.

---

### PlayerRGB
> Oyuncu modeli RGB renklendirme

**Komut:** `css_rgb`

**Yetki:** `@css/cheats`

**Özellik:** Oyuncu modelini sürekli renk değiştiren RGB döngüsüyle renklendirir.

---

### Silahsil
> Yere düşen silahları temizleme

**Komut:** `css_silahsil`

**Yetki:** `@css/slay`

---

### Sustum
> Jailbreak için hızlı yazma yarışı sistemi

**Komutlar:**
- `css_ctsustum` - CT'ler arası yarış (son kalan CT-ban yer)
- `css_tsustum` - T'ler arası yarış
- `css_dsustum` - Ölüler arası yarış (kazanan turuncu Deagle kazanır)
- `css_olusustum` - Tüm oyuncular arası yarış
- `css_ctsustum0` / `css_tsustum0` / `css_dsustum0` / `css_olusustum0` - Kelime havuzunu yeniden yükle
- `css_sustum0` - Genel yeniden yükleme

**Yetki:** `@css/root` veya `@jailbreak/warden`

**Ayarlar:**
| Ayar | Açıklama |
|------|----------|
| `chat_prefix` | Sohbet etiketi |
| `sustum.json` | Yarış için kullanılacak kelime havuzu |

**Özellikler:**
- HUD üzerinde geri sayım ve hedef kelime
- DSustum kazananı turuncu parlayan Deagle ile ödüllendirilir
- Ateş sonrası silah otomatik alınır