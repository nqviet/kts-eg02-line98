# LINE 98: Color Lines — Design Review Rubric

Per **GDD §35**: *"The original Line 98 mechanic is the asset. The player should immediately recognize: 'This is Line 98.' But after playing, they should think: 'This feels like a modern mobile game.'"*

## Review Dimensions

### 1. Nostalgic Authenticity (Is it Line 98?)
- 9×9 grid with 81 cells.
- 7 distinct, readable ball colors.
- 4-directional BFS movement (no diagonal paths).
- Lines of 5 or more match horizontally, vertically, and diagonally.
- Exactly 3 preview balls displayed and spawned on non-clearing moves.
- Spawned balls do not trigger immediate line clears.

### 2. Modern Premium Casual Execution (Does it feel modern?)
- 3D orthogonal camera (Orthographic projection, Pitch 58°) with uniform cell scale, zero edge distortion, and zero touch-offset penalties.
- Tactile recessed board with soft ambient shadows and rounded ceramic aesthetic.
- Glossy crystal gemstone spheres with fresnel rims and inner refraction depth.
- Fluid ball motion: Selection Feedback → Path Flight → Soft Settle Bounce.
- Cascading clear sequence: Connect → Pulse → Glow → Burst → Gem Shards → Floating Score Popup.
- Tiered payoffs: noticeable escalation between 5, 6–7, 8, and 9+ ("PERFECT LINE").
- Soft neumorphic UI floating cleanly over serene alpine scenery.

### 3. Polish & Game Feel
- Responsive touch controls (<250ms tap gesture).
- Clear, non-punitive audio feedback for invalid moves.
- Rich acoustic and crystalline soundscape routed through 5 dedicated mixer buses.
- Seamless safe-area adaptation across 9:16 to 9:22 portrait screens.
