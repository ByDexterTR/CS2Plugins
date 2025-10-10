## Kurulum

Kurmak istediğiniz eklentiyi derleyebilirsiniz veya .Compiled klasöründen derlenmiş halini alıp direkt sunucunuza yükleyebilirsiniz.

> Not: Bazı eklentiler harici kütüphaneler kullanır.

## Eklentiler

### Cekilis

- **Açıklama:** T veya CT takımlarından, ölü/canlı durumuna göre rastgele oyuncu seçer.
- **Komutlar:** `css_cek all | dead | live | T | Tdead | Tlive | CT | CTdead | CTlive`
- **Yetki:** `@css/chat`

### ChatCleaner

- **Açıklama:** Oyuncuların kendi sohbetini temizlemesini, adminlerin tüm sohbeti sıfırlamasını sağlar.
- **Komutlar:** `css_cc`, `css_selfcc`
- **Yetki:** `css_cc` için `@css/chat`; `css_selfcc` yetkisizler kullanabilir

### Cit

- **Açıklama:** Warden kontrolünde haritada görünmez bariyerler (çitler) oluşturur.
- **Komutlar:** `css_cit`
- **Yetki:** `@css/root` veya `@jailbreak/warden`
- **Gereksinim:** [CS2TraceRay](https://github.com/schwarper/CS2TraceRay)

### CTBan

- **Açıklama:** Oyuncuların CT takımına geçmesini engeller veya yasakları kaldırır.
- **Komutlar:** `css_ctban`, `css_ctunban`, `cs_ctaddban`, `css_ctbanlist`
- **Yetki:** `@css/ban`; `css_ctbanlist` yetkisizler kullanabilir
- **Ayar:** `chat_prefix`

### MapBlock

- **Açıklama:** Oyuncu sayısına göre belirli bölgeleri girişe kapatır.
- **Komutlar:** Manuel kullanım; harita dosyalarıyla beraber çalışır.
- **Yetki:** Sunucu Ayarsına bağlı (önerilen `@css/root`).
- **Ayar:** `mapblock_mode` (0: kapalı, 1: CT sayısına göre, 2: toplam oyuncuya göre), `mapblock_count` (tetiklenecek oyuncu sayısı).

### Meslekmenu

- **Açıklama:** Terörist takımının tur başında Doktor, Flash, Bombacı, Rambo veya Zeus gibi roller seçmesini sağlar.
- **Komutlar:** `css_meslek`, `css_meslek doktor`, `css_meslek flash`, `css_meslek bombacı`, `css_meslek rambo`, `css_meslek zeus`
- **Yetki:** Oyuncular tarafından doğrudan kullanılabilir (ekstra yetki gerekmez).
- **Ayar:** `chat_prefix`, `doktor_*`, `flash_*`, `bombaci_*`, `rambo_*`, `zeus_*` anahtarlarıyla rol davranışı ayarlanır.
- **Notlar:** Roller sadece canlı T oyuncuları tarafından turda bir kez seçilebilir.

### PlayerRGB

- **Açıklama:** Oyuncu modelini RGB renk döngüsüyle renklendirir.
- **Komutlar:** `css_rgb`
- **Yetki:** `@css/cheats`

### Silahsil

- **Açıklama:** Haritada yere düşen silahları hızlıca temizler.
- **Komutlar:** `css_silahsil`
- **Yetki:** `@css/slay`

### Sustum

- **Açıklama:** Jailbreak sunucularında hızlı yazma yarışları düzenler; farklı modlarla CT, T veya ölü oyuncular arasında rekabet oluşturur.
- **Komutlar:** `css_ctsustum`, `css_tsustum`, `css_dsustum`, `css_olusustum`, `css_ctsustum0`, `css_dsustum0`, `css_tsustum0`, `css_olusustum0`, `css_sustum0`
- **Yetki:** `@css/root` veya `@jailbreak/warden`
- **Ayar:** `chat_prefix` (sohbet etiketi), `sustum.json` (kelime havuzu)
- **Notlar:** HUD üzerinde geri sayım ve hedef kelime gösterilir. `DSustum` kazananı turuncu parlayan `Deagle` ile ödüllendirir ve ateş sonrası silah otomatik alınır.