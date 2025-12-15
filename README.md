# RimModManager

<div align="center">

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet)
![C#](https://img.shields.io/badge/C%23-13.0-239120?style=for-the-badge&logo=csharp)
![Platform](https://img.shields.io/badge/Platform-Cross--Platform-0078D6?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)
![ImGui](https://img.shields.io/badge/ImGui-UI-orange?style=for-the-badge)

</div>

A modern, high-performance mod manager for RimWorld built with .NET 9 and ImGui.

**A high-performance alternative to RimSort and RimPy** - Built from the ground up with native code and GPU acceleration for superior speed and efficiency.

---

## ? Features

### ?? Core Functionality
- **Automatic Mod Sorting** - Intelligent dependency resolution with topological sorting algorithms (Kahn's and DFS)
- **Profile Management** - Create, save, and switch between different mod configurations
- **Mod Load Order Management** - Drag-and-drop interface for manual reordering
- **Dependency Tracking** - Automatic detection and resolution of mod dependencies
- **Problem Detection** - Real-time analysis of missing dependencies and conflicts
- **Steam Workshop Integration** - Direct integration with Steam Workshop database for mod rules
- **Auto-Update Checker** - Check for updates for installed mods

### ?? Advanced Features
- **GPU-Accelerated Texture Optimization** - Fast IPC-based texture processing using dedicated GPU worker process
- **Multi-Language Support** - Full Unicode support including CJK characters (Chinese, Japanese, Korean), Cyrillic, and Thai
- **Search and Filtering** - Filter mods by name, author, path, package ID, or messages
- **Mod Categorization** - Filter by mod type (Base, DLC, Local, Steam Workshop)
- **Visual Feedback** - Icons, colors, and preview images for better mod identification
- **Rule System** - Custom rule sets for mod ordering and compatibility
- **Save Game Management** - View and manage save game metadata
- **Cross-Platform** - Runs on Windows, Linux, and macOS

## ??? Architecture

The project consists of three main components:

### 1. RimModManager (Main Application)
- **UI Framework**: Hexa.NET.KittyUI (ImGui-based)
- **Target**: .NET 9 with Native AOT support
- **Key Components**:
  - Mod loader and manager
  - Profile system
  - Sorting algorithms
  - Steam Workshop database integration
  - Problem checker and dependency resolver

### 2. GPUWorker
- **Purpose**: GPU-accelerated texture processing
- **Target**: .NET 9 with Native AOT
- **Communication**: IPC via Protobuf

### 3. WorkerShared
- **Purpose**: Shared code and IPC protocol definitions
- **Target**: .NET 9
- **Dependencies**: Hexa.NET.Protobuf for message serialization

## ?? Requirements

- **OS**: Windows, Linux, or macOS
- **.NET**: .NET 9 SDK
- **RimWorld**: Any version (automatic path detection supported)
- **Steam** (optional): For Workshop integration

## ?? Building

```bash
# Clone the repository
git clone https://github.com/JunaMeinhold/RimModManager.git
cd RimModManager

# Build all projects
dotnet build

# Or build for release with Native AOT
dotnet publish -c Release
```

### Build Options

- `BUILD_STEAM_BROWSER` - Enables Steam Workshop browser feature (requires additional dependencies)

## ?? Usage

1. **First Launch**: The application will attempt to auto-detect your RimWorld installation path. If detection fails, you'll be prompted to select the path manually.

2. **Managing Mods**:
   - Left panel shows inactive mods
   - Right panel shows active mods in load order
   - Double-click or press Enter to move mods between panels
   - Drag and drop to reorder active mods

3. **Sorting**:
   - Click the "Sort" button to automatically organize mods based on dependencies
   - The system will warn you about missing dependencies before sorting

4. **Profiles**:
   - Create new profiles from the Profiles menu
   - Switch between profiles to quickly change your mod setup
   - Profiles are stored in `%AppData%\RimModManager\profiles` (Windows) or `~/.config/RimModManager/profiles` (Linux/macOS)

5. **Texture Optimization**:
   - Access from the Textures menu
   - Uses GPU acceleration for fast batch processing

## ?? Configuration

Configuration is stored in:
- **Windows**: `%AppData%\RimModManager`
- **Linux/macOS**: `~/.config/RimModManager`

Contents:
- `profiles/` - Saved mod profiles
- `database/` - Steam Workshop rules database

## ??? Key Technologies

- **UI**: Hexa.NET.KittyUI (ImGui)
- **Serialization**: Hexa.NET.Protobuf, Newtonsoft.Json
- **Version Control**: LibGit2Sharp (for mod update checking)
- **Graphics**: Native GPU acceleration via DirectX/OpenGL/Vulkan
- **AOT Compilation**: Native AOT for improved performance and reduced memory footprint

## ?? Contributing

Contributions are welcome! If you have ideas for improvements or new features, feel free to:
- Submit a pull request
- Open an issue for bug reports or feature requests
- Improve documentation

## ?? License

This project is licensed under the MIT License. See the [LICENSE.txt](https://github.com/JunaMeinhold/RimModManager/blob/master/LICENSE.txt) file for details.

## ?? Acknowledgments

- RimWorld by Ludeon Studios
- ImGui by Omar Cornut
