# The DisableEnabler

DisableEnabler is a modding tool for **ACE COMBAT™ 7: SKIES UNKNOWN** that lets you **turn individual playable planes on or off** inside a mod `.pak` file using a simple checklist‑style window.

Under the hood, it works on the game’s `PlayerPlaneDataTable` asset (the data table that tells the game which planes exist and whether they are enabled/disabled), so you don’t have to edit that file manually.

Doesn't involve users needing to manually modify files and unpack to disable; just run the tool, untick which planes you want to disable, and it'll do the job.

**Download the program here:** [Nexus Mods — Ace Combat 7 Disable-Enabler](https://www.nexusmods.com/acecombat7skiesunknown/mods/2154?tab=files)

The program was vibe‑coded with Cursor Pro.

### Requirements (to run a build)

- **UnrealPak Enhanced** — [Download from ModDB](https://www.moddb.com/downloads/unrealpak-enhanced). Extract it and point the tool at `UnrealPak.exe` (usually under `Engine\Binaries\Win64`). This tool was built and tested against that specific UnrealPak Enhanced build.
- **Windows 10+** and the **.NET 8 Desktop Runtime (x64)** if the exe does not start: [.NET 8 download](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-8.0.419-windows-x64-installer).

### Build yourself

1. **Install the .NET 8 SDK** (Windows x64) from the [.NET download page](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).

2. **Clone this repository**
   ```bash
   git clone https://github.com/sincerity21/ace7_addon_disabler_enabler.git
   cd ace7_addon_disabler_enabler
   ```

3. **Build Release**
   ```bash
   cd DisableEnabler
   dotnet build -c Release
   ```

4. **Run the output**
   - Exe and bundled files land in:
     `DisableEnabler\bin\Release\net8.0-windows\`
   - Open `DisableEnabler.exe` from that folder (keep `addon_database.json` next to it).

5. **Optional: publish a self-contained folder**
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained false
   ```
   Output is under `DisableEnabler\bin\Release\net8.0-windows\win-x64\publish\` (adjust if your SDK layout differs).

You still need **UnrealPak Enhanced** separately; it is not built by this project.

### Extra notes

- **Config file** — `DisableEnabler.config` next to the exe remembers UnrealPak path, last `~mods` folder, dark mode, and hide filters. Optional: `AddonDatabaseUrl=https://...` to override the GitHub raw update URL.

- **Addon database (`addon_database.json`)**
  - Shipped next to `DisableEnabler.exe` and updated automatically from GitHub on startup when a newer `revision` is published.
  - Maps each `PlaneStringID` to **PlaneName**, optional **Notes** (Mod column), and optional **URL** (clickable Mod link).
  - **Publish workflow:** edit [`DisableEnabler/addon_database.json`](DisableEnabler/addon_database.json), bump `revision`, push to `main`. Users get the update on the next app start.

- **Safety** — Keep backups of original `.pak` mods and saves. The tool only changes the `.pak` files you select.

### Credits & third‑party code

- **UnrealPak**
  - Original UnrealPak binaries by **Epic Games**.
  - Batch/script approach inspired by work from **FluffyQuack**.
  - Various **Stack Overflow** users for batch scripting and general coding references.

- **UAssetAPI (MIT‑licensed)**
  - This tool uses **UAssetAPI** by **atenfyr**, licensed under the **MIT License**.
  - Full license: [UAssetAPI LICENSE](https://github.com/atenfyr/UAssetAPI/blob/master/LICENSE)
