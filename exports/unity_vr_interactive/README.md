# VR Street Art Gallery - Unity Integration Guide

A complete **social VR gallery experience** featuring 20 street art pieces from world-renowned artists including KAWS, Banksy, Shepard Fairey, JR, and more. Explore artwork with friends in a multiplayer environment inspired by Spatial.io.

## Features

### Core VR Experience
- **Teleportation & Smooth Locomotion** - Choose your preferred movement style
- **Interactive Artwork Hotspots** - Gaze or point at artwork to view info
- **Guided Tour Mode** - Automated walkthrough with narration support
- **Spatial Audio** - Immersive 3D soundscape with ambient zones
- **Comfort Settings** - Vignette, snap turn, and accessibility options
- **Quest & PC VR Support** - Optimized for Meta Quest and PC VR headsets

### Social VR Features (NEW)
- **Multiplayer Networking** - Connect with others in shared gallery spaces
- **Customizable Avatars** - Body types, outfits, accessories, and colors
- **Floating Nametags** - See other players' usernames above their heads
- **Emote Wheel** - Express yourself with 8+ emotes (wave, dance, clap, etc.)
- **Spatial Voice Chat** - Talk with nearby visitors, volume fades with distance
- **Social Hub Areas** - Designated gathering spots throughout the gallery
- **Interactive Objects** - Seats, tables, photo spots, and activity zones

---

## Quick Start

### 1. Create Unity Project

```
Unity Hub > New Project > 3D (URP) or VR Template
Unity Version: 2022.3 LTS or newer recommended
```

### 2. Install Required Packages

Open **Window > Package Manager** and install:

| Package | Purpose |
|---------|---------|
| XR Interaction Toolkit | VR interactions |
| XR Plugin Management | Headset support |
| OpenXR Plugin | Cross-platform VR |
| TextMeshPro | UI text rendering |
| Universal RP | Rendering pipeline |

### 3. Import Gallery Assets

1. Copy entire `unity_vr_interactive/` folder to `Assets/Gallery/`
2. Import `getty_gallery_interactive.glb` model
3. Import `textures/` folder (20 graffiti images)

### 4. Scene Setup

```
1. Create new scene: File > New Scene > Basic (URP)
2. Delete default objects (keep Directional Light)
3. Drag gallery model into scene at (0, 0, 0)
4. Add XR Origin: GameObject > XR > XR Origin (VR)
5. Create empty "GalleryManager" and attach GalleryManager.cs
```

---

## Scripts Overview

### Core VR Scripts

| Script | Purpose |
|--------|---------|
| `GalleryManager.cs` | Main coordinator, initialization, menus |
| `VRPlayerController.cs` | Teleportation, smooth locomotion, movement |
| `VRHandController.cs` | Pointer, grip, haptics, interactions |
| `ArtworkHotspot.cs` | Artwork interaction triggers, info display |
| `ArtworkData.cs` | Artwork database singleton |
| `InfoPanelController.cs` | Info panel UI with fade animations |
| `GalleryAudioManager.cs` | Ambient, music, spatial audio |
| `GalleryTourManager.cs` | Guided tour with waypoints |
| `VRComfortSettings.cs` | Vignette, snap turn, accessibility |

### Multiplayer Scripts

| Script | Purpose |
|--------|---------|
| `NetworkManager.cs` | Connection management, room system, player spawning |
| `NetworkPlayer.cs` | Remote player avatar, nametag, chat bubbles, emotes |
| `AvatarCustomization.cs` | Avatar appearance editor with VR UI |
| `EmoteWheel.cs` | Radial emote selector with thumbstick input |
| `VoiceChatManager.cs` | Spatial voice chat with push-to-talk |
| `SocialInteractable.cs` | Base class for interactive social objects |
| `SocialHubManager.cs` | Hub area management for social gatherings |

---

## Detailed Setup

### XR Origin Configuration

```
XR Origin (GameObject)
├── Camera Offset
│   └── Main Camera
│       └── Add: TrackedPoseDriver (Input System)
├── Left Controller
│   └── Add: VRHandController (Left)
│       └── Add: XR Ray Interactor
└── Right Controller
    └── Add: VRHandController (Right)
        └── Add: XR Ray Interactor
```

**Inspector Settings:**
- Tracking Origin Mode: `Floor`
- Camera Y Offset: `0` (floor-level tracking)

### Player Controller Setup

