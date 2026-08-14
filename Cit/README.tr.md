# Cit (Çit)

*Bu dosyanın [İngilizcesi / English](README.md).*

Baktığınız noktaya çit (tel örgü) veya kapalı panel modeli yerleştirmenizi sağlar. Jailbreak sunucularında alan kapatma / oyun alanı belirleme için tasarlanmıştır.

## Özellikler

- Tek komutla açılan ekran menüsünden yönetim
- 3 farklı boyut: Küçük (64x128), Orta (128x128), Büyük (256x128)
- 2 farklı tip: **Çit** (tel örgü, arkası görünür) ve **Barikat** (panel)
- Tam olarak baktığınız noktaya hassas yerleştirme
- Yerleştirilen model bakış yönüne göre otomatik hizalanır
- Baktığınız çiti silme veya tüm çitleri tek seferde temizleme
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

Ayar dosyası yoktur; boyut ve tip menüden seçilir.

## Notlar

- Yerleştirdiğiniz çitler yerinde sabit durur, itilmez. "Hepsini Sil" yalnızca bu eklentiyle koyulan çitleri kaldırır, haritanın kendi nesnelerine dokunmaz.
- Yerleştirme noktası hesaplanamazsa oyuncuya hata mesajı gösterilir.
- Kullanılan modeller `de_nuke` chainlink fence modelleridir; tüm resmi haritalarda kullanılabilir.