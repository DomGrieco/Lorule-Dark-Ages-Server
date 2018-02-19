![#000000](https://github.com/wren11/Lorule-Dark-Ages-Server/blob/master/GitStuff/pictures/darkages.gif?raw=true)

# Lorule-Dark-Ages-Server
## Discord- https://discord.gg/PwbFH3T
## Official Website: http://darkages.creatorlink.net/

out-of-the-box Sand box Server For Dark Ages Client 7.18

![Alt text](https://github.com/wren11/Lorule-Dark-Ages-Server/blob/master/GitStuff/pictures/lorule2.png?raw=true "In Game")



## About This Project:
A Darkages Server Project - For client **7.18** Only. (Customized Client will be available soon! (Major Changes!))


## Project Design Overview.
- JSON storage management
- JSON content templates
- Object Manager Service
- Scriptable Storage System/Content
- Component driven design pattern.

## Server Development Status:
- Server is currently about 96% percent completed until it is content ready.
- Expect Glithes! Help by testing and reporting them!

## Working Features
- [x] ![#eeeccc](https://placehold.it/15/ecceee/000000?text=+) Monsters, Items, Mundanes
- [x] ![#eeeccc](https://placehold.it/15/ecceee/000000?text=+) Spells/Skills
- [x] ![#eeeccc](https://placehold.it/15/ecceee/000000?text=+) Equipment
- [x] ![#eeeccc](https://placehold.it/15/ecceee/000000?text=+) Shops, Dialogs, Pursuits
- [x] ![#eeeccc](https://placehold.it/15/ecceee/000000?text=+) Death Handling, you can die, revive. ect
- [x] ![#eeeccc](https://placehold.it/15/ecceee/000000?text=+) Elements, Fire, Water, Earth, Wind, Dark, Light
- [x] ![#eeeccc](https://placehold.it/15/ecceee/000000?text=+) Regen, Stats, AC and other Attributes implemented.
- [x] ![#eeeccc](https://placehold.it/15/ecceee/000000?text=+) Player PVP, Arena is working, Similar to USDA.
- [x] ![#eeeccc](https://placehold.it/15/ecceee/000000?text=+) Assails, Ambush, Crasher, Kelb, Wff, pramh - all working
- [x] ![#eeeccc](https://placehold.it/15/ecceee/000000?text=+) Spell Bars, Buffs, Debuffs
- [x] ![#eeeccc](https://placehold.it/15/ecceee/000000?text=+) Quests, Examples can be found in gos/benson templates.
- [x] ![#eeeccc](https://placehold.it/15/ecceee/000000?text=+) Loot Systems, Experience System
- [x] ![#eeeccc](https://placehold.it/15/ecceee/000000?text=+) Object Spawning
- [x] ![#eeeccc](https://placehold.it/15/ecceee/000000?text=+) World Maps, World Portals, Warps
- [x] ![#eeeccc](https://placehold.it/15/ecceee/000000?text=+) Grouping
- [x] ![#eeeccc](https://placehold.it/15/ecceee/000000?text=+) Community States, Awake, daydreaming ect.
- [x] ![#eeeccc](https://placehold.it/15/ecceee/000000?text=+) In-Game Boards



## What is missing?
- [ ] ![#f03c15](https://placehold.it/15/f03c15/000000?text=+) Exchanging
- [ ] ![#f03c15](https://placehold.it/15/f03c15/000000?text=+) Whispering, Ignore
- [ ] ![#f03c15](https://placehold.it/15/f03c15/000000?text=+) Meta Database (Will be Last to Do.)


## Progress Map 17/02/2018
- Exchanging Is Up Next.

## Building & Compiling
- This was developed using Visual Studio 2017 Community (https://www.visualstudio.com/thank-you-downloading-visual-studio/?sku=Community&rel=15#)
    - Using Microsoft's Framework **4.6.1**
    - By default, It will compile to ..\Staging\bin\Debug\

So the Staging Folder is the place to seek when making changes to templates, other settings ect.
as everything will by default point to that directory.


## Using this Server:

- Download and Compile the Lorule Source code, It should compile out-of-the-box
- Download and Install Dark Ages Client 7.18
        - https://drive.google.com/file/d/1EbIf7AzQLJaUrR9Kd3wmZDQWM7qT0-hR/view?usp=sharing    
- Download our modified Darkages.exe executable and replace the previous one in the 7.18 Installation Directory.
        - https://drive.google.com/file/d/18fCo2kyL1pF6QPJ9TgO1PIGmfKn5Fm_D/view?usp=sharing
    
That is all you need to run the server on your local system.
To host your server online, you must use the Client Launcher for your users to connect to your IP Address.
Your IPAddress also needs to reflect the **server.tbl** file located in your Server's running directory.

Lorule Client Launcher Download:    
        - https://drive.google.com/file/d/16Uff3R23Qdg3qRiaMbCG2q1szgfCj4LG/view?usp=sharing 
        - (optional) - Copy the Darkages_Client folder and the config to your documents directory.
    
            -- When adding a server to the launcher config file, the settings you will be looking for client 7.18 are:
                    -- PatchTable  : 212257
                    -- HookTable   : 213435
            -- The rest of the information can be set yourself, IP, port, Ect.

One more thing, run launcher as administrator if it fails, darkages will also need to run as **administrator**.
        
        
        
        