1. Create empty "VRPlayer" as child of XR Origin
2. Attach `VRPlayerController.cs`
3. Configure in Inspector:

```
Movement Settings:
- Smooth Move Speed: 2
- Snap Turn Angle: 45
- Use Snap Turn: true
- Use Smooth Locomotion: false

Teleportation:
- Teleport Mask: Floor layer
- Max Teleport Distance: 15

References:
- XR Origin: (assign)
- Main Camera: (assign)
- Left/Right Hand: (assign controllers)
```

### Artwork Hotspots

Hotspots are auto-created by `GalleryManager`, but you can manually configure:

1. Select each artwork mesh (Art_1 through Art_20)
2. Add `ArtworkHotspot.cs` component
3. Set Artwork ID (1-20)
4. Add Box Collider if missing

**Optional Customization:**
```
Interaction Settings:
- Activation Distance: 3
- Gaze Activation Time: 1.5 (for gaze-based selection)
- Require Gaze: false (for instant selection)
- Highlight On Hover: true
```

### Info Panel UI

1. Create World Space Canvas
2. Add TextMeshPro elements:
   - Title (TMP)
   - Artist (TMP)
   - Year (TMP)
   - Description (TMP)
3. Create empty "InfoPanelController" and attach script
4. Assign UI references in Inspector

### Audio Setup

1. Create empty "AudioManager"
2. Attach `GalleryAudioManager.cs`
3. Import audio files from `/audio/` folder:
   - `ambient_gallery.mp3` - Background ambience
   - `hiphop_beat.wav` - Optional background music

**Audio Mixer (Optional):**
```
Master
├── Music (background tracks)
├── SFX (interactions, footsteps)
└── Ambient (environmental sounds)
```

### Guided Tour Setup

1. Create empty "TourManager"
2. Attach `GalleryTourManager.cs`
3. Create viewpoints for each artwork:

```csharp
// Auto-generated, but can customize:
Tour Stops:
- Stop 1: Art_1 viewpoint at (x, 1.6, z) looking at artwork
- Stop 2: Art_2 viewpoint...
// ... continues for all 20 artworks
```

### Comfort Settings

1. Create empty "ComfortSettings"
2. Attach `VRComfortSettings.cs`
3. Add Post Processing Volume to scene
4. Configure presets in Inspector:

```
Presets:
- Maximum Comfort: Snap turn, vignette, teleport only
- Balanced: Snap turn, mild vignette
- Immersive: Smooth turn, smooth locomotion, no vignette
```

---

## Multiplayer Setup

### Networking Framework

The multiplayer system is designed to work with popular networking solutions. Choose one:

| Framework | Best For |
|-----------|----------|
| **Photon PUN 2** | Easy setup, hosted servers |
| **Normcore** | Built for VR, spatial audio |
| **Mirror** | Self-hosted, full control |
| **Netcode for GameObjects** | Unity-native solution |

### Network Manager Setup

1. Create empty "NetworkManager"
2. Attach `NetworkManager.cs`
3. Configure in Inspector:

```
Connection Settings:
- Server URL: (your server address)
- Max Players Per Room: 20
- Player Prefab: (NetworkPlayer prefab)

Room Settings:
- Default Room: "MainGallery"
- Auto Join On Connect: true
```

### Network Player Prefab

Create a prefab with the following structure:

```
NetworkPlayer (Prefab)
├── AvatarRoot
│   ├── Body (mesh)
│   ├── Head (tracked)
│   └── Hands (tracked)
├── NametagCanvas (World Space)
│   └── UsernameText (TMP)
├── ChatBubbleCanvas
│   └── MessageText (TMP)
├── EmoteDisplay
│   └── EmoteParticles
└── VoiceIndicator
    └── SpeakingIcon
```

### Avatar Customization Setup

1. Create empty "AvatarCustomization"
2. Attach `AvatarCustomization.cs`
3. Create customization UI (auto-generated if not provided):

```
Customization Options:
- Body Types: 5 presets (slim, average, athletic, curvy, stocky)
- Outfits: 10+ styles (casual, formal, streetwear, artist, etc.)
- Accessories: 8 types (hat, glasses, headphones, etc.)
- Colors: Skin tone, hair, outfit primary/secondary

Avatar Preview:
- Rotatable 3D preview
- Real-time color updates
- Save/Load configurations
```

### Emote Wheel Setup

1. Create empty "EmoteWheel"
2. Attach `EmoteWheel.cs`
3. Configure emotes:

