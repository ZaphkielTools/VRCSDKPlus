# VRCSDK+ (Zaphkiel fork)

Fork of VRCSDK+ 1.6.0 by **[Dreadrith](https://github.com/Dreadrith)** (menu inspector in collaboration with **[@foxscore](https://github.com/foxscore)**), licensed under GPL-3.0 (see `LICENSE.md`).
Modified by Zaphkiel on 2026-11-21: split into modules, both inspectors rebuilt on UI Toolkit, updated for Unity 6 and VRChat SDK 3.11. See `CHANGELOG.md`.

The original repository (github.com/Dreadrith/VRCSDKPlus), its releases and the DreadScripts VPM listing were offline when checked on 2026-11-21. Its last archived page is on the [Wayback Machine](http://web.archive.org/web/20241229010952/https://github.com/Dreadrith/VRCSDKPlus). The untouched 1.6.0 package, from the VCC cache, is the first commit of this repository.

This fork is not distributed through the VCC: do not also install the VCC package, or both copies will register their inspectors and menu items.

VRCSDK+ replaces the VRChat SDK inspectors of Expression Parameters and Expression Menus. It does not modify the SDK. Right-click an asset → **[SDK+] Toggle Editor** to switch back to the SDK inspector; the choice is remembered.

## Expression Parameters inspector
- Synced bits bar, counted like the SDK (bit widths of SDK 3.11 included).
- Table with resizable columns (Name, Type, Default, Saved, Synced), reorderable, virtualized.
- Search by name (Ctrl+F), case-insensitive.
- Picker of the parameters declared by the active avatar's controllers, with their controller type shown next to the name.
- Warnings for blank, duplicate, and missing parameters; "Add" puts a missing parameter in one of the avatar's controllers; "Cleanup" removes the faulty ones.
- Merge another Expression Parameters asset (every field is copied).
- Ctrl+D duplicates, Delete removes; right-click a row for the same actions.

## Expression Menu inspector
- Parent menus: every menu that opens this one as a submenu, with the controls that do it, and the avatars of the open scenes that use it as their root menu. Clicking a parent opens it with that control selected; "‹" comes back. A menu that opens itself is flagged.
- Menu history (first, previous, next, last).
- Controls list with copy, paste (values or as new), duplicate with smart increment, cut and place across menus. Ctrl+C / X / V / D and Delete work on the list; Escape cancels a move.
- Control details: name, type (with parameter conversion), color/bold/italic style, icon, parameter picker with type-matched value, "Add" to the expression parameters, submenu creation, puppet axes and labels.
- Compact mode from the Inspector window's ⋮ menu.

## Avatar descriptor
- Right-click the VRC Avatar Descriptor → **[SDK+] Quick Setup**: view position from the eyes, eye bones and look rotations, blink blend shape, lip sync auto-detection.

## Layout
- `Core/` — inspector override, avatar context, expression parameter rules, text helpers, logging.
- `UI/` — shared UI Toolkit elements and `VRCSDKPlus.uss`.
- `Parameters/` — Expression Parameters inspector.
- `Menus/` — Expression Menu inspector.
- `Avatar/` — Quick Setup.
- `Tests/` — EditMode tests (Window → General → Test Runner).

If you like VRCSDK+, consider supporting its author: [Ko-fi](https://ko-fi.com/Dreadrith) · [Gumroad](https://dreadrith.gumroad.com/l/VRCSDKPlus).
