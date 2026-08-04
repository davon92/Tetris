# Adventure Battle Tetris — Vertical Slice Plan

## Game promise

An original character-driven adventure where conversations, choices, and relationships lead into competitive falling-block battles. The same battle system also supports a polished standalone solo mode, CPU versus, and local two-player play.

The release should use an original title, characters, visual identity, sound, and terminology. Treat “Tetris” as a development shorthand and confirm trademark/licensing constraints before publishing commercially.

## Core loop

1. Explore a story node presented like a visual novel.
2. Talk, choose a response, and update relationship/story variables.
3. Enter a battle with rules selected by the story encounter.
4. Resolve the outcome back into dialogue.
5. Unlock the next scene, rematch, or alternate branch.

## Battle-screen direction

Use the theatrical shape of classic console puzzle battles without copying their
licensed characters or artwork:

- Two tall playfields frame the left and right sides.
- The center column is a character stage, not unused space.
- Next-piece, score, combo, and incoming-attack information stack clearly in the
  center.
- Character portraits and reactions make line clears, garbage attacks, danger,
  victory, and defeat feel like story events.
- Each encounter can skin the frame and stage while keeping the board geometry
  and competitive information consistent.

### Reference-study notes

The N64 and PlayStation story-mode footage establishes a consistent presentation
grammar to adapt with original characters and artwork:

- Story scenes use full character staging and a large readable dialogue panel.
- Battles preserve the location theme while transforming it into a two-well
  theatrical set.
- The center stack prioritizes level, next piece, score, combo, and character
  reactions.
- Characters remain visible during play and react to attacks, danger, victory,
  and defeat.
- Pre-battle dialogue and post-battle results happen inside the same visual
  world, so the battle feels like part of the scene rather than a detached mode.
- Special attacks need clear anticipation, travel, impact, and counter feedback.

## Pixel-perfect rendering baseline

- `PlayArea` is the authoritative rectangular Unity 2D Grid.
- One logical board cell is exactly one 1×1 Grid cell.
- Board origins use integer Grid coordinates; cell centers come from
  `Grid.GetCellCenterLocal`.
- Tetromino blocks use the existing 16×16 sprite at 16 pixels per unit.
- Sprite filtering stays Point, mipmaps stay disabled, and texture compression
  stays disabled.
- The URP Pixel Perfect Camera owns orthographic sizing at 16 assets PPU and a
  640×480 reference resolution. Gameplay code must not override its size.
- Decorative frames and effects may animate freely, but settled blocks and board
  geometry remain grid-snapped.

## Current vertical slice

- Seven-bag randomizer for all seven tetrominoes.
- Movement, soft drop, hard drop, clockwise/counter-clockwise rotation, full SRS piece states and kick tables, hold, gravity, lock delay, line clears, scoring, levels, ghost pieces, and top-out.
- The seven existing tetromino prefabs provide the active, ghost, and locked-cell artwork.
- Battle garbage, attack cancellation, combos, and winner detection.
- Solo, versus CPU, and local versus modes.
- A pixel-aligned main menu with Story Mode, selectable CPU difficulty, and
  local two-player entry points.
- Both versus routes now pass through a horizontal character-select screen.
  Player one chooses first, followed by the CPU rival or player two. Lyra and
  Bram are playable starters, with four persistent unlock slots using a shared
  mystery-character portrait until their final characters and artwork exist.
- A playable Moon Gate prologue starring the original rivals Lyra and Bram:
  opening dialogue, one response choice, a named CPU encounter, win/loss
  dialogue, rematch, and return-to-menu flow.
- Story Mode opens on a new game / load game page before the prologue starts,
  backed by ten save slots.
- A story pause menu (Escape or Start) with resume, save, load, and return to
  title. Pressing pause again resumes; returning to the title asks first, and
  overwriting an occupied slot asks first.
- Per-chapter playtime tracking written into every slot alongside the chapter
  title, the line the player stopped on, and a timestamp.
- A statistics document recording total and story playtime, matches per mode,
  and player one's win/loss record per character.
- A 640×480 virtual presentation canvas that scales cleanly at modern
  resolutions. The battle HUD now includes next-piece previews, larger
  score/line/level panels, story character portraits, a versus badge, and a
  themed encounter title.
- Two keyboard layouts plus support for the first two Input System gamepads.
- A deterministic heuristic CPU with Easy, Normal, and Hard profiles. Easy uses
  slower inputs, longer thinking/drop pauses, and chooses imperfectly among
  several reasonable placements.
- A first battle-feedback layer with piece-lock sparks, line-clear flashes,
  visible magic attacks traveling between wells, garbage-impact bursts, and
  pixel-snapped board shake.
- A `StoryBattleBridge` that lets dialogue request a named CPU battle and receive the result without depending on board internals.
- EditMode coverage for bounds, line clearing, garbage, the seven-bag
  invariant, save/load round trips, slot recovery, pause-menu branching, title
  navigation, and the statistics counters.

## Controls

### Player 1

- Move: A / D
- Soft drop: S
- Rotate: W / Q
- Hard drop: Space
- Hold: Left Shift or C

