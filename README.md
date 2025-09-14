# ScrapLine - Mobile Factory Automation Game

[![Unity Version](https://img.shields.io/badge/Unity-6000.2.3f1%20LTS-blue.svg)](https://unity3d.com/get-unity/download)
[![Platform](https://img.shields.io/badge/Platform-Mobile%20First-green.svg)](https://unity3d.com/)

ScrapLine is a mobile-first factory automation game built in Unity 6 LTS where players create efficient conveyor belt systems to process scrap materials. Design and optimize your factory layout using touch controls to transform aluminum cans into valuable shredded aluminum.

## 🎮 Game Overview

Build the ultimate recycling factory! Place machines, design conveyor routes, and process materials efficiently:

- **🏭 Factory Building**: Create complex production lines with conveyors and processing machines
- **📱 Mobile Optimized**: Touch-friendly controls designed for smartphones and tablets  
- **⚙️ Machine System**: Spawners generate materials, shredders process them, sellers complete the cycle
- **🔄 Conveyor Networks**: Smart directional belt system that moves items smoothly
- **💾 Save & Load**: Persistent factory designs with JSON-based save system
- **📈 Upgrades**: Improve machine speed and capacity with earned resources

### Core Gameplay Loop

1. **Place Spawners** on the bottom edge to generate aluminum cans
2. **Build Conveyor Networks** to transport materials through your factory
3. **Install Shredders** to process cans into valuable shredded aluminum
4. **Position Sellers** on the top edge to complete the production cycle
5. **Upgrade Machines** to increase processing speed and efficiency
6. **Optimize Layout** for maximum throughput and profit

## 🛠️ Technical Details

### System Requirements

- **Unity Version**: 6000.2.3f1 (Unity 6 LTS) - **Required**
- **Platforms**: iOS, Android (primary), Windows, macOS, Linux, WebGL
- **Graphics**: DirectX 11/OpenGL 3.3+/Metal/Vulkan
- **RAM**: 2GB minimum, 4GB recommended
- **Storage**: 8GB available space for development

### Project Structure

```
ScrapLine/
├── Assets/
│   ├── Scenes/
│   │   └── MobileGridScene.unity    # Main gameplay scene
│   ├── Scripts/                     # C# game logic
│   │   ├── Game/GameManager.cs      # Core game systems
│   │   ├── Grid/UIGridManager.cs    # Grid management
│   │   ├── Machines/                # Machine behaviors
│   │   ├── Conveyors/              # Belt system
│   │   └── UI/                     # User interface
│   ├── Resources/                   # Game data
│   │   ├── items.json              # Item definitions
│   │   ├── machines.json           # Machine configs
│   │   └── recipes.json            # Processing rules
│   ├── Prefabs/                    # Reusable objects
│   └── Sprites/                    # 2D graphics
├── ProjectSettings/                # Unity configuration
└── Packages/                       # Dependencies
```

### Key Components

#### Machines (`machines.json`)
- **Conveyor**: Moves items in specified directions
- **Spawner**: Generates items (max 1-3 with upgrades)
- **Shredder**: Processes cans → shredded aluminum
- **Seller**: Removes items and awards currency

#### Items (`items.json`)
- **Aluminum Can**: Raw material spawned by machines
- **Shredded Aluminum**: Processed valuable output

#### Processing (`recipes.json`)
- **Can → Shredded Aluminum**: Primary transformation recipe

## 🚀 Development Setup

### Prerequisites

1. **Install Unity Hub**
   ```bash
   # Download from https://unity.com/download
   # Or via command line (Linux):
   wget https://public-cdn.cloud.unity3d.com/hub/prod/UnityHub.AppImage
   chmod +x UnityHub.AppImage
   ```

2. **Install Unity 6000.2.3f1 LTS**
   ```bash
   # Via Unity Hub
   ./UnityHub.AppImage --headless install --version 6000.2.3f1 --changeset c7638eb16d91
   ```

### Opening the Project

1. Clone this repository
2. Open Unity Hub
3. Select "Open" and navigate to the ScrapLine folder
4. Unity will automatically resolve dependencies (5-15 minutes first time)
5. Open `Assets/Scenes/MobileGridScene.unity`
6. Press Play to test the game

### Building

#### Development Build (Standalone)
```bash
Unity -batchmode -quit -projectPath ./ScrapLine \
  -buildTarget StandaloneLinux64 \
  -buildPath ./Builds/Linux/ \
  -logFile build.log
```

#### Mobile Build (Android)
```bash
Unity -batchmode -quit -projectPath ./ScrapLine \
  -buildTarget Android \
  -buildPath ./Builds/Android/ \
  -logFile android_build.log
```

## 🧪 Testing

### Manual Testing Scenarios

1. **Basic Factory Setup**
   - Place spawner on bottom edge
   - Create conveyor path upward
   - Add shredder machine in path
   - Place seller on top edge
   - Verify items flow and process correctly

2. **Touch Controls**
   - Test machine placement with finger/stylus
   - Verify drag gestures work smoothly
   - Check UI responsiveness on different screen sizes

3. **Save/Load System**
   - Build complex factory layout
   - Save game state
   - Restart application
   - Load save and verify everything restored

### Automated Testing
```bash
# Run Unity Test Runner
Unity -batchmode -quit -projectPath ./ScrapLine \
  -runTests -testPlatform PlayMode \
  -testResults TestResults.xml
```

## 📱 Mobile Optimization

ScrapLine is optimized for mobile devices with:

- **Touch-First UI**: Large buttons and drag-friendly interfaces
- **Performance Tuning**: Efficient rendering and memory usage
- **Screen Adaptation**: Responsive layout for phones and tablets
- **Battery Optimization**: Reduced CPU/GPU usage for longer play sessions
- **Gesture Support**: Intuitive pinch, drag, and tap controls

## 🔧 Configuration

### Game Balance

Modify JSON files in `Assets/Resources/` to adjust:

- **Machine Costs**: Upgrade prices in `machines.json`
- **Processing Times**: Speed values for each machine type
- **Item Values**: Economic balance in items and recipes
- **Spawn Rates**: How frequently new materials appear

### Graphics Settings

- **Render Pipeline**: Universal Render Pipeline (URP)
- **Quality Levels**: Configurable graphics presets
- **Resolution Scaling**: Automatic mobile adaptation

## 🤝 Contributing

1. Fork the repository
2. Create feature branch: `git checkout -b feature-name`
3. Follow Unity coding conventions
4. Test thoroughly on mobile devices
5. Submit pull request with clear description

### Development Guidelines

- Always test changes in `MobileGridScene.unity`
- Maintain mobile performance standards
- Update relevant JSON data files
- Document new machine types or behaviors
- Ensure backwards compatibility for save files

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

Having issues? Check these common solutions:

- **Build Errors**: Clean project and rebuild (`Build > Clean All`)
- **Performance Issues**: Use Unity Profiler to identify bottlenecks  
- **Save Corruption**: Validate JSON syntax in Resource files
- **Touch Not Working**: Verify EventSystem is present in scene
- **Items Not Moving**: Check conveyor belt direction settings

For additional help, open an issue with:
- Unity version and platform
- Detailed error description
- Steps to reproduce
- Console log output

---

**Happy factory building!** 🏭✨