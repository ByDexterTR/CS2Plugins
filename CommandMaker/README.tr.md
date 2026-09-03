# CommandMaker

*Bu dosyanın [İngilizcesi / English](README.md).*

Kod yazmadan, JSON dosyası üzerinden özel sunucu komutları oluşturmanızı sağlar. Hedefli admin komutları, bilgi komutları, cvar/exec makroları ve oyuncu komutları tek dosyadan tanımlanır.

## Özellikler

- `commands.json` içinde sınırsız özel komut tanımı; ilk çalıştırmada 11 örnek komutla oluşturulur
- 5 komut tipi: `default`, `target`, `playertarget`, `execute`, `menu`
- 30'dan fazla eylem: can/zırh/para/hız/yerçekimi ayarlama, silah verme/alma, ışınlama, dondurma, noclip, godmode, slap, respawn, model/isim değiştirme, ses çalma ve daha fazlası
- Zengin placeholder sistemi: oyuncu/hedef bilgileri, sunucu bilgileri, skorlar, rastgele oyuncu seçimi
- Chat renk etiketleri: `[GOLD]`, `[RED]`, `[GREEN]`, `[ORCHID]` vb.
- Hedef seçiciler: isim, `#userid`, `@all`, `@ct`, `@t`, `@alive`, `@dead`, `@me`, `@random`, `@aim`, `@nearest`, `@spec`, `@bot`, `@human`, `@!me`
- Komut başına: yetki bayrakları, takım filtresi, canlı/ölü filtresi, bekleme süresi (cooldown), argüman doğrulama (sayı aralığı / kelime uzunluğu)
- WASD menüleri: komutlarınızı tek bir menü komutu altında toplayın
- `css_cmdlist` oyuncunun kullanabileceği komutları listeler
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
| `css_cm_reload` / `css_commandmaker_reload` | `commands.json` dosyasını yeniden yükler | `reload_flag` |
| `css_cmdlist` / `css_komutlar` | Oyuncunun kullanabileceği komutları listeler | herkes |
| *(tanımladıklarınız)* | `commands.json` içindeki tüm komutlar otomatik kaydedilir | tanıma göre |

## Yapılandırma

### Ana config

```
csgo/addons/counterstrikesharp/configs/plugins/CommandMaker/CommandMaker.json
```

| Ayar | Tip | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `ConfigPath` | string | `"commands.json"` | Komut tanım dosyasının eklenti klasörüne göre yolu |
| `reload_cmd` | string | `"css_cm_reload,css_commandmaker_reload"` | Yeniden yükleme komutlarının adları, virgülle ayrılır |
| `reload_flag` | string | `"@css/root"` | Yeniden yükleme komutu için gereken yetki bayrağı |
| `list_cmd` | string | `"css_cmdlist,css_komutlar"` | Komut listesi komutlarının adları, virgülle ayrılır |

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
| `type` | string | `default`, `target`, `playertarget`, `execute`, `menu` |
| `args` | int | Beklenen ek argüman sayısı (0-3) |
| `arg1..arg3` | string | Argüman tipi: `number`, `float`, `word`, `list`, `player` |
| `argN_number_min` / `argN_number_max` | int | `number` / `float` argümanı için sınırlar |
| `argN_word_length` | int | `word` argümanı için en fazla uzunluk |
| `argN_list` | string | `list` argümanı için izin verilen değerler (`"t,ct,spec"`) |
| `argN_default` | string | Argüman yazılmazsa kullanılacak değer |
| `flag` | string / dizi | Gerekli yetki bayrakları (herhangi biri yeterli) |
| `target_flag` | string / dizi | Komutu başka bir oyuncu üzerinde kullanmak için gereken yetki bayrağı |
| `ignore_immunity` | bool | Yetkili dokunulmazlık kontrolünü atlar (varsayılan `false`) |
| `team_filter` | string | `T` veya `CT` — yalnızca o takım kullanabilir |
| `alive_filter` | string | `alive` veya `dead` |
| `cooldown` | float | Oyuncu başına bekleme süresi (saniye) |
| `global_cooldown` | float | Sunucu geneli bekleme süresi (saniye) |
| `uses_per_round` | int | Bir oyuncunun tur başına kaç kez kullanabileceği |
| `min_players` | int | Sunucuda olması gereken en az gerçek oyuncu sayısı |
| `warmup_only` / `no_warmup` | bool | Isınma turu filtresi |
| `description` | string | `css_cmdlist` içinde görünen açıklama |
| `announce` | bool | Komut kullanımını tüm sunucuya duyur |

#### Komut tipleri

