# VIPCore

*Bu dosyanın [İngilizcesi / English](README.md).*

Modüler VIP sistemi. 75'ten fazla yerleşik VIP özelliği (modül), grup tabanlı yetkilendirme, JSON veya MySQL depolama ve üç farklı menü tipiyle eksiksiz bir VIP altyapısı sunar.

## Özellikler

- **75+ hazır modül** — hepsi eklentinin içinde gelir, ayrıca kurulum gerektirmez
- **Grup sistemi** — `vipgroups.json` içinde sınırsız grup; her grup hangi modülleri hangi değerlerle alacağını belirler
- **Depolama** — JSON (varsayılan) veya MySQL; MySQL bağlantısı koparsa otomatik JSON'a düşer
- **3 menü tipi** — `hud` (ekranda), `chat`, `wasd` (W/S/E/R tuşlarıyla gezilen menü)
- Süreli veya kalıcı VIP; süresi dolan oyuncunun tüm özellikleri kapanır, VIP kaydı **ve** oyuncu ayarları depolamadan (JSON/MySQL) otomatik silinir
- Oyuncu bazlı özellik ayarları (aç/kapat veya seçim) kalıcı olarak saklanır
- **Efekt görünürlüğü** (`css_hidefx`) — her oyuncu kendi trail/partikül/glow/ses efektini kimin göreceğini seçer (herkes, takım, rakipler, sadece kendisi, hiç kimse)
- Tüm komut adları config'ten değiştirilebilir
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Kurulum

1. Derlenmiş `VIPCore` klasörünü **bağımlılık DLL'leriyle birlikte** sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/VIPCore/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load VIPCore` komutunu çalıştırın.
3. İlk yüklemede eklenti klasöründe `settings.json` ve örnek gruplarla (`#Lite`, `#Plus`) `vipgroups.json` oluşturulur.
4. Grupları düzenleyin, ardından `css_addvip` ile VIP ekleyin.

## Komutlar

