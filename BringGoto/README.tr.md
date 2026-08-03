# BringGoto

Yetkililerin oyuncuları nişangâhındaki noktaya ışınlamasını (`!bring`) veya bir oyuncunun yanına ışınlanmasını (`!goto`) sağlar. Işınlama noktası native trace ile anlık hesaplanır.

## Özellikler

- `!bring <hedef>` ile hedef(ler)i tam olarak baktığın noktaya ışınlar; nokta native trace (CNavPhysicsInterface) ile bulunur
- `!goto <hedef>` ile hedef oyuncunun yanına ışınlanır
- Çoklu hedef desteği: `@all`, `@t`, `@ct`, `#userid`, tam veya kısmi isim
- Dokunulmazlık (immunity) kontrolü: hedefin immunity değeri sizinkinden yüksekse ışınlama engellenir (`ignore_immunity` ile kapatılabilir)
- Komut adları ve yetki flag'leri config'ten değiştirilebilir
- Işınlanan oyunculara bilgi mesajı gönderilir; botlar hedeflenebilir
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Kurulum

1. Derlenmiş `BringGoto` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/BringGoto/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load BringGoto` komutunu çalıştırın.
3. İlk yüklemede config dosyası otomatik oluşturulur.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_bring` / `css_gel <hedef/@t/@ct/@all>` | Hedef(ler)i nişangâhındaki noktaya ışınlar | `@css/cheats` |
| `css_goto` / `css_git <hedef>` | Hedef oyuncunun yanına ışınlanır | `@css/generic` |

## Yapılandırma

```
csgo/addons/counterstrikesharp/configs/plugins/BringGoto/BringGoto.json
```

| Ayar | Tip | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `bring_cmd` | string | `"css_bring,css_gel"` | Virgülle ayrılmış bring komutu adları |
| `goto_cmd` | string | `"css_goto,css_git"` | Virgülle ayrılmış goto komutu adları |
| `bring_flag` | string | `"@css/cheats"` | Bring için gereken yetki (boş = herkes) |
| `goto_flag` | string | `"@css/generic"` | Goto için gereken yetki (boş = herkes) |
| `ignore_immunity` | bool | `false` | `true` iken immunity kontrolü yapılmaz |

### Immunity Davranışı

| `ignore_immunity` | Kullanan | Hedef | Sonuç |
| --- | --- | --- | --- |
| `false` | 90 | 100 | Engellenir |
| `false` | 90 | 90 | Işınlanır |
| `false` | 90 | 80 | Işınlanır |
| `true` | 90 | 100 | Işınlanır |

### Örnek Config

```json
{
  "bring_cmd": "css_bring,css_gel",
  "goto_cmd": "css_goto,css_git",
  "bring_flag": "@css/cheats",
  "goto_flag": "@css/generic",
  "ignore_immunity": false
}
```

## Notlar

- Her iki komut için de kullanan oyuncunun hayatta olması gerekir; ölü ve GOTV oyuncular hedeflenemez.
- Bring'de nişangâh noktası bulunamazsa (açık gökyüzü / trace hatası) hedef 128 birim önüne ışınlanır; noktadan 24 birim geri çekilip 6 birim yukarı alınarak duvara gömülme önlenir.
- Çoklu bring'de tüm hedefler aynı noktaya gelir; immunity'si yüksek hedefler sessizce atlanır, hiçbiri uygun değilse mesajla bildirilir.
- Goto'da hedefin 80 birim üzerine ışınlanılır, böylece iki oyuncu iç içe takılmaz.
- Botlarda immunity kontrolü yapılmaz; goto ile kendinizi hedef alamazsınız.
- Komut adı değişiklikleri eklenti yeniden başlatıldığında etkinleşir.
