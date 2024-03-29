# ChangeLog
## v2023.3.20 [The Easy Update]
- Added Thryrallo's VRC-Avatar-Performance-Tools package to kebinImports as an optional Avatar Essential.
- Fixed a bug that occurs when updating from older versions of kebinImports.


## v2023.1.30 [The Bee Kind Update]
- Fixed lilToon Shader not being imported properly.


## v2023.1.2 [The Dango Update]
- Added a warning prompt to Remove Missing Scripts to prevent the accidental deletion of assets that are still in use but have not been imported.
- Renamed all EditorPrefs used by kebinImports to include 'kebinImports.' as a prefix to avoid conflicts with other packages.
- Corrected a typo that prevented the "Hide Warnings" and "Show at Startup" features from functioning properly.


## v2023.1.1 [The UNNAMED Update]
- Rewritten as a Package, enabling import as a VPM (VRChat Package Manager) Package, a UPM (Unity Package Manager) Package, and as an Asset
- Updated .NET Framework from 4.8 to 4.8.1
- Replaced SemVer with System.Version for versioning
- Removed UdonSharp and VRWorld Toolkit, as they are now a part of VRChat's Creator Companion Tool
- Optimized PANIC HARD RESET ALL and added cache clearing functionality
- Created an EditorWindow for Customize Essentials to improve usability

---

## v2022.07.31 [The Essential Update]
- Fixed Customizing of Essentials not updating checkboxes and saving settings. It now works.


## v2022.07.14 [The Birthday Update]
- Fun Fact: It is my birthday today! (July 14)
- UdonSharp repository link updated.
- Updated .NET Framework from 4.7.1 to 4.8.
- Revamped Fix Materials as a Custom Editor to include any shader, even hidden ones, eg. Hidden/InternalErrorShader.
- Renamed Customize Essentials to Customize and moved it under the Essentials list.
- Reversed the order of the sections More, and the Utilities section.
- Swapped the Remove Missing Scripts and Fix Scripting Define Symbols Menu Items.
- Renamed Support tab back to Donate.
- Fixed Hide Warnings Toggle.


## v2022.06.20 [The Gobbledygook Update]
- Fun Fact: Gobbledygook is language that is made meaningless or unintelligble by excessive use of technical terminology. This CHANGELOG is full of nasty gobbledygook.
- Fixed up importing of ComboGestureExpressions and the reimporting method for Hai's assets.
- Added a Social button for my website: https://kebin.dev.
- Renamed Donate MenuItem Folder to Support.


## v2022.04.22 [The New Era Update]
- The era of Dynamic Bone has ended. As Avatar Dynamics with PhysBones have recently been enabled for Live, it has been decided to move Dynamic Bone to the Legacy Components. This change was a vote taken on the Discord. Thanks for the amazing years, Dynamic Bone. Long Live Avatar Dynamics and PhysBones!
- Corrected Extra Shaders section, renamed as a More section. This section is now more complete and contains all the extras, including legacy assets which are not shaders, such as Dynamic Bone.
- Spaced out CHANGELOG more to make it more legible.


## v2022.03.27 [The Fixer Update]
- Added a Menu Item for PANIC HARD RESET ALL. This option displays a warning, if warnings are enabled, before clearing all Registry Keys tied to Unity Editor (Also Known As Unity Editor Preferences and some per project settings) and exits the Unity Editor. It is left up to the End User to reopen the project through Unity Hub.
- Fixed Up the wrapping of CHANGELOG to take up less space, be more optimized, and ease deployment process.


## v2022.03.21 [The Full Stack Dev Update]
- Added a Donation Tab for my PayPal and Cash App. Please, feel free to donate to me.
- Made Fix Scripting Define Symbols to be more aggressive by forcing the Unity Editor to recompile all scripts. This prioritises the recompilation process instead of recompiling when convenient for Unity.
- Reorganized kebinImports Menu Items structure to allocate more space and decrease clutter while adding clarity.

---

## v2021.11.25 [The Manual Update]
- Rewritten kebinImports with ADigitalFrontier.
- New Logo Art drawn by bentbones#5193.
- Switched from WebClient to kebinClient, an implementation of HttpClient.
- Simplified kebinImports Import Menu.
- Importing of Dynamic Bone and Muscle Animation Editor are now restricted to the people that have purchased those assets.
- Added Support for Importing AssetBundles through the Assets/kebinImports in the Right-Click Menu.
- Added MenuItem for automatically removing all Missing scripts recursively inside the selected Object and/or Prefab.
- Dropped Support for ChilloutVR.

---

## v3.3.0 [The Hyperbole Update]
- Added Customization options for the Avatar Essentials and World Essentials butttons. This allows you to import what you want when you click the most commonly used buttons.
- Added option to Fix Materials that have Missing or broken shaders. This gives the ability to set them to either Poiyomi Toon Shader, if it's in the project, or Standard.
- Enabled Pre-Authentication and Authentication to hopefully increase the rate limits for GitHub. This allows us to have higher amount of imports from GitHub, as the update name suggests.
- Enabled Updating Import for Unity-Chan ToonShader 2.0. This means that it is the Latest version of it everytime.
- Removed Updating Asset Import method setting as now it is the default, standard method for kebinImports. Trust me, this is a better importing method.
- Merged the webClients for kebinUpdater and kebinImports into one. This makes kebinImports more stable, dependent, and more optimized all-together.
- Updated and Relocated all backup links.
- Got rid of lingering settings that didn't matter, such as SkipImportDialog. It was phased out in v2.7.