Komut adları `settings.json` → `commands` bölümünden değiştirilebilir; varsayılanlar:

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_vip` / `css_vipmenu` | VIP menüsünü açar, kalan süreyi gösterir | VIP |
| `css_vips` / `css_onlinevip` | Çevrimiçi VIP'leri listeler | — (herkes) |
| `css_viplist` | Tüm VIP kayıtlarını (süreleriyle) listeler | `admin_flag` |
| `css_addvip <steamid64> <grup> <süre>` | VIP ekler (`0`/`perm` = kalıcı; `1h`, `2d`, `1mo`… birleştirilebilir) | `admin_flag` |
| `css_removevip <steamid64>` / `css_delvip` | VIP kaydını siler | `admin_flag` |
| `css_reloadvip` / `css_vipreload` | Config, grup ve VIP verilerini yeniden yükler | `admin_flag` |
| `css_tp` / `css_thirdperson` | Üçüncü şahıs kamerayı açar/kapatır (Thirdperson modülü) | VIP (grupta tanımlıysa) |
| `css_updatevip <steamid64>` / `css_vipupdate` | Oyuncunun VIP kaydını depodan (JSON/MySQL) yeniden okur; web panelden yazılan değişikliği sunucu yeniden başlamadan uygular | `admin_flag` |
| `css_hidevip` / `css_hidefx` | Efekt görünürlük menüsü; oyuncu kendi efektini kimin göreceğini seçer: Herkes → Takım → Rakipler → Kendim → Kapalı. Tercih kalıcı saklanır | — (herkes) |
| *(modül komutları)* | `settings.json` → `module_commands` ile tanımlanır; Toggle modülü anında açar/kapatır, seçmeli/kategorili modülün menüsünü açar. Bind edilebilir (`bind x "css_fall"`) | VIP (grupta tanımlıysa) |

Süre birimleri: `s` saniye, `m` dakika (varsayılan), `h` saat, `d` gün, `w` hafta, `mo` ay, `y` yıl.

## Yapılandırma

### `settings.json` (eklenti klasöründe)

| Ayar | Tip | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `storage` | string | `"json"` | `"json"` veya `"mysql"` |
| `menu_type` | string | `"hud"` | `"hud"`, `"chat"` veya `"wasd"` |
| `admin_flag` | string | `"@css/root"` | Yönetim komutları için gereken yetki |
| `commands` | nesne | — | Komut adları (virgülle çoklu takma ad) |
| `buy_commands` | nesne | — | BuyTeamWeapon komut adları; silah anahtarı → virgülle komutlar (örn. `"ak47": "css_ak47,css_ak"`) |
| `module_commands` | nesne | — | Modüle doğrudan komut bağlar (bind edilebilir). Toggle modüller komutla anında açılıp kapanır, seçmeli/kategorili modüllerin menüsü açılır. İstemediğin satırı sil, yenisini `"ModulAdi": "css_komut,css_takma"` biçiminde ekle; boş bırakılırsa hiç komut eklenmez |
| `hide` | nesne | — | Efekt görünürlüğü varsayılanları — oyuncunun kendi efektini kimin göreceği: `all` herkes, `team` takım, `enemy` rakipler, `self` sadece kendisi, `hidden` hiç kimse, `off` kilitli (menüde çıkmaz). Oyuncunun kendi tercihi varsayılanı ezer |
| `mysql` | nesne | — | MySQL bağlantı ayarları (`host`, `port`, `database`, `user`, `password`, `table_prefix`) |

```json
{
  "storage": "json",
  "menu_type": "hud",
  "admin_flag": "@css/root",
  "commands": {
    "menu": "css_vip,css_vipmenu",
    "list_online": "css_vips,css_onlinevip",
    "list_all": "css_viplist",
    "addvip": "css_addvip,css_vipadd",
    "removevip": "css_removevip,css_delvip",
    "reload": "css_reloadvip,css_vipreload",
    "tp": "css_tp,css_thirdperson",
    "hidevip": "css_hidevip,css_hidefx"
  },
  "module_commands": {
    "GiveWeapon": "css_weapons,css_kit",
    "GlueGrenade": "css_glue,css_gluegrenade",
    "PlayerModel": "css_vipmodel",
    "PlayerParticle": "css_particle",
    "Aura": "css_aura",
    "HitSound": "css_hitsound",
    "SaySound": "css_saysound"
  },
  "hide": {
    "BulletTrail": "all",
    "C4Effect": "team",
    "KillEffect": "all",
    "PlayerTrail": "all",
    "PlayerGlow": "self",
    "GrenadeTrail": "all",
    "SaySound": "all",
    "PlayerParticle": "all"
  },
  "mysql": {
    "host": "",
    "port": 3306,
    "database": "",
    "user": "",
    "password": "",
    "table_prefix": "vip_"
  }
}
```

### `vipgroups.json` (eklenti klasöründe)

Grup adı → modül adı → modül değeri eşlemesidir. Bir grupta **tanımlı olmayan modül o gruba kapalıdır**. İlk çalıştırmada tüm modülleri kapsayan `#Lite` ve `#Plus` örnekleriyle oluşturulur.

```json
{
  "#Lite": {
    "Armor": { "value": 100, "helmet": false },
    "ExtraHP": 110,
    "Bhop": { "autostrafe": false, "max_speed": 350, "jump_boost": 1.05, "jump_velocity": 300 },
    "Tag": { "tag": "{BlueGrey}[LITE]", "name_color": "bluegrey", "chat_color": "default", "tab": "[LITE]" }
  }
}
```

### Depolama dosyaları

| Depolama | Konum |
| --- | --- |
| JSON | Eklenti klasöründe `vips.json` (VIP kayıtları) ve `players.json` (oyuncu ayarları) |
| MySQL | `table_prefix` önekiyle tablolar otomatik oluşturulur; oyuncu girişinde kayıt canlı yenilenir. `vip_users` oyuncu başına tek satır, `vip_settings` ise oyuncunun tüm ayarlarını tek bir JSON kolonunda tutar. Her ayarı ayrı satırda tutan eski kurulum açılışta yeni düzene otomatik taşınır |

## Modüller

Modül adları `vipgroups.json` içinde anahtar olarak kullanılır (büyük/küçük harf duyarlı).

