# VR Street Art Gallery

A **social VR gallery experience** featuring 20 street art pieces from world-renowned artists including KAWS, Banksy, Shepard Fairey, JR, and more. Explore artwork with friends in a multiplayer environment inspired by Spatial.io.

![Unity](https://img.shields.io/badge/Unity-2022.3+-black?logo=unity)
![C#](https://img.shields.io/badge/C%23-100%25-239120?logo=csharp)
![License](https://img.shields.io/badge/License-MIT-blue)
![VR](https://img.shields.io/badge/VR-Quest%20%7C%20PC-orange)

---

## Features

### Core VR Experience
- **Teleportation & Smooth Locomotion** - Choose your preferred movement style
- **Interactive Artwork Hotspots** - Gaze or point at artwork to view info
- **Guided Tour Mode** - Automated walkthrough with narration support
- **Spatial Audio** - Immersive 3D soundscape with ambient zones
- **Comfort Settings** - Vignette, snap turn, and accessibility options

### Social VR (Spatial.io-inspired)
- **Multiplayer Networking** - Connect with others in shared gallery spaces
- **Customizable Avatars** - Body types, outfits, accessories, and colors
- **Floating Nametags** - See other players' usernames above their heads
- **Emote Wheel** - Express yourself with 8+ emotes (wave, dance, clap, etc.)
- **Spatial Voice Chat** - Talk with nearby visitors, volume fades with distance
- **Social Hub Areas** - Designated gathering spots throughout the gallery
- **Interactive Objects** - Seats, tables, photo spots, and activity zones

---

## Quick Start

1. **Clone the repository**
   ```bash
   git clone https://github.com/Hempp/street-art-gallery.git
   ```

2. **Open in Unity**
   - Unity 2022.3 LTS or newer
   - Install XR Interaction Toolkit from Package Manager

3. **Import the project**
   - Copy `exports/unity_vr_interactive/` to your Unity `Assets/` folder

4. **Build for your platform**
   - Meta Quest 2/3/Pro (Android)
   - PC VR via SteamVR or Oculus Link

---

## Project Structure

```
street-art-gallery/
├── exports/
│   └── unity_vr_interactive/      # Unity VR Project
│       ├── Scripts/
│       │   ├── GalleryManager.cs
│       │   ├── VRPlayerController.cs
│       │   ├── VRHandController.cs
│       │   └── Multiplayer/
│       │       ├── NetworkManager.cs
│       │       ├── NetworkPlayer.cs
│       │       ├── AvatarCustomization.cs
│       │       ├── EmoteWheel.cs
│       │       ├── VoiceChatManager.cs
│       │       ├── SocialInteractable.cs
│       │       └── SocialHubManager.cs
│       ├── textures/              # 20 street art images
│       ├── artwork_metadata.json
│       └── README.md              # Detailed setup guide
├── .gitignore
├── LICENSE
└── README.md
```

---

## Documentation

See the full setup guide: **[exports/unity_vr_interactive/README.md](exports/unity_vr_interactive/README.md)**

---

## Tech Stack

| Component | Technology |
|-----------|------------|
| Engine | Unity 2022.3+ |
| VR SDK | XR Interaction Toolkit 2.5+ |
| Platform | OpenXR (Quest, PC VR) |
| Networking | Photon PUN 2 / Normcore / Mirror |
| Voice | Spatial audio with push-to-talk |

---

## Featured Artists

The gallery showcases street art from renowned artists:

| Artist | Style |
|--------|-------|
| KAWS | Pop art, companion figures |
| Banksy | Political stencil art |
| Shepard Fairey | Obey, propaganda art |
| JR | Large-scale photography |
| Os Gemeos | Brazilian graffiti |
| Vhils | Carved murals |
| Felipe Pantone | Digital/analog fusion |
| And 13 more... | |

---

## License

[MIT License](LICENSE) - Feel free to use, modify, and distribute.

---

## Contributing

Contributions welcome! Please open an issue or submit a pull request.

---

*Built with NEXUS-PRIME*
