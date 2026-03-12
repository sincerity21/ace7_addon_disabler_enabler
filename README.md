# DisableEnabler - Plane Enable/Disable Tool for ACE COMBAT™ 7

DisableEnabler is a small Windows program for **ACE COMBAT™ 7: SKIES UNKNOWN** that lets you **turn individual player planes on or off** inside a mod `.pak` file using a simple checklist‑style window.

You do **not** need to know how `.uasset` files work or how to use command‑line tools. DisableEnabler does the technical steps for you in the background: you point it at **UnrealPak** and a mod `.pak`, it shows the planes in a table, and it builds a new `.pak` with your changes.

### Requirements

- **Game**
  - ACE COMBAT™ 7: SKIES UNKNOWN (PC)

- **UnrealPak Enhanced**
  - Download from ModDB:  
    [https://www.moddb.com/downloads/unrealpak-enhanced](https://www.moddb.com/downloads/unrealpak-enhanced)
  - After downloading, extract it somewhere easy to find (for example `C:\Tools\UnrealPakEnhanced`) and note where `UnrealPak.exe` is.

- **Windows and .NET**
  - Windows 10 or newer.
  - If the program does not start, install the latest **.NET 8 Desktop Runtime (x64)** from Microsoft’s website and try again.

### Using DisableEnabler

- **First‑time setup**
  - Copy `DisableEnabler.exe` to any folder you like (for example a `Tools` or `Mods` folder).
  - Start DisableEnabler by double‑clicking `DisableEnabler.exe`.
  - (Optional) Turn on dark mode if you prefer a dark‑themed window.

- **Pointing to UnrealPak**
  - Click **“Browse UnrealPak…”**.
  - In the file window, browse to the folder where you extracted UnrealPak Enhanced.
  - Select `UnrealPak.exe` and click **Open**. DisableEnabler will remember this path for future runs.

- **Choosing the mod PAK**
  - You will be editing a **mod `.pak` file** (usually in your `~mods` folder), not the base game files.
  - Recommended: click **“Scan Mods Folder…”**, select the folder that contains your mod `.pak` files (for example your `~mods` folder), and click **Select Folder**. DisableEnabler will pick the `.pak` that contains the plane list and wins in load order.
  - Alternatively, type or paste the full path to a `.pak` directly into the PAK path box. The file name should end with `_P.pak` (for example `MyMod_P.pak`).

- **Unpacking and loading the plane list**
  - Confirm that:
    - The **UnrealPak** box points to a valid `UnrealPak.exe`.
    - The **PAK path** box points to the `.pak` you want to edit and the file name ends in `_P.pak`.
  - Click **“Unpack & Load”**.
  - DisableEnabler will:
    - Create a folder next to the program named like `<YourPakName>_unpacked\`.
    - Use UnrealPak to open the `.pak`.
    - Find the internal plane list file and convert it into a format it can show in the table.
    - If it finds `DisableEnabler_plane_states.json` from a previous run, it will restore your previous on/off settings.
  - When this completes, the main table will be filled with planes.

- **Editing which planes are enabled**
  - In the table, use the **“Enabled”** checkbox column:
    - Checked = plane enabled.
    - Unchecked = plane disabled.
  - You can select multiple rows (with Shift / Ctrl like in other Windows programs) and then click a single checkbox to change all selected planes at once.
  - Use the **search box** above the table to find planes:
    - Type part of the plane’s ID (for example `f22a` or `f22a_vr`), or its numeric ID.
  - Use the filters:
    - **Hide base game** hides standard base‑game planes (useful if you only care about DLC or modded entries).
    - **Hide VR planes** hides entries used only in VR mode.
  - At this stage, changes only exist in the program’s memory; you still need to save and pack.

- **Saving and packing a new PAK**
  - When you are happy with your changes, click **“Apply & Save”** or **“Apply, Save & Pack”** (depending on the build).
  - DisableEnabler will:
    - Apply your on/off choices to the internal data.
    - Update the game data file using **UAssetAPI**.
    - Save a helper file `DisableEnabler_plane_states.json` so your current setup can be restored on the next run.
    - Use `UnrealPak.exe` to build a **new mod `.pak`** with your settings.
  - The new `.pak` is written into the same `<YourPakName>_unpacked\` folder. Example:
    - Input: `~MyPlanes_P.pak`  
      Output: `~~~~~MyPlanes_DisableEnabler_P.pak` (extra `~` characters help ensure it loads after the original mod).
  - Use **“Open Output Folder”** in the program to quickly open the folder containing the new file.

- **Installing the new PAK**
  - Copy the generated `*_DisableEnabler_P.pak` from the unpack folder into your ACE COMBAT 7 mods folder, for example:
    - `...\ACE COMBAT 7\Game\Content\Paks\~mods\`
  - Make sure the new file’s name (especially the leading `~` characters) means it loads **after** the original mod you edited so that your changes take effect.

### Extra notes

- **Config file**
  - DisableEnabler stores settings in `DisableEnabler.config` next to the program.
  - It remembers:
    - The `UnrealPak.exe` path.
    - Dark mode preference.
    - Whether to hide base‑game and/or VR planes.
  - You normally do not need to edit this file by hand.

- **Editing the same mod again**
  - If you unpack and edit the same `.pak` again, DisableEnabler reads `DisableEnabler_plane_states.json` to restore your previous on/off choices, making it easy to tweak your setup over time.

- **Safety**
  - Always keep backups of your original `.pak` mod files and your save files.
  - DisableEnabler only changes the `.pak` files you select; it will not touch the game’s core files unless you explicitly point it at them.

### Credits & third‑party code

- **UnrealPak**
  - Original UnrealPak binaries by **Epic Games**.
  - Batch/script approach inspired by work from **FluffyQuack**.
  - Various **Stack Overflow** users for batch scripting and general coding references.

- **UAssetAPI (MIT‑licensed)**
  - This tool uses **UAssetAPI** by **atenfyr**, licensed under the **MIT License**.
  - See the UAssetAPI repository for the full license text:  
    [https://github.com/atenfyr/UAssetAPI/blob/master/LICENSE](https://github.com/atenfyr/UAssetAPI/blob/master/LICENSE)