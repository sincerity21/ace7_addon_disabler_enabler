# The DisableEnabler

DisableEnabler is a modding tool for **ACE COMBAT™ 7: SKIES UNKNOWN** that lets you **turn individual playable planes on or off** inside a mod `.pak` file using a simple checklist‑style window.

Doesn't involve users needing to manually modify files and unpack to disable; just run the tool, untick which planes you want to disable, and it'll do the job.

The program was vibe‑coded with Cursor Pro.

### Requirements

- **UnrealPak Enhanced**
  - [Download from ModDB](https://www.moddb.com/downloads/unrealpak-enhanced)
  - After downloading, extract it somewhere easy to find (for example `C:\Tools\UnrealPakEnhanced`) and note where `UnrealPak.exe` is (should be inside `UnrealPakEnhanced\Engine\Binaries\Win64`).
  - This tool was built and tested against that specific UnrealPak Enhanced build; other versions may not work as intended.

- **Windows and .NET**
  - Windows 10 or newer.
  - If the program does not start, install the latest **.NET 8 Desktop Runtime (x64)** from the official [.NET 8 page](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-8.0.419-windows-x64-installer) and try again.

### Using DisableEnabler

1. **Start the tool**
   - Put `DisableEnabler.exe` anywhere you like (for example in a `Tools` or `Mods` folder) and double‑click it to run.

2. **Point it at UnrealPak**
   - Click **“Browse UnrealPak…”**, browse to where you extracted UnrealPak Enhanced, select `UnrealPak.exe`, and click **Open**.  
     (This is remembered for next time.)

3. **Pick a mod PAK**
   - Click **“Scan Mods Folder…”** and choose the folder that contains your mod `.pak` files (usually your `~mods` folder), or type/paste the full path to a `.pak`.

4. **Load the plane list**
   - Click **“Unpack & Load”**. DisableEnabler unpacks the PAK, finds the plane list, and fills the table with all planes it finds.

5. **Choose which planes are enabled**
   - Tick or untick the **“Enabled”** checkboxes to turn planes on or off.  
   - Use the search box and the **Hide base game / Hide VR planes** checkboxes to quickly filter the list.

6. **Build the new PAK and install it**
   - Click **“Apply, Save & Pack”**, then use **“Open Output Folder”** to get the new `*_DisableEnabler_P.pak`.  
   - Copy that file into your `~mods` folder so it loads after the original mod.

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
  - Full license: [UAssetAPI LICENSE](https://github.com/atenfyr/UAssetAPI/blob/master/LICENSE)
