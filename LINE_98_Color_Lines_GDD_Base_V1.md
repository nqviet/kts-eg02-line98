# LINE 98: COLOR LINES
## GDD Base Reference — V1

> **Positioning:** The classic Color Lines puzzle, rebuilt for modern mobile.

---

# 1. Product Overview

**Working Product Name:** LINE 98: Color Lines  
**Brand:** LINE 98  
**Genre:** Evergreen Puzzle  
**Platform:** Android first (production) + Win x64 (development)
**Engine:** Unity  
**Orientation:** Portrait  
**Business Model:** Ads + Remove Ads IAP  
**Product Strategy:** Low-cost, long-tail organic acquisition, evergreen lifecycle.

## Core Principle

**Simple Core + Premium Execution + Long Lifetime**

The original Line 98 mechanic is the asset. Do not replace the core puzzle simply because it is old.

Modernize:
- Visual quality
- Animation and game feel
- Sound
- UX
- Daily engagement
- Statistics
- Cosmetics
- Monetization
- Onboarding

Keep the fundamental puzzle intact.

---

# 2. Core Gameplay

## Board

- 9 × 9 grid
- 81 cells
- Portrait mobile layout
- Board occupies the visual center of the screen
- Responsive to common Android aspect ratios

## Balls

- 7 colors
- One ball occupies one cell
- Balls cannot overlap
- Empty cells are walkable

## Player Action

1. Player taps a ball.
2. Player taps an empty destination cell.
3. If a valid path exists:
   - Ball moves to destination.
   - Movement follows a visually pleasing path animation.
   - Board resolves.
   - Check whether a line has been created.
4. If no line is created:
   - Spawn new balls.
5. If no valid path exists:
   - Do not move the ball.
   - Give subtle visual feedback.

## Line Rules

A line is created when **5 or more balls of the same color** are connected:

- Horizontally
- Vertically
- Diagonally in either direction

When a valid line exists:
- Clear all balls in that line.
- Award score.
- Play satisfying VFX and audio.
- Do not spawn new balls after a successful clear.

If multiple lines are created simultaneously:
- Clear all relevant balls.
- Award combined score.
- Apply combo or perfect bonus.

## Spawn

Default:
- Spawn 3 new balls after a non-clearing move.
- Spawn only into empty cells.
- Colors are selected from the configured 7-color palette.

The next 3 balls must always be visible before they appear.

## Game Over

Game ends when no valid empty cells remain.

Show:
- Final score
- Best score
- Longest line
- Total lines cleared
- Total moves
- Continue option
- New Game option

---

# 3. Game Modes

## Classic

Primary endless mode.

- Standard 9 × 9 board
- Standard rules
- High score
- No artificial level progression

## Daily Challenge

Generate a deterministic challenge based on:

`YYYY-MM-DD`

Requirements:
- Same seed for all players
- Same date = same challenge
- Same spawn sequence
- Daily score
- Daily completion state
- 7-day streak
- Architecture ready for future leaderboard integration

V1 does not require a custom backend.

## Zen Mode

Relaxed version of the core game.

- No score pressure
- Reduced effects
- Calm presentation
- Ambient audio
- No aggressive monetization

Keep implementation lightweight.

---

# 4. Core Technical Architecture

Suggested project structure:

```text
Assets/
  _Project/
    Core/
    Gameplay/
    UI/
    Audio/
    VFX/
    Data/
    Services/
    Monetization/
    Analytics/
    Editor/
    Tests/
```

Suggested core classes:
- GameManager
- GameState
- BoardManager
- BoardCell
- Ball
- BallColor
- BallSpawner
- BallQueue
- PathfindingService
- LineDetectionService
- ScoreService
- GameSession
- DailyChallengeService
- SaveService
- StatisticsService

Use modular C# architecture.

Do not over-engineer.

---

# 5. Pathfinding

Use **BFS**.

Board maximum:
- 9 × 9
- 81 nodes

Requirements:
- 4-directional movement
- No diagonal pathfinding
- Balls act as obstacles
- Destination must be empty
- Return path for animation
- Return false when unreachable

