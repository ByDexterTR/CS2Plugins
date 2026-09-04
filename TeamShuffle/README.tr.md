# TeamShuffle

*Bu dosyanın [İngilizcesi / English](README.md).*

Oyuncuları gücüne göre T ve CT arasında dağıtan takım dengeleme eklentisi. Güç skor tablosundan değil, her rauntta verilen hasar, alınan kill ve MVP'lerden anlık hesaplanır. Takım değişimi ölüm eventi tetiklemez, kimse rank puanı kaybetmez.

## Özellikler

- Takımları oyuncu sayısına ve **hasar + kill + MVP + clutch + nişan gücüne** göre dengeler
- İstatistikler anlık toplanıp diske yazılır, skor tablosu kullanılmaz; kaydı olmayan oyuncu sunucu ortalaması sayılır
- Hasar, rakibin gerçekten kaybettiği candan hesaplanır (dolu canlıya AWP kafa = 100 hasar)
- Takım değişimi `SwitchTeam` ile yapılır, oyuncu ölmez; taşıma yeni raunt başlamadan hemen önce uygulanır
- Otomatik karıştırma: üst üste galibiyet (`streak`), her X raunt (`interval`) veya takım gücü farkı (`points`); pistol rauntları tetiklemez
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
| `css_debugshuffle` | Takım güçlerini ve oyuncu dökümünü konsola yazar | `@css/root` |

Varsayılan olarak `css_karistir` komutu da tanımlıdır.

## Yapılandırma

```
csgo/addons/counterstrikesharp/configs/plugins/TeamShuffle/TeamShuffle.json
```

| Ayar | Tip | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `shuffle_mode` | string | `"points"` | `off`, `streak`, `interval` veya `points` |
| `shuffle_streak_round` | int | `3` | `streak`: bir taraf üst üste bu kadar raunt kazanınca karıştırır |
| `shuffle_interval_round` | int | `5` | `interval`: son karıştırmadan bu kadar raunt sonra karıştırır |
| `shuffle_points_ratio` | float | `0.3` | `points`: güçlü takım zayıf takımdan bu oranda öndeyse karıştırır (`0.3` = %30) |
| `shuffle_points_min_round` | int | `3` | `points`: son karıştırmadan beri geçmesi gereken en az raunt |
| `shuffle_cmd` | string | `"css_shuffle,css_karistir"` | Karıştırma komutları, virgülle ayrılır |
| `shuffle_cmd_flag` | string | `"@css/generic,@css/ban"` | Karıştırma komutunun flagleri |
| `shuffle_debug_cmd` | string | `"css_debugshuffle"` | Döküm komutları, virgülle ayrılır |
| `shuffle_debug_flag` | string | `"@css/root"` | Döküm komutunun flagleri |
| `disable_valve_balance` | bool | `true` | `mp_autoteambalance 0` ve `mp_limitteams 0` yapar |
| `disable_changeteam` | bool | `true` | Oyuncular kendi takımını değiştiremez, katılan oyuncu uygun takıma alınır |
| `disable_select_spec` | bool | `true` | Oyuncular izleyiciye geçemez |
| `shuffle_spec_immune_flag` | string | `"@css/ban"` | Bu flaglere sahip oyuncular izleyiciye geçebilir; boş bırakılırsa herkes geçebilir |
| `shuffle_min_players` | int | `4` | Altındayken eklenti hiçbir şeye karışmaz (en az 2) |
| `shuffle_priority` | int | `1` | Tek sayıda oyuncu varken +1 üstünlüğün verileceği taraf: `0` kapalı, `1` skorda geride olan takım, `2` T, `3` CT. Karıştırmada, otomatik eşitlemede ve sunucuya katılan oyuncuyu yerleştirirken kullanılır |
| `shuffle_limitteams` | int | `2` | Takımlar arası oyuncu farkı bu sayıya ulaşınca eşitlenir (en az 2) |
| `shuffle_damage_rating` | int | `1` | Raunt başı ortalama hasarın puan çarpanı |
| `shuffle_kill_rating` | int | `50` | Raunt başı ortalama killin puan çarpanı |
| `shuffle_mvp_rating` | int | `25` | Raunt başı ortalama MVP'nin puan çarpanı |
| `shuffle_clutch_rating` | int | `40` | Raunt başı ağırlıklı clutch'ın puan çarpanı, `0` = kapalı |
| `shuffle_aim_rating` | int | `60` | Kafa isabet oranının puan çarpanı, `0` = kapalı |
| `shuffle_tolerance_ratio` | float | `0.15` | Bu orana kadar güç farkı dengeli sayılır (`0.15` = %15) |
| `shuffle_announce` | bool | `true` | Karıştırmayı sohbetten herkese duyurur |

Mesajlar `lang/tr.json` / `lang/en.json` üzerinden düzenlenebilir.

## Puanlama

```
temel = (raunt başı hasar × shuffle_damage_rating)
      + (raunt başı kill × shuffle_kill_rating)
      + (raunt başı MVP × shuffle_mvp_rating)
      + (raunt başı ağırlıklı clutch × shuffle_clutch_rating)
      + (kafa isabet oranı × shuffle_aim_rating)

puan  = (raunt × temel + 5 × sunucuOrtalaması) / (raunt + 5)
```

Ağırlıklı clutch: 1v2 = 1, 1v3 = 2, 1v4 = 3, 1v5 = 5. Kafa isabet oranı `kafa / (toplam isabet + 20)` ile hesaplanır, birkaç isabetle zirveye çıkılmaz. Son satırdaki harmanlama az raunt oynamış oyuncuyu sunucu ortalamasına yaklaştırır.

Oyuncular güçlüden zayıfa sıralanır ve teker teker o an puanı düşük olan takıma verilir, oyuncu sayıları eşit tutulur. Zayıf takıma göre fark `shuffle_tolerance_ratio` altındaysa kimse oynatılmaz. Oyuncu sayısı tekse fazla oyuncu `shuffle_priority` ile seçilen tarafa verilir.

## Kullanım Örneği

```
!shuffle
```

> `Takımlar raunt sonunda karıştırılacak.`
> Raunt sonunda: `Takımlar karıştırıldı (manuel), 4 oyuncu yeni raunt başında yer değiştirecek.`

## Notlar

- İstatistikler `players/<steamid>.json` dosyalarında kalıcı tutulur; oyuncu sunucuya her döndüğünde geçmişiyle gelir.
- Eklenti warmup sırasında ve oyuncu sayısı `shuffle_min_players` altındayken hiç çalışmaz; oyuncular istediği takıma serbestçe geçer.
- Engeller sadece oyuncunun kendi `jointeam` komutuna uygulanır; admin ve diğer eklentilerin takım değişimleri engellenmez.
- Botlar karıştırmaya ve puanlamaya dahil edilmez.
