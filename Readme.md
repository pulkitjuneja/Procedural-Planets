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