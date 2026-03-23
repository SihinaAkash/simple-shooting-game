using Microsoft.Xna.Framework;

namespace SimpleShooter
{
    /// <summary>
    /// A yellow cube projectile fired by the player.
    /// </summary>
    public struct Bullet
    {
        public Vector3 Position;
        public Vector3 Direction;
        public float Speed;
        public float LifeRemaining;   // seconds until auto-despawn
    }
}
