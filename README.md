# Dusk Survivors

A small **Vampire Survivors–style roguelite** made in Unity. You move, and your weapons attack on their own.
Survive the night for **10 minutes** while the horde grows. Gold you earn carries over between runs and buys permanent upgrades.

Everything (sprites, UI, enemies, effects) is generated from code at runtime, so the project has **no art assets** and opens instantly.

## How to play

| Action | Input |
| --- | --- |
| Move | WASD / Arrow keys / Gamepad left stick |
| Pause | Esc or P |
| Pick an upgrade | Click a card or press 1–3 |
| Restart after a run | R |

## Features

- **6 weapons**, each upgradable to level 6
  - Arcane Bolt: shoots the nearest enemies
  - Throwing Knives: rapid knives in your movement direction
  - Holy Aura: damages and pushes back nearby enemies
  - Spirit Orbs: orbit around you
  - Storm Call: lightning on random on-screen enemies
  - Hand Axe: heavy arcing axes that pierce
- **9 passive items**: max HP, damage, speed, cooldown, area, pickup range, regen, armor, extra projectiles
- **Level-up choices**: pick 1 of 3 random upgrades (up to 6 weapons + 6 passives)
- **Enemies**: Bats, Zombies, Ghosts, Brutes, and a **Reaper boss** every 2 minutes that drops a treasure chest
- Surround waves, XP gems, gold coins, healing, and a gem vacuum pickup
- **Roguelite meta-progression**: spend gold on permanent upgrades (Vitality, Might, Swiftness, Haste, Magnetism, Growth, Greed, Revival), choose your starting weapon, and track your best time. Saved with `PlayerPrefs`.

## Running it

1. Install **Unity 6** (made with `6000.0.85f1`; any Unity 6 version should work) through Unity Hub.
2. In Unity Hub: **Add → Add project from disk** and pick this folder.
3. Open `Assets/Scenes/Main.unity` and press **Play**.

The game bootstraps itself (`Bootstrap.cs` uses `RuntimeInitializeOnLoadMethod`), so pressing Play in any scene works.

To build: **File → Build Profiles** (or Build Settings), and `Main` is already in the scene list.

## Code layout

```
Assets/Scripts/
  Core/        Bootstrap, GameManager (state, camera, XP, all UI via IMGUI), SpriteFactory, Effect, GameInput
  Gameplay/    Player, Enemy + EnemyTypes, EnemySpawner, Projectile, Pickup, Weapons, Upgrades (passives + level-up rolls)
  Meta/        MetaProgression (permanent upgrades, save data)
```

Adding a weapon: subclass `Weapon` in `Weapons.cs` and add it to `WeaponCatalog`. It then appears in level-up rolls and in the starting-weapon picker automatically.

## License

MIT