| Modül | Açıklama | Grup değeri örneği |
| --- | --- | --- |
| `AdminFlags` | VIP'e otomatik yetki bayrağı verir | `["@css/reservation", "@css/vip"]` |
| `AdminGroups` | VIP'e admin grubu üyeliği verir (SimpleAdmin vb. `#Grup` adları) | `["#VIP"]` |
| `AntiFlash` | Flashbang'i engeller | `{ "self": true, "enemy": true, "teammates": true, "limit": 0 }` |
| `AntiHS` | Headshot hasarını azaltır | `{ "percent": 0, "only_with_weapon": "", "limit": 0 }` |
| `Armor` | Spawn'da zırh (+kask) | `{ "value": 100, "helmet": true }` |
| `ArmorRegen` | Zırh yenilenmesi | `{ "armor_per_tick": 10, "interval": 1.0, "delay_after_dmg": 2, "max_armor": 100, "give_helmet_when_full": true }` |
| `Aura` | Oyuncunun etrafında sürekli etki alanı (iyileştirme/zehir/yavaşlatma/hız); alan bir halka ile gösterilir, `duration_on`/`duration_off` ile yanıp söner | `{ "heal": { "heal": 2, "tick": 0.5, "radius": 180, "beamcolor": "0 255 0", "duration_on": 1, "duration_off": 0, "ignore_teammates": false, "ignore_self": false, "ignore_enemy": true } }` |
| `AutoHS` | Vuruşlar Headshot sayılır | `{ "multiplier": 4, "only_with_weapon": "", "ignore_teammates": true, "limit": 0 }` |
| `Berserk` | Öldürme başına hasar çarpanı artar; `dpk` kill başına eklenen çarpan, `maxdpk` tavan | `{ "dpk": 0.2, "maxdpk": 5.0 }` |
| `Bhop` | Bunny hop (+opsiyonel autostrafe) | `{ "autostrafe": true, "max_speed": 500, "jump_boost": 1.1, "jump_velocity": 300 }` |
| `BombsiteAnnouncer` | Bomba kurulunca CT'lere HUD görseli (yalnız görsel) + sohbet mesajı | `{ "img_a": "...Site-A.png", "img_b": "...Site-B.png", "duration": 5.0 }` |
| `BulletEffect` | Menüden seçilen etki vurduğun oyuncuya uygulanır: `poison` zehir, `slow` yavaşlatma, `lower` küçültme, `upper` büyütme. Tekrar vurmak süreyi uzatır | `{ "poison": { "damage": 2, "tick": 0.5, "duration": 3, "ignore_teammates": true, "ignore_self": true, "ignore_enemy": false }, "slow": { "percent": 20, "duration": 3 }, "lower": { "size": 0.85, "duration": 5 }, "upper": { "size": 1.25, "duration": 5 }, "only_with_weapon": "" }` |
| `BulletTrail` | Mermi izi efekti | `{ "width": 1.5, "lifetime": 0.6, "colors": [...] }` |
| `BuyTeamWeapon` | Karşı takım silahlarını satın alma (yalnız buyzone içinde ve `mp_buytime` dolmadan); Komut adları `settings.json` → `buy_commands` | `{ "ak47": true, "m4a4": true, ... }` |
| `C4Effect` | Bomba kurarken ve imha ederken partikül efekti; iki ayrı kategori, boş olan menüde gizlenir | `[{ "name": "Duman", "particle": "...", "time": 6, "defuse": false }]` |
| `ColoredModel` | Renkli oyuncu modeli; başka eklenti (ör. jRandomSkills) rengi değiştirirse o el geri çekilir | `["Rainbow rainbow", "Mavi #0000FF"]` |
| `CustomWeaponModel` | Silaha özel görünüm; `model` sayı verilirse el modeli de değişir, dosya yolu verilirse yalnız yerdeki silah değişir | `[{ "name": "M4A4 - AK47", "weapon": "weapon_m4a1", "model": "weapons/models/ak47/weapon_rif_ak47.vmdl" }]` |
| `DamageDealt` | Verilen hasarı artırır; **negatif `percent` = debuff** (`-50` verilen hasarı yarıya düşürür) | `{ "percent": 50, "only_with_weapon": "", "ignore_teammates": true, "ignore_self": true, "limit": 0 }` |
| `DamageResist` | Alınan hasarı azaltır; **negatif `percent` = debuff** (`-50` alınan hasarı %50 artırır) | `{ "percent": 40, "only_with_weapon": "", "ignore_teammates": true, "ignore_self": true, "limit": 0 }` |
| `Dash` | Havadayken zıplama tuşuna basınca bastığın yön tuşuna doğru atılır (yön yoksa ileri); `limit`: raunt başına hak (0 = sınırsız), `unit`: itme hızı, `sound_volume`: zıplama sesinin seviyesi (0 = sessiz) | `{ "limit": 3, "unit": 600, "sound_volume": 1 }` |
| `DecoyEffect` | Decoy'a özellik verir: zehirli, iyileştiren, yavaşlatan veya WallHack. Etki alanı yerde bir halka ile gösterilir, halkanın boyutu `radius` ile büyür | `{ "poison": { "minhp": 10, "damage": 2, "tick": 0.5, "radius": 200, "ignore_teammates": true, "ignore_self": true, "limit": 0 }, "wallhack": { "tick": 0.25, "radius": 200, "color": "#612D53", "see_teammates": false, "limit": 0 } }` |
| `DecoyTeleport` | Decoy'un düştüğü yere ışınlanma | `{ "limit": 3 }` |
| `DefuseKit` | Spawn'da imha kiti (CT) | `true` |
| `DuckEndurance` | Sınırsız çömelme; arka arkaya çömelince yavaşlamaz | `true` |
| `DuckSpeed` | Çömelirken hareket hızı; `percent` normal koşu hızının yüzde kaçıyla gidileceği. Oyunun kendi değeri `34`, `100` = çömelmek yavaşlatmaz | `{ "percent": 100 }` |
| `ExtraHP` | Spawn HP değeri | `150` |
| `ExtraJump` | Çoklu zıplama; `count` bir havalanmadaki ekstra zıplama, toplam hak = `count × limit` (`limit: 0` = sınırsız). `Dash` da açıksa Dash önceliklidir. `sound_volume`: zıplama sesinin seviyesi (0 = sessiz) | `{ "count": 2, "limit": 0, "sound_volume": 1 }` |
| `ExtraKillAwards` | Öldürme şekline göre ekstra para: kafadan, dürbünsüz, havadayken, kör düşman, silaha özel ve mesafeye göre | `{ "headshot": 150, "noscope": 100, "inair": 200, "blind": 50, "distance": { "unit": 2048, "money": 100 }, "weapon_knife": 1000 }` |
| `ExtraMoney` | Spawn'da ekstra para | `{ "amount": 4000 }` |
| `ExtraSpeed` | Hız çarpanı | `{ "multiplier": 1.3, "only_with_weapon": "" }` |
| `FallDamage` | Düşme hasarının `percent` kadarını alır (`0` = hiç, `100` = normal); **negatif = debuff** (`-50` düşme hasarını %50 artırır); `limit` raunt başına kaç kez (0=sınırsız) | `{ "percent": 0, "limit": 0 }` |
| `FastDefuse` | Hızlı bomba imhası; `immune_while_burning: false` ise yanarken / ateşin veya havadaki molotofun yakınında hız avantajı devre dışı | `{ "time": 1, "immune_while_burning": true }` |
| `FastPlant` | Hızlı bomba kurma;  `immune_while_burning: false` ise yanarken / ateşin veya havadaki molotofun yakınında hız avantajı devre dışı | `{ "time": 1, "immune_while_burning": true }` |
| `FastReload` | Şarjör normal boşalır; son mermide yedekten anında dolar — (yedekten 1 şarjör düşer) | `{ "only_with_weapon": "", "limit": 0 }` |
| `FortniteArmor` | Hasarı önce zırh karşılar. `percent` hasarın ne kadarının zırha gideceği; zırh biterse kalan hasar cana işler | `{ "percent": 100, "absorb_fall_damage": false }` |
| `Fov` | FOV seçenekleri | `[50, 60, 70, 80, 90]` |
| `GiveWeapon` | Spawn'da silah seçimi; her kategoriden bir silah. Menüdeki "Daima Ver" açıkken o slottaki silah değiştirilir | `{ "rifle": ["weapon_ak47", "weapon_awp"], "pistol": ["weapon_deagle"] }` |
| `GiveZeus` | Spawn'da taser | `true` |
| `Glaz` | Sis içini görme | `true` |
| `GlueGrenade` | Atılan bombalar ilk temasta yapışır (decoy eklersen DecoyTeleport ile duvar içine ışınlanma riski) | `{ "only_grenades": "flashbang,hegrenade", "limit": 0 }` |
| `Gravity` | Yerçekimi seçenekleri | `[1.0, 0.8, 0.5]` |
| `GrenadeKit` | Spawn'da bomba seti; zaten varsa vermez, 2+ ise atınca yeniden verir (InfiniteAmmo açıkken yeniden vermez) | `{ "flash": 2, "smoke": 1, "he": 3, "molotov": 1, "decoy": 0 }` |
| `GrenadeResist` | Bomba (HE/molotov/inferno) hasarını azaltır; **negatif `percent` = debuff** (`-50` bomba hasarını %50 artırır) | `{ "percent": 50, "only_with_grenade": "he,molotov,inferno", "ignore_teammates": true, "ignore_self": true, "limit": 0 }` |
| `GrenadeTrail` | Bomba izi efekti | `{ "width": 1.5, "lifetime": 2.5, "colors": [...] }` |
| `HealthRegen` | Can yenilenmesi | `{ "hp_per_tick": 10, "interval": 1.0, "delay_after_dmg": 2 }` |
| `Healthshot` | Spawn'da healthshot | `2` |
| `HitSound` | Düşmana vurunca ses çalar; izleyenler de duyar. 2 kategori: `hs: true` girdiler kafa vuruşunda, diğerleri normal vuruşta. HS seçili değilse normal ses çalar. `path` dosya yolu veya `emit` soundevent adı | `[{ "name": "Killcard", "path": "sounds/ui/killcard_1.vsnd" }, { "name": "Ping", "emit": "UI.PlayerPing", "volume": 1, "hs": true }]` |
| `InfiniteAmmo` | Sınırsız mermi | `{ "only_weapon": "" }` |
| `Invisibility` | Görünmezlik (düşmanlara transmit edilmez) | `{ "only_stopped": true, "dmg_after_invis": 2.0, "only_with_weapon": "" }` |
| `Jammer` | Yaklaşan oyuncuların radarını kapatır (`radius` menzil); ölü izleyici jam'li birini izliyorsa onun radarı da kapanır | `{ "radius": 500, "ignore_teammates": true, "ignore_enemy": false }` |
| `JoinMessage` | Giriş/çıkış duyurusu | `{ "join_message": "...", "leave_message": "..." }` |
| `KillEffect` | Öldürünce partikül efekti; normal, kafadan ve son öldürme için ayrı kategoriler. Seçim yoksa bir üst kategoriye düşer | `[{ "name": "Simsek", "particle": "...", "time": 3, "hs": false, "lastkill": false }]` |
| `KillHeal` | Öldürme şekline göre can yeniler: `distance` içinde `hp` (veya `money`) anahtarı | `{ "headshot": 15, "noscope": 10, "inair": 20, "blind": 5, "distance": { "unit": 2048, "hp": 10 }, "weapon_knife": 50 }` |
| `KillScreen` | Öldürme ekran efekti (FFA kapalıysa takım arkadaşında çalışmaz) | `{ "duration": 1.0 }` |
| `MagneticDecoy` | Decoy yere düşüp öttüğü sürece `radius` içindekileri kendine çeker; çekim mesafeyle azalır (`strength` taban güç); `limit` raunt başına kaç decoy | `{ "radius": 180, "strength": 30, "ignore_teammates": true, "ignore_enemy": false, "ignore_self": true, "limit": 0 }` |
| `Mole` | Hasar verilen oyuncu `time` saniye `unit` birim yere gömülür ve hareket edemez; `limit` raunt başına kaç gömme (0=sınırsız) | `{ "time": 2.5, "unit": 30, "only_with_weapon": "weapon_deagle", "ignore_teammates": true, "ignore_enemy": false, "ignore_self": true, "limit": 0 }` |
| `OneShot` | Belirli silahlarla tek atış | `{ "weapons": "weapon_awp,weapon_ssg08", "limit": 0 }` |
| `PistolRoundDisable` | Listelenen modüller pistol rauntlarda devre dışı kalır (modül değil, grup ayarı) | `["GiveWeapon", "WeaponAmmo"]` |
| `Force` | Listelenen **Toggle** modüller daima aktif olur; menüde gösterilmez, oyuncu açıp/kapatamaz (modül değil, grup ayarı; modül grupta tanımlı olmalı; seçmeli/komut-tabanlı modüller etkilenmez) | `["Dash", "ExtraHP"]` |
| `PlayerGlow` | Oyuncu glow (duvar arkası parlama) | `{ "range": 300, "team": -1, "colors": [...] }` |
| `Postprocessing` | Ekrana renk/ton efekti uygular; yalnız o oyuncunun ve onu izleyenlerin ekranında görünür. `fade` geçiş süresi (sn) | `[{ "name": "Kanli", "file": "lighting/postprocessing/effects/death_cam_phase1.vpost", "fade": 0.25 }]` |
| `PlayerParticle` | Oyuncuya yapışan ve onu takip eden partikül; ölünce ve raunt başında silinir (`css_hidefx` ile gizlenebilir). `offset` yerden yüksekliği. Sürekli yayan (loop) partikül seçin, tek seferlik patlama efektleri takip etmez | `[{ "name": "Duman", "particle": "particles/ambient_fx/ambient_smokestack.vpcf", "offset": 10 }]` |
| `PlayerModel` | Takıma göre oyuncu modeli seçimi (CT ve T ayrı menü); `leg: false` birinci şahıs bacakları gizler. Yalnız spawn'da uygulanır | `{ "ct": [{ "name": "Special Agent Ava", "model": "agents/models/ctm_swat/ctm_swat_variante.vmdl", "arm": "", "leg": true }], "t": [...] }` |
| `PlayerSize` | Oyuncu boyutu seçimi; yalnız spawn'da uygulanır; boyut zaten başka eklentiyle değiştiyse dokunmaz | `[0.5, 0.75, 1.25, 1.5]` |
| `PlayerTrail` | Oyuncu hareket izi | `{ "width": 1.5, "lifetime": 2.5, "colors": [...] }` |
| `Pyro` | VIP'in molotof/yanıcı bombası hasar yerine can yeniler (`multiplier` × hasar; 1'den büyükse net can basar) | `{ "multiplier": 1.5, "ignore_teammates": false, "ignore_enemy": true, "ignore_self": false, "limit": 0 }` |
| `RadarHack` | Tüm düşmanları (ve C4'ü) radarda gösterir; `duration_on`/`duration_off` ile yanıp söner (`duration_off: 0` = sürekli açık, `duration_on` en az 1 sn) | `{ "duration_on": 1, "duration_off": 0 }` |
| `RapidFire` | `firepercent` atış hızı (`0.1` – `2.0`): `1.0` normal, `2.0` en hızlı, altı yavaşlatır. `recoilpercent` kalan sekme (`0.0` – `1.0`): `0.0` sekme yok, `1.0` normal | `{ "only_with_weapon": "", "recoilpercent": 0.0, "firepercent": 2.0 }` |
| `ReflectDamage` | Hasar yansıtma | `{ "reflect_percent": 50, "max_per_shot": 100, "only_with_weapon": "", "ignore_teammates": true, "ignore_self": true, "limit": 0 }` |
| `Respawn` | Ölen oyuncu `time` saniye sonra yeniden doğar; `limit` raunt başına hak (0 = sınırsız), raunt değişince iptal | `{ "limit": 1, "time": 3 }` |
| `Sacrifice` | VIP ölünce yaşayan takım arkadaşlarına can (kendi MaxHealth tavanlı), zırh (+`helmet` ile kask) ve `weapons` listesindeki silahları verir | `{ "hp": 25, "armor": 25, "helmet": false, "weapons": "weapon_hegrenade,weapon_flashbang" }` |
| `SaySound` | Sohbete mesaj yazınca ses çalar (`say` herkese, `say_team` takıma); `cooldown` saniye, `0` = beklemesiz; `path` dosya yolu veya `emit` soundevent adı (`volume` yalnız `emit` için); eski düz liste de desteklenir | `{ "cooldown": 2, "sounds": [{ "name": "Beep", "path": "sounds/ui/beepclear.vsnd" }, { "name": "Sohbet", "emit": "UI.Lobby.Chat", "volume": 1 }] }` |
| `Silent` | Ayak seslerini diğer oyunculardan gizler | `{ "only_with_weapon": "" }` |
| `SmokeColor` | Renkli sis bombası; sis rengini başka eklenti ayarladıysa dokunmaz | `["Beyaz #FFFFFF", "Kirmizi #FF0000"]` |
| `SmokeEffect` | Sise özellik verir: zehirli, iyileştiren, yavaşlatan veya WallHack sisi. `time` sisin etkisinin kaç sn süreceği (0 = sis dağılana kadar), `radius` etki alanı, `limit` raunt başına hak | `{ "poison": { "minhp": 10, "damage": 2, "time": 20, "tick": 0.5, "radius": 180, "smokecolor": [255, 0, 255], "ignore_teammates": true, "ignore_self": true, "limit": 0 }, "heal": { "heal": 2, "time": 20, "tick": 0.5, "radius": 180, "smokecolor": [0, 255, 0], "ignore_teammates": false, "ignore_self": false, "ignore_enemy": true, "limit": 0 }, "slow": { "percent": 30, "time": 20, "minspeed": 100, "radius": 180, "smokecolor": [0, 0, 255], "ignore_teammates": true, "ignore_self": true, "ignore_enemy": false, "limit": 0 }, "wallhack": { "time": 20, "tick": 0.25, "radius": 180, "smokecolor": [97, 45, 83], "color": "#612D53", "see_teammates": false, "limit": 0 } }` |
| `SpawnProtection` | Spawn koruması; `time` saniye, `limit` raunt başına kaç kez (0=sınırsız) | `{ "time": 4, "limit": 0 }` |
| `Spy` | Rastgele bir düşmanın modelini giyer | `true` |
| `Tag` | Sohbet etiketi/renkleri + skorbord (TAB) etiketi (`tab` boşsa TAB'a dokunulmaz) | `{ "tag": "{Gold}[{Orchid}PLUS{Gold}]", "name_color": "gold", "chat_color": "default", "tab": "[PLUS]" }` |
| `TeamHeal` | Takım arkadaşına ateş edince hasar yerine iyileştirme | `{ "minhp": 5, "percent": 50, "sound_volume": 0.5, "only_with_weapon": "" }` |
| `Thirdperson` | Üçüncü şahıs kamera | `{ "distance": 120 }` |
| `WallHack` | Rakipleri duvar arkasından parlayarak gösterir. `duration_on`/`duration_off` ile yanıp söner (`duration_off: 0` = sürekli açık), `see_teammates` takım arkadaşlarını da gösterir, `color` parlama rengi | `{ "duration_on": 1, "duration_off": 3, "color": "#612D53", "see_teammates": false }` |
| `Vampire` | Verilen hasar kadar can çalma | `{ "heal_percent": 75, "only_with_weapon": "", "max_overheal": 120, "ignore_teammates": true }` |
| `VIPChat` | VIP'lere özel sohbet kanalı | `true` |
| `WeaponAmmo` | Silah bazlı özel şarjör/yedek mermi (çoğu silahta reserve = şarjör adedi; nova/sawedoff/xm1014'te mermi adedi). Silahı silip yeniden veren eklentilerle (WeaponPaints `css_wp`) uyumlu, mermi korunur | `[{ "weapon_name": "weapon_ak47", "ammo": 30, "reserve": 3 }]` |
| `ZeusCooldown` | Zeus'un yeniden şarj süresini kısaltır (`limit`: raunt başına hak, 0 = sınırsız) | `{ "cooldown": 5, "limit": 0 }` |

## Kullanım Örnekleri

```
!addvip 76561198000000000 #Plus 1mo   → 1 aylık Plus VIP
!addvip 76561198000000000 #Lite 0     → kalıcı Lite VIP
!vip                                  → VIP menüsü + kalan süre
!viplist                              → tüm kayıtlar
!removevip 76561198000000000          → kaydı sil
```

## Notlar

- Config dosyası CounterStrikeSharp'ın `configs/plugins` dizininde değil, **eklenti klasörünün içindedir** (`settings.json`, `vipgroups.json`).
- Bir modül hiçbir grupta tanımlı değilse hiç çalışmaz.
- Ses modüllerinde (`HitSound`, `SaySound`) iki yöntem var: `path` kendi ses dosyanızı çalar, `emit` ise oyunun hazır seslerinden birini çalar. İkisi birden yazılırsa `emit` geçerli olur. Her hazır ses adı `emit` ile çalışmaz; çalıştığı bilinenler: `UI.PlayerPing`, `UI.Lobby.Chat`, `UI.CompetitiveAccept`, `UI.CoinLevelUp`. Her iki yöntemde de ses yalnız gitmesi gereken oyunculara gider, yani `css_hidefx` tercihleri ve `say_team` filtresi ikisinde de çalışır.
- Efekt sesleri (zehir, iyileştirme, zıplama) sadece etkilenen oyuncuya duyulur, seviyesi `sound_volume` ile ayarlanır; `0` hiç çalmaz.
- Renk listelerinde (`ColoredModel`, `PlayerGlow`, `PlayerTrail`, `BulletTrail`, `GrenadeTrail`, `SmokeColor`) `"Rainbow rainbow"` ve `"Rastgele random"` girdileri kullanılabilir. Rastgele seçiliyse oyuncuya her el tek bir ortak renk atanır — model, glow, izler ve sis aynı elde aynı rengi kullanır.