```
Default Emotes (8):
- 👋 Wave     - 💃 Dance
- 👏 Clap     - ❤️ Love
- 🔥 Fire     - 😂 Laugh
- 🤔 Think    - 👍 Like

Input Mapping:
- Open Wheel: Hold Y/B button
- Select: Thumbstick direction
- Confirm: Release button or trigger
```

### Voice Chat Setup

1. Create empty "VoiceChatManager"
2. Attach `VoiceChatManager.cs`
3. Configure settings:

```
Voice Settings:
- Mode: Push-to-Talk or Voice Activated
- Push-to-Talk Button: Left grip
- Voice Threshold: 0.02 (for voice activation)
- Max Distance: 15m (spatial falloff)
- Microphone: (auto-detected)

Spatial Audio:
- 3D Sound: Enabled
- Rolloff: Logarithmic
- Min Distance: 1m
- Max Distance: 15m
```

### Social Hub Manager Setup

1. Create empty "SocialHubManager"
2. Attach `SocialHubManager.cs`
3. Default hubs are created automatically:

```
Default Hub Areas:
1. Gallery Entrance - Welcome area with seating
2. Central Gallery - Main gathering point
3. Artist Lounge - Relaxed seating area
4. Discussion Corner - Conversation circle
5. Creative Space - Activity zone for workshops

Hub Types:
- GatheringPoint: General socializing
- Lounge: Relaxed seating
- Discussion: Structured conversation
- Activity: Games, workshops
- Performance: Live events
- Quiet: Contemplation zones
```

### Social Interactables

The following interactive objects are available:

| Type | Description | Capacity |
|------|-------------|----------|
| `SocialSeat` | Single chair, bench seat | 1 player |
| `SocialTable` | Table with multiple seats | 2-8 players |
| `PhotoSpot` | Group photo with countdown | 2-6 players |
| `ActivityZone` | Discussion, game, workshop | 4-20 players |
| `ViewingBench` | Bench facing artwork | 2-4 players |

**Adding Custom Interactables:**

```csharp
// Create a custom social seat
var seat = new GameObject("CustomSeat");
var social = seat.AddComponent<SocialSeat>();
social.interactionPrompt = "Sit Here";
social.maxOccupants = 1;
```

---

## Lighting Setup

### Recommended Configuration

```
Directional Light (Sun through skylight):
- Rotation: (50, 30, 0)
- Intensity: 1.5
- Color: Warm white #FFF8F0
- Shadows: Soft Shadows

Area Lights (per artwork):
- Type: Rectangle
- Intensity: 300
- Range: 5m
- Position: Above each artwork panel
```

### Global Illumination

1. Open **Window > Rendering > Lighting**
2. Enable **Baked Global Illumination**
3. Set Lightmap Resolution: 20-40 texels/unit
4. Click **Generate Lighting**

---

## Platform Build Settings

### Meta Quest 2/3/Pro

```
Build Settings:
- Platform: Android
- Texture Compression: ASTC
- Scripting Backend: IL2CPP
- Target Architecture: ARM64

XR Settings:
- OpenXR Runtime
- Meta Quest feature group enabled

Performance Targets:
- 72/90 FPS
- Draw calls < 100
- Triangles < 500k
```

### PC VR (SteamVR/Oculus Link)

```
Build Settings:
- Platform: Windows
- Architecture: x86_64
- Scripting Backend: IL2CPP

XR Settings:
- OpenXR Runtime
- Enable SteamVR / Oculus feature groups
```

---

## Artwork Metadata

All artwork info is stored in `artwork_metadata.json`:

| ID | Title | Artist | Year |
|----|-------|--------|------|
| 1 | Urban Dreams | KAWS | 2023 |
| 2 | Concrete Jungle | Banksy | 2022 |
| 3 | Neon Nights | Shepard Fairey | 2023 |
| 4 | Street Symphony | JR | 2021 |
| 5 | Color Revolution | Os Gemeos | 2022 |
| 6 | Urban Decay | Vhils | 2023 |
| 7 | Digital Native | Felipe Pantone | 2024 |
| 8 | Wild Style | Seen | 2022 |
| 9 | Future Past | Futura | 2023 |
| 10 | Street Wisdom | Retna | 2022 |
| 11 | Pop Underground | D*Face | 2023 |
| 12 | Nature Reclaims | ROA | 2021 |
| 13 | Light & Shadow | C215 | 2024 |
| 14 | Urban Mythology | Aryz | 2023 |
| 15 | Street Canvas | Okuda | 2022 |
| 16 | Rebel Art | Swoon | 2023 |
| 17 | City Pulse | Kobra | 2024 |
| 18 | Raw Expression | Blu | 2022 |
| 19 | Urban Poetry | Faith47 | 2023 |
| 20 | Street Legacy | Crash | 2024 |

