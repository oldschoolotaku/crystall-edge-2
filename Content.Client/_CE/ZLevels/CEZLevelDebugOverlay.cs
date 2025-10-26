using System.Numerics;
using Content.Shared._CE.ZLevels;
using Content.Shared._CE.ZLevels.EntitySystems;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Console;
using Robust.Shared.Enums;

namespace Content.Client._CE.ZLevels;

public sealed class CEZLevelDebugOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IResourceCache _cache = default!;
    private readonly CESharedZLevelsSystem _zLevels;
    private readonly SharedTransformSystem _transform = default!;
    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    private readonly Font _font;

    public CEZLevelDebugOverlay()
    {
        IoCManager.InjectDependencies(this);

        _zLevels = _entityManager.System<CESharedZLevelsSystem>();
        _transform = _entityManager.System<SharedTransformSystem>();

        _font = new VectorFont(_cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 8);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var query = _entityManager.EntityQueryEnumerator<CEZPhysicsComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var zPhys, out var xform))
        {
            if (xform.MapUid != xform.ParentUid)
                continue;

            var worldPos = _transform.GetWorldPosition(uid);
            var screenPos = args.ViewportControl?.WorldToScreen(worldPos) ?? Vector2.Zero;

            var localPos = MathF.Round(zPhys.LocalPosition, 2);
            var groundDis = MathF.Round(_zLevels.DistanceToGround(uid, out var sticky), 2);
            var velocity = MathF.Round(zPhys.Velocity, 2);

            var depthText = $"ZLocalHeight: {localPos}\nDistance to ground: {groundDis}\nVelocity: {velocity}\nSticky: {sticky}";

            args.ScreenHandle.DrawString(_font, screenPos, depthText, Color.White);
        }
    }
}

public sealed class CEShowZLevelDebugCommand : LocalizedCommands
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    public override string Command => "showzleveldebug";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_overlayManager.HasOverlay<CEZLevelDebugOverlay>())
            _overlayManager.RemoveOverlay<CEZLevelDebugOverlay>();
        else
            _overlayManager.AddOverlay(new CEZLevelDebugOverlay());
    }
}
