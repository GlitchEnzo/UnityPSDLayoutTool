What is the Unity PSD Layout Tool?
==============================

A tool used to import a Photoshop Documents (.psd files) into the Unity Game Engine. 

It can import each layer as separate textures, create Unity 4.3+ Sprites laid out in a scene, and generate an entire prefab with the layout.

How to Install
==============
Simply copy the files into your project.  A .unitypackage file will be provided in the future.


How to Use
==========
The PSD Layout Tool is implemented as a Unity Custom Inspector.  If you select a PSD file that you have in your project (Assets folder) special buttons will appear above the default importer settings.

![](screenshots/inspector.png?raw=true)

* **Export Layers as Textures**
  * Creates a .png image file for each layer in the PSD file, using the same folder structure.
* **Layout in Current Scene**
  * Creates a Unity 4.3+ Sprite object for each layer in the PSD file.  It is laid out to match the PSD's layout and folder structure.
* **Generate Prefab**
  * Identical to the previous option, but it generates a .prefab file instead of putting the objects in the scene.

Photoshop Compatibility
=======================
Photoshop's "Smart Objects" are not supported, and therefore must be flattened/rasterized in Photoshop before attempting to import.

1. Click **Layer** in the Photoshop menu
2. Click **Rasterize**
3. Click **All Layers**

![](screenshots/photoshop.jpg?raw=true)