## v3.2.1 [The Titter Update]
- Fun Fact: To titter means to giggle or laugh.
- Renamed 'Fix Script Definition Symbols' to the proper name: 'Fix Scripting Define Symbols'. I'm not sure why I was writing English instead of programming.
- Flipped the order of refreshing of the Unity Editor FROM: Clear the Log, Refresh the AssetDatabase. TO: Refresh the AssetDatabase. Clear the Log. This is to have less warnings and feed. This also may or may not fix some pseudo-errors.
- Removed redundant code within kebinUpdater that was already present in kebinImports. This has little to no effect on performance, just saves lines of code and space required to store the code.
- Removed deprecated method of remaking Poiyomi Toon Shader's csc.rsp file.
- Fixed the warning that Unity gives when deleting folders or files by deleting the corresponding *.meta file's along with them.
- Fixed a mistake that would remove reroStandard Shaders when importing Cubed's Unity Shaders. Simple mistake on my part. But since no one uses Cubed's Unity Shaders anymore, it went unnoticed for a while.
- Added supported Unity version's that the ChilloutVR CCK can be imported into. (Honestly, I just gave up on them to give me an exact release date for the CCK 3.0. Because of this, I have allowed the importing of CCK 2.3 to be allowed on any Unity in the 2019.4.* version family)


## v3.1.3 [The Fucky Wucky Update]
- Added checks to make sure the most updated VRCSDK supports the Unity version that is currently installed and running. Otherwise, a nice fucky wucky dialog box pops up to let the user know.
- Fixed the Unity version requirement to import the ChilloutVR CCK. Unfortunately but understandably, the developers don't allow direct api access. We will continue to work with them to work towards a better, optimal solution. However, until ChilloutVR CCK 3.0 comes out, this is our solution. Be aware that support for ChilloutVR may or may not be dropped in future releases of kebinImports.
- Added the ability to import .zip extensions as packages, not just .unitypackage extensions. This will enable current and future ability to import assets that do not have their binaries uploaded to their respective GitHubs.
- Added Menu Item for Xiexe's Unity Shaders with the help of The aforementioned ability.
- kebinImports now clears the Console upon loading up or when a new asset is refreshed, or imported.
- Beautified the formatting of kebinImports, kebinUpdater, and their dependencies codes.
- Corrected typo for Dynamic Bone.


## v3.1.1
- Integrated Splash Screen into the Settings menu.
- Added some optimizations to the kebinUpdater.
- Fixed some spacing issues in the kebinImports Settings window. It looked like the Checkboxes needed a little room to breath. :)
- Settings reset for everyone! Opted for new, cleaner EditorPref names. It is just to help delete old, lingering EditorPrefs, if the user is skilled enough with the Registry in Windows and/or the Library/Preferences/*.plist on a Mac. Otherwise, this change is unnoticable.


## v3.0.0
- Switched to Semantic Versioning with the help of ThryEditor scripts. Shout out to Thryrallo#0001 for the authorized use of his function. Thanks again.
- Fixed up persistency issues with Paths. This will help fix up problems with importing when the project folder is not in the same drive Unity is installed in.
- Added the Setting 'Update Asset'. This setting allows for the importing of an asset to completely remove an older version if such exist and to completely get rid of any lingering files from them. Then it reimports the new asset so there is no old lingering files from older versions. Be careful when using this to update shaders, you may have to reselect them as the shader when they turn pink and error out. All it takes is to just remap them as Poiyomi or whatever shader that you are using. The settings may or may not break.
- Added Menu Item for Cubed's Unity Shaders for legacy support. This is only meant to be used for old avatars, old unity versions, or just for messing around and/or referencing. DO NOT USE on new avatars. PLEASE!!!

---

## v2.8
- Added Menu Item for ChilloutVR CCK v2.3. This is for current support. The ChilloutVR CCK v3.0, being released around June or May 2021, will make this menu item useless until kebinImports updates. Whether this update removes support for the ChilloutVR CCK entirely, or actually brings CCK 3.0 is still up in the air.
- Corrected Typo for Pumkins Avatar Tools. ~ Thanks to Ephah#6969


## v2.7
- Removed Skip Import Dialog Setting due to an internal error in Unity. There are workarounds to this, but I don't know any efficient ones. If you know any, feel free to let me know to make kebinImports better for everyone! :)


## v2.6
- Fixed an issue with UnityToonShader2.0 not importing. Unfortunately, this will happen everytime they update, but fortunately, they haven't updated in a while.


## v2.5
- Fixed an issue with kebinUpdater not allowing VRCSDK to upload due to an internal issue with how Unity handles Assets. VRCSDK thought that kebinUpdater was part of the prefab that it was supposed to upload to VRChat Servers. This would break everything. ~ Thanks to Ghosti#3435


## v2.4
- Added Menu Item for ComboGestureExpressions. This unitypackage helps with the development of gestures and expressions with the use of shapekeys on Avatars. ~ Thanks to Julio#1818


## v2.3
- Added Menu Item to fix a common error with having VRCSDK, Poiyomi Toon Shader, Pumkins Avatar Tools, and Dynamic Bones together. Thank God for this one. Most people do not know how to fix this error manually, so now you can do it with aclick of a button. The Warning it shows will tell you how to do it manually for Educational Purposes.


## v2.2
- Revamped Settings Integration with proper utilization of Unity's EditorPrefs.
- Patch fix for bugs relating to Settings not saving and loading properly. Allows for better optimization.


## v2.1
- Patch fix for bugs relating to AutoUpdater not working properly.
- New Settings Menu with the ability to disable Warnings, Import Dialogs, and the AutoUpdater.


## v2.0
- New GUI Layout and Design with more optimized functionality.
- Implemented the ability to grab the latest version of most assets.

---

## v1.0
- Initial private release.
- Created Menu Items for each unitypackage.
