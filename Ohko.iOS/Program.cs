using System;
using Foundation;
using Ohko.Core;
using UIKit;

namespace Ohko.iOS;

[Register("AppDelegate")]
public class AppDelegate : UIApplicationDelegate
{
    public override UIWindow Window { get; set; }

     static void Main(string[] args)
     {
         UIApplication.Main(args, null, typeof(AppDelegate));
     }

    public override bool FinishedLaunching(UIApplication app, NSDictionary options)
    {
        Window = new UIWindow(UIScreen.MainScreen.Bounds);

        // Create and assign your GameViewController
        var gameController = new GameViewController();
        Window.RootViewController = gameController;

        Window.MakeKeyAndVisible();

        return true;
    }

    // (Optional) you can also enforce orientation here:
    public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations(UIApplication application, UIWindow forWindow)
    {
        return UIInterfaceOrientationMask.Portrait;
    }
}

[Register("GameViewController")]
public class GameViewController : UIViewController
{
    private OhkoGame _game;

    public GameViewController(IntPtr handle) : base(handle)
    {
    }

    public GameViewController()
    {
    }

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();

        // Start your MonoGame game
        _game = new OhkoGame(isFullScreen: true);
        _game.Run();
    }

    // 👇 This is where you force portrait:
    public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations()
    {
        // Only portrait
        return UIInterfaceOrientationMask.Portrait;
        // or: return UIInterfaceOrientationMask.Portrait | UIInterfaceOrientationMask.PortraitUpsideDown;
    }

    public override bool ShouldAutorotate()
    {
        // iOS can rotate, but only within the mask above
        return true;
    }
}