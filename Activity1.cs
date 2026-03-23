using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Microsoft.Xna.Framework;

namespace SimpleShooter
{
    /// <summary>
    /// Android entry point. Sets up the MonoGame view and starts the game loop.
    /// </summary>
    [Activity(
        Label = "SimpleShooter",
        MainLauncher = true,
        AlwaysRetainTaskState = true,
        LaunchMode = LaunchMode.SingleInstance,
        ScreenOrientation = ScreenOrientation.SensorLandscape,
        ConfigurationChanges =
            ConfigChanges.Orientation | ConfigChanges.Keyboard |
            ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize)]
    public class Activity1 : AndroidGameActivity
    {
        private Game1? _game;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            _game = new Game1();

            // MonoGame on Android exposes the GL surface as an Android View
            var view = _game.Services.GetService(typeof(View)) as View;
            SetContentView(view!);
            _game.Run();
        }

        protected override void OnDestroy()
        {
            _game?.Dispose();
            base.OnDestroy();
        }
    }
}
