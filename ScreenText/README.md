# ScreenText

Oyuncunun ekranında sabit duran, config'ten tamamen özelleştirilebilir yazılar gösterir (`point_worldtext`). Yazılar oyuncunun pawn'ına bağlanır (hareket client prediction ile taşınır, koşarken sürüklenmez) ve her tick bakış açısına hizalanır; `CheckTransmit` ile yalnızca sahibine gönderilir, diğer oyuncular, izleyiciler ve GOTV hiçbir şekilde göremez.

## Özellikler

- Sınırsız sayıda yazı; her biri için ekran konumu (X/Y), boyut, renk, hizalama ve arka plan ayrı ayrı ayarlanabilir
- Pawn'a bağlandığı için koşarken/zıplarken yazı ekranda sabit durur; bakış dönüşü her tick hizalanır
- Yazılar yalnızca sahibinin ekranında görünür (`CheckTransmit`); dışarıdan bakan kimse göremez
- `css_hidetext` ile oyuncu bazında aç/kapat; tercih JSON'a kaydedilir, yeniden bağlanınca hatırlanır
- `\n` ile çok satırlı yazı desteği
- Font, ekrana uzaklık ve piksel ölçeği config'ten ayarlanabilir
- Ölüm, takım değişimi, ayrılma ve harita sonunda yazılar güvenle temizlenir
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Kurulum

1. Derlenmiş `ScreenText` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/ScreenText/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load ScreenText` komutunu çalıştırın.
3. İlk yüklemede config dosyası otomatik oluşturulur.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_hidetext` | Ekran yazılarını açar/kapatır (tercih kalıcıdır) | Yok |

## Yapılandırma

```
csgo/addons/counterstrikesharp/configs/plugins/ScreenText/ScreenText.json
```

| Ayar | Tip | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `screentext_cmd` | string | `"css_hidetext"` | Virgülle ayrılmış komut adları |
| `screentext_default_on` | bool | `true` | Yeni bağlanan oyuncular için yazıların başlangıç durumu |
| `screentext_font` | string | `"Arial Bold"` | Yazı tipi |
| `screentext_forward` | float | `7` | Yazıların göze uzaklığı (dünya birimi, minimum 1) |
| `screentext_units_per_px` | float | `0.012` | Piksel başına dünya birimi; büyütünce tüm yazılar büyür |
| `screentext_texts` | array | 2 örnek | Gösterilecek yazı listesi (aşağıya bakın) |

### Yazı Elemanı (`screentext_texts`)

| Alan | Tip | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `text` | string | `""` | Gösterilecek metin; `\n` ile çok satır |
| `x` | float | `0` | Yatay konum: `0` merkez, negatif sol, pozitif sağ (görünür aralık ≈ -7 … +7) |
| `y` | float | `0` | Dikey konum: `0` merkez, pozitif yukarı, negatif aşağı (görünür aralık ≈ -3.9 … +3.9) |
| `size` | float | `32` | Yazı boyutu (px) |
| `color` | string | `"#FFFFFF"` | Renk (`#RRGGBB` veya `R G B`) |
| `justify` | string | `"left"` | Yatay hizalama: `left` / `center` / `right` |
| `background` | bool | `false` | Yazının arkasına yarı saydam panel çizer |

### Örnek Config (solda radar altında GitHub, sağ üstte site adresi)

```json
{
  "screentext_cmd": "css_hidetext",
  "screentext_default_on": true,
  "screentext_font": "Arial Bold",
  "screentext_forward": 7,
  "screentext_units_per_px": 0.012,
  "screentext_texts": [
    {
      "text": "github.com/ByDexterTR/CS2Plugins",
      "x": -6.4,
      "y": 1.3,
      "size": 32,
      "color": "#FFFFFF",
      "justify": "left",
      "background": false
    },
    {
      "text": "bydexter.com",
      "x": 6.4,
      "y": 2.3,
      "size": 32,
      "color": "#7CFC00",
      "justify": "right",
      "background": false
    }
  ]
}
```

## Koordinat Sistemi

Ekran merkezi `(0, 0)` kabul edilir; `x` sağa doğru, `y` yukarı doğru artar. Değerler `screentext_forward` uzaklığındaki düzleme yerleştirilir: varsayılan `7` uzaklıkta 90° FOV ile yatayda yaklaşık `±7`, dikeyde (16:9) yaklaşık `±3.9` birim görünür. Köşeye yaslarken kenar taşmasını `justify` ile çözün: sol kenar için `left`, sağ kenar için `right`.

## Notlar

- Oyunun kendi HUD öğeleri (radar, skor, para, sağlık) her zaman yazının üstünde çizilir.
- `css_hidetext` tercihi `plugins/ScreenText/ScreenText.json` dosyasına SteamID olarak kaydedilir; `screentext_default_on: false` iken açma tercihi oturumluk kalır.
- Yazı listesi değişiklikleri eklenti yeniden yüklenince (`css_plugins reload ScreenText`) etkinleşir.
- Yazı görünürlüğü izleyicilere de kapalıdır: bir oyuncuyu birinci şahıs izleyen kişi o oyuncunun yazılarını görmez.
- Çok hızlı bakış savurmalarında (flick) yazı bir anlığına kayıp toparlanabilir; bu, sunucu tick + istemci interpolasyonundan kaynaklanır ve normaldir.
- Üçüncü şahıs kameradayken (ör. Thirdperson eklentisi) yazı karakterin göz hizasında havada görünür.
