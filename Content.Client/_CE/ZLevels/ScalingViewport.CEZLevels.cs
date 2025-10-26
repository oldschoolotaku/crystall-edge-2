using System.Numerics;
using Content.Client._CE.ZLevels;
using Content.Shared._CE.ZLevels;
using Content.Shared._CE.ZLevels.EntitySystems;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Client.Viewport;

public sealed partial class ScalingViewport
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private CEClientZLevelsSystem? _zLevels;
    private SharedMapSystem? _mapSystem;

    private EntityQuery<TransformComponent>? _xformQuery;
    private EntityQuery<MapComponent>? _mapQuery;

    private IEye? _fallbackEye;

    /// <summary>
    /// From the incoming list of maps, we filter only those that require rendering.
    /// </summary>
    public List<EntityUid> GetFilteredMapList(List<EntityUid> sourceList, EntityUid currentMap)
    {
        var mapList = new List<EntityUid>();

        if (_eye is null)
            return mapList;

        var mapIdx = sourceList.IndexOf(currentMap);
        if (mapIdx < 0)
            return mapList;

        for (var i = mapIdx; i >= 0; i--)
        {
            var targetMap = sourceList[i];
            mapList.Add(targetMap);

            //if (!TryFindEmptyTiles(targetMap))
            //    break;
        }

        // Reverse a new list
        if (mapList.Count > 0)
        {
            var tempList = new List<EntityUid>(mapList.Count);
            for (var i = mapList.Count - 1; i >= 0; i--)
            {
                tempList.Add(mapList[i]);
            }

            mapList = tempList;
        }

        return mapList;
    }

    /// <summary>
    /// We are looking for at least one empty tile on the screen.
    /// This is used to ensure that it makes sense to draw the z-planes and that they are visible.
    /// </summary>
    public bool TryFindEmptyTiles(EntityUid mapUid)
    {
        if (_xformQuery is null || !_xformQuery.Value.TryComp(mapUid, out var xform))
            return true;

        var drawBox = GetDrawBox();

        var bottomLeftPos = _eyeManager.ScreenToMap(drawBox.BottomLeft).Position;
        var topRightPos = _eyeManager.ScreenToMap(drawBox.TopRight).Position;
        var mapId = xform.MapID;

        var mapCoordsBottomLeft = new MapCoordinates(bottomLeftPos, mapId);
        var mapCoordsTopRight = new MapCoordinates(topRightPos, mapId);

        if (!_mapManager.TryFindGridAt(mapUid, mapCoordsBottomLeft.Position, out _, out var grid))
            return true;

        var tileBottomLeft = grid.TileIndicesFor(mapCoordsBottomLeft);
        var tileTopRight = grid.TileIndicesFor(mapCoordsTopRight);

        var minX = tileBottomLeft.X - 1;
        var maxX = tileTopRight.X + 1;
        var minY = tileBottomLeft.Y - 1;
        var maxY = tileTopRight.Y + 1;

        Vector2i tilePos = default;

        for (tilePos.X = minX; tilePos.X <= maxX; tilePos.X++)
        {
            for (tilePos.Y = minY; tilePos.Y <= maxY; tilePos.Y++)
            {
                var tile = grid.GetTileRef(tilePos);

                if (tile.Tile.IsEmpty)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryRenderLevelsBelow(IRenderHandle handle, IClydeViewport viewport)
    {
        if (_eye is null)
            return false;

        _fallbackEye = _eye;

        // Cache frequently accessed components/systems
        _xformQuery ??= _entityManager.GetEntityQuery<TransformComponent>();
        _mapQuery ??= _entityManager.GetEntityQuery<MapComponent>();

        // Cache systems and components
        _zLevels ??= _entityManager.System<CEClientZLevelsSystem>();
        _mapSystem ??= _entityManager.System<SharedMapSystem>();

        if (_player.LocalEntity is null)
            return false;

        if (!_entityManager.TryGetComponent<CEZLevelViewerComponent>(_player.LocalEntity.Value, out var zLevelViewer))
            return false;

        if (!_xformQuery.Value.TryComp(_player.LocalEntity, out var playerXform))
            return false;

        if (playerXform.MapUid is null)
            return false;

        var lookUp = zLevelViewer.LookUp ? 1 : 0;

        int? realLowestDepth = null; //Used for ClearColor

        var rendered = false;
        for (var depth = CESharedZLevelsSystem.MaxZLevelsBelowRendering; depth >= -lookUp; depth--)
        {
            if (depth == 0)
            {
                if (!_mapQuery.Value.TryComp(playerXform.MapUid.Value, out var mapComp))
                    continue;

                viewport.Eye =_fallbackEye;
            }
            else
            {
                if (!_zLevels.TryMapOffset(playerXform.MapUid.Value, -depth, out var mapUidBelow))
                    continue;

                if (!_mapQuery.Value.TryComp(mapUidBelow.Value, out var mapComp))
                    continue;

                viewport.Eye =new ZEye
                {
                    Position = new MapCoordinates(_eye.Position.Position, mapComp.MapId),
                    DrawFov = _eye.DrawFov && depth <= 0,
                    DrawLight = _eye.DrawLight && depth <= 0,
                    Offset = _eye.Offset + new Vector2(0f, depth * CEClientZLevelsSystem.ZLevelOffset),
                    Rotation = _eye.Rotation,
                    Scale = _eye.Scale,
                    Depth = depth,
                };
            }
            realLowestDepth ??= depth;

            viewport.ClearColor = depth == realLowestDepth ? Color.Black : null;
            viewport.Render();
            rendered = true;
        }

        // Restore the Eye
        Eye = _fallbackEye;
        viewport.Eye = Eye;

        return rendered;
    }

    //FIXME: This is nasty!
    public sealed class ZEye : Robust.Shared.Graphics.Eye
    {
        public int Depth;
    }
}
