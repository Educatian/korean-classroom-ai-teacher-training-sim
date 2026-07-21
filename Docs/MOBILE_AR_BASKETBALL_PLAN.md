# Pocket Court AR: Unity Plan and Storyboard

## Product

**Tier:** Small-game prototype. **Platform:** AR-capable iOS/Android phones. **Stack:** Unity 6, URP, AR Foundation, ARCore/ARKit XR plug-ins, Input System.

**Promise:** Turn a safe floor or tabletop into a responsive basketball challenge in under one minute.

Pillars: instant setup, readable skill, satisfying physical feedback, and stationary safe play.

**ASSUMPTION:** Phone-first, single-player. **IMPACT:** Touch defines shooting; headset/networking are excluded. **IF WRONG:** Input and architecture change. **VALIDATE:** Confirm before Sprint 1.

**ASSUMPTION:** Floor and tabletop modes are candidates. **IMPACT:** Visual scale changes while simulation stays canonical. **IF WRONG:** A second mode adds needless complexity. **VALIDATE:** Compare first-shot time and preference with five users per mode.

## Success and MVP

- New players place and shoot without verbal help.
- After three shots, players predict aim/power effects.
- Skill improves scoring consistency.
- Misses communicate short, long, left, right, or rim-out.
- Sessions work from one safe standing zone.

MVP: placement/recovery, hoop/court/ball, touch-drag input, partial arc preview, physics, downward hoop-plane scoring, 60-second score attack, combo, tutorial, results/retry, local best, multimodal feedback, and accessibility. Excluded: multiplayer, avatars, outdoor play, phone swinging, economy/ads, licensed content, and headset interaction.

## Loop and starting values

Scan -> place -> aim/power -> release -> observe -> feedback -> retry.

Makes score; consecutive makes raise a capped combo. Misses break the combo without a long delay. Virtual shot spots may move, but the player stays still. At zero time, new shots stop and the active ball resolves.

| Variable | Start | Pass condition | Adjustment |
|---|---:|---|---|
| Round | 60 s | 4/5 testers retry; no fatigue | 45 s or 75 s |
| Aim | 0.25-1.25 s | 80% intended releases register | Widen range |
| Arc | First 35% | Explain misses after three shots | Extend/shorten |
| Reset | 0.6 s | Readable, continuous pacing | Adjust by 0.1 s |
| Combo | +0.25x; cap 3x | Matters without deciding round | Lower cap |
| Safe zone | 0.8 m | Testers remain stationary | Enlarge/reposition |

All numbers are starting values, not standards.

## Shot state machine

Press the ball/shot zone. Vertical drag sets elevation/power; horizontal drag sets aim. Arc, reticle, and power update continuously. Release commits immediately. Device motion adds parallax, never hidden force. Score once only on downward hoop-plane crossing from above. Use continuous ball collision and simple rim colliders.

| State | Exit | Interrupts/edges |
|---|---|---|
| Setup | Round starts | Input locked until anchor stable |
| Ready | Valid press | Timer/pause; ignore UI touches |
| Aiming | Release/cancel | Tracking loss, pause, multi-touch; clamp aim |
| Flight | Score/miss/timeout | Pause only; no overlapping shot |
| Resolve | Feedback done | Prevent duplicate score/stuck ball |
| Results | Retry/exit | Save best locally |

Physics reports facts; pure domain logic owns outcomes.

## Twelve-panel storyboard

| # | Player view/action | System/feedback |
|---:|---|---|
| 1 | Tap **Find a court** | Explain/request permissions |
| 2 | Slowly scan surface | Plane visualization/guidance |
| 3 | Ghost court follows surface | Green valid/red invalid footprint |
| 4 | Standing-zone circle appears | Anchor locks; safety reminder |
| 5 | Ball pulses | Drag/release tutorial |
| 6 | Arc bends to hoop | Power, reticle, charge tone |
| 7 | Ball flies | Ballistics, trail, wind, collision |
| 8A | Ball scores | Net snap, burst, haptic, combo |
| 8B | Ball misses | Contact sound and diagnosis |
| 9 | New ball appears | Peripheral timer/score |
| 10 | Origin shifts sideways | Arrow and ghost ball telegraph |
| 11 | Timer ends | Block input; last shot; buzzer |
| 12 | Results float above court | Score, best, accuracy, combo, Retry/Exit |

Tracking loss freezes input and time, subdues the last pose, and prompts slow rescanning. Resume after stability; otherwise offer **Replace court** without losing score.

## Quality, safety, and accessibility

| Component | Support | Acceptance test |
|---|---|---|
| Clarity | Arc, reticle, power, miss labels | Predict direction on 4/5 shots |
| Motivation | Score, best, accuracy, combo | Most testers retry once |
| Response | Continuous feedback, immediate release | Launch next rendered frame absent stall |
| Satisfaction | Net/rim audio, burst, haptic | Observer distinguishes outcomes without UI |
| Fit | Stylized street court, arcade physics | Player describes basketball and AR |

