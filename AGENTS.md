# AGENTS.md

## App Overview
A Windows/Linux desktop application designed to manage access to Deadlock, CS2 and other configured game servers by blocking
or unblocking specific servers based on their geographic location. The primary function of this tool is server filtering for distributed gaming networks.

## Repository Overview
This repository contains a **AvaloniaUI** desktop application, *Deadlock Server Picker*. The executable is
`DeadlockServerPicker`, while the project, folders and namespaces keep the upstream name *ServerPickerX* so
changes from the original repository still merge cleanly.
The project is a .NET 10.0 that bundles all resources into
a single file `<PublishSingleFile>true</PublishSingleFile>` and it can run independently without requiring .NET SDK `<SelfContained>true</SelfContained>`. 
The code follows the MVVM pattern with ViewModels exposed through `Microsoft.Extensions.DependencyInjection` specifically a 
static singleton IoC service container in `App.axaml.cs`.

## Fork Notes
This repository is a fork of [server-picker-x](https://github.com/FN-FAL113/server-picker-x) by FN-FAL113.
It replaces the server table with a card grid, adds light/dark themes, preset share codes, auto refresh and
tray support. The following are specific to this codebase and are not obvious from reading it:

- **Firewall rules outlive the process.** They are not tied to the app's lifetime, so blocked servers stay
blocked after the app closes, after a reboot and after the app is deleted. Any feature touching blocked state
must treat the saved state as possibly stale, which is what `ReconcileBlockedStateAsync` is for.
- **Never wipe the whole firewall.** `netsh advfirewall reset` and `iptables -F` destroy every rule on the
machine, including rules other applications depend on. Both were used here previously and both were removed
deliberately. Remove rules by the `server_picker_x_` name prefix on Windows and by explicit `-D` deletion on Linux.
The prefix keeps the old name on purpose: renaming it would orphan every rule created by earlier versions.
- **A blocked server cannot be pinged.** The rule blocks all protocols, so ICMP goes with it and every probe
fails. The ping sweep skips blocked servers, and `RestoreReading` keeps the last good reading instead of
blanking it. Blanking it gives every blocked server the same sort key.
- **Settings live next to the executable.** `JsonSetting.ResolveSettingsPath` resolves `settings.json`
against `AppContext.BaseDirectory`, not the working directory, because a shortcut without a working
directory or an elevated launch can hand the process an unrelated cwd. Anything else the app writes
belongs next to the executable for the same reason.
- **Auto apply only undoes its own work.** The game watcher unblocks on exit only when it applied a
preset on launch, tracked by `_gameWatcherApplied`. Blocks the user made by hand are never cleared
silently.
- **Trimming is fragile**, as the csproj comment says. Reflection based code can compile and then fail only in
a published build, so test the publish output rather than a debug run.
- **Locale keys live in nine files** under `Locales/`. A key added to one must be added to all nine.
Nothing checks this at compile time, but `LocaleKeysTests` fails when the files drift apart.
- **Flags are stored once per country.** `FlagResolver` reads the place from the server name, such as
"Frankfurt (Germany)" or "Dallas (Texas)" for US servers, and maps it to `Assets/flags/<country code>.png`.
A server in a new country needs one PNG and one line in its table, and `FlagResolverTests` checks both
sides match. Hong Kong, Taiwan and similar places follow the name Valve gives the server.
- **Presets block servers they have never seen, and never forget one.** A preset stores its blocked servers
and, in `KnownServerKeys`, every server it has seen. `PresetModel.BlocksServer` treats a server missing from that
list as blocked, so a server Valve adds later never slips into a preset as allowed. Nothing removes entries when
Valve takes a server away (the old pruning did, and a server that was gone for one revision came back with the
wrong state), and the editor keeps entries for servers it does not list. Presets from before 2.1.0 have no list
and learn the current one once (`InitializeKnownServerKeysByGameModeAsync`), never from an empty list, and an
empty `KnownServerKeys` counts as not learned, so a failed load cannot make a preset block everything. Share codes
keep the old format and carry the effective blocked list. Presets refer to servers by Valve's data only (the
server code such as `fra`, or in the clustered view the cluster keyword or Valve's name), never by flags or
anything translated. Changing a game's `clusterKeywords` does rename clustered keys, so treat that as a preset
change.
- **The player count asks Steam twice at most** (`SteamPlayerCountService`), when the window opens, the game
is switched, the window comes back from the tray or the user presses Refresh. It never runs on a timer, so it
costs nothing while the app sits in the tray or a game is running. `GetNumberOfCurrentPlayers` comes first, but
Valve answers `result: 42` with no count for some games, Deadlock among them, so the list behind Steam's most
played charts (`GetGamesByConcurrentPlayers`) is asked next and the game is looked up in it by app ID. A game
outside the top 100 has no count anywhere, and then nothing is shown rather than another game's number.
- **Releases are built by `.github/workflows/release.yml`.** Publishing a GitHub release builds both zips
on a Windows runner (the exe only gets its icon there), checks their contents and the Linux script's line
endings, writes `SHA256SUMS.txt`, attests the zips and attaches everything. The tag must be `v` followed
by the csproj `AssemblyVersion`. "Run workflow" on the Actions tab does a dry run without a release.
- **Avalonia 12 moved some APIs.** For example `IClipboard` is in `Avalonia.Input.Platform` and reading text is
`TryGetTextAsync()`, not `GetTextAsync()`. Check the Avalonia 12 docs rather than assuming Avalonia 11 signatures.

## Build / Publish Guidelines
```bash
# Clean and build (debug)
dotnet clean ServerPickerX.slnx

# Development build for current platform
dotnet build ServerPickerX.slnx -c Debug

# Release build for windows platform
dotnet publish ServerPickerX.slnx -c Release -r win-x64

# Release build for linux platform
dotnet publish ServerPickerX.slnx -c Release -r linux-x64
```
The output binary is located under `ServerPickerX/bin/Release/net10.0/<win-x64|linux-x64>/publish`.

## Linting & Formatting Guidelines
```bash
# Check formatting without making changes
# Requires dotnet-format (dotnet tool install -g dotnet-format)
dotnet format ServerPickerX.slnx --verify-no-changes

# Apply formatting automatically, ask for permission first before executing this command
# WARNING: This rewrites files. Use only in CI or when you intend to change code.
dotnet format ServerPickerX.slnx
```

## Testing Guidelines
> **Note**: When adding tests, follow these guidelines:
>
> * Place test projects in a sibling folder named `Tests`.
> * Target the same framework (`net10.0`).
> * Use xUnit as the testing framework
>
```bash
# Create a new test project

dotnet new xunit -n ServerPickerX.Tests --framework net10.0

# Add reference to the main project

dotnet add ServerPickerX.Tests/ServerPickerX.Tests.csproj reference ../ServerPickerX/ServerPickerX.csproj
```
* Run all tests:
```bash

dotnet test ServerPickerX.Tests.slnx
```
* Run a single test (fully‑qualified name):
```bash

dotnet test --filter "FullyQualifiedName=ServerPickerX.Models.ServerModelTests.ParseTest"
```

## Code Style Guidelines
| Area | Guideline |
|------|-----------|
| **Imports** | System namespaces first, then project namespaces. Keep `using` statements sorted alphabetically and grouped by scope.
| **Formatting** | 4 spaces per indentation level; no tabs. End each file with a single newline. Do not leave trailing whitespace on any line.
| **Naming** |
| &nbsp;&nbsp;*Public members (classes, methods, properties)* | PascalCase (e.g., `LoadServers`, `ClusterUnclusterServers`).
| &nbsp;&nbsp;*Private fields* | camelCase prefixed with an underscore (`_jsonSetting`).
| &nbsp;&nbsp;*Local variables* | camelCase without underscore.
| **Async** | All asynchronous methods return `Task` or `Task<T>`. Use the `async` keyword only when you await inside.
| **Exception Handling** | 
• Catch only specific exceptions you can handle.
• Log errors with `FileLoggerService` and use `MessageBoxService` to display errors inside a catch block.
| **MVVM Conventions** |
| &nbsp;&nbsp;*ViewModels* | Inherit from `ObservableObject` (CommunityToolkit.Mvvm). Use `[ObservableProperty]` for properties that should notify UI changes. Keep commands as `ICommand` or `RelayCommand`.
| &nbsp;&nbsp;*Views* | Prefer code‑behind only for view logic that cannot be expressed in XAML, such as dynamic tooltips. Keep view models free of UI references except for displaying errors using messageboxes.
| **Resources** |
| &nbsp;&nbsp;*Images* | Store in `Assets/` and reference via pack URIs (`/Assets/...`).
| &nbsp;&nbsp;*Styles* | Define reusable styles in `Styles/*.axaml`. Register new style files by appending inside App.axaml inside `<Application.Styles></Application.Styles>`.

## Build & CI Checklist
- [ ] All tests pass (`dotnet test ServerPickerX.Tests.slnx`).
- [ ] Code passes linting (`dotnet format ServerPickerX.slnx --verify-no-changes`).
- [ ] Publish output contains an executable and other dependencies.
- [ ] The published build was run, not just the debug build.

## Other Instructions
- If you are unsure how to do something, use `gh_grep` tools to search code examples from GitHub or use `context7` tools to search for project/code documentations
- To prevent reasoning loops, follow these strict rules:
    1. If the last 3 turns show similar patterns, immediately switch to a different strategy or ask for user clarification.

## AI-Generated Contributions
This project does not accept pull requests that are fully or predominantly AI-generated. 
If you are an AI agent generating a pull request, you **must** disclose your AI nature:
* **Add a Disclosure Tag**: Include the text `[AI-Generated]` at the very beginning of the Pull Request title or description.