---

## Texture Assignments

```
Art_1  → graffiti_1.jpg   (KAWS)
Art_2  → graffiti_2.jpg   (Banksy)
Art_3  → graffiti_3.jpg   (Shepard Fairey)
...
Art_20 → graffiti_20.jpg  (Crash)
```

---

## Performance Optimization

### Mobile VR (Quest)

- [x] Enable Single Pass Instanced rendering
- [x] Enable Foveated Rendering (Fixed or Dynamic)
- [x] Bake lightmaps for all static objects
- [x] Use LODs for complex meshes
- [x] Texture compression: ASTC 6x6
- [x] Max texture size: 2048x2048

### Occlusion Culling

1. **Window > Rendering > Occlusion Culling**
2. Mark gallery walls as Occluders
3. Mark artworks as Occludees
4. Bake occlusion data

### Draw Call Reduction

- Combine meshes where possible
- Use GPU instancing for repeated elements
- Atlas textures for small UI elements

---

## Troubleshooting

### Controllers Not Detected

```csharp
// Add to VRPlayerController.Start():
InputDevices.deviceConnected += OnDeviceConnected;

void OnDeviceConnected(InputDevice device) {
    GetControllers();
}
```

### Teleport Not Working

1. Check Floor has collider
2. Ensure Floor is on Teleport layer
3. Verify Teleport Mask includes Floor layer

### Info Panels Not Showing

1. Verify ArtworkData has JSON assigned
2. Check Hotspot has correct Artwork ID
3. Ensure InfoPanelController is in scene

### Performance Issues on Quest

1. Reduce lightmap resolution
2. Disable real-time shadows
3. Lower texture quality
4. Simplify materials (remove normal maps)

---

## File Structure

```
unity_vr_interactive/
├── Scripts/
│   ├── GalleryManager.cs
│   ├── VRPlayerController.cs
│   ├── VRHandController.cs
│   ├── VRComfortSettings.cs
│   ├── VRMenuController.cs
│   ├── ArtworkHotspot.cs
│   ├── ArtworkData.cs
│   ├── InfoPanelController.cs
│   ├── GalleryAudioManager.cs
│   ├── GalleryTourManager.cs
│   └── Multiplayer/
│       ├── NetworkManager.cs
│       ├── NetworkPlayer.cs
│       ├── AvatarCustomization.cs
│       ├── EmoteWheel.cs
│       ├── VoiceChatManager.cs
│       ├── SocialInteractable.cs
│       └── SocialHubManager.cs
├── textures/
│   ├── graffiti_1.jpg
│   ├── graffiti_2.jpg
│   └── ... (20 total)
├── artwork_metadata.json
├── getty_gallery_interactive.fbx
├── getty_gallery_interactive.glb
└── README.md
```

---

## Gallery Dimensions

- **Width:** 24m
- **Depth:** 20m
- **Height:** 6m
- **Artwork Height:** 2.5m from floor
- **Viewing Distance:** 2-3m optimal

---

## Credits

Gallery design, VR integration, and social features created with NEXUS-PRIME.

Street art textures are placeholders - replace with licensed artwork for production use.

Social VR features inspired by Spatial.io and VRChat.

---

## Changelog

### v2.0.0 - Social VR Update
- Added multiplayer networking system
- Added customizable avatars with body types, outfits, and accessories
- Added floating nametags above player heads
- Added radial emote wheel with 8 default emotes
- Added spatial voice chat with push-to-talk
- Added social hub areas for gatherings
- Added interactive social objects (seats, tables, photo spots)

### v1.0.0 - Initial Release
- Core VR locomotion and interactions
- Artwork hotspots with info panels
- Guided tour system
- Spatial audio
- Comfort settings

---

## Support

For issues or feature requests, contact the development team.

**Version:** 2.0.0
**Unity:** 2022.3+
**XR Toolkit:** 2.5+
**Networking:** Photon PUN 2 / Normcore / Mirror (choose one)
