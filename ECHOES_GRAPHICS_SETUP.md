# ECHOES - Grafik Kurulum Kılavuzu (Echoes Sahnesi)

## 🎯 Otomatik Kurulum (Önerilen)

### Adım 1: Unity'yi Açın
```
Unity Editor'de Echoes.unity sahnesini açın
```

### Adım 2: Otomatik Kurulumu Çalıştırın

**Yöntem A - Menu'den:**
1. Unity üst menüde **ECHOES** > **Setup Graphics for Current Scene**
2. "Success" dialog'u çıkacak
3. Tamamlandı! ✅

**Yöntem B - Otomatik (Önerilen):**
- Echoes sahnesini her açtığınızda script otomatik çalışır
- Console'da "[Auto Setup] Graphics already configured!" yazısı görürseniz zaten kurulu demektir

### Ne Yapıldı?

✅ **Global Volume** oluşturuldu ve `DefaultVolumeProfile.asset` atandı
- Post-processing efektleri artık aktif
- Karanlık, mavi-gri, desatüre görünüm
- Vignette, film grain, chromatic aberration

✅ **Fog** yapılandırıldı
- Mode: Exponential Squared
- Density: 0.08 (5-15m görüş mesafesi)
- Color: RGB(0.02, 0.02, 0.05) - Koyu mavi-siyah

✅ **Lighting** ayarlandı
- Ambient Light: RGB(0.05, 0.05, 0.08) - Çok karanlık, mavi ton
- Directional Light: Intensity 0.3, soğuk mavi-gri renk
- Soft shadows aktif

---

## 🏢 Kapalı Alanlar İçin Ekstra Korku Efektleri

Hastane içindeki kapalı koridorlar, odalar için:

### IndoorVolumeZone Kullanımı

1. **Kapalı Alan Objesi Oluşturun**
   ```
   Hierarchy > Sağ tık > Create Empty
   İsim: "IndoorZone_Corridor01" (veya başka bir isim)
   ```

2. **Script Ekleyin**
   - Inspector > Add Component > **IndoorVolumeZone**

3. **BoxCollider Ayarlayın**
   - BoxCollider otomatik eklenir (trigger modunda)
   - **Size** ve **Center**'ı kapalı alanı kaplayacak şekilde ayarlayın
   - Örnek: Koridor için `Size: (10, 3, 30)`

4. **Pozisyonlandırın**
   - Transform ile zone'u kapalı alanın ortasına yerleştirin

5. **Test Edin**
   - Play modunda karakterle zone'a girin
   - Console'da: "[IndoorVolumeZone] Player entered indoor area"
   - Ekranın daha karanlık ve kapalı hissedilmesi gerekir

### IndoorVolumeZone Efektleri

Zone içine girdiğinizde:
- ⬇️ **Extra Darkness:** -0.3 exposure (daha karanlık)
- 🔲 **Tighter Vignette:** +0.15 vignette (claustrophobia - kapalı alan korkusu)
- 🌈 **More Chromatic:** +0.1 aberration (rahatsız edici bozulma)

### Örnek Zone Yerleştirmeleri

```
Hospital01 (Hastane modeli)
├── IndoorZone_MainCorridor
│   └── BoxCollider: Size (50, 3, 5)
├── IndoorZone_Room101
│   └── BoxCollider: Size (8, 3, 8)
├── IndoorZone_Basement
│   └── BoxCollider: Size (30, 3, 30)
└── IndoorZone_Surgery
    └── BoxCollider: Size (12, 3, 15)
```

**Gizmo Renkleri:**
- 🔵 Mavi transparent/wireframe = IndoorVolumeZone
- Scene view'da görünür, game view'da görünmez

---

## 🔧 Manuel Ayarlar (Gerekirse)

Otomatik kurulum çalışmazsa:

### Global Volume Elle Ekleme

1. **GameObject Oluştur**
   ```
   Hierarchy > Sağ tık > Volume > Global Volume
   ```

2. **Volume Ayarları**
   - Is Global: ✓
   - Priority: 1
   - Profile: `Assets/Settings/DefaultVolumeProfile.asset`

### Fog Elle Ayarlama

1. **Window > Rendering > Lighting**
2. **Environment sekmesi**
   - Fog: ✓ Enabled
   - Mode: Exponential Squared
   - Density: 0.08
   - Color: Siyaha yakın mavi (koyu)

### Lighting Elle Ayarlama

1. **Window > Rendering > Lighting**
2. **Environment**
   - Source: Color
   - Ambient Color: RGB(13, 13, 20) hex: #0D0D14

3. **Directional Light seçin (Hierarchy'de)**
   - Intensity: 0.3
   - Color: Hafif mavi-gri
   - Shadows: Soft

---

## ✅ Kontrol Listesi

Graphics kurulumunu test etmek için:

- [ ] Scene'i aç, otomatik kurulum mesajını gör
- [ ] Play'e bas
- [ ] Ekran çok karanlık ve mavi-gri tonlarda mı?
- [ ] Ekran kenarları kararmış mı? (vignette)
- [ ] Film tanecikleri görünüyor mu?
- [ ] Sis 5-15m mesafede mi?
- [ ] IndoorZone'a girdiğinde daha da karanlık oluyor mu?

## 🐛 Sorun Giderme

### "Otomatik kurulum çalışmadı"
- Console'u aç (Ctrl+Shift+C)
- Hata varsa göster bana
- Manuel kurulumu dene

### "Grafik efektleri görünmüyor"
- Main Camera'da "Post Processing" enabled mi?
- Global Volume objesi var mı?
- DefaultVolumeProfile null değil mi?

### "IndoorZone çalışmıyor"
- Player'da CharacterController var mı?
- BoxCollider IsTrigger = true mi?
- Console'da "[IndoorVolumeZone]" mesajları var mı?

### "Çok karanlık, hiçbir şey görünmüyor"
- El feneri ekle (sonraki adım)
- Veya DefaultVolumeProfile > Post Exposure: -1.5 → -1.0

---

## 📊 Sahne Yapısı (Grafik Sonrası)

```
Echoes (Scene)
├── Global Volume ← YENİ! ✅
│   └── Volume (DefaultVolumeProfile)
├── Directional Light (Intensity: 0.3, cool blue)
├── Spawn (Player spawn point)
├── SinglePlayerManager
├── Hospital01 (Hastane modeli)
│   ├── IndoorZone_Corridor ← EKLE! 🏢
│   ├── IndoorZone_Rooms ← EKLE! 🏢
│   └── [Duvarlar, objeler...]
└── [Fog settings in RenderSettings] ← OTOMATİK ✅
```

---

## 🎮 Sonraki Adımlar

1. ✅ Grafik ayarları yapıldı
2. ⏭️ Player prefab'ınızı atayın (ECHOES_SCENE_SETUP.md)
3. ⏭️ IndoorVolumeZone'ları kapalı alanlara ekleyin
4. ⏭️ El feneri sistemi ekleyin (isteğe bağlı)
5. ⏭️ FlickeringLight'ları Point Light'lara ekleyin

**Tüm grafik sistemi hazır! Test edebilirsiniz.** 🎬👻
