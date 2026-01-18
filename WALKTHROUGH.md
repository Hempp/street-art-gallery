# VR Street Art Gallery - Complete Walkthrough

---

## 1. Project Setup

### Prerequisites
```
✓ Unity 2022.3 LTS or newer
✓ XR Interaction Toolkit 2.5+
✓ OpenXR Plugin
✓ TextMeshPro
✓ VR Headset (Quest 2/3/Pro or PC VR)
```

### Installation
```bash
# Clone the repository
git clone https://github.com/Hempp/street-art-gallery.git

# Open Unity Hub → Add Project → Select folder
# Open the project in Unity 2022.3+
```

### Import Steps
```
1. Window → Package Manager
2. Install: XR Interaction Toolkit, XR Plugin Management, OpenXR
3. Copy exports/unity_vr_interactive/ → Assets/Gallery/
4. Import TextMeshPro essentials when prompted
```

---

## 2. Scene Hierarchy

```
Gallery Scene
│
├── 🎮 XR Origin
│   ├── Camera Offset
│   │   └── Main Camera (TrackedPoseDriver)
│   ├── Left Controller (VRHandController)
│   └── Right Controller (VRHandController)
│
├── 🏛️ Gallery Environment
│   ├── Floor (Teleport Area)
│   ├── Walls
│   ├── Ceiling / Skylight
│   └── Art_1 through Art_20 (ArtworkHotspot)
│
├── 🎭 Managers
│   ├── GalleryManager
│   ├── NetworkManager
│   ├── VoiceChatManager
│   ├── SocialHubManager
│   └── GalleryAudioManager
│
├── 🖼️ UI
│   ├── InfoPanel (World Space Canvas)
│   ├── VRMenu
│   ├── EmoteWheel
│   └── AvatarCustomization
│
└── 💡 Lighting
    ├── Directional Light (Sun)
    └── Area Lights (per artwork)
```

---

## 3. User Experience Walkthrough

### 🚪 Entering the Gallery

```
┌────────────────────────────────────────────────────────────┐
│                                                            │
│   You spawn at the GALLERY ENTRANCE hub                    │
│                                                            │
│   ┌─────────┐                                              │
│   │ Welcome │  ← Floating welcome sign                     │
│   └─────────┘                                              │
│                                                            │
│      👤 ← Your avatar appears                              │
│     "Guest_1234"  ← Your nametag floats above              │
│                                                            │
│   [Seats]  [Info Kiosk]  [Other Players]                   │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

### 🎨 Customizing Your Avatar

```
1. Open Menu (Menu button on controller)
2. Select "Customize Avatar"

┌─────────────────────────────────────────┐
│         AVATAR CUSTOMIZATION            │
├─────────────────────────────────────────┤
│                                         │
│  Username: [___________]                │
│                                         │
│  Body Type:  ◉ Slim  ○ Average  ○ Athletic
│                                         │
│  Outfit:     < Streetwear >             │
│                                         │
│  Accessories:                           │
│    ☑ Headphones  ☐ Hat  ☐ Glasses      │
│                                         │
│  Colors:                                │
│    Skin:   [████]                       │
│    Hair:   [████]                       │
│    Outfit: [████]                       │
│                                         │
│      ┌─────────┐                        │
│      │  👤     │  ← Live preview        │
│      │ Preview │     (rotatable)        │
│      └─────────┘                        │
│                                         │
│  [ Save ]              [ Cancel ]       │
└─────────────────────────────────────────┘
```

### 🚶 Moving Around

```
TELEPORTATION (Default)
━━━━━━━━━━━━━━━━━━━━━━
1. Point controller at floor
2. See arc trajectory + target circle
3. Press trigger to teleport

        ╭───────╮
        │   ◎   │  ← Target
        ╰───────╯
           ↑
          ╱
         ╱  ← Arc
        ╱
       👤


SMOOTH LOCOMOTION (Optional)
━━━━━━━━━━━━━━━━━━━━━━━━━━━
• Left thumbstick = Move
• Right thumbstick = Turn (snap or smooth)
```

### 🖼️ Viewing Artwork

```
┌────────────────────────────────────────────────────────────┐
│                                                            │
│   ┌──────────────────────────────────────┐                 │
│   │                                      │                 │
│   │         🎨 ARTWORK #7                │                 │
│   │                                      │                 │
│   │      [Felipe Pantone Mural]          │                 │
│   │                                      │                 │
│   │                                      │                 │
│   └──────────────────────────────────────┘                 │
│                                                            │
│              ↓ Point at artwork                            │
│                                                            │
│   ┌──────────────────────────────────────┐                 │
│   │  "Digital Native"                    │  ← Info Panel   │
│   │  Artist: Felipe Pantone              │     appears     │
│   │  Year: 2024                          │                 │
│   │                                      │                 │
│   │  A fusion of digital glitches and    │                 │
│   │  analog gradients exploring our      │                 │
│   │  relationship with technology...     │                 │
│   └──────────────────────────────────────┘                 │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

### 🗣️ Voice Chat