Do not use a third-party pathfinding system.

---

# 6. Line Detection

After a ball is placed, check four axes:

1. Horizontal
2. Vertical
3. Diagonal \
4. Diagonal /

For each axis:
- Count connected same-color balls in both directions.
- Include the placed ball.

If total is `>= 5`:
- Return all connected balls belonging to the line.

Requirements:
- Support simultaneous lines.
- Avoid duplicate clearing.
- Handle edge and corner cases.

---

# 7. Scoring

Suggested baseline:

| Line Length | Score |
|---|---:|
| 5 | 100 |
| 6 | 180 |
| 7 | 300 |
| 8 | 500 |
| 9 | 800 |

Multiple lines in the same move:
- Award all line scores.
- Apply a small combo multiplier.

Keep score configuration data-driven where practical.

---

# 8. Game Feel

This is a major production priority.

## Ball Movement

Sequence:

`Tap → Selection Feedback → Path Preview → Ball Move → Soft Bounce → Settle`

## Clear Sequence

`Connect → Pulse → Glow → Burst → Particles → Score Popup`

Longer lines must feel more rewarding.

### Feedback Tiers

- 5 balls: satisfying standard clear
- 6–7 balls: stronger feedback
- 8 balls: major payoff
- 9+ balls: special Perfect Line presentation

Effects must remain clean and readable.

---

# 9. Visual Direction

## Direction

**Modern 2.5D Premium Casual**

Do not make this:
- Pixel art
- A Windows 98 clone
- A literal retro emulator

Target visual characteristics:
- Colorful
- Clean
- Polished
- Slightly toy-like
- Soft depth
- Rounded UI
- Modern mobile composition
- Premium casual puzzle aesthetic

## Ball Theme — V1

**Crystal / Gem Balls**

7 colors:
- Red
- Orange
- Yellow
- Green
- Cyan / Blue
- Purple
- Pink

Material direction:
- Glossy
- Glass / crystal
- Soft plastic or gemstone-like
- Clear color readability

## Board

Use:
- Subtle 3D depth
- Rounded cells
- Soft shadows
- Elegant background
- Clear selection states

The board must remain readable immediately.

---

# 10. Camera

Portrait mobile.

Use a slight 2.5D perspective.

Requirements:
- Board clearly readable
- Balls visually separated
- Minimal distortion
- Comfortable one-handed play

Keep camera settings data-driven.

---

# 11. UI

## In-Game Layout

Top:
- LINE 98
- Score
- Best Score

Center:
- 9 × 9 Board

Bottom:
- Next 3 balls
- Undo
- Hint

## Main Menu

- PLAY
- Daily Challenge
- Zen Mode
- Statistics
- Settings

Optional:
- Current streak
- Best score

Avoid clutter.

Do not add:
- Energy
- Lives
- Coins
- Gems
- Battle Pass
- Forced progression
- Campaign map

---

# 12. Undo

Allow:
- 3 free undo actions per session

After free undo is exhausted:
- Rewarded Ad may grant an additional undo

Undo must restore:
- Board state
- Score
- Queue state
- RNG state
- Move count
- Relevant statistics

Do not implement a fake visual-only undo.

---

# 13. Hint

Optional V1 feature.

Hint should:
- Identify a reasonable legal move
- Highlight source ball
- Highlight destination
- Never automatically execute

If implementation risks delaying launch, move Hint to V1.1.

---

# 14. Continue

After Game Over:

- Show final score
- Show best score
- Offer Continue via Rewarded Ad
- Always allow New Game without watching an ad

Continue may:
- Remove several balls
- Restore several empty cells
- Allow one recovery move

Continue must not feel mandatory.

---

# 15. Daily Challenge

Suggested seed format:

`YYYY-MM-DD-v1`

Requirements:
- Same seed produces same challenge
- Same spawn sequence
- Same starting configuration
- Local result persistence
- Daily streak
- Shareable result

Prepare interfaces:
- IDailyChallengeProvider
- ILeaderboardProvider

V1 implementation may remain local-only.

---

# 16. Cosmetics

