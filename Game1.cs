using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;

namespace SimpleShooter
{
    /// <summary>
    /// Simple 3D first-person shooter for Android.
    ///
    /// CONTROLS
    ///   Left half of screen  – Tap anywhere to shoot
    ///   Right half of screen – Drag to look around
    ///
    /// GAME RULES
    ///   Red cube enemies spawn at distance ~15-25 m and walk toward you.
    ///   Each enemy takes 3 bullet hits to destroy. +10 score per kill.
    ///   If an enemy touches you, you lose health. Reach 0 HP = Game Over.
    ///   Spawn rate increases over time — survive as long as possible!
    /// </summary>
    public class Game1 : Game
    {
        // ─── Core MonoGame ────────────────────────────────────────────────
        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch = null!;
        private BasicEffect _effect = null!;
        private SpriteFont? _font;

        // ─── Geometry (pre-built, never rebuilt after load) ───────────────
        private VertexPositionColor[] _floorVerts = Array.Empty<VertexPositionColor>();

        // Three damage-state cubes for enemies (red gets darker as health drops)
        private readonly VertexPositionColor[][] _enemyCubes = new VertexPositionColor[3][];

        // Bullet and flash cubes
        private VertexPositionColor[] _bulletCube = Array.Empty<VertexPositionColor>();
        private VertexPositionColor[] _flashCube  = Array.Empty<VertexPositionColor>();

        // ─── Camera / Player ──────────────────────────────────────────────
        private const float CAM_HEIGHT = 1.7f;            // eye height in metres
        private Vector3 _camPos = new(0, CAM_HEIGHT, 0);
        private float   _yaw   = 0f;    // left/right rotation (radians)
        private float   _pitch = 0f;    // up/down rotation   (radians)

        // ─── Touch input tracking ─────────────────────────────────────────
        private readonly Dictionary<int, Vector2> _prevTouchPos = new();
        private const float LOOK_SENSITIVITY = 0.004f;
        private float _shootCooldown = 0f;
        private const float SHOOT_RATE = 0.25f;   // seconds between shots
        private float _muzzleFlash = 0f;           // cosmetic flash timer

        // ─── Game state ───────────────────────────────────────────────────
        private readonly List<Enemy>  _enemies = new();
        private readonly List<Bullet> _bullets = new();
        private float _health      = 100f;
        private int   _score       = 0;
        private float _spawnTimer  = 0f;
        private float _spawnInterval = 2.5f;   // shrinks over time
        private bool  _gameOver    = false;
        private float _gameOverTimer = 0f;

        // 1×1 white texture for drawing coloured rectangles in 2-D UI
        private Texture2D _pixel = null!;

        private readonly Random _rng = new();

