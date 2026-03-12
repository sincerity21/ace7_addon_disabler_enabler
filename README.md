# DisableEnabler for ACE COMBAT™ 7

DisableEnabler is a small Windows program for **ACE COMBAT™ 7: SKIES UNKNOWN**.  
It lets you **turn individual player planes on or off** inside a mod `.pak` file, using a simple checklist‑style window.

You do **not** need to know how `.uasset` files work or how to use command‑line tools. DisableEnabler does the technical steps for you in the background:

- You pick your **UnrealPak** (tool used to open `.pak` files).
- You choose the **mod `.pak`** which contains the plane list.
- DisableEnabler shows all planes in a table where you can tick **Enabled** on or off.
- It then creates a **new mod `.pak`** with your changes, ready to drop into your `~mods` folder.

---

## Table of Contents

- [What You Need](#what-you-need)
- [First Time Setup (One‑Time)](#first-time-setup-one-time)
- [Step 1 – Tell the Program Where UnrealPak Is](#step-1--tell-the-program-where-unrealpak-is)
- [Step 2 – Pick the Mod PAK You Want to Edit](#step-2--pick-the-mod-pak-you-want-to-edit)
- [Step 3 – Unpack the PAK and Load the Plane List](#step-3--unpack-the-pak-and-load-the-plane-list)
- [Step 4 – Turn Planes On or Off](#step-4--turn-planes-on-or-off)
- [Step 5 – Save Changes and Build a New PAK](#step-5--save-changes-and-build-a-new-pak)
- [Step 6 – Put the New PAK Into Your Mods Folder](#step-6--put-the-new-pak-into-your-mods-folder)
- [Extra Notes](#extra-notes)
- [Credits](#credits)

---

## What You Need

- **The game**
  - **ACE COMBAT™ 7: SKIES UNKNOWN** on PC.

- **UnrealPak Enhanced**
  - Download from ModDB:  
    `https://www.moddb.com/downloads/unrealpak-enhanced`
  - After downloading, **right‑click → Extract** the archive somewhere easy to find (for example `C:\Tools\UnrealPakEnhanced`).

- **Windows and .NET**
  - Windows 10 or newer.
  - If the program does not start, install the latest **.NET 8 Desktop Runtime (x64)** from Microsoft’s website and try again.

---

## First Time Setup (One‑Time)

1. **Put DisableEnabler somewhere**
   - Copy `DisableEnabler.exe` to any folder you like (for example a `Tools` or `Mods` folder).

2. **Download and extract UnrealPak Enhanced**
   - Go to `https://www.moddb.com/downloads/unrealpak-enhanced`.
   - Download the file, then right‑click it and choose **Extract**.
   - Inside the extracted folder, you should see `UnrealPak.exe`.

3. **Start DisableEnabler**
   - Double‑click `DisableEnabler.exe`.
   - (Optional) Tick **Dark mode** if you prefer a dark‑themed window.

---

## Step 1 – Tell the Program Where UnrealPak Is

1. In DisableEnabler, click **“Browse UnrealPak…”**.
2. A file window will open. Go to the folder where you extracted **UnrealPak Enhanced**.
3. Click on `UnrealPak.exe` and press **Open**.
4. DisableEnabler will remember this path for next time, so you usually only need to do this once.

---

## Step 2 – Pick the Mod PAK You Want to Edit

You will be editing a **mod `.pak` file** (not the base game files). This is usually a file placed in your `~mods` folder.

You can choose it in two ways:

- **Option A – Let DisableEnabler scan a folder (recommended)**
  1. Click **“Scan Mods Folder…”**.
  2. In the folder window, go to the folder that contains your mod `.pak` files  
     (for example your `~mods` folder or a folder where you keep WIP mods) and click **Select Folder**.
  3. DisableEnabler will look for `.pak` files that contain the plane list and will pick the one that “wins” in the game’s load order.
  4. The full path to that `.pak` will appear in the **PAK path** box.

- **Option B – Type or paste the path yourself**
  - Click in the **PAK path** box and type or paste the path to your `.pak` file.
  - The file name should end with `_P.pak` (for example `MyMod_P.pak`).

---

## Step 3 – Unpack the PAK and Load the Plane List

1. Check that:
   - The **UnrealPak** box shows a valid path to `UnrealPak.exe`.
   - The **PAK path** box shows the mod `.pak` you want to edit and the file name ends in `_P.pak`.
2. Click **“Unpack & Load”**.
3. DisableEnabler will now:
   - Create a new folder next to the program called something like  
     `<YourPakName>_unpacked\`.
   - Use `UnrealPak.exe` to open your `.pak` file.
   - Find the game’s internal **plane list file**.
   - Convert that data into a format it can show in the table.
   - If it finds a file called `DisableEnabler_plane_states.json` from a previous run, it will restore your old on/off choices automatically.

After this finishes, you will see a list of planes in the big table in the main window.

---

## Step 4 – Turn Planes On or Off

- **Turning planes on or off**
  - In the table, look for the **“Enabled”** column.
  - Tick the box to **enable** a plane, or untick it to **disable** it.
  - To change many planes at once, you can select several rows (using Shift or Ctrl like in other Windows apps) and then click one checkbox to apply that change to all selected planes.

- **Finding a specific plane**
  - Use the **search box** above the table.
  - You can type part of the plane’s ID (for example `f22a` or `f22a_vr`), or its numeric ID.

- **Hiding some entries from view**
  - **Hide base game**: hides the standard base‑game planes (useful if you only care about DLC or modded entries).
  - **Hide VR planes**: hides planes that are only used in VR mode.

At this point, your changes are only in the program’s memory. You still need to save and build a new `.pak` file.

---

## Step 5 – Save Changes and Build a New PAK

1. When you are happy with your on/off choices, click the button that says **“Apply & Save”** or **“Apply, Save & Pack”** (the wording may vary slightly).
2. DisableEnabler will then:
   - Apply your changes to the internal data.
   - Update the game data file using **UAssetAPI**.
   - Save a helper file called `DisableEnabler_plane_states.json` so that your current setup can be restored next time.
   - Use `UnrealPak.exe` to create a **new mod `.pak` file** with your settings.

3. The new `.pak` file is placed in the same `<YourPakName>_unpacked\` folder as the unpacked data. For example:
   - Original input: `~MyPlanes_P.pak`  
     New output: `~~~~~MyPlanes_DisableEnabler_P.pak` (the extra `~` characters help ensure it loads after the original mod).

4. To quickly open the folder where the new file is, click **“Open Output Folder”** in the program.

---

## Step 6 – Put the New PAK Into Your Mods Folder

1. In File Explorer, open the folder that contains the new `*_DisableEnabler_P.pak` file (you can use **“Open Output Folder”** to get there).
2. Copy that new `.pak` file into your ACE COMBAT 7 mods folder, for example:
   - `...\ACE COMBAT 7\Game\Content\Paks\~mods\`
3. Make sure the new file’s name (especially the `~` characters at the start) means it loads **after** the original mod you edited. This way, your new `.pak` will override the old plane list.

---

## Extra Notes

- **Settings file**
  - DisableEnabler keeps a small text file called `DisableEnabler.config` next to the program.
  - It remembers:
    - Where your `UnrealPak.exe` is.
    - Whether dark mode is on or off.
    - Whether you want to hide base‑game or VR planes in the list.
  - You do not normally need to edit this file by hand.

- **Editing the same mod again later**
  - If you unpack and edit the same `.pak` again, DisableEnabler will read `DisableEnabler_plane_states.json` so it can restore your previous on/off choices. This makes it easy to fine‑tune your setup over time.

- **Be safe with your files**
  - Always keep a copy of your original mod `.pak` files and saves somewhere else, just in case.
  - DisableEnabler only works on the `.pak` files you choose; it does not touch the core game files unless you point it at them.

---

## Credits

- **UnrealPak**
  - **Epic Games** – for the original UnrealPak binaries.
  - **FluffyQuack** – for the original UnrealPak batch script approach.
  - **Various Stack Overflow users** – for batch scripting and general coding references.

- **UAssetAPI (MIT‑licensed)**
  - This tool uses **UAssetAPI** by **atenfyr**, licensed under the **MIT License**.
  - Copyright:
    - `Copyright (c) 2020 - 2026 atenfyr`
  - The full license text is included in `UAssetAPI/LICENSE` and must remain with any redistributions of UAssetAPI.