Implement lightweight cosmetic architecture.

## Ball Themes

Potential future themes:
- Crystal
- Candy
- Marble
- Neon
- Fruit
- Ocean
- Galaxy

## Board Themes

Potential future themes:
- Classic
- Marble
- Wooden
- Dark
- Sakura
- Ocean
- Space

## Clear Effects

Potential future themes:
- Bubble
- Crystal
- Firework
- Confetti
- Lightning

V1 only requires:
- Crystal
- One alternative theme

Do not build a large economy or shop for V1.

---

# 17. Achievements

Initial achievements:
- First Line
- First 5-Line
- Long Shot — 7 balls
- Perfect — 9 balls
- Score 1,000
- Score 10,000
- Score 50,000
- 100 Moves
- 7-Day Streak
- 30-Day Streak

Keep achievements data-driven.

---

# 18. Statistics

Track locally:
- Games Played
- Games Completed
- Best Score
- Total Score
- Total Lines Cleared
- Longest Line
- Total Moves
- Average Score
- Highest Combo
- Current Streak
- Longest Streak

Only display useful player-facing statistics.

---

# 19. Audio

Required SFX:
- Ball select
- Ball move
- Ball place
- Invalid move
- Ball spawn
- Standard clear
- Long-line clear
- Combo
- Game over
- Button click
- Reward success

Music:
- Calm looping background
- Separate Zen ambience

Include:
- Music toggle
- SFX toggle

---

# 20. VFX

Use lightweight Unity VFX.

Required:
- Ball trail
- Selection glow
- Placement pulse
- Line clear burst
- Score popup
- Combo effect
- Game Over effect

Target:
- Stable 60 FPS
- Low-end Android compatibility
- Low memory usage

Avoid expensive effects that do not materially improve the experience.

---

# 21. Monetization

## Primary

- Rewarded Continue
- Rewarded Undo
- Rewarded Hint

## Secondary

- Frequency-capped Interstitial

Interstitial rules:
- Never during active gameplay
- Never after every move
- Never interrupt a satisfying clear
- Never appear before the player understands the game
- Suggested minimum interval: 2–3 minutes

## IAP

Primary purchase:

**Remove Ads**

Optional future monetization:
- Cosmetic packs

Do not build a complex economy.

---

# 22. Offline-First

The game must work without network access.

Offline:
- Classic
- Zen
- Daily Challenge
- Statistics
- Cosmetics
- Achievements

Network only required for:
- Ads
- Optional leaderboard
- Analytics
- Remote Config

Offline failures must never block gameplay.

---

# 23. Save System

Save:
- Current board
- Selected ball
- Next-ball queue
- RNG state
- Score
- Move count
- Game mode
- Statistics
- Achievements
- Cosmetics
- Settings
- Streak
- Daily Challenge state

Autosave:
- After every completed move
- On application pause
- On application quit

The player must not lose an active game.

---

# 24. Analytics

Use an abstraction such as:

`IAnalyticsService`

Track:
- game_start
- game_resume
- game_move
- game_invalid_move
- line_clear
- long_line
- combo
- game_over
- continue_offer
- continue_ad_started
- continue_ad_completed
- undo_used
- hint_used
- daily_start
- daily_complete
- zen_start
- theme_selected
- remove_ads_purchase

Do not hardcode one analytics vendor into gameplay logic.

---

# 25. Technical Quality

Requirements:
- No compiler warnings
- No normal-play runtime exceptions
- No null-reference spam
- No unnecessary Update loops
- Object pooling where useful
- Avoid excessive allocations
- Avoid LINQ in hot paths
- Deterministic RNG
- Clean separation of gameplay and presentation
- Unit tests for core logic

---

# 26. Testing

## Board
- Add ball
- Remove ball
- Empty detection
- Full detection

## Pathfinding
- Direct path
- Blocked path
- Multiple obstacles
- Unreachable destination
- Corner cases

## Line Detection
- Horizontal
- Vertical
- Both diagonals
- Exactly 5
- 6
- 7+
- Cross
- Multiple simultaneous lines

## Scoring
- Correct score
- Combo score
- Long-line score