        // ─────────────────────────────────────────────────────────────────
        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this)
            {
                IsFullScreen = true,
                SupportedOrientations =
                    DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight,
                PreferredBackBufferWidth  = 1280,
                PreferredBackBufferHeight = 720,
                PreferMultiSampling = true,
            };
            Content.RootDirectory = "Content";
            TouchPanel.EnabledGestures = GestureType.None;
        }

        // ══════════════════════════════════════════════════════════════════
        //  LOAD
        // ══════════════════════════════════════════════════════════════════
        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Font is optional – UI falls back to health/score bars if absent
            try   { _font = Content.Load<SpriteFont>("Font"); }
            catch { _font = null; }

            // 1×1 white pixel used for drawing filled rectangles
            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            // BasicEffect for all 3D drawing – vertex colours, no lighting
            _effect = new BasicEffect(GraphicsDevice)
            {
                VertexColorEnabled = true,
                LightingEnabled    = false,
            };

            _effect.Projection = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.ToRadians(70f),
                GraphicsDevice.Viewport.AspectRatio,
                0.05f, 250f);

            // Build geometry
            BuildFloor();

            // Enemy cubes: 3 hits → 3 health states
            _enemyCubes[0] = CubeMesh.Create(new Color(255, 80,  80),  // full health  — bright red
                                              new Color(200, 50,  50),
                                              new Color(160, 40,  40));
            _enemyCubes[1] = CubeMesh.Create(new Color(200, 60,  30),  // 2 hits left  — orange-red
                                              new Color(160, 40,  20),
                                              new Color(120, 30,  10));
            _enemyCubes[2] = CubeMesh.Create(new Color(140, 40,  10),  // 1 hit left   — dark brown-red
                                              new Color(100, 25,   5),
                                              new Color( 70, 15,   5));

            _bulletCube = CubeMesh.Create(Color.Yellow,      Color.Gold,    Color.Orange);
            _flashCube  = CubeMesh.Create(Color.White,       Color.Yellow,  Color.Yellow);
        }

        // ══════════════════════════════════════════════════════════════════
        //  BUILD FLOOR — checkered green, 40×40 tiles
        // ══════════════════════════════════════════════════════════════════
        private void BuildFloor()
        {
            const int HALF = 20;
            var light = new Color(72, 140, 72);
            var dark  = new Color(55, 110, 55);
            var verts = new List<VertexPositionColor>(HALF * HALF * 4 * 6);

            for (int x = -HALF; x < HALF; x++)
            for (int z = -HALF; z < HALF; z++)
            {
                Color c = ((x + z) % 2 == 0) ? light : dark;
                float x0 = x, x1 = x + 1, z0 = z, z1 = z + 1;

                verts.Add(new VertexPositionColor(new Vector3(x0, 0, z0), c));
                verts.Add(new VertexPositionColor(new Vector3(x1, 0, z0), c));
                verts.Add(new VertexPositionColor(new Vector3(x1, 0, z1), c));

                verts.Add(new VertexPositionColor(new Vector3(x0, 0, z0), c));
                verts.Add(new VertexPositionColor(new Vector3(x1, 0, z1), c));
                verts.Add(new VertexPositionColor(new Vector3(x0, 0, z1), c));
            }

            _floorVerts = verts.ToArray();
        }

        // ══════════════════════════════════════════════════════════════════
        //  UPDATE
        // ══════════════════════════════════════════════════════════════════
        protected override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_gameOver)
            {
                _gameOverTimer += dt;
                if (_gameOverTimer > 2.5f && TouchPanel.GetState().Count > 0)
                    RestartGame();
                return;
            }

            HandleTouch(dt);
            UpdateBullets(dt);
            UpdateEnemies(dt);
            UpdateSpawner(dt);

            if (_shootCooldown > 0) _shootCooldown -= dt;
            if (_muzzleFlash  > 0) _muzzleFlash  -= dt;

            if (_health <= 0f)
            {
                _health = 0f;
                _gameOver = true;
                _gameOverTimer = 0f;
            }

            base.Update(gameTime);
        }

        // ─── Touch input ─────────────────────────────────────────────────
        private void HandleTouch(float dt)
        {
            var touches   = TouchPanel.GetState();
            int screenW   = GraphicsDevice.Viewport.Width;
            var stillDown = new HashSet<int>();

            foreach (var touch in touches)
            {
                stillDown.Add(touch.Id);
                bool rightSide = touch.Position.X > screenW * 0.5f;

                switch (touch.State)
                {
                    case TouchLocationState.Pressed:
                        _prevTouchPos[touch.Id] = touch.Position;
                        if (!rightSide)
                            TryShoot();   // left half tap = shoot
                        break;

                    case TouchLocationState.Moved:
                        if (rightSide && _prevTouchPos.TryGetValue(touch.Id, out var prev))
                        {
                            var delta = touch.Position - prev;
                            _yaw   -= delta.X * LOOK_SENSITIVITY;
                            _pitch -= delta.Y * LOOK_SENSITIVITY;
                            _pitch  = MathHelper.Clamp(_pitch,
                                        -MathHelper.PiOver2 + 0.05f,
                                         MathHelper.PiOver2 - 0.05f);
                        }
                        _prevTouchPos[touch.Id] = touch.Position;
                        break;

                    case TouchLocationState.Released:
                        _prevTouchPos.Remove(touch.Id);
                        break;
                }
            }

            // Clean up stale entries
            var toRemove = new List<int>();
            foreach (var id in _prevTouchPos.Keys)
                if (!stillDown.Contains(id)) toRemove.Add(id);
            foreach (var id in toRemove) _prevTouchPos.Remove(id);
        }

        private void TryShoot()
        {
            if (_shootCooldown > 0f) return;
            _shootCooldown = SHOOT_RATE;
            _muzzleFlash   = 0.08f;

            var dir = LookDirection();
            _bullets.Add(new Bullet
            {
                Position      = _camPos + dir * 0.6f,
                Direction     = dir,
                Speed         = 35f,
                LifeRemaining = 4f,
            });
        }

        // ─── Bullet update & collision ───────────────────────────────────
        private void UpdateBullets(float dt)
        {
            for (int i = _bullets.Count - 1; i >= 0; i--)
            {
                var b = _bullets[i];
                b.Position      += b.Direction * b.Speed * dt;
                b.LifeRemaining -= dt;
                _bullets[i] = b;

                if (b.LifeRemaining <= 0f)
                {
                    _bullets.RemoveAt(i);
                    continue;
                }

                // Test against all enemies
                bool hit = false;
                for (int j = _enemies.Count - 1; j >= 0; j--)
                {
                    var e = _enemies[j];
                    if (Vector3.Distance(b.Position, e.Position) < 0.9f)
                    {
                        e.HitsLeft--;
                        if (e.HitsLeft <= 0)
                        {
                            _enemies.RemoveAt(j);
                            _score += 10;
                        }
                        else
                        {
                            _enemies[j] = e;
                        }
                        _bullets.RemoveAt(i);
                        hit = true;
                        break;
                    }
                }
                if (hit) continue;
            }
        }

        // ─── Enemy update & player damage ────────────────────────────────
        private void UpdateEnemies(float dt)
        {
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];

                // Move toward player (XZ plane only)
                var toPlayer = _camPos - e.Position;
                toPlayer.Y = 0f;
                float dist = toPlayer.Length();

                if (dist > 0.1f)
                {
                    e.Position += Vector3.Normalize(toPlayer) * e.Speed * dt;
                    _enemies[i] = e;
                }

                // Deal damage when touching the player
                if (dist < 1.3f)
                    _health -= 25f * dt;
            }
        }

        // ─── Enemy spawner ────────────────────────────────────────────────
        private void UpdateSpawner(float dt)
        {
            _spawnTimer += dt;
            if (_spawnTimer < _spawnInterval) return;

            _spawnTimer = 0f;
            _spawnInterval = MathF.Max(0.6f, _spawnInterval - 0.04f); // ramp difficulty

            // Spawn 1–3 enemies at once (group size grows with score)
            int group = 1 + _score / 100;
            group = Math.Min(group, 4);
            for (int k = 0; k < group; k++)
                SpawnEnemy();
        }

        private void SpawnEnemy()
        {
            float angle = (float)(_rng.NextDouble() * MathF.PI * 2f);
            float dist  = 15f + (float)_rng.NextDouble() * 12f;

            _enemies.Add(new Enemy
            {
                Position  = new Vector3(
                    _camPos.X + MathF.Cos(angle) * dist,
                    0.5f,   // sits on floor
                    _camPos.Z + MathF.Sin(angle) * dist),
                Speed     = 2f + (float)_rng.NextDouble() * 1.5f,
                HitsLeft  = 3,
            });
        }

        // ─── Restart ─────────────────────────────────────────────────────
        private void RestartGame()
        {
            _enemies.Clear();
            _bullets.Clear();
            _health        = 100f;
            _score         = 0;
            _spawnTimer    = 0f;
            _spawnInterval = 2.5f;
            _gameOver      = false;
            _gameOverTimer = 0f;
            _yaw = _pitch  = 0f;
        }

        // ── Camera helper ────────────────────────────────────────────────
        private Vector3 LookDirection() =>
            Vector3.Transform(Vector3.Forward,
                Matrix.CreateRotationX(_pitch) * Matrix.CreateRotationY(_yaw));

        // ══════════════════════════════════════════════════════════════════
        //  DRAW
        // ══════════════════════════════════════════════════════════════════
        protected override void Draw(GameTime gameTime)
        {
            // Sky colour
            GraphicsDevice.Clear(new Color(100, 165, 255));

            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState        = BlendState.Opaque;
            GraphicsDevice.RasterizerState   = RasterizerState.CullNone;

            // Camera matrix
            _effect.View = Matrix.CreateLookAt(
                _camPos, _camPos + LookDirection(), Vector3.Up);
            _effect.Projection = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.ToRadians(70f),
                GraphicsDevice.Viewport.AspectRatio,
                0.05f, 250f);

            // ── Floor ────────────────────────────────────────────────────
            DrawVerts(_floorVerts, Matrix.Identity);

            // ── Enemies ──────────────────────────────────────────────────
            foreach (var e in _enemies)
            {
                int cubeIdx = 3 - e.HitsLeft;                       // 0, 1, or 2
                cubeIdx = Math.Clamp(cubeIdx, 0, 2);
                var world = Matrix.CreateTranslation(e.Position);
                DrawVerts(_enemyCubes[cubeIdx], world);
            }

            // ── Bullets ──────────────────────────────────────────────────
            foreach (var b in _bullets)
            {
                var world = Matrix.CreateScale(0.18f) *
                            Matrix.CreateTranslation(b.Position);
                DrawVerts(_bulletCube, world);
            }

            // ── Muzzle flash ─────────────────────────────────────────────
            if (_muzzleFlash > 0f)
            {
                var dir = LookDirection();
                var world = Matrix.CreateScale(0.25f) *
                            Matrix.CreateTranslation(_camPos + dir * 0.8f);
                DrawVerts(_flashCube, world);
            }

            // ── 2D HUD overlay ───────────────────────────────────────────
            DrawHUD();

            base.Draw(gameTime);
        }

        private void DrawVerts(VertexPositionColor[] verts, Matrix world)
        {
            _effect.World = world;
            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(
                    PrimitiveType.TriangleList, verts, 0, verts.Length / 3);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  HUD — drawn in screen space with SpriteBatch
        // ══════════════════════════════════════════════════════════════════
        private void DrawHUD()
        {
            var vp = GraphicsDevice.Viewport;
            int W  = vp.Width;
            int H  = vp.Height;

            _spriteBatch.Begin();

            if (_gameOver)
            {
                // Semi-transparent dark overlay
                Rect(0, 0, W, H, new Color(0, 0, 0, 170));

                if (_font != null)
                {
                    CenteredText("GAME OVER", W / 2, H / 2 - 50, Color.Red,   2.2f);
                    CenteredText($"Score: {_score}", W / 2, H / 2 + 10, Color.White, 1.6f);

                    if (_gameOverTimer > 2.5f)
                        CenteredText("Tap to play again", W / 2, H / 2 + 70, Color.Yellow, 1.2f);
                }
                else
                {
                    // Font unavailable — draw a wide red banner
                    Rect(W / 2 - 100, H / 2 - 10, 200, 30, Color.DarkRed);
                }
            }
            else
            {
                // ── Health bar ───────────────────────────────────────────
                Rect(18, H - 44, 202, 22, new Color(40, 0, 0));                 // border
                Rect(20, H - 42, (int)(198f * (_health / 100f)), 18,
                     _health > 50 ? new Color(220, 50, 50)
                   : _health > 25 ? new Color(220, 140, 30)
                                  : new Color(255, 60, 60));                    // bar

                // ── Score & enemy count ──────────────────────────────────
                if (_font != null)
                {
                    _spriteBatch.DrawString(_font, $"HP: {(int)_health}",
                        new Vector2(20, H - 68), Color.White);
                    _spriteBatch.DrawString(_font, $"Score: {_score}",
                        new Vector2(W - 160, 20), Color.White);
                    _spriteBatch.DrawString(_font, $"Enemies: {_enemies.Count}",
                        new Vector2(20, 20), Color.Yellow);

                    // Controls reminder (fades after 10 kills)
                    if (_score < 100)
                    {
                        var hint = "< TAP: Shoot     DRAG: Look >";
                        var sz   = _font.MeasureString(hint);
                        _spriteBatch.DrawString(_font, hint,
                            new Vector2((W - sz.X) / 2f, H - 30),
                            new Color(220, 220, 220, 160));
                    }
                }

                // ── Crosshair ────────────────────────────────────────────
                int cx = W / 2, cy = H / 2;
                int gap = 6, arm = 14, thick = 3;

                // Horizontal arms
                Rect(cx - gap - arm, cy - thick / 2, arm, thick, Color.White);
                Rect(cx + gap,       cy - thick / 2, arm, thick, Color.White);
                // Vertical arms
                Rect(cx - thick / 2, cy - gap - arm, thick, arm, Color.White);
                Rect(cx - thick / 2, cy + gap,       thick, arm, Color.White);
                // Centre dot
                Rect(cx - 2, cy - 2, 4, 4, Color.White);

                // ── Left shoot-zone indicator ────────────────────────────
                Rect(0, H / 2 - 50, 5, 100, new Color(255, 255, 255, 60));

                // ── Right look-zone indicator ────────────────────────────
                Rect(W - 5, H / 2 - 50, 5, 100, new Color(255, 255, 255, 60));
            }

            _spriteBatch.End();
        }

        // ── Helpers ───────────────────────────────────────────────────────
        private void Rect(int x, int y, int w, int h, Color c) =>
            _spriteBatch.Draw(_pixel, new Rectangle(x, y, w, h), c);

        private void CenteredText(string text, int cx, int cy, Color color, float scale = 1f)
        {
            if (_font is null) return;
            var size = _font.MeasureString(text) * scale;
            _spriteBatch.DrawString(_font, text,
                new Vector2(cx - size.X * 0.5f, cy - size.Y * 0.5f),
                color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}
