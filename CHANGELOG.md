(Zaphkiel fork, 2026-11-21) — Quality pass
--------
- [Fix] "Add" on a puppet axis created a second parameter when one with that name existed with another type. Adding now refuses any existing name.
- [Fix] Displaying a control could change the menu asset (bool values forced to 1, missing axes filled in), with an undo step. Displaying never writes now: a "Set 1" button and a Repair box do it on a click.
- [Fix] "Place Here" was disabled in a full menu, even to move a control within that menu.
- [Fix] Quick Setup threw on avatars without a humanoid head bone, and could run the lip sync detection on another avatar's inspector.
- [Fix] Copied controls lost their icon and submenu after an editor restart (references by session ID). Copies now store asset GUIDs; old copies still paste.
- [Fix] The first/last history buttons did nothing when that menu had been deleted.
- [Fix] Parameters declared by an Animator Override Controller's base controller were reported missing.
- [Fix] Unknown keyboard commands fell through to Delete.
- [Improvement] Cleanup asks for confirmation: Modular Avatar and VRCFury add parameters to the controllers only at build time.
- [Improvement] A puppet axis that points to a parameter of the wrong type is flagged.
- [Misc] Enums read and written by value, field names through nameof, one undo-safe path for whole-control edits, the details panel built once per action, SerializedObjects disposed.
- [Misc] 65 EditMode tests (Tests/), including one that fails if a Unity update breaks the inspector override.
- [Misc] The credits link to github.com/Dreadrith: dreadrith.com/links and the original repository are offline.

(Zaphkiel fork, 2026-11-21) — Parent menus
--------
- [Feature] The Menu inspector lists the menus that open it as a submenu (and through which controls), and the scene avatars using it as their root menu. Opening a parent selects the linking control.
- [Feature] Menus that open themselves as a submenu are flagged.
- [Misc] The lookup uses a reverse index built in the background from saved dependencies (about 160 ms for 420 menus, spread over editor frames), updated per saved, moved or deleted menu, and confirmed in memory so unsaved links count too.

(Zaphkiel fork, 2026-11-21) — based on v1.6.0
--------
- [Misc] Moved into Assets/Zaphkiel/Editor/VRCSDKPlus, removed from the VCC manifest. Assembly renamed Zaphkiel.Editor.VRCSDKPlus, namespace Zaphkiel.VRCSDKPlus.
- [Misc] Split the single 3,500-line file into Core, UI, Parameters, Menus and Avatar modules.
- [Improvement] Both inspectors rebuilt on UI Toolkit. Parameters inspector repaint: about 24 ms → 2 ms with 51 parameters, 29 ms → 3 ms with 152. Menu inspector: 4.3 ms → 1.9 ms.
- [Feature] Synced bits bar on the Parameters inspector, counted with the SDK's CalcTotalCost (SDK 3.11 bit widths).
- [Feature] Resizable columns; parameter names are no longer cut off or overlapped by the type hint.
- [Feature] "[SDK+] Toggle Editor" now persists across recompiles.
- [Fix] Unity 6: the editor override used CustomEditorAttributes fields that no longer exist (NullReferenceException on load).
- [Fix] Searching with characters such as ( [ + * ? threw an exception on every repaint (the search was a regex).
- [Fix] Merge Parameters dropped "saved" and the SDK 3.11 fields (bit width, lerp duration, representation).
- [Fix] Changing a parameter type now resets its bit width and representation, and clamps its default value, like the SDK does.
- [Fix] Adding, removing or cleaning parameters keeps the SDK's isEmpty flag in sync.
- [Fix] Moving a control within the same menu could re-insert it at its old place; moving into an empty menu duplicated it instead of moving it.
- [Fix] Compact two-axis puppets labeled the Up label slot as "Left" and the Left slot as "Up".
- [Fix] The avatar selector could pick the wrong avatar, or throw every repaint, after an avatar was deleted from the scene.
- [Fix] Dropdown highlight colors were shared with other textures and changed at random.
- [Fix] Creating a submenu from a styled control name produced file names with rich-text tags.
- [Fix] Unity 6: the "Clear" icon (winbtn_win_close) no longer exists; replaced with CrossIcon.

(v1.5.1)
--------
- [Misc] Changed license to GPL-3.0 and shared the source code.
- [Fix] Fixed pasting or moving a control into an empty menu throwing an error
- [Fix/Misc] New controls' values will now be set to 1 by default like original VRC does.

(v1.5.0)
--------
- [Feature] Added Keyboard commands (shortcuts) and context menus (right click) to Parameters and Menu Editors
	- Parameter editor can now handle Ctrl+D and 'DEL' for Duplicate and Delete respectively.
	- Menu editor can now handle Ctrl+C, Ctrl+X, Ctrl+V, Ctrl+D, and 'DEL' for Copy, Cut, Paste, Duplicate and Delete respectively.
- [Improvement] Parameters and Controls now have a 'smart increment' to them. (@buddy)
	- Newly added parameters will be unique by appending/incrementing numbers.
	- Duplicated control's name will count up if it has a number.
	- Duplicated control's parameter name will count up if it's a bool and has a number (Disabled when holding shift).
	- Duplicated control's parameter value will count up if it's an int (Disabled when holding shift).
- [Improvement] Submenus created through editor will use the control name instead of the parent menu name. (@m1s0)
- [Fix] Fixed parameter editor rendering when unity is not in fullscreen.
- [Misc] Added unity version to package.json to suppress VCC warning about the version.

(v1.4.0)
--------
- [Feature] Double clicking a submenu control will now switch to that submenu
- [Improvement] Decent performance boost to parameters editor.
- [Fix] Fixed compatibility with 2022 SDK
- [Fix] Fixed merged parameters editing the source parameters
- [UI] Buttons will now change cursor
- [Misc] Changelog will follow the same format as my other scripts from here on.

## [1.3.5] - 2023-11-30
### Fixed
- Fixed the VPM structure of the package

## [1.3.4] - 2023-04-02
### Fixed
- Fixed a code issue that used exponentially more performance the more ex parameters you have

## [1.3.3] - 2023-04-02
### Added
- Support for 'synced' field to Expression Parameters
- Delete Expression Parameters with the Delete button when focused
- Shortcut to search expression parameters: Ctrl + F
### Fixed
- Fixed Text field in Expression Parameters always selecting entire text when clicked
- Fixed case where Editors might not override and throw an error. Possibly also crashing.
- Fixed script throwing an error when using a debugger through an IDE
### UI
- Reworked UI to be more stable and less funky when scrunched
- Type of the controller parameter is now displayed on the expression parameters

## [1.3.1] - 2022-08-27
### Added
- Added "New" next to Empty SubMenu to quickly create a new SubMenu
### Fixed
- Fixed 'Toggle Editor' not working as intended if the VRCSDK is imported through VCC

## [1.3.0] - 2022-08-27
### Added
- Added Styling, allows bold, italic and color selection

## [1.2.1] - 2022-06-28
### Added
- Added parameter filter in expression parameters
### Fixed
- Fixed Parameter value field missing in non-compact mode

## [1.2.0] - 2022-06-17
### Added
- Implemented Expressions Menu editor
### Improvements
- Improved performance of Expression Parameters editor

## [1.0.1] - 2022-06-03
### Fixed
- Fixed parameters in a row all showing warnings after the first warning
- Fixed cleanup removing all parameters if no descriptor is found/selected
- Fixed error when merging a newly created Parameters asset

## [1.0.0] - 2022-05-29
### Added
- Implemented official [Unity Package layout] (https://docs.unity3d.com/2019.4/Documentation/Manual/cus-layout.html) 