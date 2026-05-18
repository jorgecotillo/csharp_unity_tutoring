# ⚔️ GOBLIN SIEGE — Game Design Spec

## Overview

**Title:** Goblin Siege
**Genre:** Medieval squad-defense strategy (endless, high-score)
**Engine:** Unity 6 (2D, top-down)
**Platform:** WebGL (for jam submissions) + Windows build
**Target audience:** All ages, strategy game fans
**Development time:** 5–7 days (Game Jam pace)

---

## 🎯 Concept

You are the **Warden** of a medieval fortress under endless goblin assault. You directly control a squad of defenders, choosing their composition and moving them tactically around the battlefield. Goblins draw random war cards each wave, escalating in size and power over time — eventually building their own siege fortress.

**Win:** Reach the gold quota (500g hoarded) before your castle falls.
**Lose:** All troops die OR castle is fully destroyed.

---

## 🕹️ Controls

| Input | Action |
|-------|--------|
| WASD | Move your commander (squad follows) |
| 1-5 | Select formation / group focus |
| E | Open recruit menu (between waves) |
| R | Repair walls (between waves) |
| Space | Rally troops to commander |
| Enter | Start next wave early during intermission |
| Mouse | Click to direct squad focus-fire |

---

## 💰 Gold Economy

Gold is earned by defeating goblins. Gold is BOTH your score AND your currency.

**The core tension:** Spend gold to survive → delays winning. Hoard gold to win → risk dying.

### Earning Gold
| Enemy | Gold Drop |
|-------|-----------|
| Goblin Scout | 1g |
| Goblin Warrior | 2g |
| Wolfrider | 3g |
| Sapper | 4g |
| Shaman | 5g |
| Troll | 10g |
| Siege Tower | 15g |
| Troll King (boss) | 50g |

### Spending Gold
| Purchase | Cost | Effect |
|----------|------|--------|
| Recruit Knight | 10g | +1 melee defender to squad |
| Recruit Archer | 15g | +1 ranged defender |
| Recruit Mage | 25g | +1 AoE caster |
| Recruit Shieldbearer | 20g | +1 tank defender |
| Repair Walls | 20g | Restore one castle section |
| Repair Gate | 30g | Restore main gate HP |
| Fortify Tower | 40g | Restore + buff one tower |

### Intermission (Between Waves)
- After each cleared wave, the game enters an intermission phase (default: 20 seconds)
- During intermission you spend gold (recruit, repair, fortify, regroup)
- Press Enter to start the next wave early when ready
- If the timer expires, the next wave starts automatically

### Win Condition
| Difficulty | Gold Quota |
|------------|-----------|
| Normal | 500g saved (unspent) |
| Hard | 750g saved |
| Endless (high-score) | No quota — survive as long as possible, gold = score |

---

## 🛡️ Your Squad

You pick your group composition. Troops fight automatically when enemies are in range, but YOU control positioning.

### Defender Types
| Unit | HP | Damage | Range | Speed | Role |
|------|-----|--------|-------|-------|------|
| 🗡️ Knight | 100 | 15 | Melee | Normal | Frontline blocker |
| 🏹 Archer | 50 | 12 | Long | Normal | Ranged DPS |
| 🧙 Mage | 40 | 25 (AoE) | Medium | Slow | Area control |
| 🛡️ Shieldbearer | 150 | 5 | Melee | Slow | Absorbs damage for others |
| ⚒️ Engineer | 60 | 8 | Short | Normal | Repairs structures during combat |

### Squad Size
- Start: Max 5 troops
- Fixed cap: Max 5 troops (no squad size upgrades)

### Movement
- Commander moves with WASD
- Squad follows in formation (wedge/line/circle)
- Troops auto-attack enemies in their range
- Player's job: position the squad at chokepoints, walls, flanks

---

## 🧌 The Goblin Horde

### Enemy Types (unlock over time)
| Unit | HP | Damage | Speed | First Appears |
|------|-----|--------|-------|---------------|
| Scout | 20 | 5 | Fast | Wave 1 |
| Warrior | 40 | 10 | Normal | Wave 1 |
| Wolfrider | 30 | 12 | Very Fast | Wave 4 |
| Shielded Goblin | 60 | 8 | Slow | Wave 6 |
| Sapper | 25 | 20 (structures only) | Normal | Wave 8 |
| Shaman | 35 | 0 (heals/revives) | Slow | Wave 10 |
| Troll | 200 | 30 | Slow | Wave 12 |
| Siege Tower | 300 | 0 (spawns goblins inside walls) | Very Slow | Wave 16 |
| Troll King | 500 | 50 | Slow | Wave 20+ |
