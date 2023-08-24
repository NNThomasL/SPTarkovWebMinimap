# TechHappy's SPTarkov Web Minimap Mod

This mod creates a web accessable map to view your current location and look direction in order to help you navigate throughout Escape From Tarkov. I made this mod to help myself and a family member learn where the caches and extracts are.

This mod was heavily inspired by [CactusPie's Minimap](https://github.com/CactusPie/SPT-Minimap). Without it, this mod wouldn't exist. Especially because I didn't know the polynomial math that was required.

## Instructions
Copy the latest [release](https://github.com/NNThomasL/SPTarkovWebMinimap/releases) to your SPTarkov folder.

After opening the game, go to http://localhost:8080/index.html to view the map.

You can also open the map on a mobile device using the same URL but replacing 'localhost' with your computer's IP address.

Hit the 'F' on the top left of the map to enable follow mode.

Here is the latest [VirusTotal scan (Beta 1.1 of project)](https://www.virustotal.com/gui/file/591db34aac0198326f83f940a709a048bf56c4bb8ace395c97c648f5e0c608e4?nocache=1). If you are worried about security, I ask that you check and compile the code yourself. The project is pretty small so it should be a quick read and simple compile/bundle.



## Current Status

### Maps:
- [x] Woods
- [x] Customs
- [x] Interchange
- [x] Reserve
- [x] Shoreline
- [x] Lighthouse
- [x] Streets
- [ ] Factory - Is the map needed?
- [ ] The Lab

### Features:
- [x] Follow player button
- [x] In-game config of port and update frequency
- [x] Enhanced Interchange map (When you travel to the second floor of the mall, it will move your marker to the second floor map on the right)

### To Do:
- Show connect URL in in-game config section. Not sure how to do that yet...
- Add toggle layers for loot and other important locations
- Possible Lab map but I think it would need loot and key layers before adding
- Shoreline Health Resort interior map
- Remove hardcoded path to plugin folder for web assets
- Make a build script to package the folders in the right structure automatically
- Fix the parking garage section of the Interchange map. It should have some exterior on the eastern side
- Possibly modify the Woods map to line up cache spots with my coordinates

### Notes:
This is my first SPTarkov mod and it is very messy. I will clean up the code in the near future but I am currently focusing on how it functions before I worry about maintenance. Silly of me but that is how I work.