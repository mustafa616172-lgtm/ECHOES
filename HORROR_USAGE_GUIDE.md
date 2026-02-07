# ECHOES Horror Game - Kullanım Kılavuzu

## Shader'ları Kullanma

### HorrorWallShader
Duvarlar için özel shader. Kullanmak için:
1. Material oluşturun (sağ tık > Create > Material)
2. Shader'ı seçin: `Custom/HorrorWallShader`
3. Texture'ları atayın:
   - **Main Tex**: Ana duvar texture
   - **Normal Map**: Normal map (detay için)
   - **Detail Tex**: Detay texture (kir, çatlaklar)
   - **AO Map**: Ambient occlusion map

**Önemli Parametreler:**
- `Darkness`: 0.3-0.5 arası korku atmosferi için ideal
- `Wetness`: 0.3-0.5 nemli duvar efekti
- `Detail Strength`: 0.3-0.5 detay görünürlüğü

### HorrorFloorShader
Zemin için parallax mapping shader. Kullanmak için:
1. Material oluşturun
2. Shader: `Custom/HorrorFloorShader`
3. Texture'ları atayın:
   - **Main Tex**: Ana zemin texture
   - **Height Map**: Yükseklik haritası (parallax için)
   - **Grime Tex**: Kir overlay
   - **Puddle Mask**: Su birikintisi maskeleme

**Önemli Parametreler:**
- `Darkness`: 0.25-0.4 zemin karanlığı
- `Parallax`: 0.02-0.05 derinlik efekti
- `Puddle Reflection`: 0.7-0.9 su yansıması

### GhostShader
Hayalet/ruh varlıkları için. Kullanmak için:
1. Material oluşturun
2. Shader: `Custom/GhostShader`
3. Renkleri ayarlayın:
   - **Color**: Ana hayalet rengi (mavi-beyaz tonları)
   - **Glow Color**: Parlama rengi

**Önemli Parametreler:**
- `Transparency`: 0.3-0.6 hayali görünüm
- `Glow Intensity`: 2-4 ışıma gücü
- `Flicker Speed`: 3-5 titreme hızı

## Script'leri Kullanma

### FlickeringLight
Titreyenışık efekti için:
1. Bir Light objesine component ekleyin
2. Inspector'da ayarları düzenleyin:
   - **Enable Flicker**: Aktif/pasif
   - **Min/Max Intensity**: Intensity aralığı
   - **Random Flicker Chance**: Rastgele titreme olasılığı
   - **Allow Complete Shutoff**: Tamamen kapanma izni

**Kullanım senaryoları:**
- Koridorlarda eski neon lambalar
- Kırık elektrik sistemleri
- Gerilim yaratmak için rastgele kapanmalar

### HorrorAtmosphereManager
Sahne atmosferini yönetir:
1. Boş GameObject oluşturun, adını "AtmosphereManager" yapın
2. Script'i ekleyin
3. Volume referansını atayın (sahnenizde Global Volume)

**Fear Level Sistemi:**
```csharp
// Fear level artırma (0-1 arası)
atmosphereManager.IncreaseFearLevel(0.1f);

// Maksimum korku
atmosphereManager.SetFearLevel(1.0f);
```

**Atmosphere Zone'ları:**
- Farklı bölgeler için farklı atmosfer ayarları
- Zone Transform ve radius belirleyin
- Exposure ve saturation offset'leri ayarlayın

### FogController
Dinamik sis yönetimi:
1. Boş GameObject oluşturun
2. FogController script'ini ekleyin
3. Ayarları düzenleyin:
   - **Fog Mode**: ExponentialSquared (önerilen)
   - **Fog Density**: 0.08 (5-15m görüş mesafesi için)
   - **Fog Color**: (0.02, 0.02, 0.05) koyu mavi-siyah

**Dinamik Efektler:**
- `Enable Dynamic Fog`: Sis yoğunluğu değişimi
- `Random Fog Surges`: Rastgele sis patlamaları
- `Enable Color Variation`: Renk değişimi

## Hızlı Kurulum

1. **Sahneyi Hazırlayın:**
   - Map_Hosp1.unity sahnesini açın
   - Hierarchy'de sağ tık > Create Empty > "Horror Systems"

2. **Sistemleri Ekleyin:**
   ```
   Horror Systems
   ├── AtmosphereManager (HorrorAtmosphereManager)
   ├── FogController (FogController)
   └── Directional Light (FlickeringLight - opsiyonel)
   ```

3. **Global Volume Ayarı:**
   - DefaultVolumeProfile otomatik yüklü
   - Kontrol edin: Window > Rendering > Volume > Profile

4. **Işıkları Ayarlayın:**
   - Directional Light intensity: 0.3-0.5
   - Ambient light: (0.05, 0.05, 0.08)
   - Mevcut ışıklara FlickeringLight ekleyin

5. **Test:**
   - Play'e basın
   - Sahne çok karanlık olmalı
   - Vignette ve film grain görünmeli
   - Sis 5-15m mesafede olmalı

## El Feneri Ekleme (Gelecek)

Kullanıcı el feneri eklemek istediğinde:
1. Player objelerine Spotlight ekleyin
2. Parametreler:
   - Intensity: 2-3
   - Range: 10-15
   - Spot Angle: 30-45
   - Color: Hafif sarı (1, 0.95, 0.85)

3. Script ile kontrol:
```csharp
if (Input.GetKeyDown(KeyCode.F))
{
    flashlight.enabled = !flashlight.enabled;
}
```

## Performans Optimizasyonu

- Shadow Distance: 25m (optimize)
- Shadow Resolution: 4096 (yüksek kalite)
- LOD kullanın uzak objeler için
- Occlusion Culling aktif edin
- Bake etmeyi düşünün statik objeler için

## Sorun Giderme

**Çok karanlık görünüyor:**
- DefaultVolumeProfile > ColorAdjustments > Post Exposure: -1.5 → -1.0

**Shader çalışmıyor:**
- URP aktif olduğundan emin olun
- Material'in shader'ı doğru seçili mi kontrol edin
- Edit > Project Settings > Graphics > Render Pipeline Asset

**Fog görünmüyor:**
- Window > Rendering > Lighting > Environment
- Fog enabled mi kontrol edin
- FogController script enabled mi

**Post-processing yok:**
- Global Volume objesivarmıkontrol edin
- Volume component enabled mi
- Camera'da "Post Processing" aktif mi
