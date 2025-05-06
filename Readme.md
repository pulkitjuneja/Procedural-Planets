# Procedural planet generator

## Features
* GPU accelerated heightmap generation
* Fully configurable biome generation
* Dynamic LOD switching
* Object placement and vegetation
* Triplanar Texture mapping 

## Instructions to run the generation
* The scene included with the project has a GameObject with a `ChunkedPlanetBodyGenerator` script attached to it.
* changing any parameter of the script will cause the planet to regenerate
* Trees are only generated in play mode 
* Or you can just enter the play mode and hte planet will generate itself
*  **important** The biome texture does not apply itself automatically due to some Unity issues. So for that you would need to click on a chunk, go to the material, click on the texture and change any setting of the texture (like aniso level). This would apply the texture immediately

![screenshot1](https://raw.githubusercontent.com/pulkitjuneja/Procedural-Planets/refs/heads/master/Screenshots/Screenshot%202021-03-01%20183553.png)
![screenshot1](https://raw.githubusercontent.com/pulkitjuneja/Procedural-Planets/master/Screenshots/Screenshot_2020-12-01_011100.png?token=ACFO6GTOD6WIHNF6MHOQQ63BL2LPQ)
![screenshot1](https://raw.githubusercontent.com/pulkitjuneja/Procedural-Planets/master/Screenshots/Screenshot_2020-12-01_011706.png?token=ACFO6GXV2U2PF5RE6LCGUATBL2LR2)
![screenshot1](https://raw.githubusercontent.com/pulkitjuneja/Procedural-Planets/master/Screenshots/Screenshot_2020-12-01_011821.png?token=ACFO6GQSHDPKG3V24UREVXLBL2LTE)
![screenshot1](https://raw.githubusercontent.com/pulkitjuneja/Procedural-Planets/master/Screenshots/Screenshot_2020-12-01_012008.png?token=ACFO6GTCNMNEGUBLYFV2S4DBL2LT6)
