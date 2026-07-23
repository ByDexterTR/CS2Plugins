# VIPCore

Modüler VIP sistemi. 75'ten fazla yerleşik VIP özelliği (modül), grup tabanlı yetkilendirme, JSON veya MySQL depolama ve üç farklı menü tipiyle eksiksiz bir VIP altyapısı sunar.

## Özellikler

- **75+ yerleşik modül** — hepsi tek DLL içinde, otomatik keşfedilir (reflection)
- **Grup sistemi** — `vipgroups.json` içinde sınırsız grup; her grup hangi modülleri hangi değerlerle alacağını belirler
- **Depolama** — JSON (varsayılan) veya MySQL; MySQL bağlantısı koparsa otomatik JSON'a düşer
- **3 menü tipi** — `hud` (CenterHtml), `chat`, `wasd` (W/S/E/R tuşlarıyla gezilen menü)
- Süreli veya kalıcı VIP; süresi dolan oyuncunun tüm özellikleri kapanır, VIP kaydı **ve** oyuncu ayarları depolamadan (JSON/MySQL) otomatik silinir
- Oyuncu bazlı özellik ayarları (aç/kapat veya seçim) kalıcı olarak saklanır
- **Efekt görünürlüğü** (`css_hidefx`) — her oyuncu trail/partikül/glow/ses efektlerini kendisi için kapatabilir
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
| `css_hidevip` / `css_hidefx` | Efekt görünürlük menüsü (BulletTrail, C4Effect, KillEffect, PlayerTrail, PlayerGlow, GrenadeTrail, SaySound); Herkes → Kendim → Kapalı döngüsü, tercih kalıcı saklanır | — (herkes) |

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
| `hide` | nesne | — | Efekt görünürlüğü modül varsayılanları: `all` herkesinki görünür, `self` sadece kendisininki, `hidden` hiçbiri, `off` özellik kilitli (herkes görür, oyuncu değiştiremez, menüde çıkmaz). Depoda kayıtlı oyuncu tercihi varsayılanı ezer |
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
  "hide": {
    "BulletTrail": "all",
    "C4Effect": "all",
    "KillEffect": "all",
    "PlayerTrail": "all",
    "PlayerGlow": "self",
    "GrenadeTrail": "all",
    "SaySound": "all"
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
| MySQL | `table_prefix` önekiyle tablolar otomatik oluşturulur; oyuncu girişinde kayıt canlı yenilenir |

## Modüller

Modül adları `vipgroups.json` içinde anahtar olarak kullanılır (büyük/küçük harf duyarlı).

| Modül | Açıklama | Grup değeri örneği |
| --- | --- | --- |
| `AdminFlags` | VIP'e otomatik yetki bayrağı verir | `["@css/reservation", "@css/vip"]` |
| `AdminGroups` | VIP'e admin grubu üyeliği verir (SimpleAdmin vb. `#Grup` adları) | `["#VIP"]` |
| `AntiFlash` | Flashbang'i engeller | `{ "self": true, "enemy": true, "teammates": true }` |
| `AntiHS` | Headshot hasarını azaltır | `{ "percent": 0, "only_with_weapon": "" }` |
| `Armor` | Spawn'da zırh (+kask) | `{ "value": 100, "helmet": true }` |
| `ArmorRegen` | Zırh yenilenmesi | `{ "armor_per_tick": 10, "interval": 1.0, "delay_after_dmg": 2, "max_armor": 100, "give_helmet_when_full": true }` |
| `Aura` | Sürekli aktif alan etkisi (heal/poison/slow/speed), oyuncuyu takip eden dönen bir env_beam ile radius görselleştirilir; `duration_on`/`duration_off` yanıp sönme | `{ "heal": { "heal": 2, "tick": 0.5, "radius": 180, "beamcolor": "0 255 0", "duration_on": 1, "duration_off": 0, "ignore_teammates": false, "ignore_self": false, "ignore_enemy": true } }` |
| `AutoHS` | Vuruşlar Headshot sayılır | `{ "multiplier": 4, "only_with_weapon": "", "ignore_teammates": true }` |
| `Berserk` | Öldürme başına hasar çarpanı artar; `dpk` kill başına eklenen çarpan, `maxdpk` tavan | `{ "dpk": 0.2, "maxdpk": 5.0 }` |
| `Bhop` | Bunny hop (+opsiyonel autostrafe) | `{ "autostrafe": true, "max_speed": 500, "jump_boost": 1.1, "jump_velocity": 300 }` |
| `BombsiteAnnouncer` | Bomba kurulunca CT'lere HUD görseli (yalnız görsel) + sohbet mesajı | `{ "img_a": "...Site-A.png", "img_b": "...Site-B.png", "duration": 5.0 }` |
| `BulletTrail` | Mermi izi efekti | `{ "width": 1.5, "lifetime": 0.6, "colors": [...] }` |
| `BuyTeamWeapon` | Karşı takım silahlarını satın alma (yalnız buyzone içinde ve `mp_buytime` dolmadan); Komut adları `settings.json` → `buy_commands` | `{ "ak47": true, "m4a4": true, ... }` |
| `C4Effect` | 2 kategori — kurma efekti (`defuse: false`, bombayı takip eder, `time` sn sonra veya imha/patlamada silinir) ve imha efekti (`defuse: true`, imha edende `time` sn oynar); boş kategori menüde gizlenir | `[{ "name": "Duman", "particle": "...", "time": 6, "defuse": false }]` |
| `ColoredModel` | Renkli oyuncu modeli | `["Rainbow rainbow", "Mavi #0000FF"]` |
| `CustomWeaponModel` | Silaha özel model; `model` sayıysa ChangeSubclass (görünüm + viewmodel), path ise yalnız dünya modeli; silah düşürülüp başkası alırsa native'e döner | `[{ "name": "M4A4 - AK47", "weapon": "weapon_m4a1", "model": "weapons/models/ak47/weapon_rif_ak47.vmdl" }]` |
| `DamageDealt` | Verilen hasarı artırır | `{ "percent": 50, "only_with_weapon": "", "ignore_teammates": true, "ignore_self": true }` |
| `DamageResist` | Alınan hasarı azaltır | `{ "percent": 40, "only_with_weapon": "", "ignore_teammates": true, "ignore_self": true }` |
| `Dash` | Havadayken zıplama tuşuna basınca bastığın yön tuşuna doğru atılır (yön yoksa ileri); `limit`: raunt başına hak (0 = sınırsız), `unit`: itme hızı | `{ "limit": 3, "unit": 600 }` |
| `DecoyTeleport` | Decoy'un düştüğü yere ışınlanma | `{ "limit": 3 }` |
| `DefuseKit` | Spawn'da imha kiti (CT) | `true` |
| `ExtraHP` | Spawn HP değeri | `150` |
| `ExtraJump` | Çoklu zıplama; `count` bir havalanmadaki ekstra zıplama, toplam hak = `count × limit` (`limit: 0` = sınırsız) | `{ "count": 2, "limit": 0 }` |
| `ExtraKillAwards` | Öldürme şekline göre ekstra para: `headshot`, `noscope`, `inair`, `blind`, `weapon_*` (silaha özel), `distance` (her `unit` birim mesafe için `money`) | `{ "headshot": 150, "noscope": 100, "inair": 200, "blind": 50, "distance": { "unit": 2048, "money": 100 }, "weapon_knife": 1000 }` |
| `ExtraMoney` | Spawn'da ekstra para | `{ "amount": 4000 }` |
| `ExtraSpeed` | Hız çarpanı | `{ "multiplier": 1.3, "only_with_weapon": "" }` |
| `FallDamage` | Düşme hasarını azaltır/kaldırır | `{ "percent": 0, "count": 0 }` |
| `FastDefuse` | Hızlı bomba imhası; `immune_while_burning: false` ise yanarken / ateşin veya havadaki molotofun yakınında hız avantajı devre dışı | `{ "time": 1, "immune_while_burning": true }` |
| `FastPlant` | Hızlı bomba kurma;  `immune_while_burning: false` ise yanarken / ateşin veya havadaki molotofun yakınında hız avantajı devre dışı | `{ "time": 1, "immune_while_burning": true }` |
| `FastReload` | Şarjör normal boşalır; son mermide yedekten anında dolar — (yedekten 1 şarjör düşer) | `{ "only_with_weapon": "" }` |
| `Fov` | FOV seçenekleri | `[50, 60, 70, 80, 90]` |
| `GiveWeapon` | Spawn'da silah seçimi; kategori bazlı (her kategoriden bir seçim `rifle`/`pistol`), Menüdeki "Daima Ver" açıkken slottaki mevcut silah silinip verilir; boş kategori menüde gizlenir | `{ "rifle": ["weapon_ak47", "weapon_awp"], "pistol": ["weapon_deagle"] }` |
| `GiveZeus` | Spawn'da taser | `true` |
| `Glaz` | Sis içini görme | `true` |
| `GlueGrenade` | Atılan bombalar ilk temasta yapışır (decoy eklersen DecoyTeleport ile duvar içine ışınlanma riski) | `{ "only_grenades": "flashbang,hegrenade" }` |
| `Gravity` | Yerçekimi seçenekleri | `[1.0, 0.8, 0.5]` |
| `GrenadeKit` | Spawn'da bomba seti; zaten varsa vermez, 2+ ise atınca yeniden verir (InfiniteAmmo açıkken yeniden vermez) | `{ "flash": 2, "smoke": 1, "he": 3, "molotov": 1, "decoy": 0 }` |
| `GrenadeResist` | Bomba (HE/molotov/inferno) hasarını azaltır | `{ "percent": 50, "only_with_grenade": "he,molotov,inferno", "ignore_teammates": true, "ignore_self": true }` |
| `GrenadeTrail` | Bomba izi efekti | `{ "width": 1.5, "lifetime": 2.5, "colors": [...] }` |
| `HealthRegen` | Can yenilenmesi | `{ "hp_per_tick": 10, "interval": 1.0, "delay_after_dmg": 2 }` |
| `Healthshot` | Spawn'da healthshot | `2` |
| `HitSound` | Düşmana vurunca seçilen ses çalar (FFA kapalıysa takım arkadaşında çalmaz) | `[{ "name": "Killcard", "path": "sounds/ui/killcard_1.vsnd" }]` |
| `InfiniteAmmo` | Sınırsız mermi | `{ "only_weapon": "" }` |
| `Invisibility` | Görünmezlik (düşmanlara transmit edilmez) | `{ "only_stopped": true, "dmg_after_invis": 2.0, "only_with_weapon": "" }` |
| `Jammer` | Yaklaşan oyuncuların radarını kapatır (`radius` menzil); ölü izleyici jam'li birini izliyorsa onun radarı da kapanır | `{ "radius": 500, "ignore_teammates": true, "ignore_enemy": false }` |
| `JoinMessage` | Giriş/çıkış duyurusu | `{ "join_message": "...", "leave_message": "..." }` |
| `KillEffect` | Öldürmede partikül (`time` sn oynar); 3 kategori — normal, HS (`hs: true`), son öldürme (`lastkill: true`). Kategoriye seçim yoksa kademeli düşer (last → hs → normal), boş kategori menüde gizlenir (FFA kapalıysa takım arkadaşında çalmaz) | `[{ "name": "Simsek", "particle": "...", "time": 3, "hs": false, "lastkill": false }]` |
| `KillHeal` | Öldürme şekline göre can yeniler: `distance` içinde `hp` (veya `money`) anahtarı | `{ "headshot": 15, "noscope": 10, "inair": 20, "blind": 5, "distance": { "unit": 2048, "hp": 10 }, "weapon_knife": 50 }` |
| `KillScreen` | Öldürme ekran efekti (FFA kapalıysa takım arkadaşında çalışmaz) | `{ "duration": 1.0 }` |
| `MagneticDecoy` | Decoy yere düşüp öttüğü sürece `radius` içindekileri kendine çeker; çekim mesafeyle azalır (`strength` taban güç) | `{ "radius": 180, "strength": 30, "ignore_teammates": true, "ignore_enemy": false, "ignore_self": true }` |
| `Mole` | Hasar verilen oyuncu `time` saniye `unit` birim yere gömülür ve hareket edemez | `{ "time": 2.5, "unit": 30, "only_with_weapon": "weapon_deagle", "ignore_teammates": true, "ignore_enemy": false, "ignore_self": true }` |
| `OneShot` | Belirli silahlarla tek atış | `{ "weapons": "weapon_awp,weapon_ssg08" }` |
| `PistolRoundDisable` | Listelenen modüller pistol rauntlarda devre dışı kalır (modül değil, grup ayarı) | `["GiveWeapon", "WeaponAmmo"]` |
| `PlayerGlow` | Oyuncu glow (duvar arkası parlama) | `{ "range": 300, "team": -1, "colors": [...] }` |
| `PlayerModel` | Takım bazlı oyuncu model seçimi (CT/T ayrı menü); `leg: false` birinci şahıs bacakları gizler, `arm` yalnız precache edilir; yalnız spawn'da uygulanır | `{ "ct": [{ "name": "Special Agent Ava", "model": "agents/models/ctm_swat/ctm_swat_variante.vmdl", "arm": "", "leg": true }], "t": [...] }` |
| `PlayerSize` | Oyuncu boyutu seçimi; yalnız spawn'da uygulanır | `[0.5, 0.75, 1.25, 1.5]` |
| `PlayerTrail` | Oyuncu hareket izi | `{ "width": 1.5, "lifetime": 2.5, "colors": [...] }` |
| `PoisonBullet` | Vurulan düşmanı zehirler, periyodik hasar verir | `{ "minhp": 10, "damage": 2, "damagetick": 1.0, "only_with_weapon": "", "ignore_teammates": true }` |
| `Pyro` | VIP'in molotof/yanıcı bombası hasar yerine can yeniler (`multiplier` × hasar; 1'den büyükse net can basar) | `{ "multiplier": 1.5, "ignore_teammates": false, "ignore_enemy": true, "ignore_self": false }` |
| `RadarHack` | Tüm düşmanları (ve C4'ü) radarda gösterir; `duration_on`/`duration_off` ile yanıp söner (`duration_off: 0` = sürekli açık, `duration_on` en az 1 sn) | `{ "duration_on": 1, "duration_off": 0 }` |
| `RapidFire` | Silahlar bekleme süresi olmadan ateşlenir; `norecoil: true` sekme/sarsıntıyı da sıfırlar | `{ "only_with_weapon": "", "norecoil": true }` |
| `ReflectDamage` | Hasar yansıtma | `{ "reflect_percent": 50, "max_per_shot": 100, "only_with_weapon": "", "ignore_teammates": true, "ignore_self": true }` |
| `Respawn` | Ölen oyuncu `timer` saniye sonra yeniden doğar; `limit` raunt başına hak (0 = sınırsız), raunt değişince iptal | `{ "limit": 1, "timer": 3 }` |
| `Sacrifice` | VIP ölünce yaşayan takım arkadaşlarına can (kendi MaxHealth tavanlı), zırh (+`helmet` ile kask) ve `weapons` listesindeki silahları verir | `{ "hp": 25, "armor": 25, "helmet": false, "weapons": "weapon_hegrenade,weapon_flashbang" }` |
| `SaySound` | Sohbete mesaj yazınca ses çalar (`say` herkese, `say_team` takıma); `cooldown` saniye, `0` = beklemesiz; eski düz liste de desteklenir | `{ "cooldown": 2, "sounds": [{ "name": "Beep", "path": "sounds/ui/beepclear.vsnd" }] }` |
| `Silent` | Ayak seslerini diğer oyunculardan gizler | `{ "only_with_weapon": "" }` |
| `SmokeColor` | Renkli sis bombası | `["Beyaz #FFFFFF", "Kirmizi #FF0000"]` |
| `SmokeEffect` | Sis özelliği; zehirli / iyileştiren / yavaşlatan sis seçer (yalnız config'te tanımlı modlar listelenir; `time`: sis patladıktan kaç sn sonra etkinin biteceği, 0 = sis dağılana kadar; `limit`: raunt başına hak, 0 = sınırsız; `radius`: etki alanı) | `{ "poison": { "minhp": 10, "damage": 2, "time": 20, "tick": 0.5, "radius": 180, "smokecolor": [255, 0, 255], "ignore_teammates": true, "ignore_self": true, "limit": 0 }, "heal": { "heal": 2, "time": 20, "tick": 0.5, "radius": 180, "smokecolor": [0, 255, 0], "ignore_teammates": false, "ignore_self": false, "ignore_enemy": true, "limit": 0 }, "slow": { "percent": 30, "time": 20, "minspeed": 100, "radius": 180, "smokecolor": [0, 0, 255], "ignore_teammates": true, "ignore_self": true, "ignore_enemy": false, "limit": 0 } }` |
| `SpawnProtection` | Spawn koruması (saniye) | `4` |
| `Spy` | Rastgele bir düşmanın modelini giyer | `true` |
| `Tag` | Sohbet etiketi/renkleri + skorbord (TAB) etiketi (`tab` boşsa TAB'a dokunulmaz) | `{ "tag": "{Gold}[{Orchid}PLUS{Gold}]", "name_color": "gold", "chat_color": "default", "tab": "[PLUS]" }` |
| `TeamHeal` | Takım arkadaşına ateş edince hasar yerine iyileştirme | `{ "minhp": 5, "percent": 50, "only_with_weapon": "" }` |
| `Thirdperson` | Üçüncü şahıs kamera | `{ "distance": 120 }` |
| `Vampire` | Verilen hasar kadar can çalma | `{ "heal_percent": 75, "only_with_weapon": "", "max_overheal": 120, "ignore_teammates": true }` |
| `VIPChat` | VIP'lere özel sohbet kanalı | `true` |
| `WeaponAmmo` | Silah bazlı özel şarjör/yedek mermi; (çoğu silahta reserve = şarjör adedi; nova/sawedoff/xm1014'te mermi adedi). | `[{ "weapon_name": "weapon_ak47", "ammo": 30, "reserve": 3 }]` |
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
- Bir modül hiçbir grupta tanımlı değilse hiç yüklenmez (sıfır maliyet).
- Trail modülleri kullanılıyorsa beam sprite'ı otomatik precache edilir.
- Renk listelerinde (`ColoredModel`, `PlayerGlow`, `PlayerTrail`, `BulletTrail`, `GrenadeTrail`, `SmokeColor`) `"Rainbow rainbow"` ve `"Rastgele random"` girdileri kullanılabilir. Rastgele seçiliyse oyuncuya her el tek bir ortak renk atanır — model, glow, izler ve sis aynı elde aynı rengi kullanır.

