@echo off

if exist bin\Debug\net472\ (
    if exist client\dist\ (
        if not exist packaged\ (mkdir packaged)
        if not exist packaged\BepInEx (mkdir packaged\BepInEx)
        if not exist packaged\BepInEx\plugins (mkdir packaged\BepInEx\plugins)
        if not exist packaged\BepInEx\plugins\TechHappy-MinimapSender (mkdir packaged\BepInEx\plugins\TechHappy-MinimapSender)
        
        xcopy bin\Debug\net472\TechHappy.MinimapSender.dll packaged\BepInEx\plugins\TechHappy-MinimapSender\
        xcopy bin\Debug\net472\NetCoreServer.dll packaged\BepInEx\plugins\TechHappy-MinimapSender\
        
        xcopy client\dist packaged\BepInEx\plugins\TechHappy-MinimapSender\www\ /S
    ) else (
        msg "Error" "Please build the browser project first!"
    )
) else (
    msg "Error" "Please build the dll mod project first!"
)