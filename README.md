# DynamicMaps Extended 3.0.0 for SPT 5.0 — complete installation

This branch only holds the release download, split into 20 parts of 90 MB (GitHub limits files to 100 MB). Source code is on the `claude/determined-wozniak-h9bmzc` branch.

Requires SPT 5.0.x and DynamicMaps 2.x. MapVariants and map mods are optional.

## Download and join

1. Download all 20 `DynamicMaps-Extended-3.0.0-SPT5.0.zip.001` … `.020` files into one folder (each file's page has a download button, or use **Code → Download ZIP** on this branch).
2. Join them, either way:
   - **7-Zip:** right-click `DynamicMaps-Extended-3.0.0-SPT5.0.zip.001` → 7-Zip → **Combine files…**
   - **PowerShell 7** in that folder:
     ```powershell
     Get-ChildItem DynamicMaps-Extended-3.0.0-SPT5.0.zip.0* | Sort-Object Name | Get-Content -AsByteStream -ReadCount 0 | Set-Content DynamicMaps-Extended-3.0.0-SPT5.0.zip -AsByteStream
     ```
3. Check the joined file (PowerShell: `Get-FileHash DynamicMaps-Extended-3.0.0-SPT5.0.zip`). It must be
   `3AB011583DD6F8CA02161ED59B3B53633B8C7675C1502557350A637DA5E3D128` (1,828,833,869 bytes).

## Install

Close the game. If you had an older version, remove `BepInEx/plugins/DynamicMaps-Extended`. Extract the joined ZIP into your SPT folder.

## Contents

The DLL, map definitions, Abstract artwork and all 42 Satellite layers (tile packs, previews, warm atlases), built on 2026-10-14 from the current Tarkov.dev tiles with `tools/rebuild-satellite-cache`. All 42 layers passed the official map-structure check and `bundle-maps.py` verification. Third-party map artwork keeps its original terms and attribution; see `ARTWORK-LICENSE.md` and `THIRD-PARTY.md` inside the ZIP.

## Part checksums (SHA-256)

```
b2d6ec7ada6b8428e2cb5995d820a120ea87f9904b5ebbc42d883ded87a3afd1  DynamicMaps-Extended-3.0.0-SPT5.0.zip.001
1a3cc6d22f8790229012bb561d216fca493e75aabcebdbdd90b1f7a3cb585c15  DynamicMaps-Extended-3.0.0-SPT5.0.zip.002
2a4478358d07fff7fe1dc9f707ceb2a3a4cd2f69ee6a1d91c9e63cfe3225ae2b  DynamicMaps-Extended-3.0.0-SPT5.0.zip.003
d65ec7f059f7b5dcf883d3621db77aa2ad1df8cf6fe450d32719e527e020b66e  DynamicMaps-Extended-3.0.0-SPT5.0.zip.004
b87cbdc3b914188acd1a8f81618bc638ce9d4697d1ec2816c4818bdbe883501e  DynamicMaps-Extended-3.0.0-SPT5.0.zip.005
32c8b5c5a62530cf3cf04fb8620724cf81f02d3d3d0d2c31fb08c7f41c890379  DynamicMaps-Extended-3.0.0-SPT5.0.zip.006
ee3b63dd565f32ccb4e00347273536e28933e2139686093558a49980d00293ea  DynamicMaps-Extended-3.0.0-SPT5.0.zip.007
20942c7795d57233b64f8e3e1f3dbed7d3addf734ae4a79173b753286149527e  DynamicMaps-Extended-3.0.0-SPT5.0.zip.008
ca165a3f7905e78c449a23aae94319309a04142cd09fe04fe95cd9394d91a2a6  DynamicMaps-Extended-3.0.0-SPT5.0.zip.009
5c7cccf6a23dee8626891769c372db8a974b3a2af286bdb3110fd46c0c9961e3  DynamicMaps-Extended-3.0.0-SPT5.0.zip.010
725b901da9cd9df29f2008a84b60707af180f6c016889f02298470c5f1051530  DynamicMaps-Extended-3.0.0-SPT5.0.zip.011
57d587eb687438ca21aabad96af179324646f3a80f2395580740ca4a90daf438  DynamicMaps-Extended-3.0.0-SPT5.0.zip.012
555f8c622d6e7f4419aaf1a4eb93c5e49bcf645d790ce8780b3cdcfe074ef30e  DynamicMaps-Extended-3.0.0-SPT5.0.zip.013
01061e41adf91b2678c9aa306e623b18afa974334e354de5ec3a72d84ff315ca  DynamicMaps-Extended-3.0.0-SPT5.0.zip.014
3beef36d63576885dd75551d7c75a31cf06ada14b212da03bfd7882af6a4881d  DynamicMaps-Extended-3.0.0-SPT5.0.zip.015
bdc610bb0261339d074d291dc6e96ca2859597c576d9283f2db490846ba6f526  DynamicMaps-Extended-3.0.0-SPT5.0.zip.016
2d3b9633f1a82c685687eee8edb821ced342971a500448801923f92ca307170c  DynamicMaps-Extended-3.0.0-SPT5.0.zip.017
8531a7f8bc9356034d9f12e39e56c2c690ae2542d0a632da4618bfd2cd430773  DynamicMaps-Extended-3.0.0-SPT5.0.zip.018
9fa0fa7a712ef840b314122d3695af5ef9cd452f259573ac4464325798cebcce  DynamicMaps-Extended-3.0.0-SPT5.0.zip.019
ab44099764b1ee03a53b9cb61ff9242ac92361e5be8c537c485be13a57ae8a12  DynamicMaps-Extended-3.0.0-SPT5.0.zip.020
```
