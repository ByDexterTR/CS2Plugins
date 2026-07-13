# VIPCore

Modüler VIP sistemi. 50'den fazla yerleşik VIP özelliği (modül), grup tabanlı yetkilendirme, JSON veya MySQL depolama ve üç farklı menü tipiyle eksiksiz bir VIP altyapısı sunar.

## Özellikler

- **50+ yerleşik modül** — hepsi tek DLL içinde, otomatik keşfedilir (reflection)
- **Grup sistemi** — `vipgroups.json` içinde sınırsız grup; her grup hangi modülleri hangi değerlerle alacağını belirler
- **Depolama** — JSON (varsayılan) veya MySQL; MySQL bağlantısı koparsa otomatik JSON'a düşer
- **3 menü tipi** — `hud` (CenterHtml), `chat`, `wasd` (W/S/E/R tuşlarıyla gezilen menü)
- Süreli veya kalıcı VIP; süresi dolan oyuncunun tüm özellikleri kapanır, VIP kaydı **ve** oyuncu ayarları depolamadan (JSON/MySQL) otomatik silinir
- Oyuncu bazlı özellik ayarları (aç/kapat veya seçim) kalıcı olarak saklanır
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
    "tp": "css_tp,css_thirdperson"
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
    "ChatTag": { "tag": "{BlueGrey}[LITE]", "name_color": "bluegrey", "chat_color": "default" }
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
| `AntiFlash` | Flashbang'i engeller | `{ "self": true, "enemy": true, "teammates": true }` |
| `AntiHS` | Headshot hasarını azaltır | `{ "percent": 0, "only_with_weapon": "" }` |
| `Armor` | Spawn'da zırh (+kask) | `{ "value": 100, "helmet": true }` |
| `ArmorRegen` | Zırh yenilenmesi | `{ "armor_per_tick": 10, "interval": 1.0, "delay_after_dmg": 2, "max_armor": 100, "give_helmet_when_full": true }` |
| `AutoHS` | Vuruşlar Headshot sayılır | `{ "multiplier": 4, "only_with_weapon": "", "ignore_teammates": true }` |
| `Bhop` | Bunny hop (+opsiyonel autostrafe) | `{ "autostrafe": true, "max_speed": 500, "jump_boost": 1.1, "jump_velocity": 300 }` |
| `BulletTrail` | Mermi izi efekti | `{ "width": 1.5, "lifetime": 0.6, "colors": [...] }` |
| `BuyTeamWeapon` | Karşı takım silahlarını satın alma; Komut adları `settings.json` → `buy_commands` | `{ "ak47": true, "m4a4": true, ... }` |
| `ChatTag` | Sohbet etiketi ve renkleri | `{ "tag": "{Gold}[PLUS]", "name_color": "gold", "chat_color": "default" }` |
| `ColoredModel` | Renkli oyuncu modeli | `["Rainbow rainbow", "Mavi #0000FF"]` |
| `DamageDealt` | Verilen hasarı artırır | `{ "percent": 50, "only_with_weapon": "", "ignore_teammates": true, "ignore_self": true }` |
| `DamageResist` | Alınan hasarı azaltır | `{ "percent": 40, "only_with_weapon": "", "ignore_teammates": true, "ignore_self": true }` |
| `DecoyTeleport` | Decoy'un düştüğü yere ışınlanma | `{ "limit": 3 }` |
| `DefuseKit` | Spawn'da imha kiti (CT) | `true` |
| `ExtraHP` | Spawn HP değeri | `150` |
| `ExtraJump` | Çoklu zıplama | `{ "count": 2, "limit": 0 }` |
| `ExtraMoney` | Spawn'da ekstra para | `{ "amount": 4000 }` |
| `ExtraSpeed` | Hız çarpanı | `{ "multiplier": 1.3, "only_with_weapon": "" }` |
| `FallDamage` | Düşme hasarını azaltır/kaldırır | `{ "percent": 0, "count": 0 }` |
| `FastDefuse` | Hızlı bomba imhası | `{ "time": 1, "immune_while_burning": true }` |
| `FastPlant` | Hızlı bomba kurma | `{ "time": 1, "immune_while_burning": true }` |
| `Fov` | FOV seçenekleri | `[50, 60, 70, 80, 90]` |
| `GiveWeapon` | Spawn'da silah seçimi | `["weapon_ak47", "weapon_awp"]` |
| `Glaz` | Sis içini görme | `true` |
| `Gravity` | Yerçekimi seçenekleri | `[1.0, 0.8, 0.5]` |
| `GrenadeTrail` | Bomba izi efekti | `{ "width": 1.5, "lifetime": 2.5, "colors": [...] }` |
| `HealthRegen` | Can yenilenmesi | `{ "hp_per_tick": 10, "interval": 1.0, "delay_after_dmg": 2, "max_hp": 150 }` |
| `Healthshot` | Spawn'da healthshot | `2` |
| `InfiniteAmmo` | Sınırsız mermi | `{ "only_weapon": "" }` |
| `JoinMessage` | Giriş/çıkış duyurusu | `{ "join_message": "...", "leave_message": "..." }` |
| `KillScreen` | Öldürme ekran efekti | `{ "duration": 1.0 }` |
| `OneShot` | Belirli silahlarla tek atış | `{ "weapons": "weapon_awp,weapon_ssg08" }` |
| `PlayerGlow` | Oyuncu glow (duvar arkası parlama) | `{ "range": 300, "team": -1, "colors": [...] }` |
| `PlayerTrail` | Oyuncu hareket izi | `{ "width": 1.5, "lifetime": 2.5, "colors": [...] }` |
| `ReflectDamage` | Hasar yansıtma | `{ "reflect_percent": 50, "max_per_shot": 100, "only_with_weapon": "", "ignore_teammates": true, "ignore_self": true }` |
| `SmokeColor` | Renkli sis bombası | `["Beyaz #FFFFFF", "Kirmizi #FF0000"]` |
| `SpawnProtection` | Spawn koruması (saniye) | `4` |
| `Thirdperson` | Üçüncü şahıs kamera | `{ "distance": 120 }` |
| `Vampire` | Verilen hasar kadar can çalma | `{ "heal_percent": 75, "only_with_weapon": "", "max_overheal": 120, "ignore_teammates": true }` |
| `VIPChat` | VIP'lere özel sohbet kanalı | `true` |
| `GiveZeus` | Spawn'da taser | `true` |
| `WeaponAmmo` | Silah bazlı özel şarjör/yedek mermi; (çoğu silahta reserve = şarjör adedi; nova/sawedoff/xm1014'te mermi adedi) | `[{ "weapon_name": "weapon_ak47", "ammo": 30, "reserve": 3 }]` |
| `FastReload` | Şarjör normal boşalır; son mermide yedekten anında dolar — (yedekten 1 şarjör düşer) | `{ "only_with_weapon": "" }` |
| `GrenadeKit` | Spawn'da bomba seti; zaten varsa vermez, 2+ ise atınca yeniden verir (InfiniteAmmo açıkken yeniden vermez) | `{ "flash": 2, "smoke": 1, "he": 3, "molotov": 1, "decoy": 0 }` |
| `TeamHeal` | Takım arkadaşına ateş edince hasar yerine iyileştirme | `{ "minhp": 5, "maxhp": 100, "percent": 50, "only_with_weapon": "" }` |
| `SmokeEffect` | Sis özelliği; zehirli / iyileştiren / yavaşlatan sis seçer (yalnız config'te tanımlı modlar listelenir; `limit`: raunt başına hak, 0 = sınırsız; `radius`: etki alanı) | `{ "poison": { "minhp": 10, "damage": 2, "tick": 0.5, "radius": 180, "smokecolor": [255, 0, 255], "ignore_teammates": true, "ignore_self": true, "limit": 0 }, "heal": { "maxhp": 100, "heal": 2, "tick": 0.5, "radius": 180, "smokecolor": [0, 255, 0], "ignore_teammates": false, "ignore_self": false, "ignore_enemy": true, "limit": 0 }, "slow": { "percent": 30, "minspeed": 100, "radius": 180, "smokecolor": [0, 0, 255], "ignore_teammates": true, "ignore_self": true, "ignore_enemy": false, "limit": 0 } }` |
| `RadarHack` | Tüm düşmanları (ve C4'ü) radarda gösterir | `true` |
| `Invisibility` | Görünmezlik (düşmanlara transmit edilmez) | `{ "only_stopped": true, "dmg_after_invis": 2.0, "only_with_weapon": "" }` |
| `PoisonBullet` | Vurulan düşmanı zehirler, periyodik hasar verir | `{ "minhp": 10, "damage": 2, "damagetick": 1.0, "only_with_weapon": "", "ignore_teammates": true }` |
| `GlueGrenade` | Atılan bombalar ilk temasta yapışır (decoy eklersen DecoyTeleport ile duvar içine ışınlanma riski) | `{ "only_grenades": "flashbang,hegrenade" }` |
| `Spy` | Rastgele bir düşmanın modelini giyer | `true` |
| `GrenadeResist` | Bomba (HE/molotov/inferno) hasarını azaltır | `{ "percent": 50, "only_with_grenade": "he,molotov,inferno", "ignore_teammates": true, "ignore_self": true }` |
| `Silent` | Ayak seslerini diğer oyunculardan gizler | `{ "only_with_weapon": "" }` |
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

