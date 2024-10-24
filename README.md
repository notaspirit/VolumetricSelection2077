# (Indefinitely Paused) VolumetricSelection2077
A selection tool for easily selecting everything basically based on a 3d selection box in game.
It does not require for manually picking out sectors to select.

why:
1. no way to get mesh data within a wscript
2. no way to get actor meshes with just wkit cli (and would require a lot of extra work to get the mesh data)
3. exporting all data would take very long and require a lot of disk space (just sectors would take 103h and 6.9GB (Nice). If meshes take 20s per mesh that would be 572h or 72d and if we assume the mesh data is the same size as the actor meshes (it's bigger) that would be 39GB) yeah I ain't doing all that
4. if the user has to run the scripts when needed and cache the data, it's not any easier than existing tools (e.g. exporting the sector into blender)
