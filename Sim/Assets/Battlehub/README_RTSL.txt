The Runtime Save & Load (RTSL) subsystem is required for saving and managing scenes, assets and projects at runtime and consists of three main parts:
1. Persistent Classes - this part allows you to choose what to serialize and generate persistent classes for serialization.
2. Asset Libraries - this part allows you to create and manage assets, as well as provide information to the RTSL to identify these assets.
3. Project - this part provides api to interact with RTSL.

Getting Started: 

0.

After importing RTSL you will see the configuration window. (also it can be opened using main menu -> Tools-> Runtime Save Load -> Config)
After clicking "Build All", several folders will be created under /Battlehub/RTSL_Data
   - Scripts for serializable persistent classes.
   - Custom Implementation for user defined persistent classes.
   - Mappings for mappings between types that must be stored and serializable persistent types.
   - Libraries for asset libraries and shader profiles.
   
Now you are ready to run demo scene:

1. Open demo scene Assets\Battlehub\RTSL\Demo\RTSL.unity
2. Hit play
3. Hit button with diskette icon in the Game view to save scene
4. Hit button with X to delete all objects in scene
5. Hit button with arrow to load scene from save file.

For more info see online documentation: http://rteditor.battlehub.net/save-load/
