# Changelog

## [2.0.0](https://github.com/ZaphkielTools/VRCSDKPlus/releases/tag/v2.0.0) (2026-11-21)

First release of the Unity 6 fork of VRCSDK+ 1.6.0 by Dreadrith.

### ⚠ BREAKING CHANGES

* Requires Unity 6000.0 and VRChat Avatars SDK 3.11.0 or later.
* The package is `dev.zaphkiel.vrcsdkplus`, with assembly `Zaphkiel.Editor.VRCSDKPlus` and namespace `Zaphkiel.VRCSDKPlus`. Installing it from the VCC removes `com.dreadscripts.vrcsdkplus`.

### Features

* Both inspectors run on UI Toolkit. The Parameters inspector repaints in 2 ms instead of 24 ms with 51 parameters, and in 3 ms instead of 29 ms with 152.
* Synced bits bar on the Parameters inspector, computed with the SDK's `CalcTotalCost`, SDK 3.11 bit widths included.
* Resizable columns in the parameter table. Long names no longer overlap the type hint.
* Parent menus: the Menu inspector lists the menus that open it as a submenu, the controls that do it, and the scene avatars that use it as their root menu. Opening a parent selects the linking control, and a menu that opens itself is flagged.
* A puppet axis that points to a parameter of the wrong type is flagged.
* Cleanup asks for confirmation, since Modular Avatar and VRCFury add parameters to the controllers at build time.
* "[SDK+] Toggle Editor" keeps its choice across recompiles.
* Copied controls store asset GUIDs and keep their icon and submenu after an editor restart. Copies made with VRCSDK+ 1.6.0 still paste.
* Both inspectors credit the Unity 6 fork under the original credits.

### Bug Fixes

* Unity 6: the inspector override read `CustomEditorAttributes` fields that no longer exist and threw a NullReferenceException on load.
* Unity 6: the Clear icon `winbtn_win_close` no longer exists. The inspectors use `CrossIcon`.
* Displaying a control changed the menu asset, forcing bool values to 1 and filling in missing axes. A "Set 1" button and a Repair box now do it on a click.
* "Add" on a puppet axis created a second parameter when one with that name existed with another type.
* Searching with `(`, `[`, `+`, `*` or `?` threw an exception on every repaint.
* Merge Parameters dropped `saved` and the SDK 3.11 fields: bit width, lerp duration and representation.
* Changing a parameter type resets its bit width and representation and clamps its default value, like the SDK.
* Adding, removing or cleaning parameters keeps the SDK's `isEmpty` flag in sync.
* Parameters declared by the base controller of an Animator Override Controller showed as missing.
* Moving a control within its menu could put it back at its old place, and moving it into an empty menu duplicated it.
* "Place Here" stayed disabled in a full menu, even to move a control within that menu.
* The first and last history buttons did nothing when that menu had been deleted.
* Compact two-axis puppets swapped the Up and Left label slots.
* The avatar selector could pick the wrong avatar, or throw on every repaint, after an avatar was deleted.
* Quick Setup threw on avatars without a humanoid head bone, and could run lip sync detection in another avatar's inspector.
* Dropdown highlight colors changed at random.
* Creating a submenu from a styled control name put rich-text tags in the file name.
* Unknown keyboard commands deleted the selected entry.

### Miscellaneous

* The code lives in Core, UI, Parameters, Menus and Avatar folders and has 65 EditMode tests.
* The credits link to github.com/Dreadrith, since dreadrith.com/links and the original repository are offline.

## 1.5.1
- [Misc] Changed license to GPL-3.0 and shared the source code.
- [Fix] Fixed pasting or moving a control into an empty menu throwing an error
- [Fix/Misc] New controls' values will now be set to 1 by default like original VRC does.

## 1.5.0
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

## 1.4.0
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