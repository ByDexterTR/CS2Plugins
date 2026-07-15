# GoBhop

Ölü T oyuncularını haritada belirlenen gizli bhop noktasına ışınlar. Oyuncu gerçekte yaşar ama TAB'da ölü gözükür, kimse onu izleyemez ve göremez; hasar almaz ve silahsız doğar. T takımında son kişi kalınca GoBhop'takiler otomatik öldürülür ve GoBhop o raunt için kapanır. CSGO'daki [csgo_GoBhop](https://github.com/ByDexterTR/csgo_GoBhop) eklentisinin CS2 uyarlamasıdır.

## Özellikler

- Ölü T oyuncusu `css_gobhop` menüsünden seçtiği noktaya canlı olarak ışınlanır; tek nokta varsa menüsüz direkt gider
- TAB'da ölü gözükür, izlenemez; GoBhop'takilerle dışarıdakiler birbirini görmez ve duymaz
- Hasar almaz, silahsız doğar; silah alsa anında silinir, yere silah atamaz, yasaklı komutları kullanamaz
- Noktalar harita başına isimli olarak `positions.json` dosyasında tutulur, oyun içinden yönetilir
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Kurulum

1. Derlenmiş `GoBhop` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/GoBhop/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load GoBhop` komutunu çalıştırın.
3. Haritada GoBhop noktasında durup `css_setbhop <isim>` ile konumu kaydedin.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_gobhop` | Nokta menüsünü açar; GoBhop dışındayken girer (tek nokta varsa direkt), içindeyken çıkış + nokta değiştirme sunar | Yok |
| `css_onbhop` | GoBhop'a gitmeyi açar | `@css/ban` |
| `css_offbhop` | GoBhop'a gitmeyi kapatır ve içindeki herkesi çıkarır | `@css/ban` |
| `css_setbhop <isim>` | Bulunduğun konumu ve bakış yönünü verilen isimle kaydeder | `@css/root` |
| `css_delbhop <isim>` | Haritadaki isimli GoBhop noktasını siler | `@css/root` |
| `css_resetbhop` | Haritanın tüm kayıtlı GoBhop noktalarını siler | `@css/root` |

Komut adları ve yetkiler config'ten değiştirilebilir.

## Yapılandırma

```
csgo/addons/counterstrikesharp/configs/plugins/GoBhop/GoBhop.json
```

| Ayar | Tip | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `gobhop_cmd` | string | `"css_gobhop"` | Virgülle ayrılmış giriş komutu adları |
| `onbhop_cmd` | string | `"css_onbhop"` | Virgülle ayrılmış açma komutu adları |
| `offbhop_cmd` | string | `"css_offbhop"` | Virgülle ayrılmış kapatma komutu adları |
| `set_cmd` | string | `"css_setbhop"` | Virgülle ayrılmış nokta kaydetme komutu adları |
| `del_cmd` | string | `"css_delbhop"` | Virgülle ayrılmış isimli nokta silme komutu adları |
| `reset_cmd` | string | `"css_resetbhop"` | Virgülle ayrılmış toplu silme komutu adları |
| `blocked_cmd` | string | `"css_wp"` | GoBhop'tayken kullanılamayacak komutlar (virgülle ayrılır) |
| `admin_flag` | string | `"@css/ban"` | Aç/kapat komutları için gereken yetki |
| `set_flag` | string | `"@css/root"` | Nokta kaydetme için gereken yetki |
| `gobhop_min_alive_t` | int | `2` | Girişe izin verilen minimum canlı T sayısı |

### Örnek Config

```json
{
  "gobhop_cmd": "css_gobhop",
  "onbhop_cmd": "css_onbhop",
  "offbhop_cmd": "css_offbhop",
  "set_cmd": "css_setbhop",
  "del_cmd": "css_delbhop",
  "reset_cmd": "css_resetbhop",
  "blocked_cmd": "css_wp",
  "admin_flag": "@css/ban",
  "set_flag": "@css/root",
  "gobhop_min_alive_t": 2
}
```

GoBhop noktaları eklenti klasöründeki `positions.json` dosyasında tutulur ve `css_setbhop`/`css_delbhop`/`css_resetbhop` ile otomatik yazılır:

```
csgo/addons/counterstrikesharp/plugins/GoBhop/positions.json
```

```json
{
  "de_dust2": {
    "KZ": {
      "pos": [-500.0, 200.0, 64.0],
      "ang": [0.0, 90.0, 0.0]
    },
    "Main": {
      "pos": [128.0, 1024.0, 8.0],
      "ang": [0.0, 180.0, 0.0]
    }
  }
}
```

## Notlar

- Skorbordda ölü gösterme `m_bPawnIsAlive` alanıyla yapılır; oyun değeri geri çevirirse her tick yeniden uygulanır.
- Raunt sonu, eklenti kapanışı ve `css_offbhop` GoBhop'taki herkesi güvenle çıkarır; bu ölümler kill feed'de gösterilmez.
- Kapalıyken (`css_offbhop` veya son T) başka bir eklenti GoBhop'taki oyuncuyu canlandırırsa spawn anında yakalanıp çıkarılır.
- Komut adı değişiklikleri sunucu/eklenti yeniden başlatıldığında etkinleşir.
