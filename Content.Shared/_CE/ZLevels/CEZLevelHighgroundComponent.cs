using Robust.Shared.GameStates;

namespace Content.Shared._CE.ZLevels;

/// <summary>
/// Allows entities not to fall if they are above this entity at a higher level.
/// Think of it as the ability to walk on top of walls, for example.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEZLevelHighgroundComponent : Component
{
    /// <summary>
    /// Height profile points, forming a simple curve (0..1 by X, height by Y).
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<float> HeightCurve = new()
    {
        1.05f,
        1.05f,
    };

    /// <summary>
    /// Forcibly attaches the entity to itself along the z-axis if the character descends smoothly. Needed for various staircases.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Stick = false;

    /// <summary>
    /// SHITCODE - we cant mapping entities rotated by 45 radians, so we just use this
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Corner = false;
}
