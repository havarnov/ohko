using System.Collections.Generic;
using nkast.Aether.Physics2D.Dynamics;

namespace Ohko.Core;

public class Karate(World world) : Hero(world)
{
    public override string Name => "Karate";
    protected override string AsepriteFile => "karate";
    protected override string AsepriteConfigFile => "karate.json";
    protected override string GetIdleAnimation() => "kIdle";

    protected override Dictionary<string, string?> AutomaticContinuations => new()
    {
        { "kPunchA_charge", "kPunchA_hit" },
        { "kPunchA_hit", null },
        { "kKickA_charge", "kKickA_hit" },
        { "kKickA_hit", null },
        { "kBack", null },
        { "kDodgeCharge", "kDodgeEndToIdle" },
        { "kDodgeEndToIdle", null },
    };

    protected override List<ComboConfig> ComboConfigs =>
    [
        new()
        {
            Buttons = [ControlPad.ButtonPosition.Center, ControlPad.ButtonPosition.MiddleRight],
            AnimationName = "kPunchA_charge",
        },
        new()
        {
            Buttons = [ControlPad.ButtonPosition.Center, ControlPad.ButtonPosition.MiddleLeft],
            AnimationName = "kBack",
        },
        new()
        {
            Buttons = [ControlPad.ButtonPosition.Center, ControlPad.ButtonPosition.BottomCenter],
            AnimationName = "kDodgeCharge",
        },
        new()
        {
            Buttons = [
                ControlPad.ButtonPosition.Center,
                ControlPad.ButtonPosition.MiddleLeft,
                ControlPad.ButtonPosition.Center,
                ControlPad.ButtonPosition.MiddleRight,
                ControlPad.ButtonPosition.TopRight
            ],
            AnimationName = "kKickA_charge",
        },
    ];
}