
# VolumetricSelection2077

![GitHub all releases](https://img.shields.io/github/downloads/notaspirit/VolumetricSelection2077/total) ![GitHub release (latest by date)](https://img.shields.io/github/v/release/notaspirit/VolumetricSelection2077) ![GitHub latest release date](https://img.shields.io/github/release-date/notaspirit/VolumetricSelection2077)

VolumetricSelection2077 is a tool for selecting nodes in the world of Cyberpunk2077 using a convienient in game selection box without any size constraints.
Currently it supports multiple output formats: Removal using ArchiveXL (in both yaml and json) as well as WorldBuilder Prefabs (Note that WorldBuilder struggles with very large selections e.g. 10k+ objects).

## Requirements
- CyberEngineTweaks ([GitHub](https://github.com/maximegmd/CyberEngineTweaks) | [Nexus](https://www.nexusmods.com/cyberpunk2077/mods/107))
- ArchiveXL 1.23.0+ ([GitHub](https://github.com/psiberx/cp2077-archive-xl) | [Nexus](https://www.nexusmods.com/cyberpunk2077/mods/4198))
- WorldBuilder ([GitHub](https://github.com/justarandomguyintheinternet/CP77_entSpawner) | [Nexus](https://www.nexusmods.com/cyberpunk2077/mods/20660))
- RedHotTools ([GitHub](https://github.com/psiberx/cp2077-red-hot-tools)): not strictly needed for tool functionality but core part of the workflow as it allows hotreloading
- .NET Runtime 8.0.0+ ([Microsoft](https://dotnet.microsoft.com/en-us/download/dotnet/8.0))

### Recommended
CET Window Manager ([GitHub](https://github.com/notaspirit/CET-Window-Manager) | [Nexus](https://www.nexusmods.com/cyberpunk2077/mods/18448)): to hide the CET windows that you aren't using so you can better see the selection

## Instructions
1. Installation
   1. CET Mod
      Install it like any other Cyberpunk2077 mod, either manually or via a mod manager.
   2. VS2077 App
      Install it like any other Windows Applications either with the installer or as a portable.

2. Selecting In Game
    1. Find the VolumetricSelection2077 window in your CET Overlay
    2. Enable the Highlighter
    3. Set the Selection to your current position and adjust the box to fit your needs.
    4. Press the green "Save Selection" button
3. Processing the Selection
    1. Open the VS2077 App
    2. Set the gamepath in the settings and restart the application
    3. On the main screen set an output filename and  adjust the parameters and filters as needed
    4. Press the "Find Selected" button on the right
4. Applying the changes
    - World Builder Prefab:
       1. Restart the game
         or
       1. Hotreload all CET Mods from the "Cyber Engine Tweaks" window
       2. Find the generated prefab under "Spawn New" > "Favorites" > "GeneratedByVS2077" > {filename you chose earlier}
          
         Note that prefabs can only be extended if they are in the "GeneratedByVS2077" category, and that if you modify the prefab it is recommended to move it to a different category that VS2077 does not save to.
   - ArchiveXL Removal:
     1. Restart the game
        or
     1. HotReload all archive resources
     2. Restream the sectors (most reliably via reloading a save file)

        
## Credits
- [keanuWheeze](https://github.com/justarandomguyintheinternet) for the cube mesh and the material
- [WolvenKit](https://github.com/WolvenKit/WolvenKit) for publishing the core functionality of WolvenKit on nuget
