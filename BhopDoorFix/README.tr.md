# BhopDoorFix

*Bu dosyanın [İngilizcesi / English](README.md).*

Bhop / KZ haritalarındaki kapıların hareket etmesini engeller. Böylece kapılar oyuncuyu fırlatamaz ve hareket ederek haritada istismar açığı oluşturamaz.

## Özellikler

- Haritadaki tüm kapıları yerinde dondurur ve kilitler
- Harita sırasında sonradan ortaya çıkan kapıları da otomatik yakalar
- Her raunt başında tüm kapılar yeniden dondurulur
- Eklentiyi harita ortasında yeniden yüklersen haritadaki kapılar anında dondurulur
- Config gerektirmez

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Kurulum

1. Derlenmiş `BhopDoorFix` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/BhopDoorFix/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load BhopDoorFix` komutunu çalıştırın.

## Notlar

- Kapılar kilitlendiği için haritadaki kapı açma mekanikleri (buton, tetikleyici vb.) çalışmaz — eklenti bhop/surf sunucuları için tasarlanmıştır.