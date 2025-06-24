
# VolumetricSelection2077

![GitHub all releases](https://img.shields.io/github/downloads/notaspirit/VolumetricSelection2077/total) ![GitHub release (latest by date)](https://img.shields.io/github/v/release/notaspirit/VolumetricSelection2077) ![GitHub latest release date](https://img.shields.io/github/release-date/notaspirit/VolumetricSelection2077)

Is a beta tool for bulk removing nodes from the world in Cyberpunk2077 using a selection box.

Note that the versioning is epoch semantic => [Epoch * 1000 + MAJOR].MINOR.PATCH-PRERELEASE[INDEX]

Roadmeshes and occluder resources are expected to fail, as well as any modded resources, if anything else fails or you have any other feedback please open an issue for it on GitHub.

## Quick Demo (with beta1)
<video src="https://github.com/user-attachments/assets/964b637a-c7c3-41a6-890c-4e555ba6640f" width="852" height="480"></video>
(Note that the processing part is sped up, in the actual clip it took ~1min)

## Requirements
- CyberEngineTweaks ([GitHub](https://github.com/maximegmd/CyberEngineTweaks) | [Nexus](https://www.nexusmods.com/cyberpunk2077/mods/107))
- ArchiveXL 1.23.0+ ([GitHub](https://github.com/psiberx/cp2077-archive-xl) | [Nexus](https://www.nexusmods.com/cyberpunk2077/mods/4198))
- RedHotTools ([GitHub](https://github.com/psiberx/cp2077-red-hot-tools)): not strictly needed for tool functionality but core part of the workflow as it allows hotreloading
- .NET Runtime 8.0.0+ ([Microsoft](https://dotnet.microsoft.com/en-us/download/dotnet/8.0))

### Recommended
CET Window Manager ([GitHub](https://github.com/notaspirit/CET-Window-Manager) | [Nexus](https://www.nexusmods.com/cyberpunk2077/mods/18448)): to hide the CET windows that you aren't using so you can better see the selection

## Instructions
1. Installation
    1. CET Mod
       Either by unzipping it into your game directory or using a mod manager like vortex or mo2.
       Make sure all requirements are installed (CET)
    2. VS2077
       Install like you would any other application
2. Selecting
    0. In Cyberpunk
    1. Enable the Visualizer
    2. Make your selection using the provided controls
       Note that the box scales from the center
    3. With the selection visible on your screen press the "Save Selection" Button
3. Processing
    1. Open the VS2077 App
    2. In the settings set the path to the root of your game directory
    3. Back in the main window provide a valid filename
    4. Click on the "Find Selected" button and wait for the process to finish
       Make sure to have enough ram available as maxing out ram will slow the process down.
4. Applying Changes
   To apply the changes you can either:
    1. restart your game
    2. use Red Hot Tools to reload archive extensions and reload the save
       
## Credits
- [keanuWheeze](https://github.com/justarandomguyintheinternet) for the cube mesh and the material
- [WolvenKit](https://github.com/WolvenKit/WolvenKit) for publishing the core functionality of WolvenKit on nuget
