# CommandMaker

Kod yazmadan, JSON dosyası üzerinden özel sunucu komutları oluşturmanızı sağlar. Hedefli admin komutları, bilgi komutları, cvar/exec makroları ve oyuncu komutları tek dosyadan tanımlanır.

## Özellikler

- `commands.json` içinde sınırsız özel komut tanımı; ilk çalıştırmada 11 örnek komutla oluşturulur
- 4 komut tipi: `default`, `target`, `playertarget`, `execute`
- 30'a yakın eylem: can/zırh/para/hız/yerçekimi ayarlama, silah verme/alma, ışınlama, dondurma, noclip, godmode, slap, respawn, model/isim değiştirme, ses çalma ve daha fazlası
- Zengin yer tutucu sistemi: oyuncu/hedef bilgileri, sunucu bilgileri, skorlar, rastgele oyuncu seçimi
- Chat renk etiketleri: `[GOLD]`, `[RED]`, `[GREEN]`, `[ORCHID]` vb.
- Hedef seçiciler: isim, `#userid`, `@all`, `@ct`, `@t`, `@alive`, `@dead`, `@me`, `@random`
- Komut başına: yetki bayrakları, takım filtresi, canlı/ölü filtresi, bekleme süresi (cooldown), argüman doğrulama (sayı aralığı / kelime uzunluğu)
- Sunucuyu yeniden başlatmadan komutları yeniden yükleme
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Kurulum

1. Derlenmiş `CommandMaker` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/CommandMaker/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load CommandMaker` komutunu çalıştırın.
3. İlk yüklemede eklenti klasöründe örneklerle dolu `commands.json` oluşturulur; düzenleyip `!cm_reload` çalıştırın.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_cm_reload` | `commands.json` dosyasını yeniden yükler | `@css/root` |
| *(tanımladıklarınız)* | `commands.json` içindeki tüm komutlar otomatik kaydedilir | tanıma göre |

## Yapılandırma

### Ana config

```
csgo/addons/counterstrikesharp/configs/plugins/CommandMaker/CommandMaker.json
```

| Ayar | Tip | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `ConfigPath` | string | `"commands.json"` | Komut tanım dosyasının eklenti klasörüne göre yolu |

### Komut tanım dosyası (`commands.json`)

```json
{
  "Commands": [
    {
      "command": ["css_hp", "css_health"],
      "type": "target",
      "args": 1,
      "arg1": "number",
      "arg1_number_min": 1,
      "arg1_number_max": 500,
      "flag": ["@css/slay", "@css/cheats"],
      "cooldown": 3,
      "sethealth": "[TARGET] [ARG1]",
      "chat": ["[GOLD][TARGET] [DEFAULT]adlı oyuncunun canı [GOLD][ARG1] [DEFAULT]olarak ayarlandı."],
      "center": "<font color='green'>Can: [ARG1]</font>",
      "centertime": 3.0
    }
  ]
}
```

#### Genel alanlar

| Alan | Tip | Açıklama |
| --- | --- | --- |
| `command` | string / dizi | Komut adları (`;` ile de ayrılabilir) |
| `type` | string | `default`, `target`, `playertarget`, `execute` |
| `args` | int | Beklenen ek argüman sayısı (0-3) |
| `arg1..arg3` | string | Argüman tipi: `number` veya `word` |
| `argN_number_min` / `argN_number_max` | int | Sayı argümanı sınırları |
| `argN_word_length` | int | Kelime argümanı maksimum uzunluğu |
| `flag` | string / dizi | Gerekli yetki bayrakları (herhangi biri yeterli) |
| `team_filter` | string | `T` veya `CT` — yalnızca o takım kullanabilir |
| `alive_filter` | string | `alive` veya `dead` |
| `cooldown` | float | Oyuncu başına bekleme süresi (saniye) |
| `announce` | bool | Komut kullanımını tüm sunucuya duyur |

#### Komut tipleri

| Tip | Davranış |
| --- | --- |
| `default` | Hedef almaz; mesaj/`execute`/`setcvar` çalıştırır |
| `target` | 1. argüman zorunlu hedef; eylemler hedef(ler)e uygulanır |
| `playertarget` | Hedef opsiyonel; verilmezse komutu yazan hedeflenir |
| `execute` | Yalnızca `execute`/`setcvar` satırlarını çalıştırır |

