using System.Numerics;
using Content.Shared._CE.ZLevels;
using Content.Shared._CE.ZLevels.EntitySystems;
using Content.Shared.Camera;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client._CE.ZLevels;

public sealed partial class CEClientZLevelsSystem : CESharedZLevelsSystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public static float ZLevelOffset = 0.7f;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new CEZLevelOverlay());

        SubscribeLocalEvent<CEZPhysicsComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CEZPhysicsComponent, GetEyeOffsetEvent>(OnEyeOffset);
    }

    private void OnEyeOffset(Entity<CEZPhysicsComponent> ent, ref GetEyeOffsetEvent args)
    {
        args.Offset += new Vector2(0, ent.Comp.LocalPosition * ZLevelOffset);
    }

    private void OnStartup(Entity<CEZPhysicsComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (sprite.SnapCardinals)
            return;

        ent.Comp.NoRotDefault = sprite.NoRotation;
        ent.Comp.DrawDepthDefault = sprite.DrawDepth;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEZPhysicsComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var zPhys, out var sprite))
        {
            if (zPhys.LocalPosition != 0)
                sprite.NoRotation = true;
            else
                sprite.NoRotation = zPhys.NoRotDefault;

            _sprite.SetOffset((uid, sprite), new Vector2(0, zPhys.LocalPosition * ZLevelOffset));
            _sprite.SetDrawDepth((uid, sprite), zPhys.LocalPosition > 0 ? zPhys.DrawDepthDefault +1 : zPhys.DrawDepthDefault);
        }
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<CEZLevelOverlay>();
    }
}