Tune response first, then preview/score reliability, rim physics, feedback, score balance, cosmetics.

- Require clear-space acknowledgement and stationary play.
- Never put essential UI behind the player or require backward movement.
- Pause during tracking loss, interruption, or excessive camera motion.
- Provide handed layouts, seated/tabletop mode, sensitivity, and preview settings.
- Encode state with color plus shape/text; caption warnings and scores.
- Haptics are optional; reduced motion disables trails, impulses, and large bursts.

## Unity architecture

| Module | Responsibility |
|---|---|
| App Bootstrap | Startup order, permissions, services, scenes |
| AR Placement | Planes, raycasts, anchors, tracking, repositioning |
| Basketball Domain | Pure C# shot/round states, score, combo, timer |
| Shot Simulation | Gesture mapping, launch, ball lifecycle, physics reports |
| Presentation | Court/ball, arc, HUD, audio, haptics, effects, net |
| Persistence/Telemetry | Versioned settings/best and privacy-safe events |

Scenes: **Bootstrap** for platform checks and services; **AR Court** with `ARSession`, `XROrigin`, plane/raycast managers, court, and HUD; **Test Court** without AR for deterministic Editor iteration.

Use one explicit bootstrap and never depend on incidental `Awake` order. Updates and tracking callbacks guard against uninitialized, paused, and invalid-tracking states.

- Scene objects own AR components, camera, court instance, colliders, and views.
- ScriptableObjects hold authored tuning, rules, feedback, themes, and audio.
- Pure C# owns runtime score, timer, combo, states, and score eligibility.
- Thin MonoBehaviours bridge touch, AR callbacks, physics, and presentation.
- Commands: `BeginRound`, `BeginAim`, `CommitShot`, `ResolveShot`, `Pause`, `Resume`.
- Events: `RoundStarted`, `ShotCommitted`, `ShotResolved`, `ScoreChanged`, `TrackingInterrupted`, `RoundEnded`.
- Avoid global singletons, string lookup, generic event buses, and runtime state in shared ScriptableObjects.

XR Interaction Toolkit is unnecessary for the touch-first phone prototype. Add it only for a later headset track.

Performance risks are camera overdraw, transparency, shadows, net simulation, per-frame arc allocations, complex colliders, and unbounded plane visualization. Use pooled balls/effects, preallocated arc samples, simple colliders, limited planes after placement, simple lighting, quality tiers, and device profiling from Sprint 2.

## Production roadmap

1. **Sprint 0, device spike:** Passthrough, planes, placement, anchor, hoop on iOS/Android. Gate: placement under 30 seconds in three indoor light conditions.
2. **Sprint 1, graybox:** Test Court, input, arc, launch, collisions, score, reset. Gate: 50 scripted shots with no duplicate, tunneling, or stuck ball.
3. **Sprint 2, AR round:** Timer, combo, results, recovery, reposition. Gate: five device rounds without restart, disruptive drift, or unsafe movement.
4. **Sprint 3, experience:** Tutorial, diagnosis, polish, handedness, seated/reduced motion. Gate: 4/5 new players set up and score unassisted.
5. **Sprint 4, release:** Quality tiers, pooling, privacy review, settings. Gate: agreed device budget, clean 15-minute soak, safety/accessibility pass.

## Playtest and definition of done

- **New player:** Say only “Play one round.” Pass at 4/5 unassisted completions.
- **Stress:** Multi-touch, release off-screen, backgrounding, low light, plane edge, pause/reposition spam, trigger re-entry. No duplicate score, locked input, lost timer, or court teleport.
- **Skill:** Compare ten practice/test shots. Most players improve; perceived cause matches launch data.
- **Abuse:** Try UI click-through, phone-motion fake power, below-hoop crossing, clock changes, force-close during save. No extra score or corruption.
- **Readability:** Observer identifies outcome and likely direction for 8/10 shots.
- **Devices:** Minimum/recent iPhone and ARCore phone, bright/dim and textured/low-texture floors, standing/tabletop, 15-minute soak.

Done means both platforms support placement/recovery; a stationary round completes; input, preview, collision, score, combo, and timer agree; tracking/background recovery is valid; all playtests are recorded; minimum devices meet agreed performance/thermal budgets; and no licensed, camera, raw spatial-map, or sensitive data enters persistence/telemetry.

## Decisions before implementation

1. Phone-only or phone plus headset?
2. Floor-scale, tabletop, or both?
3. Minimum iOS and Android devices?
4. Family arcade, basketball training, or educational physics?
5. Store release, research prototype, classroom activity, or portfolio demo?

Until answered, build a phone-first, single-player, stylized score-attack prototype and compare floor/tabletop placement in Sprint 0.
