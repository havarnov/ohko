using System.Collections.Generic;

namespace Ohko.Core;

public class ComboConfig
{
    public required List<ControlPad.ButtonPosition> Buttons { get; init; }
    public required string AnimationName { get; init; }
}