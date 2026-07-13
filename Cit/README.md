# Cit (Çit)

Baktığınız noktaya çit (tel örgü) veya kapalı panel modeli yerleştirmenizi sağlar. Jailbreak sunucularında alan kapatma / oyun alanı belirleme için tasarlanmıştır.

## Özellikler

- CenterHtml menü ile tek komuttan yönetim
- 3 farklı boyut: Küçük (64x128), Orta (128x128), Büyük (256x128)
- 2 farklı tip: **Çit** (tel örgü, arkası görünür) ve **Barikat** (panel)
- Bakılan noktaya native ray-trace ile hassas yerleştirme (`NativeTrace`)
- Yerleştirilen model bakış yönüne göre otomatik hizalanır
- Baktığınız çiti silme veya tüm çitleri tek seferde temizleme
- Modeller sunucu tarafından otomatik precache edilir
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Kurulum

1. Derlenmiş `Cit` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/Cit/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load Cit` komutunu çalıştırın.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_cit` | Çit menüsünü açar (hayatta olmak gerekir) | `@css/generic` **veya** `@jailbreak/warden` |

### Menü Seçenekleri

| Seçenek | İşlev |
| --- | --- |
| Oluştur | Seçili boyut ve tipte çiti baktığınız noktaya yerleştirir |
| Tip Değiştir | Çit ↔ Kapalı panel arasında geçiş yapar |
| Boyut Değiştir | Küçük → Orta → Büyük şeklinde döngüsel geçiş |
| Baktığını Sil | Nişan aldığınız çiti kaldırır (maks. 256 birim mesafe) |
| Hepsini Sil | Bu eklentiyle oluşturulan tüm çitleri kaldırır |

## Yapılandırma

Config dosyası yoktur. Model yolları ve boyutlar kaynak kodda `FenceOptions` sözlüğünde tanımlıdır.

## Notlar

- Yerleştirilen entity'ler `prop_physics_override` olarak oluşturulur, hareketi kapatılır (`DisableMotion`) ve `bydexter_pluginfence` adıyla etiketlenir — "Hepsini Sil" yalnızca bu etiketli propları kaldırır, harita proplarına dokunmaz.
- Ray-trace kullanılamıyorsa oyuncuya hata mesajı gösterilir (`NativeTrace.LastError`).
- Kullanılan modeller `de_nuke` chainlink fence modelleridir; tüm resmi haritalarda kullanılabilir.