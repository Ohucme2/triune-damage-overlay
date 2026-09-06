# TRIUNE Damage Overlay v1.4

A lightweight Windows overlay that watches one EverQuest character log and displays only damage attributable to that character as rising, fading combat text.

**[Download the latest Windows release](https://github.com/Ohucme2/triune-damage-overlay/releases/latest)**

Every active number is assigned its own row so rapid hits remain readable instead of overlapping.
Hits stay in chronological order and scroll upward continuously. Every hit owns its own fade timer, and the control window includes adjustable scroll-speed and fade-duration controls plus a color legend.

The version number is printed in both the Windows title bar and the main heading.

## Features

- Transparent, always-on-top, click-through floating combat text
- Strict local-character filtering from a standard EverQuest log file
- Separate colors for slashing, piercing, crushing, kicks, bashes, backstabs, ranged attacks, spells, procs, DoTs, and critical hits
- Critical hits displayed in red
- Chronological, non-overlapping upward scrolling
- Independent fade timer for every hit
- Adjustable text size, scroll speed, and fade duration
- Movable positioning handle, tray controls, and global **Ctrl+Shift+D** shortcut
- No injection, game-memory reading, network service, or MacroQuest dependency

## What it deliberately excludes

- Other players and boxed characters
- Pets
- Incoming damage
- Generic damage lines that cannot be safely attributed to the selected character

## Quick start

1. In EverQuest, enter `/log on`.
2. Run `TriuneDamageOverlay.exe`.
3. Choose the client's `eqlog_Character_Server.txt` file.
4. Select **Preview all colors** to verify the display.
5. Drag the gold handle to position the text.
6. Double-click the handle or press **Ctrl+Shift+D** to show or hide the controls.

Closing or hiding the control window does not stop floating combat text. Use the tray menu to exit completely.

The app starts reading at the end of the selected log so old fights are never replayed.

## Build from source

Requirements: Windows and the .NET 10 SDK.

```powershell
dotnet build src/TriuneDamageOverlay/TriuneDamageOverlay.csproj -c Release
dotnet run --project tests/TriuneDamageOverlay.SelfTest/TriuneDamageOverlay.SelfTest.csproj -c Release
```

## Accuracy and compatibility

EverQuest servers can use slightly different combat-log wording. The parser rejects lines it cannot safely attribute to the selected character instead of guessing. If your own damage is missing, open an issue with a few relevant combat lines after removing any names or chat you do not want to share.

## Disclaimer

This is an independent fan-made utility. It is not affiliated with, endorsed by, or sponsored by Daybreak Game Company. EverQuest is a trademark of Daybreak Game Company LLC. Check the rules for the server where you play before using third-party utilities.

## License

[MIT](LICENSE)
