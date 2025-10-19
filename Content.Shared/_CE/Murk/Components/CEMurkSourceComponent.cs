using Content.Shared._CE.Murk.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.Murk.Components;

/// <summary>
/// Edits the strength of the murk on the map within a radius around the source
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), Access(typeof(CESharedMurkSystem))]
public sealed partial class CEMurkSourceComponent : Component
{
    /// <summary>
    /// Defines the distance that this entity clears of murk. If murk intensity = 0, then this value is equal to the radius in tiles.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Intensity = 5f;

    /// <summary>
        /// only for the client: constantly strives for the Intensity value<see cref="Intensity"/>, so that there are no sudden jumps
    /// </summary>
    [NonSerialized]
    public float LerpedIntensity = 0f;

    [DataField, AutoNetworkedField]
    public bool Active = true;
}
