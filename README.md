# TechHappy's SPTarkov Web Minimap Mod

This mod creates a web accessable map to view your current location and look direction in order to help you navigate throughout Escape From Tarkov. I made this mod to help myself and a family member learn where the caches and extracts are.

This mod was heavily inspired by [CactusPie's Minimap](https://github.com/CactusPie/SPT-Minimap). Without it, this mod wouldn't exist. Especially because I didn't know the polynomial math that was required.

## Instructions
Copy the latest [release](https://github.com/NNThomasL/SPTarkovWebMinimap/releases) to your SPTarkov folder.

After opening the game, go to http://localhost:8080/index.html to view the map.

You can also open the map on a mobile device using the same URL but replacing 'localhost' with your computer's IP address.

Hit the '🧭' on the top left of the map to enable follow mode.

Here is the latest [VirusTotal scan (Release 1.2 of project)](https://www.virustotal.com/gui/file/96d9cb598ec9a260a6343c60b177378245ef682e07018e980ed8e1a2d8fd8b97?nocache=1). If you are worried about security, I ask that you check and compile the code yourself. The project is pretty small so it should be a quick read and simple compile/bundle.



## Current Status

### Maps:
- [x] Woods
- [x] Customs
- [x] Interchange (Enhanced with floor number detection)
- [x] Reserve
- [x] Shoreline
- [x] Lighthouse
- [x] Streets Of Tarkov
- [x] The Lab (Enhanced with floor number detection)
- [ ] Factory - Is the map needed? Currently unable to map coordinates to image.

### Features:
- [x] Follow player button
- [x] In-game config of port and update frequency

### To Do:
- Show connect URL in in-game config section. Not sure how to do that yet...
- Add toggle layers for loot and other important locations (maybe)
- Shoreline Health Resort interior map
- Remove hardcoded path to plugin folder for web assets
- Possibly modify the Woods map to line up cache spots with my coordinates
- Make is so that the index.html is not needed in the connect URL

### Notes:
This is my first SPTarkov mod and it is very messy. I will clean up the code in the near future but I am currently focusing on how it functions before I worry about maintenance. Silly of me but that is how I work.
