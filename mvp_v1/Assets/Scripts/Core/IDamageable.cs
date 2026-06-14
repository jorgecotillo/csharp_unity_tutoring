namespace GoblinSiege.Core
{
    /// <summary>Anything that can take damage and be destroyed (units, gates, caches).</summary>
    public interface IDamageable
    {
        bool IsAlive { get; }

        /// <summary>Apply damage. Returns true if this hit destroyed the target.</summary>
        bool TakeDamage(float amount);
    }
}
