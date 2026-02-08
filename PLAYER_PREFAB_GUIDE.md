# ECHOES - Player Prefab Kullanım Kılavuzu

## Sorun Çözüldü! ✅

**Sorun:** SinglePlayerManager scriptinde player prefab atanmış olsa bile, kod otomatik olarak basit bir capsule player yaratıyordu.

**Çözüm:** `CreatePlayer()` metodu güncellendi. Artık Inspector'da atadığınız player prefab'ı kullanılıyor!

## Nasıl Kullanılır

### 1. Karakter Prefabınızı Hazırlayın

Karakter prefabınızda şunlar olmalı:
- **CharacterController** component (hareket için)
- Varsa **Camera** (first person için)
- Varsa özel animasyon, model vb.

### 2. Sahneyi Açın

```
Unity'de: Assets/Dnk_Dev/HospitalHorrorPack/Map_Hosp1.unity
```

### 3. SinglePlayerManager'ı Bulun

Hierarchy'de `SinglePlayerManager` objesini bulun veya arayın.

### 4. Player Prefab'ı Atayın

- SinglePlayerManager'ı seçin
- Inspector'da **Player Prefab** alanını bulun
- Karakter prefabınızı buraya sürükleyip bırakın

### 5. Spawn Point Ayarlayın (Opsiyonel)

- **Spawn Point** alanına bir Transform atayabilirsiniz
- Boş bırakırsanız (0, 2, 0) pozisyonunda spawn olur

### 6. Test Edin!

- Play'e basın
- Artık kendi karakteriniz spawn olacak! 🎮

## Önemli Notlar

### Karakter Prefab Gereksinimleri

✅ **Gerekli Componentler:**
- `CharacterController` - Hareket için
- Player prefabınızda kendi camera yoksa, varsayılan camera oluşturulur

✅ **Otomatik Eklenen:**
- `PlayerController` (yoksa otomatik eklenir)
- `SinglePlayerPauseMenu` (ESC menü için)

### Fallback Sistem

Eğer Player Prefab alanı **boş** bırakılırsa:
- Eski sistem devreye girer
- Basit bir capsule player yaratılır
- Kamera ve kontroller otomatik eklenir

## Örnek Prefab Yapısı

```
YourCharacter (Prefab)
├── Model (3D model)
├── CharacterController
├── CameraHolder
│   └── Camera
│       └── AudioListener
└── PlayerController (varsa)
```

## Multiplayer için

**Not:** Multiplayer modda NetworkManager'ın kendi player prefab sistemi var.
Bu düzenleme sadece **SinglePlayer** modu için geçerlidir.

## Sorun Giderme

### "Hala eski capsule spawn oluyor"
- Player Prefab alanının boş olmadığından emin olun
- Console'da "[SinglePlayerManager] Spawning assigned player prefab" mesajını kontrol edin
- Prefab'ın doğru atandığından emin olun (null değil)

### "Karakter hareket etmiyor"
- Prefabda CharacterController var mı kontrol edin
- PlayerController script'i doğru çalışıyor mu kontrol edin

### "Kamera yok"
- Prefabınızda kamera yoksa, kod otomatik ekleyecek
- Eğer kendi kameranız varsa, MainCamera tag'i olmalı

## Debug

Console'da şu mesajları görebilirsiniz:

✅ **Başarılı:**
```
[SinglePlayerManager] Spawning assigned player prefab: YourCharacterName
[SinglePlayerManager] Player prefab spawned successfully!
```

⚠️ **Prefab Yok:**
```
[SinglePlayerManager] No player prefab assigned! Creating default player...
```

❌ **Component Eksik:**
```
[SinglePlayerManager] Player prefab doesn't have PlayerController! Adding it...
```
