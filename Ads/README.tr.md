# Ads

*Bu dosyanın [İngilizcesi / English](README.md).*

Haritaya prop yerleştirir, ekrana yazı basar ve sohbete duyuru gönderir. Zamanlı reklamların yanında oyun olaylarına bağlı anlık reklamlar da gösterir; aynı türden birden fazla reklam tanımlandığında bunlar sıraya girer ve üst üste binmez.

## Özellikler

- **Prop reklamı**: `css_ads` menüsüyle baktığınız noktaya model yerleştirir; prop olduğu yerde sabit kalır, düşmez ve itilmez
- **Ayrık dosya düzeni**: reklamlar `ads.json`, prop kataloğu `props.json`, haritaya yerleştirilenler `maps.json`, ayarlar `settings.json`
- **Oyun içi düzenleme**: `css_ads` menüsüyle prop koyma, eksen bazlı döndürme/taşıma, boyut, collision, skin ve flag ayarı
- **Flag sistemi**: her reklam türünde `flag` (yalnızca bu yetkidekiler görür) ve `ignoreflag` (bu yetkidekiler görmez); görmemesi gereken oyuncuya prop hiç gönderilmez
- **ScreenText**: oyuncunun ekranına sabitlenen dünya yazısı; konum (x/y), boyut, renk, hizalama ve arka plan ayarlanabilir
- **HudSay**: ekran ortasına HTML destekli yazı (`<br>`, `<font color>`, `class='fontSize-m'`)
- **ChatSay**: sohbete renk kodlu (`{Lime}`, `{Orchid}` …) duyuru
- **Event reklamları**: 10 oyun olayında hedefe (kurban / saldırgan / takım / herkes) anlık ChatSay, HudSay veya ScreenText; oyuncu bazlı cooldown, yüzdelik şans ve `{victim}` `{damage}` `{winner}` gibi placeholder'lar
- **Modüler**: yalnızca kullandığınız bölümler devreye girer
- **Çakışma önleyici sıra sistemi**: her kanal (ScreenText / HudSay / ChatSay) aynı anda yalnızca tek reklam gösterir; `ads_queue_mode: "global"` ile üç kanal tek sıraya indirilir; event reklamları süresi boyunca sadece o oyuncuda dönen reklamın önüne geçer
- Harita bazlı filtre (`map`), tüm haritalar için `*`
- JSON ↔ MySQL çift yönlü aktarım (`css_adsimportsql` / `css_adsexportsql`)
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371
- `gamedata` dosyası: `addons/counterstrikesharp/gamedata/NativeTrace.gamedata.json`

## Kurulum

