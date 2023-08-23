# TechHappy's SPTarkov Web Minimap Mod

This mod creates a web accessable map to view your current location and look direction in order to help you navigate throughout Escape From Tarkov. I made this mod to help myself and a family member learn where the caches and extracts are.

This mod was heavily inspired by [CactusPie's Minimap](https://github.com/CactusPie/SPT-Minimap). Without it, this mod wouldn't exist. Especially because I didn't know the polynomial math that was required.

### Instructions
Copy the latest [release](https://github.com/NNThomasL/SPTarkovWebMinimap/releases) to your SPTarkov folder.

After opening the game, go to http://localhost:8080/index.html to view the map.

You may also open the map on a mobile device using the same URL but replacing 'localhost' with your computer's IP address.

Hit the 'F' on the top left of the map to enable follow mode.

Here is the latest [VirusTotal scan (Beta 1.1 of project)](https://www.virustotal.com/gui/file/591db34aac0198326f83f940a709a048bf56c4bb8ace395c97c648f5e0c608e4?nocache=1). If you are worried about security, I ask that you check and compile the code yourself. The project is pretty small so it should be a quick read and simple compile/bundle.



# Current Status

### Maps:
- [ ] Factory
- [x] Woods
- [x] Customs
- [x] Interchange
- [x] Reserve
- [x] Shoreline
- [ ] The Lab
- [x] Lighthouse
- [x] Streets

### Features:
- [x] Follow player button
- [x] In-game config of port and update frequency
- [x] Enhanced Interchange map 
	When you travel to the second floor of the mall, it will move your marker to the second floor map on the right
- [ ] Show connect URL in in-game config section

### To Do:
- Add toggle layers for loot and other important locations
- Possible Lab map but I think it would need loot and key layers before release
- Shoreline Health Resort interior map
- Remove hardcoded path to plugin folder for web assets
- Make a default landing page for when the page is opened
- Make a build script to package the folders in the right structure automatically

### Notes:
This is my first SPTarkov mod and it is very messy. I will clean up the code in the near future but I am currently focusing on how it functions before I worry about maintenance. Silly of me but that is how I work.