| Tip | Davranış |
| --- | --- |
| `default` | Hedef almaz; mesaj/`execute`/`setcvar` çalıştırır |
| `target` | 1. argüman zorunlu hedef; eylemler hedef(ler)e uygulanır |
| `playertarget` | Hedef opsiyonel; verilmezse komutu yazan hedeflenir |
| `execute` | Yalnızca `execute`/`setcvar` satırlarını çalıştırır |
| `menu` | `menu` girdilerinden bir WASD menüsü açar |

#### Eylem alanları (hedefe uygulanır)

`sethealth`, `setmaxhealth`, `setarmor`, `sethelmet`, `setmoney`, `setclip`, `setammo`, `giveweapon`, `dropweapon`, `stripweapons`, `setfreeze`, `setnoclip`, `setgodmode`, `setmovetype`, `setspeed`, `setgravity`, `kill`, `respawn`, `slapdamage`, `teleport`, `setangle`, `setplayercolor`, `setmodel`, `setname`, `setclantag`, `changeteam`, `addhealth`, `addarmor`, `addmoney`, `screencolor`, `playsound`, `emitsound`

| Eylem | Değer |
| --- | --- |
| `addhealth` / `addarmor` / `addmoney` | Göreli değişim: `"50"` veya `"-25"` |
| `setangle` | `pitch yaw roll` |
| `setclantag` | Klan etiketi metni |
| `screencolor` | `R G B alpha sönme bekleme` — örn. `"255 0 0 90 0.35 0.05"`. Yalnızca `R G B` zorunlu, kalanı `90 0.35 0.05` olur |
| `emitsound` | `soundevent ses` — örn. `"Player.DamageHelmet 1.0"` |
| `dropweapon` | Eldeki silahı düşürür (değer kullanılmaz) |

Değer biçimi `"[TARGET] <değer>"` şeklindedir; örn. `"sethealth": "[TARGET] [ARG1]"`. `[TARGET]` öneki isteğe bağlıdır — `"sethealth": "[ARG1]"` de aynı şekilde çalışır.

`setspeed` ve `setgravity` birer çarpandır: `1.0` normal, `2.0` iki katı, kabul edilen aralık `0` - `10`. `sethelmet` alanı `true` / `false` alır.

#### Mesaj alanları

| Alan | Hedef |
| --- | --- |
| `chat` | Komutu kullanana sohbet mesajı (dizi olabilir) |
| `targetchat` | Hedefe/hedeflere sohbet mesajı (dizi olabilir) |
| `targetcenter` | Hedefe/hedeflere ekran ortası mesaj |
| `console` | Komutu kullanana konsol mesajı |
| `center` + `centertime` | Komutu kullanana ekran ortası mesaj |
| `serverchat` | Tüm sunucuya sohbet mesajı |
| `servercenter` | Tüm sunucuya ekran ortası mesaj (`centertime` süresi burada da geçerli) |
| `execute` | Sunucu konsolunda komut çalıştır |
| `setcvar` | Cvar ayarla (`"mp_warmuptime 60"`) |

#### Placeholder'lar

- **Oyuncu:** `[PLAYER]`, `[PLAYERHEALTH]`, `[PLAYERARMOR]`, `[PLAYERMONEY]`, `[PLAYERSTEAMID]`, `[PLAYERTEAM]`, `[PLAYERWEAPON]`, `[PLAYERCOORDINATE]`
- **Hedef:** `[TARGET]`, `[TARGETHEALTH]`, `[TARGETARMOR]`, `[TARGETMONEY]`, `[TARGETSTEAMID]`, `[TARGETTEAM]`, `[TARGETWEAPON]`, `[TARGETCOORDINATE]`
- **Argümanlar:** `[ARG1]`, `[ARG2]`, `[ARG3]`
- **Sunucu:** `[HOSTNAME]`, `[SERVERIP]`, `[SERVERPORT]`, `[MAPNAME]`, `[TIME]`, `[ROUND]`, `[CTSCORE]`, `[TSCORE]`
- **Sayımlar:** `[PLAYERCOUNT]`, `[ALIVECOUNT]`, `[TCOUNT]`, `[CTCOUNT]`, `[SPECCOUNT]`, `[ALIVET]`, `[ALIVECT]`
- **Rastgele:** `[RANDOMPLAYER]`, `[RANDOMT]`, `[RANDOMCT]`, `[RANDOMALIVE]`, `[RANDOMDEAD]`, `[RANDOMTALIVE]`, `[RANDOMTDEAD]`, `[RANDOMCTALIVE]`, `[RANDOMCTDEAD]`
- **İstatistik:** `[PLAYERKILLS]`, `[PLAYERDEATHS]`, `[PLAYERASSISTS]`, `[PLAYERSCORE]`, `[PLAYERKDR]` ve `[TARGET...]` karşılıkları
- **Teknik:** `[PLAYERUSERID]`, `[TARGETUSERID]`, `[PLAYERPING]`, `[TARGETPING]`, `[PLAYERCLAN]`, `[TARGETCLAN]`
- **Konum:** `[PLAYERANGLE]`, `[TARGETANGLE]`, `[TARGETDISTANCE]`, `[PLAYERAIMTARGET]`
- **Silah:** `[PLAYERCLIP]`, `[PLAYERAMMO]`, `[TARGETCLIP]`, `[TARGETAMMO]`
- **Ek sunucu bilgisi:** `[MAXPLAYERS]`, `[DATE]`, `[BOTCOUNT]`, `[DEADCOUNT]`, `[DEADT]`, `[DEADCT]`, `[TIMELEFT]`, `[WARMUP]`
- **Rastgele sayı:** `[RANDOM:1-100]` — verilen aralıktan bir sayı seçer
- **Renkler:** `[DEFAULT]`, `[RED]`, `[LIGHTRED]`, `[DARKRED]`, `[BLUEGREY]`, `[BLUE]`, `[DARKBLUE]`, `[PURPLE]`, `[ORCHID]`, `[YELLOW]`, `[GOLD]`, `[LIGHTGREEN]`, `[GREEN]`, `[LIME]`, `[GREY]`, `[GREY2]`

