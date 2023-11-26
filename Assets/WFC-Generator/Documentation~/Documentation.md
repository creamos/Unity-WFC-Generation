# WFC - Generator : Official Documentation

## System Requirements

WFC Generator was developed for Unity **2021.3** and later versions. It may works on previous versions, though it wasn't tested.

## Installation

It can be added to you project via the git url. Add the following line to your project *Packages/* **manifest.json**
```
"com.bugsarefeatures.wfc-generator": "https://github.com/creamos/Unity-WFC-Generation.git#package"
```
The package also has its own scoped registry. You can add it to your project by referencing it in your project *Packages/* **manifest.json**
```
{
  "dependencies": {
    // ...
    "com.bugsarefeatures.wfc-generator": "0.1", 
    // ...
  },
  "scopedRegistries": [
    {
      "name": "Unofficial Unity Package Manager Registry",
      "url": "https://upm-packages.dev",
      "scopes": [
        "com.bugsarefeatures"
      ]
    }
  ]
}
```

# Overview

### Modules

The modules are the pieces used by the generator to assemble an environment.
They are assemble in onto a 3d grid with one module per cell.

### Sockets

Modules are placed on the grid following some connectivity rules.
1. Each module has a socket on each of its 6 sides.
2. A module can be placed against to another module if their connecting sockets are compatible.

Sockets are defined by a number followed by a suffix that defines their type (and behaviour).

Their is 3 defined socket types so far:
- Symetrical sockets:
  - Naming convention: The only version of a Symetrical socket is named `#s`.
  - Rule:
    - These come in 1 version.
    - They can be assigned to any side of a module.
    - Two modules with identical sockets can be placed against each other without further restrictions.
  - Example: if the socket 4 is a symetrical one, it will be called "4s".

- Flippable/Sided Socket:
  - Naming convention: The 2 versions of a same Flippable socket are named `#` and `#f`.
  - Rule:
    - These come in 2 versions.
    - They can be assigned to any side of a module.
    - Two modules can be placed against each other if one has the **#** socket while the other has the **#f** variant of the same socket.
  - Example: if the socket 3 is flippable, there will be a "3" and "3f" versions.

- Vertical/Quarter Socket:
  - Naming convention: The 4 versions of a same Vertical socket are named `#_0`, `#_1`, `#_2` and `#_3`.
  - Rule:
    - These come in 4 versions.
    - They can only be assigned to the top and bottom side of a module.
    - Two modules can be placed on top of each other if bottom socket of the top one is the same socket variant while the other has the **#f** variant of the same socket.
    - See ### Baking: A module with a vertical socket will be baked in 4 versions each rotated in one of the 4 possible position, the vertical socket variant on these module variants are updated accordingly.
  - Examples:
    - if the socket 7 is vertical, there will be a "7_0", "7_1", "7_2" and "7_3" versions.
    - if a module has the socket version "7_2" on its top side, when baked, the module version rotated by 90° will have a "7_3" socket instead, the one rotate by 180° will have a "7_0" and the one rotated by 270° will have "7_1".

### Additional properties

A socket on a module can be set to `?`, in that case, it can't be connected to anything and may only spawn on the edges of the map where it doesn't have neighbour on the side with such socket.

Modules can be associated with various flags:
  - Grounded: can be spawned at altitude 0
  - Roof: can be spawned at max altitude
  - Airborne: can be spawn at any other altitude
  - Inside: can be spawned anywhere except on the edges of the map (if false, can be spawn anywhere INCLUDING the edges of the map)

Modules can be given a custom weight, increasing the propability of them being picked during the generation process.

### Managing Sockets

The available sockets are made, stored and edited from the **Socket Lookup Table** (or SocketLUT). It's a Scriptable Object that must be instantiated somewhere in the project Assets folder (or any of its subfolders).
It offers an interface to create and modify existing sockets. 

### Making modules

Modules are made by creating a prefab for each of them. These prefabs must have a ModuleSockets component attached to their root transform.
This component allows the user to define and visualize the sockets on each side of the module, as well as setting the constaint flags of the module.

The ModuleSocket component must have a reference to the **Socket Lookup Table**. If none exists in the project folders, an empty one is automatically created under *Assets/*.

### Baking

Before using the modules to generate environments, they must be baked in order to calculate all the variants of each modules (one variant for each rotation of the module) and pre-calculate the possible connectivity between all the resulting modules. Baking data are stored in the **Prototype Lookup Table** (or PrototypeLUT). It's a Scriptable Object containing the baked data that must be instantiated somewhere in the project Assets folder (or any of its subfolders).

Baking can be performed using **Prototyper**, a custom tool available under the Tools tab in Unity.
When clicking the ***Extract & Bake Data*** button, **Prototyper** clear all the previously baked data, and generate new ones using the modules within the current scene that are instance of prefabs.

**Prototyper** must have a reference to the **Prototype Lookup Table**. If none exists in the project folders, an empty one is automatically created under *Assets/*.

Warning: 
- **Prototyper** only care for the modules data on the prefabs, if you have made changes on modules in the scene, they won't be taken into account unless you apply the changes to the prefabs.
- Multiple instances of a same module prefab in the scene won't contribute more than once in the baking process.

## Generation

To generate a new environment, simply add a object to your scene with a WFCWorldGenerator component attached to it.
In the inspector, you can then configure how you want the generation to happens.
Various parameters such as the size of the map can be configured.
Genertion can be performed in a single frame outside of playmode but it will results in a freeze while the script performs the wfc generation.
Generation can be configured to asynchronous, this way in playmode, generation will be performed progressively over time or in a step-by-step fashion controlled by pressing the spacebar.