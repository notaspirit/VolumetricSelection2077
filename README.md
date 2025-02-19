
# VolumetricSelection2077
Is a beta tool for bulk removing nodes from the world in Cyberpunk2077 using a selection box.

Note that the versioning is epoch semantic => [Epoch * 1000 + MAJOR].MINOR.PATCH-PRERELEASE[INDEX]

Roadmeshes and occluder resources are expected to fail, as well as any modded resources, if anything else fails or you have any other feedback please open an issue for it on GitHub.

## Instructions
1. Installation
    1. CET Mod
       Either by unzipping it into your game directory or using a mod manager like vortex or mo2.
       Make sure all requirements are installed (RedHotTools and CET)
    2. VS2077
       Install like you would any other application
2. Selecting
    0. In Cyberpunk
    1. Enable the Visualizer
    2. Make your selection using the provided controls
       Note that the box scales from the center
    3. With the selection visible on your screen press the "Read RHT Scan" Button
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

## Recommended Mods
CET Window Manager ([GitHub](https://github.com/notaspirit/CET-Window-Manager) | [Nexus](https://www.nexusmods.com/cyberpunk2077/mods/18448)): to hide the CET windows that you aren't using so you can better see the selection

## Credits
- [keanuWheeze](https://github.com/justarandomguyintheinternet) for the cube mesh and the material
- [WolvenKit](https://github.com/WolvenKit/WolvenKit) for publishing the core functionality of WolvenKit on nuget