### Menü komutları

```json
{
  "command": "css_adminmenu",
  "type": "menu",
  "flag": "@css/generic",
  "menu_title": "[GOLD]Admin Menüsü",
  "menu": [
    { "text": "Isınma başlat", "command": "css_warmup", "flag": "@css/root" },
    { "text": "Canımı yenile", "command": "css_can" }
  ]
}
```

| Alan | Açıklama |
| --- | --- |
| `menu_title` | Menü başlığı (placeholder ve renkler çalışır) |
| `text` | Satır metni |
| `command` | Satır seçilince çalışacak komut |
| `flag` | Bayrağı olmayan oyuncudan satırı gizler |
| `close` | Seçimden sonra menüyü kapatır (varsayılan `true`) |

Seçilen satır oyuncu adına çalıştırılır, yani o komutun bayrağı, bekleme süresi ve filtreleri geçerli kalır.

## Kullanım Örnekleri

```
!hp Oyuncu 200        → hedefin canını 200 yapar
!slap @t 10           → tüm T'lere 10 hasarlık slap
!team #42 3           → 42 id'li oyuncuyu CT'ye taşır
!serverinfo           → sunucu bilgilerini gösterir
!can                  → (T, canlı, 30 sn cooldown) kendi canını yeniler
```

## Kullanışlı kalıplar

Bazı şeyler ayrı bir alan değil, var olanları birleştirmekten çıkar.

| Amaç | Tanım |
| --- | --- |
| Hedefi durduğun yere getir | `"teleport": "[TARGET] [PLAYERCOORDINATE]"` |
| Zırh + kask ver | `"giveweapon": "[TARGET] item_assaultsuit"` |
| İmha kiti ver | `"giveweapon": "[TARGET] item_cutters"` |
| Sadece zırh ver | `"giveweapon": "[TARGET] item_kevlar"` |
| Birden fazla sohbet satırı | `"chat": ["birinci satır", "ikinci satır"]` — `console` ve `serverchat` de aynı şekilde çalışır |
| Hedefi at | `"execute": "kickid [TARGETUSERID] sebep"` |

`giveweapon` hem `weapon_*` hem `item_*` adlarını kabul eder; öneksiz yazılan ad `weapon_*` sayılır.

## Notlar

- `setspeed` / `setgravity` etkileri siz değiştirene kadar kalır; sıfırlamak için değeri `1.0` yapan ikinci bir komut tanımlayın.
- `screencolor` ekrana renkli bir katman basar; düşük `alpha` ton verir, yüksek `alpha` ekranı kaplar.
- `@aim` baktığınız oyuncuyu, `@nearest` en yakındaki oyuncuyu seçer.
- `playertarget` komutunda oyuncu yalnızca kendini etkileyebilir. Başkaları üzerinde kullanılabilmesi için tanıma `target_flag` ekleyin.
- Yetkililer kendi dokunulmazlık seviyesinin üstündeki oyuncuları hedef alamaz; bunu bir komut için kapatmak isterseniz `"ignore_immunity": true` ekleyin.
- `setgodmode` alan oyuncular sunucudan çıkana veya kapatılana kadar hasar almaz.
- Grup hedeflerinde (`@all` vb.) mesajlardaki `[TARGET]` grup etiketiyle değiştirilir.