### Player 2

- Move: Left / Right arrows
- Soft drop: Down arrow
- Rotate: Up arrow / Right Ctrl
- Hard drop: Enter
- Hold: Right Shift

### Modes

- Main menu: Story Mode, Versus CPU, or Versus Player
- Story Mode: New Game or Load Game
- Menu navigation: Arrow keys or W/S, Enter/Space to confirm, Escape to go back
- Gamepad menu navigation: D-pad, South button to confirm, East button to go back
- During development, 1/2/3 still jump directly to Solo/Versus CPU/Local Versus
- R: Restart/rematch
- Escape or a gamepad Start button: open the pause menu in story mode, or
  return to the main menu from a battle

## Story integration

Use Yarn Spinner 3 for dialogue and variables. Keep Yarn responsible for narrative flow and `StoryBattleBridge` responsible for crossing into gameplay.

The current Moon Gate prologue is deliberately code-driven scaffolding. It
proves the complete story-to-battle-to-result seam before Yarn is added. When
Yarn Spinner is installed, migrate its lines and choices into Yarn nodes while
keeping `StoryBattleBridge` as the battle boundary.

Suggested Yarn-facing commands:

```text
<<battle "chapter1_rival">>
<<if $chapter1_rival_won>>
    Rival: I underestimated you.
<<else>>
    Rival: Train and challenge me again.
<<endif>>
```

Implementation sequence:

1. Install Yarn Spinner and create a dedicated `StoryScene`.
2. Build a visual-novel dialogue view with nameplate, portrait slots, text box, choices, skip, backlog, and auto mode.
3. Register a Yarn command that calls `StoryBattleBridge.RequestBattle`.
4. Pause Yarn while the battle scene is active.
5. Write the result to Yarn variable storage, return to the story scene, and continue the node.
6. Add Yarn's variable storage and current node to `StorySaveData`, which
   already persists chapter progress, playtime, and slot metadata.

## Save data and statistics

Both systems write JSON through one `IJsonStore` seam. `FileJsonStore` keeps a
document per key under `persistentDataPath/saves`, writing to a temporary file
and replacing the real one so an interrupted write cannot corrupt a slot.
`MemoryJsonStore` backs the EditMode tests.

- `SaveSlotCatalog` owns the ten story slots and caches a `SaveSlotInfo`
  summary per slot, so drawing the list never touches the disk.
- `StorySaveData` is the serialised payload: chapter id, beat, line index,
  response, battle result, chapter playtime, a dialogue preview, and a UTC
  timestamp. A slot from a newer build, or one that fails to parse, is listed
  as damaged and refuses to load rather than throwing.
- `StoryDirector.Capture`/`Restore` are the only seam between the chapter and
  the save layer. Restore clamps every index against the current script, so
  editing authored content cannot break an existing save, and it refuses a save
  whose chapter id does not match.
- `GameStats` accumulates counters in memory and flushes on meaningful beats —
  match end, story save, returning to the title, quit, and once a minute of
  play — so nothing writes to disk mid-match.

When Yarn Spinner lands, its variable storage becomes another field on
`StorySaveData`; the slot browser, pause menu, and catalog do not change.

## Battle rules to tune

- Garbage table and combo bonuses.
- Lock delay and reset cap.
- Rotation feel, input buffering, and lock-delay reset limits.
- Back-to-back bonuses, T-spins, perfect clears, and garbage timing.
- CPU difficulty presets (thinking delay, search depth, evaluation weights, intentional mistakes).
- Story encounter modifiers such as faster gravity, pre-filled garbage, limited hold, target score, or survival turns.

## Milestones

### M1 — Battle prototype (now)

Prove the complete match loop, CPU behavior, local controls, garbage exchange, and top-out.

### M2 — Story/battle vertical slice (playable)

One original scene, two characters, one choice, one battle, and two outcome
branches are playable, with ten save slots, a pause menu, and playtime and
match statistics. Next: migrate dialogue to Yarn Spinner and persist its
variable storage into the existing save slots.

### M3 — Presentation pass

Build on the existing authored tetromino sprites with animation, particles, sound, music, responsive UI, accessibility options, and controller rebinding. Replace the temporary IMGUI HUD.

### M4 — Content tools

Encounter data assets, CPU profiles, battle modifiers, story validation, localization-ready dialogue, and editor utilities for writers.

### M5 — Production

Campaign content, arcade ladders, difficulty tuning, full QA matrix, platform builds, performance work, credits, legal review, and storefront preparation.

## BMAD decision

Do not add BMAD yet. This is still a small vertical slice, and the project benefits more from a short design document, tests, and disciplined milestones than from a large agent/workflow layer. Reconsider BMAD Game Dev Studio when one of these becomes true:

- multiple contributors or AI agents work on separate features;
- the campaign has enough content to require formal story, architecture, QA, and release workflows;
- milestone scope is repeatedly drifting;
- design decisions need durable review/approval records.

At that point, adopt only the Game Dev Studio workflows that solve an observed coordination problem.