#### Eylem alanları (hedefe uygulanır)

`sethealth`, `setmaxhealth`, `setarmor`, `sethelmet`, `setmoney`, `setclip`, `setammo`, `giveweapon`, `stripweapons`, `setfreeze`, `setnoclip`, `setgodmode`, `setmovetype`, `setspeed`, `setgravity`, `kill`, `respawn`, `slapdamage`, `teleport`, `setplayercolor`, `setmodel`, `setname`, `changeteam`, `playsound`

Değer biçimi genellikle `"[TARGET] <değer>"` şeklindedir; örn. `"sethealth": "[TARGET] [ARG1]"`.

#### Mesaj alanları

| Alan | Hedef |
| --- | --- |
| `chat` | Komutu kullanana sohbet mesajı (dizi olabilir) |
| `console` | Komutu kullanana konsol mesajı |
| `center` + `centertime` | Komutu kullanana ekran ortası mesaj |
| `serverchat` | Tüm sunucuya sohbet mesajı |
| `servercenter` | Tüm sunucuya ekran ortası mesaj |
| `execute` | Sunucu konsolunda komut çalıştır |
| `setcvar` | Cvar ayarla (`"mp_warmuptime 60"`) |

#### Yer tutucular

- **Oyuncu:** `[PLAYER]`, `[PLAYERHEALTH]`, `[PLAYERARMOR]`, `[PLAYERMONEY]`, `[PLAYERSTEAMID]`, `[PLAYERTEAM]`, `[PLAYERWEAPON]`, `[PLAYERCOORDINATE]`
- **Hedef:** `[TARGET]`, `[TARGETHEALTH]`, `[TARGETARMOR]`, `[TARGETMONEY]`, `[TARGETSTEAMID]`, `[TARGETTEAM]`, `[TARGETWEAPON]`, `[TARGETCOORDINATE]`
- **Argümanlar:** `[ARG1]`, `[ARG2]`, `[ARG3]`
- **Sunucu:** `[HOSTNAME]`, `[SERVERIP]`, `[SERVERPORT]`, `[MAPNAME]`, `[TIME]`, `[ROUND]`, `[CTSCORE]`, `[TSCORE]`
- **Sayımlar:** `[PLAYERCOUNT]`, `[ALIVECOUNT]`, `[TCOUNT]`, `[CTCOUNT]`, `[SPECCOUNT]`, `[ALIVET]`, `[ALIVECT]`
- **Rastgele:** `[RANDOMPLAYER]`, `[RANDOMT]`, `[RANDOMCT]`, `[RANDOMALIVE]`, `[RANDOMDEAD]`, `[RANDOMTALIVE]`, `[RANDOMTDEAD]`, `[RANDOMCTALIVE]`, `[RANDOMCTDEAD]`
- **Renkler:** `[DEFAULT]`, `[RED]`, `[LIGHTRED]`, `[DARKRED]`, `[BLUEGREY]`, `[BLUE]`, `[DARKBLUE]`, `[PURPLE]`, `[ORCHID]`, `[YELLOW]`, `[GOLD]`, `[LIGHTGREEN]`, `[GREEN]`, `[LIME]`, `[GREY]`, `[GREY2]`

## Kullanım Örnekleri

```
!hp Oyuncu 200        → hedefin canını 200 yapar
!slap @t 10           → tüm T'lere 10 hasarlık slap
!team #42 3           → 42 id'li oyuncuyu CT'ye taşır
!serverinfo           → sunucu bilgilerini gösterir
!can                  → (T, canlı, 30 sn cooldown) kendi canını yeniler
```

## Notlar

- `setspeed` / `setgravity` etkileri kalıcıdır (tick bazlı uygulanır); sıfırlamak için `1.0` değerini ayarlayan ikinci bir komut tanımlayın.
- `setgodmode` alan oyuncular sunucudan çıkana veya kapatılana kadar hasar almaz.
- Grup hedeflerinde (`@all` vb.) mesajlardaki `[TARGET]` grup etiketiyle değiştirilir.

