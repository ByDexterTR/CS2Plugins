# TeamShuffle

*Bu dosyanın [İngilizcesi / English](README.md).*

Oyuncuları gücüne göre T ve CT arasında dağıtan takım dengeleme eklentisi. Güç skor tablosundan değil, her rauntta verilen hasar, alınan kill ve MVP'lerden anlık hesaplanır. Takım değişimi ölüm eventi tetiklemez, kimse rank puanı kaybetmez.

## Özellikler

- Takımları oyuncu sayısına ve **hasar + kill + MVP gücüne** göre dengeler
- İstatistikler anlık toplanır, skor tablosu kullanılmaz; kaydı olmayan oyuncu sunucu ortalaması sayılır
- Hasar, rakibin gerçekten kaybettiği candan hesaplanır (dolu canlıya AWP kafa = 100 hasar)
- Takım değişimi `SwitchTeam` ile yapılır, oyuncu ölmez; taşıma yeni raunt başlamadan hemen önce uygulanır
- Otomatik karıştırma: üst üste galibiyet (`streak`) veya her X raunt (`interval`); pistol rauntları tetiklemez
- Takımlar arası oyuncu farkı `shuffle_limitteams`'e ulaşınca sayılar her raunt başında eşitlenir
- Oyuncuların takım değiştirmesi ve izleyiciye geçmesi kapatılabilir, izleyici için muafiyet flagi vardır
- Valve'ın kendi takım dengelemesini kapatabilir
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Kurulum

1. Derlenmiş `TeamShuffle` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/TeamShuffle/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load TeamShuffle` komutunu çalıştırın.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_shuffle` | Takımları karıştırır; dağıtım raunt sonunda hesaplanır, yeni raunt başında uygulanır | `@css/generic` **veya** `@css/ban` |
| `css_power` | İki takımın gücünü ve oyuncu sayısını söyler | `@css/generic` **veya** `@css/ban` |

Varsayılan olarak `css_karistir` ve `css_guc` komutları da tanımlıdır.

## Yapılandırma

```
csgo/addons/counterstrikesharp/configs/plugins/TeamShuffle/TeamShuffle.json
```

| Ayar | Tip | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `shuffle_mode` | string | `"streak"` | `off`, `streak` veya `interval` |
| `shuffle_streak_round` | int | `3` | `streak`: bir taraf üst üste bu kadar raunt kazanınca karıştırır |
| `shuffle_interval_round` | int | `5` | `interval`: son karıştırmadan bu kadar raunt sonra karıştırır |
| `shuffle_cmd` | string | `"css_shuffle,css_karistir"` | Karıştırma komutları, virgülle ayrılır |
| `shuffle_cmd_flag` | string | `"@css/generic,@css/ban"` | Karıştırma komutunun flagleri |
| `shuffle_power_cmd` | string | `"css_power,css_guc"` | Güç komutları, virgülle ayrılır |
| `shuffle_power_flag` | string | `"@css/generic,@css/ban"` | Güç komutunun flagleri |
| `disable_valve_balance` | bool | `true` | `mp_autoteambalance 0` ve `mp_limitteams 0` yapar |
| `disable_changeteam` | bool | `true` | Oyuncular kendi takımını değiştiremez, katılan oyuncu uygun takıma alınır |
| `disable_select_spec` | bool | `true` | Oyuncular izleyiciye geçemez |
| `shuffle_spec_immune_flag` | string | `"@css/ban"` | Bu flaglere sahip oyuncular izleyiciye geçebilir; boş bırakılırsa herkes geçebilir |
| `shuffle_min_players` | int | `4` | Altındayken eklenti hiçbir şeye karışmaz (en az 2) |
| `shuffle_limitteams` | int | `2` | Takımlar arası oyuncu farkı bu sayıya ulaşınca eşitlenir (en az 2) |
| `reset_on_map_change` | bool | `true` | Harita değişince istatistikler sıfırlanır |
| `shuffle_damage_rating` | int | `1` | Raunt başı ortalama hasarın puan çarpanı |
| `shuffle_kill_rating` | int | `50` | Raunt başı ortalama killin puan çarpanı |
| `shuffle_mvp_rating` | int | `25` | Raunt başı ortalama MVP'nin puan çarpanı |
| `shuffle_balance_tolerance` | int | `10` | Yüzde kaça kadar puan farkı dengeli sayılır |
| `shuffle_announce` | bool | `true` | Karıştırmayı sohbetten herkese duyurur |

Mesajlar `lang/tr.json` / `lang/en.json` üzerinden düzenlenebilir.

## Puanlama

```
puan = (raunt başı ortalama hasar × shuffle_damage_rating)
     + (raunt başı ortalama kill × shuffle_kill_rating)
     + (raunt başı ortalama MVP × shuffle_mvp_rating)
```

Oyuncular güçlüden zayıfa sıralanır ve teker teker o an puanı düşük olan takıma verilir, oyuncu sayıları eşit tutulur. Puan farkı `shuffle_balance_tolerance` yüzdesinin altındaysa kimse oynatılmaz.

## Kullanım Örneği

```
!shuffle
```

> `Takımlar raunt sonunda karıştırılacak.`
> Raunt sonunda: `Takımlar karıştırıldı (manuel), 4 oyuncu yeni raunt başında yer değiştirecek.`
> `Güç: CT 612 - T 598`

## Notlar

- İstatistikler bellekte, SteamID bazlı tutulur; harita içinde yeniden bağlanan oyuncu kaydını korur.
- Eklenti warmup sırasında ve oyuncu sayısı `shuffle_min_players` altındayken hiç çalışmaz; oyuncular istediği takıma serbestçe geçer.
- Engeller sadece oyuncunun kendi `jointeam` komutuna uygulanır; admin ve diğer eklentilerin takım değişimleri engellenmez.
- Botlar karıştırmaya ve puanlamaya dahil edilmez.
