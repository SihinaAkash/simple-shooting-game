using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SimpleShooter
{
    /// <summary>
    /// Procedurally generates a unit-cube (1x1x1) centered at origin
    /// as a flat VertexPositionColor array (36 vertices, 12 triangles).
    /// No model files needed — geometry is built in code.
    /// </summary>
    public static class CubeMesh
    {
        /// <param name="topColor">Color of the top face.</param>
        /// <param name="sideColor">Color of the four side faces.</param>
        /// <param name="bottomColor">Color of the bottom face.</param>
        public static VertexPositionColor[] Create(Color topColor, Color sideColor, Color bottomColor)
        {
            var v = new VertexPositionColor[36];
            int i = 0;

            // Helper: add two triangles (a quad) with the same color
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color col)
            {
                v[i++] = new VertexPositionColor(a, col);
                v[i++] = new VertexPositionColor(b, col);
                v[i++] = new VertexPositionColor(c, col);

                v[i++] = new VertexPositionColor(a, col);
                v[i++] = new VertexPositionColor(c, col);
                v[i++] = new VertexPositionColor(d, col);
            }

            // Winding is clockwise when viewed from outside — matches MonoGame
            // CullCounterClockwise (DirectX convention, clockwise = front face).

            // Front (+Z)
            Quad(new Vector3(-0.5f, -0.5f,  0.5f),
                 new Vector3( 0.5f, -0.5f,  0.5f),
                 new Vector3( 0.5f,  0.5f,  0.5f),
                 new Vector3(-0.5f,  0.5f,  0.5f), sideColor);

            // Back (-Z)
            Quad(new Vector3( 0.5f, -0.5f, -0.5f),
                 new Vector3(-0.5f, -0.5f, -0.5f),
                 new Vector3(-0.5f,  0.5f, -0.5f),
                 new Vector3( 0.5f,  0.5f, -0.5f), sideColor);

            // Left (-X)
            Quad(new Vector3(-0.5f, -0.5f, -0.5f),
                 new Vector3(-0.5f, -0.5f,  0.5f),
                 new Vector3(-0.5f,  0.5f,  0.5f),
                 new Vector3(-0.5f,  0.5f, -0.5f), sideColor);

            // Right (+X)
            Quad(new Vector3( 0.5f, -0.5f,  0.5f),
                 new Vector3( 0.5f, -0.5f, -0.5f),
                 new Vector3( 0.5f,  0.5f, -0.5f),
                 new Vector3( 0.5f,  0.5f,  0.5f), sideColor);

            // Top (+Y)
            Quad(new Vector3(-0.5f,  0.5f,  0.5f),
                 new Vector3( 0.5f,  0.5f,  0.5f),
                 new Vector3( 0.5f,  0.5f, -0.5f),
                 new Vector3(-0.5f,  0.5f, -0.5f), topColor);

            // Bottom (-Y)
            Quad(new Vector3(-0.5f, -0.5f, -0.5f),
                 new Vector3( 0.5f, -0.5f, -0.5f),
                 new Vector3( 0.5f, -0.5f,  0.5f),
                 new Vector3(-0.5f, -0.5f,  0.5f), bottomColor);

            return v;
        }
    }
}
