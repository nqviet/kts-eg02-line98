# LINE 98: Color Lines — Release Checklist (Definition of Done)

Per **GDD §33**, V1 is complete when:

- [ ] Game launches cleanly into a polished main menu
- [ ] Player understands core gameplay within 20–30 seconds
- [ ] Classic mode is fully playable (9×9 board, 7 colors, line ≥5, spawn 3)
- [ ] Board simulation and pathfinding are 100% deterministic (XorShift128)
- [ ] All line detection axes work (Horizontal, Vertical, Diagonal Ascending, Diagonal Descending)
- [ ] Save / Autosave system persists and restores active game state crash-safely
- [ ] Daily Challenge generates deterministic layout from date seed (`YYYY-MM-DD-v1`) locally
- [ ] Zen Mode functions with ambient audio and relaxed presentation
- [ ] Real Undo restores full board, queue, RNG, move count, and score state
- [ ] Game Over correctly trips when no legal moves remain or spawn cannot fit
- [ ] Continue via Rewarded Ad restores board and grants a recovery move
- [ ] Ads integration respects interstitial caps (≥150s, never during resolve or clear)
- [ ] Remove Ads IAP entitlement persists and disables interstitials
- [ ] No compiler warnings (`-warnaserror`), no runtime exceptions, no null-ref spam
- [ ] Stable 60 FPS maintained on target devices
- [ ] Memory and GC allocation budget respected (0 B / frame in gameplay)
- [ ] English and Vietnamese localizations fully supported with proper font diacritics
- [ ] Release AAB size within budget (target ≤ 25 MB, hard ceiling ≤ 40 MB)
- [ ] Store screenshots and 1024×1024 app icon verified across resolutions (48/72/96/512 px)
- [ ] Consent / privacy flow (UMP) handled where legally required
- [ ] Game remains 100% functional and playable offline