```
PUSH-TO-TALK MODE
━━━━━━━━━━━━━━━━━
Hold Left Grip → Speak → Release

     👤 You                    👤 Other Player
    [🎤]  ──── audio ────►    [🔊]

    Distance affects volume:
    • 0-3m = Full volume
    • 3-10m = Fading
    • 10m+ = Inaudible


VOICE ACTIVATION MODE
━━━━━━━━━━━━━━━━━━━━━
Just speak! Auto-detects voice.

    👤 Speaking indicator
   [🎤 ●]  ← Red dot when transmitting
```

### 😄 Using Emotes

```
1. Hold Y button (left) or B button (right)
2. Emote wheel appears

              👋 Wave
               │
      🤔 Think ┼──── 💃 Dance
              ╱╲
             ╱  ╲
     👍 Like     👏 Clap
            ╲  ╱
             ╲╱
      😂 Laugh ┼──── ❤️ Love
               │
              🔥 Fire

3. Push thumbstick toward emote
4. Release button to trigger

Your avatar performs animation + emoji floats above head!
```

### 🪑 Social Interactions

```
SITTING
━━━━━━━
1. Approach a seat
2. See prompt: "Press A to Sit"
3. Press A → Avatar sits down
4. Press B to stand up

    ┌─────┐
    │ 🪑  │  "Sit Here"
    └─────┘
       👤


PHOTO SPOT
━━━━━━━━━━
1. Gather friends at 📸 marker
2. One person triggers photo
3. Countdown: 3... 2... 1...
4. 📷 Flash! Screenshot saved

    ┌─────────────────┐
    │  📸 PHOTO SPOT  │
    │                 │
    │  👤  👤  👤     │
    │   Ready? [A]    │
    └─────────────────┘


SOCIAL TABLE
━━━━━━━━━━━━
• Multiple seats around table
• Great for group discussions

    ┌─────────────────┐
    │   👤       👤   │
    │     ┌───┐       │
    │     │ 🪑 │       │
    │     └───┘       │
    │   👤       👤   │
    └─────────────────┘
```

### 🏛️ Gallery Hub Areas

```
┌─────────────────────────────────────────────────────────────────┐
│                        GALLERY MAP                              │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   ┌─────────────┐     ┌─────────────┐     ┌─────────────┐       │
│   │   ARTIST    │     │   CENTRAL   │     │  CREATIVE   │       │
│   │   LOUNGE    │     │   GALLERY   │     │   SPACE     │       │
│   │             │     │             │     │             │       │
│   │  🛋️ Sofas   │     │  🎨 Main    │     │  🎭 Workshop │       │
│   │  ☕ Relaxed │     │     Hub     │     │     Area    │       │
│   └─────────────┘     └──────┬──────┘     └─────────────┘       │
│                              │                                  │
│   ┌─────────────┐            │            ┌─────────────┐       │
│   │ DISCUSSION  │◄───────────┴───────────►│  GALLERY    │       │
│   │   CORNER    │                         │  ENTRANCE   │       │
│   │             │                         │             │       │
│   │  💬 Debate  │                         │  🚪 Spawn   │       │
│   │     Zone    │                         │    Point    │       │
│   └─────────────┘                         └─────────────┘       │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘

Each hub has:
• Seating arrangements
• Activity-specific objects
• Proximity voice boost (hear others better in hubs)
```

### 🎧 Guided Tour

```
1. Open Menu → Start Tour
2. Tour guide appears

┌─────────────────────────────────────────┐
│         🎧 GUIDED TOUR                  │
├─────────────────────────────────────────┤
│                                         │
│  Stop 1 of 20: "Urban Dreams" by KAWS   │
│                                         │
│  🔊 "Welcome to the VR Street Art       │
│     Gallery. Our first piece is by      │
│     KAWS, known for his iconic..."      │
│                                         │
│  ────────────●─────────── 0:45 / 2:30   │
│                                         │
│  [ ◀ Prev ]  [ ⏸ Pause ]  [ Next ▶ ]    │
│                                         │
│  ☐ Auto-advance to next artwork         │
│                                         │
└─────────────────────────────────────────┘

• Auto-teleports you to each artwork
• Narration plays for each piece
• Manual or auto-advance modes
```

---

## 4. Code Architecture

```
                    ┌─────────────────┐
                    │  GalleryManager │ ← Main coordinator
                    │    (Singleton)  │
                    └────────┬────────┘
                             │
         ┌───────────────────┼───────────────────┐
         │                   │                   │
         ▼                   ▼                   ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│VRPlayerController│ │ NetworkManager  │ │GalleryAudioManager│
│  - Locomotion    │ │  - Connections  │ │  - Spatial Audio│
│  - Teleport      │ │  - Sync         │ │  - Ambience     │
└─────────────────┘ └────────┬────────┘ └─────────────────┘
                             │
              ┌──────────────┼──────────────┐
              │              │              │
              ▼              ▼              ▼
      ┌─────────────┐ ┌─────────────┐ ┌─────────────┐
      │NetworkPlayer│ │VoiceChatMgr │ │SocialHubMgr │
      │ - Avatar    │ │ - PTT       │ │ - Hubs      │
      │ - Nametag   │ │ - Spatial   │ │ - Objects   │
      │ - Emotes    │ └─────────────┘ └─────────────┘
      └─────────────┘
```

