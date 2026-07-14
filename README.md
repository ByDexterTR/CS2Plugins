# CS2Plugins

Tüm eklentiler Türkçe/İngilizce dil desteğiyle gelir, kendi klasöründe bağımsız bir proje olarak durur ve kendi `README.md` dosyasıyla belgelenmiştir.

## Eklentiler

### 🔒 Jailbreak

| Eklenti | Açıklama |
| --- | --- |
| [CTBan](CTBan/README.md) | Oyunculara süreli CT (gardiyan) yasağı verir; kalıcı JSON kayıt |
| [CTKit](CTKit/README.md) | CT'lere spawn'da otomatik verilen silah kiti; menüden seçim |
| [CTKov](CTKov/README.md) | Warden olmayan tüm CT'leri tek komutla T'ye taşır |
| [CTPerk](CTPerk/README.md) | T sayısına göre hak tanınan raunt bazlı CT güçlendirmeleri |
| [CTRev](CTRev/README.md) | Ölen CT'leri menüden/otomatik canlandırma; raunt başına hak sistemi |
| [CTSpawnKill](CTSpawnKill/README.md) | CT'lere spawn sonrası kısa süreli hasar koruması |
| [JBDoors](JBDoors/README.md) | Tüm hücre kapılarını tek komutla açar/kapatır |
| [JBRace](JBRace/README.md) | Başlangıç/bitiş noktalı yarış etkinliği; kazanan sayısı ayarlanabilir |
| [JBTeams](JBTeams/README.md) | Canlı T'leri renkli takımlara böler; takım içi hasar kapalı |
| [Meslekmenu](Meslekmenu/README.md) | T'lere raunt başına bir meslek: Doktor, Flash, Bombacı, Rambo, Zeus |
| [Sustum](Sustum/README.md) | Kelime yazma etkinliği; 4 mod (CTSustum, TSustum, DSustum, ÖlüSustum) |
| [Cit](Cit/README.md) | Baktığın noktaya çit/barikat modeli yerleştirme menüsü |
| [Silahsil](Silahsil/README.md) | Yerdeki sahipsiz silahları tek komutla temizler |
| [Cekilis](Cekilis/README.md) | Takım/durum filtreli rastgele oyuncu çekilişi |

### ⚙️ Genel / Yardımcı

| Eklenti | Açıklama |
| --- | --- |
| [1v1Slay](1v1Slay/README.md) | 1v1 durumunda geri sayım; süre dolunca kalanları öldürür |
| [AntiTeamFlash](AntiTeamFlash/README.md) | Takım arkadaşı flash'larının kör etmesini engeller |
| [BhopDoorFix](BhopDoorFix/README.md) | Bhop/KZ haritalarındaki kapıları dondurur |
| [ChatCleaner](ChatCleaner/README.md) | Kendi ekranını veya tüm sunucu sohbetini temizleme |
| [CommandMaker](CommandMaker/README.md) | JSON ile kod yazmadan özel sunucu komutları oluşturma |
| [FortniteArmor](FortniteArmor/README.md) | Hasar önce zırhtan düşer, zırh bitmeden can azalmaz |
| [Lazer](Lazer/README.md) | Ölü oyunculara canlıların baktığı yeri lazerle gösterir |
| [MapBlock](MapBlock/README.md) | Oyuncu sayısı düşükken harita bölgelerini çitle kapatır |
| [PlayerRGB](PlayerRGB/README.md) | Oyuncu modeline RGB (gökkuşağı) efekti |
| [Redbull](Redbull/README.md) | Süreli hız artışı; limit ve cooldown destekli |
| [Sesler](Sesler/README.md) | Bıçak/silah/ayak/oyuncu/MVP seslerini kategori bazında susturma |
| [Slowmode](Slowmode/README.md) | Sohbete genel yavaş mod; mesajlar arasına saniye sınırı koyar |
| [SpawnkillProtection](SpawnkillProtection/README.md) | Flag ve takım bazlı, renk geçişli spawn koruması |
| [Speedometer](Speedometer/README.md) | HUD'da renk geçişli anlık hız göstergesi (u/s) |
| [Thirdperson](Thirdperson/README.md) | Üçüncü şahıs kamera; duvar engelleme ve yetki desteği |

### 🛡️ Altyapı / Yönetim

| Eklenti | Açıklama |
| --- | --- |
| [AdminList](AdminList/README.md) | Çevrimiçi yetkilileri grup etiketi ve renkleriyle listeler; gruplar config'ten |
| [DiscordLogger](DiscordLogger/README.md) | 35+ sunucu olayını 10 Discord webhook kanalına ve günlük dosyaya loglar |
| [PlayerHourCheck](PlayerHourCheck/README.md) | CS2 oynama saati kontrolü; kademeli kick/ban cezaları |
| [VIPCore](VIPCore/README.md) | 50+ modüllü, grup tabanlı, JSON/MySQL destekli VIP sistemi |

## Kurulum (Sunucu)

1. Sunucuda CounterStrikeSharp kurulu olmalıdır.
2. İstediğiniz eklentinin derlenmiş klasörünü kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/<EklentiAdı>/
   ```
3. Sunucuyu yeniden başlatın veya `css_plugins load <EklentiAdı>` komutunu çalıştırın.
4. Eklentiye özel ayarlar için ilgili eklentinin README dosyasına bakın.

## Derleme

```powershell
# Tek eklenti
dotnet build 1v1Slay/1v1Slay.csproj -c Debug

# Çıktı: <Eklenti>/bin/Debug/ → sunucuya kopyalanmaya hazır
```

## Repository Yapısı

```
CS2Plugins/
├── <EklentiAdı>/            # Her eklenti kendi klasöründe
│   ├── <EklentiAdı>.csproj
│   ├── <EklentiAdı>.cs
│   ├── README.md            # Eklentiye özel dokümantasyon
│   └── lang/                # tr.json / en.json dil dosyaları
├── img/                     # HUD/menü ikonları (raw URL ile kullanılır)
└── LICENSE                  # MIT
```