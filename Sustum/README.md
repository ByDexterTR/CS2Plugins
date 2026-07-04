# Sustum

Jailbreak "sustum" etkinliği: ekranda beliren kelimeyi sohbete ilk yazan oyuncu kazanır (veya CTSustum'da yazamayan kaybeder). 4 farklı oyun modu içerir.

## Oyun Modları

| Mod | Komut | Kural |
| --- | --- | --- |
| **CTSustum** | `css_ctsustum` | Warden hariç tüm CT'ler kelimeyi yazmak zorundadır; **son kalan CT kaybeder** ve T takımına gönderilir |
| **TSustum** | `css_tsustum` | Kelimeyi ilk yazan T, **CT takımına geçer** |
| **DSustum** | `css_dsustum` | Kelimeyi ilk yazan canlı T, **Deagle kazanır** (tek atışlık, atınca silinir) ve turuncuya boyanır |
| **ÖlüSustum** | `css_olusustum` | Kelimeyi ilk yazan **ölü T canlanır** |

## Özellikler

- 3 saniyelik geri sayım sonrası ekrana 1-4 rastgele kelimeden oluşan ifade gelir (HUD/CenterHtml)
- Kelime havuzu `sustum.json` dosyasından okunur (repo ~1000+ Türkçe kelimeyle gelir)
- Kelime karşılaştırması büyük/küçük harf duyarsızdır
- Aynı anda yalnızca bir etkinlik çalışır; kazanan/kaybeden HUD'da ve sohbette duyurulur
- Etkinlik iptal komutu
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.369+

## Kurulum

1. Derlenmiş `Sustum` klasörünü sunucuya kopyalayın (**kelime dosyası dahil**):
   ```
   csgo/addons/counterstrikesharp/plugins/Sustum/
   ```
2. Kelime dosyasının adının **`sustum.json`** (küçük harf) olduğundan emin olun — aşağıdaki nota bakın.
3. Sunucuyu yeniden başlatın veya `css_plugins load Sustum` komutunu çalıştırın.

## Komutlar

Tüm komutlar `@css/generic` **veya** `@jailbreak/warden` yetkisi ister:

| Komut | Açıklama |
| --- | --- |
| `css_ctsustum` | CTSustum başlatır |
| `css_tsustum` | TSustum başlatır |
| `css_dsustum` | DSustum başlatır |
| `css_olusustum` | ÖlüSustum başlatır |
| `css_sustum0` (+ `css_ctsustum0`, `css_tsustum0`, `css_dsustum0`, `css_olusustum0`) | Aktif etkinliği iptal eder |

## Yapılandırma

Kelime havuzu eklenti klasöründeki `sustum.json` dosyasıdır — düz bir string dizisi:

```json
[
  "elma",
  "armut",
  "bilgisayar",
  "klavye"
]
```

## Notlar

- CTSustum'da warden (`@jailbreak/warden`) otomatik muaf tutulur.
- Kelime birden fazla sözcükten oluşabilir; sohbete tamamı aynen yazılmalıdır.