using Microsoft.Xna.Framework;

namespace SimpleShooter
{
    /// <summary>
    /// A red cube enemy that walks toward the player.
    /// </summary>
    public struct Enemy
    {
        public Vector3 Position;
        public float Speed;
        public int HitsLeft;    // starts at 3, destroyed at 0
    }
}
