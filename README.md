# VolumetricSelection2077

![GitHub all releases](https://img.shields.io/github/downloads/notaspirit/VolumetricSelection2077/total) ![GitHub release (latest by date)](https://img.shields.io/github/v/release/notaspirit/VolumetricSelection2077) ![GitHub latest release date](https://img.shields.io/github/release-date/notaspirit/VolumetricSelection2077)

**VolumetricSelection2077 (VS2077)** is a tool for selecting nodes in the world of *Cyberpunk 2077* using a convenient in-game selection box without size constraints. It supports multiple output formats, including:

* Removal using ArchiveXL (YAML and JSON)
* WorldBuilder Prefabs

> **Note:** WorldBuilder may struggle with very large selections (e.g., 10k+ objects).

---

## Table of Contents

1. [Requirements](#requirements)
2. [Installation](#installation)
3. [Selecting In-Game](#selecting-in-game)
4. [Processing the Selection](#processing-the-selection)
5. [Applying the Changes](#applying-the-changes)
6. [Known Issues & Limitations](#known-issues--limitations)
7. [Credits](#credits)

---

## Requirements

### Mandatory

You need **both the VS2077 CET mod and the VS2077 App** (portable or installer). Get both from the [Releases section](https://github.com/notaspirit/VolumetricSelection2077/releases).

* **CyberEngineTweaks** ([GitHub](https://github.com/maximegmd/CyberEngineTweaks) | [Nexus](https://www.nexusmods.com/cyberpunk2077/mods/107))
* **ArchiveXL 1.23.0+** ([GitHub](https://github.com/psiberx/cp2077-archive-xl) | [Nexus](https://www.nexusmods.com/cyberpunk2077/mods/4198))
* **WorldBuilder** ([GitHub](https://github.com/justarandomguyintheinternet/CP77_entSpawner) | [Nexus](https://www.nexusmods.com/cyberpunk2077/mods/20660))
* **.NET Runtime 8.0.0+** ([Microsoft](https://dotnet.microsoft.com/en-us/download/dotnet/8.0))

### Optional / Recommended

* **RedHotTools** ([GitHub](https://github.com/psiberx/cp2077-red-hot-tools)): Not strictly required, but allows hot reloading for faster workflow.
* **CET Window Manager** ([GitHub](https://github.com/notaspirit/CET-Window-Manager) | [Nexus](https://www.nexusmods.com/cyberpunk2077/mods/18448)): Helps hide unused CET windows for better visibility during selection.

---

## Installation

### 1. CET Mod

Install like any other Cyberpunk 2077 mod, either manually or via a mod manager.

### 2. VS2077 App

Install like any other Windows application, either with the installer or as a portable version.

---

## Selecting In-Game

1. Open the **VolumetricSelection2077** window in the CET Overlay.
2. Enable the **Highlighter**.
3. Set the selection box to your current position and adjust it as needed.
4. Press the green **“Save Selection”** button.

---

## Processing the Selection

1. Open the **VS2077 App**.
2. Set the **game path** in the settings and restart the application.
3. On the main screen, choose an output filename and adjust parameters and filters as needed.
4. Press the **“Find Selected”** button.

---

## Applying the Changes

### 1. WorldBuilder Prefab

1. Reload prefabs in the Favorites tab using the refresh button (top-right).
2. Find the generated prefab under:

   ```
   Spawn New > Favorites > GeneratedByVS2077 > {your chosen filename}
   ```

> **Note:** Prefabs can only be extended if they remain in the **"GeneratedByVS2077"** category. If you modify a prefab, move it to a different category to prevent VS2077 from overwriting it.

### 2. ArchiveXL Removal

1. Restart the game
   *or*
2. HotReload all archive resources.
3. Restream sectors (most reliably by reloading a save file).

---

## Known Issues & Limitations

* WorldBuilder may struggle with selections exceeding 10,000 objects.
* Prefab modifications should be saved outside the **GeneratedByVS2077** category to avoid being overwritten.

---

## Quick Start Workflow

1. Install CET mod and VS2077 App.
2. Open game and select nodes using VolumetricSelection2077.
3. Save the selection.
4. Open VS2077 App, process selection to YAML/JSON or Prefab.
5. Apply changes in WorldBuilder or ArchiveXL.

---

## Credits

* [keanuWheeze](https://github.com/justarandomguyintheinternet) – Cube mesh and material.
* [WolvenKit](https://github.com/WolvenKit/WolvenKit) – Core WolvenKit functionality (published on NuGet).