## Save/Load
- Board
- Score
- Queue
- RNG
- Statistics

## Daily
- Same date = same seed
- Different date = different seed

---

# 27. Performance Target

Target devices:
- Low-end Android
- Mid-range Android
- Flagship Android

Target:
- 60 FPS
- Fast startup
- Small AAB
- Low RAM usage
- Minimal unnecessary network calls

Do not over-engineer performance for an 81-cell board.

---

# 28. Localization

V1:
- English
- Vietnamese

Prepare for future:
- Spanish
- Portuguese
- German
- French
- Japanese
- Korean
- Chinese

Do not hardcode player-facing strings throughout code.

---

# 29. ASO

## Store Product Name

**LINE 98: Color Lines**

## Brand

**LINE 98**

## Short Description Direction

> Classic 9×9 color ball puzzle. Match 5 or more, clear lines and beat your best score.

## Keyword Concepts

- line 98
- lines 98
- color lines
- color lines puzzle
- color ball puzzle
- match 5
- classic puzzle
- ball puzzle
- 9x9 puzzle
- offline puzzle

Do not keyword-stuff the title.

---

# 30. Icon

Requirements:
- 1–3 balls
- Recognizable grid/line concept
- High contrast
- No tiny text
- Readable at small sizes
- Premium casual aesthetic

Test at:
- 48 px
- 72 px
- 96 px
- 512 px

---

# 31. Onboarding

No long tutorial.

First game:
1. Highlight one ball.
2. Show valid destination.
3. Animate suggested move.
4. Explain a line of 5.
5. Let player perform the move.
6. End tutorial.

Maximum target:
**20–30 seconds**

Then get out of the player's way.

---

# 32. Development Milestones

## Milestone 1 — Playable Core

- Board
- Balls
- Movement
- BFS
- Line detection
- Spawn
- Game Over
- Score

Must be fully playable.

## Milestone 2 — Game Feel

- Animation
- VFX
- Audio
- Camera
- Modern visual

## Milestone 3 — Product Layer

- Save
- Statistics
- Undo
- Daily Challenge
- Zen
- Achievements

## Milestone 4 — Monetization

- Rewarded Ads
- Interstitial
- Remove Ads IAP

## Milestone 5 — Polish

- Onboarding
- Localization
- Settings
- Icon
- Store screenshots
- Performance
- Bug fixing

Do not start Milestone 3 before Milestone 1 is stable.

---

# 33. Definition of Done

V1 is complete when:
- Game launches into a polished main menu
- Player understands gameplay within 20 seconds
- Classic mode is fully playable
- Board logic is deterministic
- All line directions work
- Save/Load works
- Daily Challenge works locally
- Zen Mode works
- Undo works
- Game Over works
- Continue works
- Ads work
- Remove Ads works
- No critical bugs
- 60 FPS on target devices
- English and Vietnamese work
- Store build can be generated
- Store screenshots can be produced
- Consent/privacy flow is handled where required
- App remains playable offline

---

# 34. Explicit Non-Goals

Do NOT add:
- Multiplayer
- PvP
- Clans
- Guilds
- Energy
- Lives
- Coins
- Gems
- Battle Pass
- Complex campaign
- 1,000 levels
- Character progression
- RPG mechanics
- Complicated backend
- Real-time server
- Unnecessary frameworks
- Heavy asset-store dependencies

---

# 35. Final Product Principle

The original Line 98 mechanic is the asset.

The player should immediately recognize:

> **“This is Line 98.”**

But after playing, they should think:

> **“This feels like a modern mobile game.”**

---

# 36. Immediate Production Sequence

1. Inspect the Unity project.
2. Confirm Unity version and project structure.
3. Identify reusable packages.
4. Establish the minimum folder/class architecture.
5. Create Milestone 1.
6. Implement the playable core.
7. Run tests.
8. Proceed to visual polish only after the core is stable.

When a reasonable implementation decision is required:
- Choose the simplest production-safe option.
- Briefly document the decision.
- Do not stop at pseudocode.
- Produce working Unity C# code and working scenes.
