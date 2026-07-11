using System;

namespace GoblinSiege.Systems
{
    /// <summary>
    /// A tiny global "start-the-fight" latch (spec §door mechanic).
    ///
    /// WHAT IT'S FOR (in plain terms):
    ///   The human garrison stays ASLEEP until the Warlord opens the barracks door.
    ///   While the gate is LOCKED, humans hold their posts and cannot be fought.
    ///   The moment the Warlord swings the door open, <see cref="Unlock"/> is called
    ///   and the whole garrison "wakes up" — now the battle begins.
    ///
    /// WHY A STATIC LATCH?
    ///   Combat readiness is a single, raid-wide truth ("has the door been opened
    ///   yet?"). Every <see cref="GoblinSiege.Units.HumanUnit"/> just reads this one
    ///   bool each physics step (a single cheap comparison — WebGL-friendly, G3), so
    ///   there is no per-unit wiring, no scene references, and no event plumbing.
    ///
    /// LIFECYCLE:
    ///   • Default = UNLOCKED, so a scene WITHOUT a door never soft-locks the humans.
    ///   • <see cref="Door.Awake"/> calls <see cref="Lock"/> — a door in the scene
    ///     means "combat is gated until this door opens." This runs fresh every raid,
    ///     so it is correct even across repeated play sessions (static leakage safe).
    ///   • <see cref="Door"/> calls <see cref="Unlock"/> once, right after it finishes
    ///     swinging open.
    /// </summary>
    public static class CombatGate
    {
        /// <summary>
        /// True once the Warlord has opened the door. While false, the human
        /// garrison is dormant and invulnerable (see HumanUnit). Starts true so a
        /// door-less scene is never gated; a Door locks it in its Awake.
        /// </summary>
        public static bool HumansMayFight { get; private set; } = true;

        /// <summary>Fires once when the fight is unlocked (the garrison wakes up).</summary>
        public static event Action OnFightUnlocked;

        /// <summary>Gate the fight: humans sleep and cannot be harmed. Called by Door.Awake.</summary>
        public static void Lock() => HumansMayFight = false;

        /// <summary>
        /// Open the flood-gates: the garrison wakes and the battle begins. Idempotent —
        /// opening a second door (or re-triggering) does nothing after the first unlock.
        /// </summary>
        public static void Unlock()
        {
            if (HumansMayFight) return;
            HumansMayFight = true;
            OnFightUnlocked?.Invoke();
        }
    }
}