1. Derlenmiş `Ads` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/Ads/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load Ads` komutunu çalıştırın.
3. İlk yüklemede `settings.json`, örnek `ads.json`, `props.json` ve boş `maps.json` eklenti klasöründe otomatik oluşturulur.

Dört dosyanın tamamı `csgo/addons/counterstrikesharp/plugins/Ads/` içindedir; eklentinin `configs/plugins/` altında dosyası yoktur.

| Dosya | İçerik | Kim yazar |
| --- | --- | --- |
| `settings.json` | Ayarlar (komutlar, sıralama modu, MySQL bilgileri) | Elle; sunucuya özeldir, MySQL'e aktarılmaz |
| `ads.json` | ScreenText / HudSay / ChatSay / event reklamları | Elle |
| `props.json` | Prop kataloğu (menüde çıkan liste) | Elle |
| `maps.json` | Haritaya yerleştirilmiş proplar | Menü |

## Sıralama Sistemi (Çakışma Önleme)

Birden fazla reklam tanımlandığında hepsi bir **sıraya** girer, böylece üst üste binmezler. Sıra şöyle döner: `timer` süresi kadar beklenir, reklam `life` süresi kadar gösterilir, sonra sıradaki reklama geçilir.

Kurallar:

- Bir sırada **aynı anda yalnızca bir reklam** aktiftir. Bir ScreenText ekrandayken ikinci ScreenText asla açılmaz.
- `timer`, bir önceki reklam kapandıktan sonra o reklamın açılması için beklenecek süredir (aradaki boşluk).
- `life`, reklamın ekranda kalma süresidir. ChatSay'de `life` yoktur; mesaj basılır ve sıra hemen bir sonrakine geçer.
- Sıra sonuna gelindiğinde başa döner. Bir turun toplam uzunluğu `toplam(life + timer)` kadardır.

`ads_queue_mode` bu sıraların kaç tane olacağını belirler:

| Mod | Davranış |
| --- | --- |
| `channel` (varsayılan) | Üç ayrı sıra: ScreenText, HudSay, ChatSay. Her tür kendi içinde sıralıdır; farklı türler aynı anda görünebilir (farklı ekran bölgeleri kullandıkları için çakışmazlar) |
| `global` | Tek sıra. ScreenText + HudSay + ChatSay tek listede döner, **aynı anda hiçbir zaman iki reklam görünmez** |

Örnek: iki ScreenText (`life 8 / timer 30` ve `life 6 / timer 20`) `channel` modunda şöyle döner:

```
30sn bekle -> 1. yazı 8sn -> 20sn bekle -> 2. yazı 6sn -> 30sn bekle -> 1. yazı ...
```

## Event Reklamları

Dönen reklamların yanında, oyun olaylarına bağlı anlık reklamlar `events` bölümünde tanımlanır. Bunlar sıraya girmez; tetiklendiği anda **sadece hedef oyuncularda** çalışır ve `life` süresi boyunca o oyuncunun dönen ScreenText/HudSay reklamının önüne geçer. Süre bitince dönen reklam kaldığı yerden devam eder. Böylece event reklamı ile dönen reklam da çakışmaz.

### Desteklenen olaylar

| `event` | Ne zaman | Kullanılabilir hedefler |
| --- | --- | --- |
| `player_hurt` | Oyuncu hasar aldığında | `victim`, `attacker`, `both`, `all`, `ct`, `t` |
| `player_death` | Oyuncu öldüğünde | `victim`, `attacker`, `both`, `all`, `ct`, `t` |
| `round_start` | Raunt başladığında | `all`, `ct`, `t` |
| `round_end` | Raunt bittiğinde | `all`, `ct`, `t` |
| `bomb_plant` (`bomb_beginplant`) | Bomba kurulmaya başlandığında | `player`, `all`, `ct`, `t` |
| `bomb_planted` | Bomba kurulduğunda | `player`, `all`, `ct`, `t` |
| `bomb_defuse` (`bomb_begindefuse`) | Bomba sökülmeye başlandığında | `player`, `all`, `ct`, `t` |
| `bomb_defused` | Bomba söküldüğünde | `player`, `all`, `ct`, `t` |
| `player_connect_full` | Oyuncu sunucuya tam bağlandığında (2sn gecikmeli) | `player`, `all` |
| `player_team` | Oyuncu takım değiştirdiğinde | `player`, `all`, `ct`, `t` |

### Hedefler

| `target` | Kime gider |
| --- | --- |
| `all` | Sunucudaki herkese |
| `victim` | Hasar alan / ölen oyuncuya |
| `attacker` | Vuran / öldüren oyuncuya |
| `player` | Olayın sahibi oyuncuya (bomba kuran, bağlanan, takım değiştiren) |
| `both` | Hem kurban hem saldırgana |
| `ct` / `t` | O takımdaki herkese |

### Alanlar

| Alan | Varsayılan | Açıklama |
| --- | --- | --- |
| `event` | — | Yukarıdaki olay adı |
| `target` | `"all"` | Hedef |
| `type` | `"chatsay"` | Gösterim türü: `chatsay`, `hudsay`, `screentext` |
| `text` | — | Mesaj; placeholder ve renk etiketi destekler |
| `life` | `4` | HudSay/ScreenText'in ekranda kalma süresi (ChatSay'de kullanılmaz) |
| `cooldown` | `10` | Aynı oyuncuya bu reklamın **yeniden** basılması için beklenecek saniye; `0` = sınırsız. Reklam o oyuncunun ekranındayken tekrar tetiklenirse cooldown beklenmez, metin anında güncellenir (peşpeşe öldürmede isim taze kalır) |
| `chance` | `100` | Tetiklenme yüzdesi (0-100) |
| `flag` / `ignoreflag` | `""` | Bkz. [Flag Sistemi](#flag-sistemi) |
| `x` / `y` / `size` / `color` / `justify` / `background` | — | Sadece `type: "screentext"` için |

`player_hurt` saniyede onlarca kez tetiklenebilir; `cooldown` alanını mutlaka kullanın.

### Olay placeholder'ları

| Placeholder | Nerede |
| --- | --- |
| `{victim}` | `player_hurt`, `player_death` — hasar alan/ölen oyuncunun adı |
| `{attacker}` | `player_hurt`, `player_death` — vuran oyuncunun adı |
| `{player}` | Olayın sahibi oyuncunun adı (kurban yoksa saldırgan) |
| `{damage}` `{health}` `{armor}` | `player_hurt` |
| `{weapon}` | `player_hurt`, `player_death` |
| `{headshot}` | `player_death` (`1` / `0`) |
| `{winner}` | `round_end` (`T` / `CT` / `Draw`) |
| `{site}` | `bomb_planted`, `bomb_defused` (`A` / `B`); `bomb_plant` sırasında bomba henüz kurulmadığı için boştur |
| `{kit}` | `bomb_defuse` (`1` / `0`) |
| `{team}` | `player_team` (`T` / `CT` / `Spectator`) |
| `{map}` | Her olayda |

## Placeholder

Aşağıdakiler her yerde çalışır: ScreenText, HudSay, ChatSay ve olay reklamları.

| Placeholder | Değer |
| --- | --- |
| `{map}` `{hostname}` `{ip}` `{port}` `{maxplayers}` | Sunucu bilgisi |
| `{players}` `{bots}` | İnsan / bot sayısı |
| `{alive}` `{dead}` | Hayatta / ölü |
| `{t_count}` `{ct_count}` `{spec_count}` | Takım mevcutları |
| `{alive_t}` `{alive_ct}` `{dead_t}` `{dead_ct}` | Takım bazlı hayatta / ölü |
| `{round}` `{t_score}` `{ct_score}` | Raunt ve skor |
| `{time}` `{date}` | `HH:mm` ve `dd.MM.yyyy` |
| `{player}` `{steamid}` `{team}` | Reklamı gören oyuncu |
| `{kills}` `{deaths}` `{assists}` `{score}` | Gören oyuncunun istatistikleri |

`{players}` yalnızca insanları sayar, diğer sayılar botları da içerir.

Değerler kanala göre farklı zamanda hesaplanır:

| Kanal | Ne zaman |
| --- | --- |
| ChatSay | Reklam basılırken — anlık değer |
| HudSay | `ads_hud_tick`'te bir — canlı güncellenir |
| ScreenText | Reklam ekrana gelirken bir kez — o reklam boyunca sabit kalır |

Bu yüzden `{time}` ve sayaçlar ScreenText'te değişmez; canlı değer için HudSay kullanın.

Tanımsız `{...}` ifadeleri olduğu gibi bırakılır, renk etiketleri (`{Orchid}` vb.) bozulmaz. Metinde `{` yoksa hiçbir işlem yapılmaz.

## Komutlar

Dört komut var; her şey `css_ads` menüsünden yapılır.

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_ads` | Reklam menüsünü açar | `ads_flag` |
| `css_adsreload` | Reklamları ve propları aktif kaynaktan (JSON veya MySQL) yeniden yükler | `ads_flag` |
| `css_adsimportsql` | `ads.json` + `props.json` + `maps.json` içeriğini MySQL'e aktarır (tabloları **tamamen değiştirir**) | `ads_flag` |
| `css_adsexportsql` | MySQL içeriğini bu üç dosyaya aktarır (eskilerini `.backup` olarak saklar) | `ads_flag` |

### Menüler

Menülerde gezinme **W/S**, seçme **E**, çıkış **R**. Her seçimden sonra menü açık kalır; arka arkaya işlem yapabilirsiniz. Alt menülerde **Geri** her zaman ilk satırdır ve kırmızı gösterilir.

```
css_ads
├─ Prop Yerleştir ......... prop kataloğu
├─ Prop Düzenle
│   ├─ Prop belirle
│   ├─ Prop Konumlandırma
│   │   ├─ Yeniden konumlandır
│   │   ├─ Konum: X / Y / Z   (eksen seçici)
│   │   ├─ Döndür +/-
│   │   └─ Taşı +/-
│   ├─ Prop Özellikleri
│   │   ├─ Boyutu büyült / küçült
│   │   ├─ Collision aç / kapat
│   │   ├─ Skin değiştir
│   │   ├─ Flag değiştir
│   │   └─ Ignoreflag değiştir
│   └─ Propu sil
├─ SQL İşlemleri
│   ├─ Json dosyalarını içe aktar
│   └─ Json dosyalarını dışa aktar
└─ Eklenti Yönetimi
    ├─ Propları yenile
    ├─ Reklamları yenile
    └─ Ayarları yenile
```

**Prop Yerleştir**: katalog satır satır listelenir. Bir modeli seçtiğinde baktığın noktaya yerleştirilir ve `maps.json` içine kaydedilir. Katalogdaki `scale`/`skin`/`solid`/`flag`/`ignoreflag` değerleri yerleştirilen kayda kopyalanır.

Prop her zaman `"0 0 0"` açısıyla doğar; bakış yönü açıya karışmaz. Yönlendirme Prop Konumlandırma menüsünden yapılır.

**Prop Düzenle**: **Prop belirle** baktığın noktaya en yakın (128 birim) propu seçer; seçilenin adı satırda köşeli parantez içinde görünür. **Propu sil** seçili propu siler.

#### Prop Konumlandırma

Döndürme ve taşıma **seçili eksen** üzerinde çalışır. Eksen her oyuncu için ayrı tutulur; **Konum** satırı seçildikçe X → Y → Z → X sırasıyla döner ve satırda güncel eksen yazar.

| Satır | Ne yapar |
| --- | --- |
| Yeniden konumlandır | Seçileni baktığın noktaya taşır |
| Konum: X / Y / Z | Aşağıdaki iki işlemin çalışacağı ekseni değiştirir |
| Döndür +/- | Seçili eksende ± `ads_rotate_step` derece çevirir |
| Taşı +/- | Seçili eksende ± `ads_move_step` birim kaydırır |

Eksenlerin anlamı:

| Eksen | Döndür | Taşı |
| --- | --- | --- |
| X | Pitch (öne/arkaya yatırma) | Dünya X ekseni |
| Y | Yaw (sağa/sola çevirme) | Dünya Y ekseni |
| Z | Roll (yana yatırma) | Yükseklik |

Döndürme daima adımın tam katına oturur: açı `45.32` iken +90° yapmak `135.32` değil `90` verir. Elle girilmiş küsüratlı açılar ilk döndürmede temizlenir.

Taşıma dünya eksenlerinde olduğu için nereye bakarsan bak aynı satır aynı yöne kaydırır. Kaba yerleştirme için Yeniden konumlandır, ince ayar için Taşı.

#### Prop Özellikleri

| Satır | Ne yapar |
| --- | --- |
| Boyutu büyült / küçült | `scale` ± `ads_scale_step` (en az 0.05) | `width` ve `height` aynı oranda ölçeklenir |
| Collision aç / kapat | `solid` değerini çevirir; satırda güncel durum yazar | — |
| Skin değiştir | Katalogdaki `skins` listesinde sıradaki değere geçer | — |
| Flag değiştir | Menü kapanır, yeni değeri **sohbete** yazarsınız | aynı |
| Ignoreflag değiştir | Menü kapanır, yeni değeri **sohbete** yazarsınız | aynı |

Flag/Ignoreflag satırını seçince menü kapanır ve sohbete yazdığın ilk mesaj değer olarak kaydedilir; o mesaj sohbete düşmez. Temizlemek için `-` yaz. Kaydedildikten sonra menü kendiliğinden geri açılır.

Skin listesi `props.json` → `models` → `skins` alanından gelir (`"skins": [0, 1, 2]`). Liste yoksa satır uyarı verir.

Boyut, collision ve skin değişikliklerinde prop bir an kaybolup geri gelir. Döndürme ve taşıma propu anında hareket ettirir, yeniden oluşturma yoktur. Her adımda `maps.json` kaydedilir. Seçim oyuncu bazlıdır; harita değişiminde sıfırlanır.

#### SQL İşlemleri ve Eklenti Yönetimi

**SQL İşlemleri** menüsündeki iki satır `css_adsimportsql` / `css_adsexportsql` komutlarıyla aynı işi yapar.

**Eklenti Yönetimi** menüsü `css_adsreload`'un parçalarını ayrı ayrı çalıştırır; hangi bölümü düzenlediysen yalnızca onu tazelersin:

| Satır | Ne yenilenir |
| --- | --- |
| Propları yenile | `maps.json` → `props`, dünyadaki proplar yeniden oluşturulur |
| Reklamları yenile | `ads.json` → `screentexts` + `hudsays` + `chatsays` + `events`; sıra sıfırlanır |
| Ayarları yenile | `settings.json`; komut adları hariç her ayar anında geçerli olur |

## Yapılandırma

```
csgo/addons/counterstrikesharp/plugins/Ads/settings.json
```

Ayarlar sunucuya özeldir; `css_adsimportsql` / `css_adsexportsql` bu dosyaya dokunmaz ve MySQL'de karşılığı yoktur. Aynı database'i paylaşan sunucular farklı komut adı, sıralama modu ve yetki kullanabilir.

| Ayar | Tip | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `ads_cmd` | string | `"css_ads"` | Ana menü komutu; virgülle ayrılmış komut adları |
| `ads_rotate_step` | float | `90` | Prop Açı menüsündeki döndürme adımı (derece) |
| `ads_move_step` | float | `5` | Prop Konum menüsündeki kaydırma adımı (birim) |
| `ads_storage` | string | `"json"` | Reklamların okunacağı kaynak: `json` veya `mysql` |
| `ads_queue_mode` | string | `"channel"` | Sıralama modu: `channel` veya `global` |
| `ads_flag` | string | `"@css/root"` | Tüm yönetim komutları için gereken yetki |
| `ads_scale_step` | float | `0.25` | Prop Özellikleri menüsündeki boyut adımı |
| `ads_reload_cmd` | string | `"css_adsreload"` | Virgülle ayrılmış komut adları |
| `ads_importsql_cmd` | string | `"css_adsimportsql"` | Virgülle ayrılmış komut adları |
| `ads_exportsql_cmd` | string | `"css_adsexportsql"` | Virgülle ayrılmış komut adları |
| `ads_hud_tick` | int | `4` | HudSay kaç tick'te bir yenilenir (4 = saniyede 16) |
| `ads_font` | string | `"Arial Bold"` | ScreenText yazı tipi |
| `ads_forward` | float | `7` | ScreenText'in göze uzaklığı (minimum 1) |
| `ads_units_per_px` | float | `0.012` | ScreenText piksel ölçeği |
| `mysql.host` | string | `""` | MySQL sunucu adresi |
| `mysql.port` | uint | `3306` | MySQL portu |
| `mysql.database` | string | `""` | Database adı (yoksa oluşturulur) |
| `mysql.user` | string | `""` | Kullanıcı adı |
| `mysql.password` | string | `""` | Şifre |
| `mysql.table_prefix` | string | `"ads_"` | Tablo öneki; `ads_props`, `ads_events` … şeklinde |

### Örnek settings.json

```json
{
  "ads_cmd": "css_ads",
  "ads_rotate_step": 90,
  "ads_move_step": 5,
  "ads_scale_step": 0.25,
  "ads_storage": "json",
  "ads_queue_mode": "channel",
  "ads_flag": "@css/root",
  "ads_reload_cmd": "css_adsreload",
  "ads_importsql_cmd": "css_adsimportsql",
  "ads_exportsql_cmd": "css_adsexportsql",
  "ads_hud_tick": 4,
  "ads_font": "Arial Bold",
  "ads_forward": 7,
  "ads_units_per_px": 0.012,
  "mysql": {
    "host": "127.0.0.1",
    "port": 3306,
    "database": "cs2",
    "user": "root",
    "password": "",
    "table_prefix": "ads_"
  }
}
```

## Flag Sistemi

Her reklam türünde (`maps.json` → `props`; `ads.json` → `screentexts`, `hudsays`, `chatsays`, `events`) iki alan vardır:

| Alan | Anlamı |
| --- | --- |
| `flag` | Boş değilse reklamı **yalnızca** bu yetkiye sahip oyuncular görür |
| `ignoreflag` | Bu yetkiye sahip oyuncular reklamı **görmez**; `flag`'in önüne geçer |

Örnekler:

```json
{ "flag": "", "ignoreflag": "" }                     // herkes görür
{ "flag": "@css/vip", "ignoreflag": "" }             // sadece VIP görür
{ "flag": "", "ignoreflag": "@css/vip" }             // VIP hariç herkes görür
{ "flag": "@css/vip", "ignoreflag": "@css/root" }    // VIP görür, root görmez
```

Her iki alan da virgülle çoklu yetki alır (`"@css/vip,@css/generic"`); bunlardan **herhangi biri** yeterlidir.

İki alan `@css/root` karşısında **bilerek farklı** davranır:

| Alan | Root davranışı |
| --- | --- |
| `flag` | Root kapsar. `flag: "@css/generic"` olan bir reklamı root yetkili oyuncu da görür — root her yetkiyi taşıdığı sayılır |
| `ignoreflag` | Root kapsamaz. `ignoreflag: "@css/vip"` yalnızca gerçekten `@css/vip` yetkisi tanımlı oyuncuları gizler; root oyuncunun kendi yetki listesinde `@css/vip` yoksa reklamı görür |

Sebep: `flag` bir *erişim* kontrolüdür, root her yere erişir. `ignoreflag` ise bir *muafiyet* listesidir; root'un otomatik olarak her muafiyete girmesi, adminlerin reklamları hiç görememesi anlamına gelirdi. Bu yüzden `ignoreflag` ham yetki listesine bakar, root joker'i uygulanmaz.

Sıra sistemi flag'den etkilenmez: dönen reklam sırası herkes için aynı anda ilerler, sadece görüntülenme oyuncu bazında filtrelenir. Yani `flag: "@css/vip"` olan bir ScreenText sırası geldiğinde VIP olmayanların ekranında hiçbir şey çıkmaz, sıra normal ilerler.

## Katalog Dosyası

```
csgo/addons/counterstrikesharp/plugins/Ads/props.json
```

Prop Yerleştir menüsünde çıkan model listesi. Bu dosya yalnızca elle düzenlenir; eklenti içeriğine hiç yazmaz.

```json
{
  "Tavuk": {
    "path": "models/chicken/chicken.vmdl",
    "skins": [0]
  },
  "Otomat": {
    "path": "models/props/cs_office/vending_machine.vmdl"
  },
  "Tas heykel": {
    "path": "models/generic/stone_statue_01/stone_statue_01.vmdl"
  },
  "Sos sisesi (sadece VIP)": {
    "path": "models/de_mirage/food/magixx_sauce_01a/magixx_sauce_bottle_01a.vmdl",
    "flag": "@css/vip"
  }
}
```

| Bölüm | Alan | Açıklama |
| --- | --- | --- |
| `props` | `path` | Model dosyasının yolu (`.vmdl`) |
| | `map` | Propun çıkacağı harita; `*` tüm haritalar, virgülle çoklu harita |
| | `pos` / `angle` | `"X Y Z"` biçiminde konum ve açı; `angle` `"pitch yaw roll"` sırasındadır |
| | `scale` / `skin` | Model ölçeği ve deri (skin) indeksi |
| | `solid` | `false` ise oyuncular içinden geçer |
| hepsi | `flag` / `ignoreflag` | Bkz. [Flag Sistemi](#flag-sistemi) |

## Reklam Dosyası

```
csgo/addons/counterstrikesharp/plugins/Ads/ads.json
```

Ekrana ve sohbete basılan reklamlar ile event reklamları burada tutulur. `ads_storage` `mysql` olsa bile bu dosya oluşturulur; reklamlar buraya yazılır ve `css_adsimportsql` ile database'e aktarılır. Proplar için [props.json](#katalog-dosyası) ve [maps.json](#harita-dosyası) kullanılır.

```json
{
  "screentexts": [
    {
      "text": "bydexter.net\nGitHub: github.com/ByDexterTR",
      "life": 8,
      "timer": 30,
      "x": -6.4,
      "y": 1.3,
      "size": 32,
      "color": "#FFFFFF",
      "justify": "left",
      "background": true
    },
    {
      "text": "Sunucumuza <br> destek olun",
      "life": 6,
      "timer": 20,
      "x": -6.4,
      "y": 1.3,
      "size": 28,
      "color": "#7CFC00",
      "justify": "left",
      "background": false
    }
  ],
  "hudsays": [
    {
      "text": "<font color='#7CFC00' class='fontSize-m'>bydexter.net</font><br>GitHub: github.com/ByDexterTR",
      "life": 6,
      "timer": 45
    }
  ],
  "chatsays": [
    {
      "text": "{Orchid}[Reklam]{Default} Sunucumuza destek olmak icin {Lime}bydexter.net{Default} adresini ziyaret edin.",
      "timer": 60
    }
  ],
  "events": [
    {
      "event": "player_death",
      "target": "attacker",
      "type": "hudsay",
      "text": "<font color='#7CFC00' class='fontSize-m'>{victim} oldurdun</font><br>bydexter.net",
      "life": 3,
      "cooldown": 5,
      "chance": 100,
      "ignoreflag": ""
    },
    {
      "event": "player_hurt",
      "target": "attacker",
      "type": "screentext",
      "text": "-{damage} HP\nbydexter.net",
      "life": 2,
      "cooldown": 1,
      "chance": 100,
      "x": 0,
      "y": -1.2,
      "size": 24,
      "color": "#FF6347",
      "justify": "center",
      "background": false
    },
    {
      "event": "round_end",
      "target": "all",
      "type": "hudsay",
      "text": "<font color='#FFD700' class='fontSize-m'>{winner} kazandi</font><br>bydexter.net",
      "life": 4,
      "cooldown": 0
    },
    {
      "event": "bomb_planted",
      "target": "all",
      "type": "chatsay",
      "text": "{Orchid}[Reklam]{Default} Bomba {Red}{site}{Default} bolgesine kuruldu. {Lime}bydexter.net",
      "cooldown": 0
    },
    {
      "event": "player_connect_full",
      "target": "player",
      "type": "chatsay",
      "text": "{Orchid}[Reklam]{Default} Hos geldin {Lime}{player}{Default}! GitHub: {Blue}github.com/ByDexterTR",
      "cooldown": 0
    }
  ]
}
```

Örneklerdeki modellerin hepsi CS2 ile birlikte gelen **stock** modellerdir; hiçbir atölye paketi gerekmez. `maps.json` örneğindeki konumlar de_mirage'ın gerçek spawn bölgelerinden alınmıştır, yani eklentiyi kurar kurmaz de_mirage'da propları görürsünüz.

### Alanlar

| Bölüm | Alan | Açıklama |
| --- | --- | --- |
| `screentexts` | `text` | Alt satır için `\n` veya `<br>` |
| | `life` | Ekranda kaç saniye kalacağı |
| | `timer` | Önceki reklam bittikten sonra kaç saniye sonra çıkacağı |
| | `x` / `y` | Ekran konumu; `x` negatif = sol, `y` pozitif = yukarı |
| | `size` / `color` / `justify` | Punto, renk (`#RRGGBB` veya `R G B`), hizalama (`left`/`center`/`right`) |
| | `background` | Yazının arkasına koyu panel koyar |
| `hudsays` | `text` | HTML destekler: `<br>`, `<font color='#RRGGBB'>`, `class='fontSize-m'`, `<img src='...'>` |
| | `life` / `timer` | ScreenText ile aynı mantık |
| `chatsays` | `text` | `{Orchid}`, `{Lime}`, `{Default}` gibi renk etiketleri; `\n` ile çok satır |
| | `timer` | Önceki mesajdan sonra kaç saniye sonra basılacağı |
| `events` | — | Bkz. [Event Reklamları](#event-reklamları) |
| hepsi | `flag` / `ignoreflag` | Bkz. [Flag Sistemi](#flag-sistemi) |

## MySQL

`ads_storage` `mysql` yapıldığında her JSON bölümü kendi tablosuna karşılık gelir. Dosya düzeni database'de birebir korunur: `ads.json`, `props.json` ve `maps.json` üç ayrı grup olarak aktarılır ve hiçbir grup diğerinin yazımında etkilenmez. `settings.json`'ın karşılığı yoktur.

| Tablo | Kaynak | İçerik |
| --- | --- | --- |
| `ads_screentexts` | `ads.json` → `screentexts` | ScreenText reklamları |
| `ads_hudsays` | `ads.json` → `hudsays` | HudSay reklamları |
| `ads_chatsays` | `ads.json` → `chatsays` | ChatSay reklamları |
| `ads_events` | `ads.json` → `events` | Event reklamları |
| `ads_propmodels` | `props.json` → `models` | Prop kataloğu |
| `ads_props` | `maps.json` → `props` | Yerleştirilmiş proplar |

Tablo öneki `mysql.table_prefix` ile değişir. Tabloların sütunları JSON dosyalarındaki alanlarla birebir aynıdır, yani database üzerinden de aynı ayarları düzenleyebilirsiniz.

Yazma işlemleri de ayrı: menü yalnızca yerleştirilmiş prop tablosunu (`ads_props`) yeniden yazar; katalog, ekran, sohbet ve event tablolarına hiç dokunmaz.

Tablo ve database ilk yüklemede otomatik oluşturulur. Akış:

1. Ekran/sohbet/event reklamlarını `ads.json`, katalogları `props.json` içine yazın.
2. `css_adsimportsql` ile MySQL'e aktarın (ilgili tablolar temizlenip yeniden doldurulur).
3. `ads_storage` değerini `mysql` yapın ve `css_adsreload` çalıştırın.
4. Database'deki kayıtları dosyalara geri almak için `css_adsexportsql` kullanın; `ads.json`, `props.json` ve `maps.json` aynı düzende yeniden yazılır.

`ads_storage` `mysql` iken menü katalogları ve yerleştirilmiş kayıtlar da database'den okunur; JSON dosyaları yalnızca aktarım kaynağı/hedefi olarak kullanılır. `settings.json` her durumda dosyadan okunur.

## Notlar

- Proplar olduğu yerde sabit durur, düşmez ve itilmez. `solid: false` yaparsan oyuncular içinden geçer.
- `props.json` dosyasına yeni bir model eklediğinde, o model ancak bir sonraki harita değişiminde kullanılabilir hale gelir.
- Varsayılan katalog yalnızca CS2 ile birlikte gelen modelleri kullanır, ek paket gerekmez. Kendi modelini eklersen dosyanın hem sunucuda hem oyuncunun tarafında bulunması gerekir (workshop haritası veya addon paketi); yoksa prop görünmez.
- Menü açıkken o oyuncuya HudSay reklamı basılmaz.
- Prop Açı menüsü sağ/sol ve yukarı/aşağı döndürür. Yana yatırmak istersen `maps.json` içindeki `angle` alanının üçüncü değerini elle yaz.
- `player_death` reklamı `target: "victim"` ve `type: "screentext"` ile birlikte kullanılamaz; ölen oyuncuya ekran yazısı gösterilemez. Onun yerine `chatsay` veya `hudsay` kullanın.
- `ignoreflag` root yetkisini kapsamaz: root yetkili bir oyuncu, kendi yetki listesinde o flag yoksa reklamı görür. `flag` kontrolünde ise root her zaman kapsanır.
- ScreenText yazılarını her oyuncu yalnızca kendi ekranında görür. Ölü oyunculara ekran yazısı gösterilmez.
- HudSay, ekranın orta bölgesini kullanır; aynı bölgeyi kullanan başka bir eklenti varsa (menü, uyarı) ikisi sırayla basılıp titreme yapabilir.
- **Ayarları yenile** ile `settings.json` anında tazelenir; yalnızca komut adları, `ads_storage` ve MySQL bağlantısı için eklentiyi yeniden yüklemek gerekir.
- Flag/Ignoreflag'i sohbetten girerken başka birinin yazdığı mesaj senin ayarını değiştiremez.