---

## 5. Multiplayer Flow

```
┌──────────────────────────────────────────────────────────────┐
│                     MULTIPLAYER FLOW                         │
└──────────────────────────────────────────────────────────────┘

   Player A                Server              Player B
      │                      │                    │
      │── Connect ──────────►│                    │
      │                      │◄────── Connect ────│
      │                      │                    │
      │── Join "Gallery1" ──►│                    │
      │                      │◄── Join "Gallery1"─│
      │                      │                    │
      │◄─ Spawn PlayerB ─────│───► Spawn PlayerA ─│
      │                      │                    │
      │── Move (x,y,z) ─────►│───► Update Pos ───►│
      │                      │                    │
      │── Emote "wave" ─────►│───► Play Emote ───►│
      │                      │                    │
      │── Voice Data ───────►│───► Audio Stream ─►│
      │                      │                    │
      └──────────────────────┴────────────────────┘
```

---

## 6. Controls Reference

```
┌─────────────────────────────────────────────────────────────┐
│                    VR CONTROLS                              │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  LEFT CONTROLLER              RIGHT CONTROLLER              │
│  ════════════════             ═════════════════             │
│                                                             │
│  [Thumbstick]                 [Thumbstick]                  │
│   • Move forward/back          • Snap turn left/right       │
│   • Strafe left/right          • (or smooth turn)           │
│                                                             │
│  [Trigger]                    [Trigger]                     │
│   • Teleport                   • Select / Interact          │
│                                                             │
│  [Grip]                       [Grip]                        │
│   • Push-to-talk               • Grab objects               │
│                                                             │
│  [Y Button]                   [B Button]                    │
│   • Open emote wheel           • Open emote wheel           │
│                                                             │
│  [X Button]                   [A Button]                    │
│   • Toggle menu                • Confirm / Sit              │
│                                                             │
│  [Menu Button]                                              │
│   • Main menu                                               │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 7. The 20 Artworks

| #  | Title               | Artist           | Year |
|----|---------------------|------------------|------|
| 1  | Urban Dreams        | KAWS             | 2023 |
| 2  | Concrete Jungle     | Banksy           | 2022 |
| 3  | Neon Nights         | Shepard Fairey   | 2023 |
| 4  | Street Symphony     | JR               | 2021 |
| 5  | Color Revolution    | Os Gemeos        | 2022 |
| 6  | Urban Decay         | Vhils            | 2023 |
| 7  | Digital Native      | Felipe Pantone   | 2024 |
| 8  | Wild Style          | Seen             | 2022 |
| 9  | Future Past         | Futura           | 2023 |
| 10 | Street Wisdom       | Retna            | 2022 |
| 11 | Pop Underground     | D*Face           | 2023 |
| 12 | Nature Reclaims     | ROA              | 2021 |
| 13 | Light & Shadow      | C215             | 2024 |
| 14 | Urban Mythology     | Aryz             | 2023 |
| 15 | Street Canvas       | Okuda            | 2022 |
| 16 | Rebel Art           | Swoon            | 2023 |
| 17 | City Pulse          | Kobra            | 2024 |
| 18 | Raw Expression      | Blu              | 2022 |
| 19 | Urban Poetry        | Faith47          | 2023 |
| 20 | Street Legacy       | Crash            | 2024 |

---

## 8. Build & Deploy

### Quest Build
```
1. File → Build Settings → Android
2. Player Settings:
   • Texture Compression: ASTC
   • Scripting Backend: IL2CPP
   • Target: ARM64
3. XR Plug-in Management → OpenXR → Meta Quest
4. Build and Run
```

### PC VR Build
```
1. File → Build Settings → Windows
2. XR Plug-in Management → OpenXR
3. Enable SteamVR / Oculus features
4. Build
```

---

## 9. Troubleshooting

| Issue | Solution |
|-------|----------|
| Controllers not detected | Check XR Plugin Management settings |
| Teleport not working | Ensure floor has collider on Teleport layer |
| No audio | Check GalleryAudioManager references |
| Can't see other players | Verify NetworkManager connection settings |
| Voice chat not working | Check microphone permissions |
| Low FPS on Quest | Reduce texture quality, bake lighting |

---

## 10. Tips for Best Experience

1. **Comfort First** - Start with teleportation, enable smooth locomotion gradually
2. **Socialize** - Visit hub areas to meet other gallery visitors
3. **Take Photos** - Use photo spots to capture memories with friends
4. **Listen** - Try the guided tour for artist insights
5. **Express Yourself** - Use emotes to react to artwork and interact with others

---

**Enjoy exploring the gallery!** 🎨🥽

---

*Built with NEXUS-PRIME | [GitHub Repository](https://github.com/Hempp/street-art-gallery)*
