# ECHOES Sahnesi - Kurulum Kılavuzu

## ✅ Yapılanlar

1. **SinglePlayerManager** GameObject'i Echoes sahnesine eklendi
2. **SinglePlayerManager** script component'i eklendi
3. Sahne kaydedildi

## 📋 Unity'de Yapmanız Gerekenler

### Adım 1: Unity'de Echoes Sahnesini Açın

```
Unity Editor'de: Assets/Scenes/Echoes.unity
```

### Adım 2: SinglePlayerManager Ayarlarını Yapılandırın

1. **Hierarchy'de `SinglePlayerManager` objesini seçin**
   - Sol taraftaki Hierarchy panelinde "SinglePlayerManager" 

isimli objeni bulun

2. **Inspector'da Spawn Point'i Atayın**
   - Inspector panelinde **Single Player Manager (Script)** component'ini bulun
   - **Spawn Point** alanını bulun
   - Hierarchy'den **"Spawn"** objesini bu alana sürükleyin
   
   > Spawn objesi şu pozisyonda:
   > - Position: (-145.55, -14.51, -110.91)
   > - Bu hastane haritasının başlangıç noktasıdır

3. **Player Prefab'ınızı Atayın**
   - **Player Prefab** alanını bulun
   - Project panelinden **kendi karakter prefabınızı** buraya sürükleyin
   
   > ⚠️ **ÖNEMLİ:** Player Prefab alanını boş bırakırsanız, otomatik olarak basit bir capsule karakter oluşturulur.

### Adım 3: Test Edin

1. **Sahneyi oynat**
   - Play butonuna basın (veya Ctrl+P)
   
2. **Kontrol edin**
   - Kendi karakteriniz spawn olmalı
   - Spawn noktasında başlamalı
   - Console'da şu mesajı görmelisiniz:
     ```
     [SinglePlayerManager] Spawning assigned player prefab: YourCharacterName
     [SinglePlayerManager] Player prefab spawned successfully!
     ```

### Adım 4: Korku Atmosferini Ekleyin (Opsiyonel)

Korku oyunu atmosferi için ek sistemler:

1. **HorrorSystems GameObject'i Oluşturun**
   ```
   Hierarchy > Sağ tık > Create Empty
   İsim: "HorrorSystems"
   ```

2. **Fog Controller Ekleyin**
   - HorrorSystems'i seçin
   - Inspector > Add Component > FogController
   - Ayarlar:
     - Fog Density: 0.08
     - Fog Color: RGB(0.02, 0.02, 0.05)
     - Start Distance: 5
     - End Distance: 15

3. **Atmosphere Manager Ekleyin** (İsteğe bağlı)
   - HorrorSystems'i seçin
   - Inspector > Add Component > HorrorAtmosphereManager
   - Global Volume'ü atayın (sahne içinde bulunmalı)

4. **Işıklara Titreme Ekleyin** (İsteğe bağlı)
   - Sahne içindeki Point Light veya Spot Light'ları seçin
   - Add Component > FlickeringLight
   - Enable Flicker: ✓
   - Flicker Speed: 0.1
   - Random Flicker Chance: 0.3

## 🎯 Spawn Noktası Bilgileri

**Mevcut Spawn Noktası:**
- GameObject Name: "Spawn"
- Position: (-145.55, -14.51, -110.91)
- Instance ID: 88574

> Bu pozisyon hastane haritasının başlangıç bölgesindedir.

**Spawn Noktasını Değiştirmek İsterseniz:**

1. Sahne içinde karakterinizin başlamasını istediğiniz yere boş bir GameObject ekleyin
2. GameObject'e istediğiniz pozisyonu verin
3. SinglePlayerManager > Spawn Point alanına bu yeni GameObject'i atayın

## 🔧 Sorun Giderme

### "Player Prefab atadım ama eski capsule spawn oluyor"

**Çözüm:**
1. SinglePlayerManager Inspector'da Player Prefab alanının dolu olduğunu kontrol edin
2. Console'da şu uyarıyı görüyorsanız prefab atanmamış:
   ```
   [SinglePlayerManager] No player prefab assigned! Creating default player...
   ```
3. Prefab'ı tekrar atayın ve sahneyi kaydedin (Ctrl+S)

### "Karakterim yanlış yerde spawn oluyor"

**Çözüm:**
1. Spawn Point'in doğru atandığını kontrol edin
2. Veya Default Spawn Position'ı manuel ayarlayın:
   - SinglePlayerManager > Default Spawn Position
   - X, Y, Z değerlerini istediğiniz pozisyona ayarlayın

### "Oyun başladığında hiçbir şey olmuyor"

**Çözüm:**
1. Console'u kontrol edin (Ctrl+Shift+C)
2. GameModeManager hatası varsa:
   - MainMenu sahnesinden oyuna geçiş yapmayı deneyin
   - Veya doğrudan Echoes sahnesini Play yapın (Single Player default)

## 📊 Sahne Yapısı

```
Echoes (Scene)
├── Directional Light (DoğalışıK)
├── Spawn (Başlangıç noktası) ← BURAYI KULLAN
├── Hospital01 (Hastane modeli - Prefab)
│   ├── NavMeshSurface (AI navigation için)
│   └── [Hastane objeleri...]
└── SinglePlayerManager (Yeni eklendi!) ✅
    └── SinglePlayerManager (Script)
        ├── Player Prefab: [SİZİN PREFABINIZ]
        └── Spawn Point: [Spawn GameObject]
```

## ✨ Sonuç

Artık Echoes sahnesi hazır! 

**Yapmanz gerekenler:**
1. ✅ SinglePlayerManager > Spawn Point = "Spawn" objesini atayın
2. ✅ SinglePlayerManager > Player Prefab = Kendi karakterinizi atayın
3. ✅ Play'e basıp test edin!

**Opsiyonel eklentiler:**
- FogController (Sis efekti)
- HorrorAtmosphereManager (Dinamik atmosfer)
- FlickeringLight (Titreyenışıklar)

Tüm korku atmosfer ayarları daha önce yapılandırıldı:
- Post-processing: Karanlık, mavi-gri, desatüre ✅
- Custom shaders: Duvar, zemin, hayalet ✅  
- URP settings: Optimize shadow, HDR ✅

**Hemen test edebilirsiniz!** 🎮👻
