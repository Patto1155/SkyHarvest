// GL overlay: grid diamonds, terrain categories, player / cursor walk probes.
using System.Collections.Generic;
using UnityEngine;
using SkyHarvest.Core;
using SkyHarvest.Island;
using SkyHarvest.Player;

namespace SkyHarvest.DevTools
{
    [RequireComponent(typeof(Camera))]
    public class DevDebugOverlay : MonoBehaviour
    {
        private static Material? _lineMat;
        private IslandData? _island;
        private PlayerController? _player;

        public void SetRefs(IslandData island, PlayerController player)
        {
            _island  = island;
            _player  = player;
        }

        private static Material LineMat
        {
            get
            {
                if (_lineMat != null) return _lineMat;
                var shader = Shader.Find("Hidden/Internal-Colored");
                _lineMat = new Material(shader)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                };
                _lineMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _lineMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _lineMat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                _lineMat.SetInt("_ZWrite", 0);
                return _lineMat;
            }
        }

        private void OnPostRender()
        {
            if (!DevDebugPanel.IsEnabled || _island == null) return;
            if (!DevDebugSettings.ShowDiamondBounds && !DevDebugSettings.ShowTerrainFill &&
                !DevDebugSettings.ShowPlayerProbe && !DevDebugSettings.ShowHoverProbe &&
                !DevDebugSettings.ShowStairCorridor)
                return;

            DrawWorldOverlays();
        }

        private void DrawWorldOverlays()
        {
            float hw = Constants.TileWorldWidth  * 0.5f;
            float hh = Constants.TileWorldHeight * 0.5f;

            LineMat.SetPass(0);
            GL.PushMatrix();
            GL.LoadProjectionMatrix(GetComponent<Camera>().projectionMatrix);
            GL.modelview = GetComponent<Camera>().worldToCameraMatrix;

            var tiers = DevDebugSettings.ShowBothTiers
                ? new[] { 0, 1 }
                : new[] { _player != null ? _player.CurrentTier : 0 };

            foreach (int tier in tiers)
            {
                foreach (var kvp in _island!.Cells)
                {
                    if (_island.Tier(kvp.Key) != tier) continue;
                    var cell = kvp.Value;
                    Vector2 c = GridMath.DiamondCentre(cell.GridPos, tier);

                    if (DevDebugSettings.ShowTerrainFill)
                        FillDiamond(c, hw, hh, TerrainColor(cell.Terrain, 0.28f));

                    if (DevDebugSettings.ShowDiamondBounds)
                    {
                        Color edge = tier == 0
                            ? new Color(0.3f, 0.85f, 1f, 0.85f)
                            : new Color(1f, 0.75f, 0.2f, 0.85f);
                        if (!_island.IsWalkable(kvp.Key))
                            edge = new Color(1f, 0.2f, 0.2f, 0.9f);
                        DrawDiamondOutline(c, hw, hh, edge);
                    }
                }
            }

            if (DevDebugSettings.ShowStairCorridor && _island.StairsCarved)
            {
                foreach (var (a, b) in _island.EachStairEdge())
                    DrawStairCorridor(a, b);
            }

            if (_player != null && DevDebugSettings.ShowPlayerProbe)
                DrawProbe(_player.transform.position, _player.CurrentTier, new Color(0.2f, 1f, 0.35f, 1f));

            if (DevDebugSettings.ShowHoverProbe)
            {
                var cam = GetComponent<Camera>();
                int tier = _player != null ? _player.CurrentTier : 0;
                var screen = Input.mousePosition;
                screen.z = Mathf.Abs(transform.position.z);
                Vector2 world = cam.ScreenToWorldPoint(screen);
                DrawProbe(world, tier, new Color(1f, 0.45f, 0.9f, 1f));
            }

            GL.PopMatrix();
        }

        private void DrawProbe(Vector3 world, int tier, Color color)
        {
            float r = 0.04f;
            GL.Begin(GL.LINES);
            GL.Color(color);
            GL.Vertex3(world.x - r, world.y, 0f);
            GL.Vertex3(world.x + r, world.y, 0f);
            GL.Vertex3(world.x, world.y - r, 0f);
            GL.Vertex3(world.x, world.y + r, 0f);
            GL.End();

            bool inside = ProbeInsideIsland(new Vector2(world.x, world.y), tier, out _);
            if (!inside)
            {
                // X marker when outside walk diamond (likely "walking off tile" bug)
                GL.Begin(GL.LINES);
                GL.Color(new Color(1f, 0.15f, 0.1f, 1f));
                float m = 0.06f;
                GL.Vertex3(world.x - m, world.y - m, 0f);
                GL.Vertex3(world.x + m, world.y + m, 0f);
                GL.Vertex3(world.x - m, world.y + m, 0f);
                GL.Vertex3(world.x + m, world.y - m, 0f);
                GL.End();
            }
        }

        public static bool ProbeInsideIsland(Vector2 world, int tier, IslandData island, out Vector2Int cell)
        {
            cell = GridMath.WorldToGrid(world, tier);
            return island.IsWalkableAt(world, tier);
        }

        private void DrawStairCorridor(Vector2Int a, Vector2Int b)
        {
            if (_island == null) return;

            StairWalkMath.CorridorSegment(a, b, _island, out var start, out var end);
            Vector2 normal = StairWalkMath.CorridorNormal(a, b, _island) * StairWalkMath.HalfWidth;

            Vector2 v0 = start + normal;
            Vector2 v1 = end   + normal;
            Vector2 v2 = end   - normal;
            Vector2 v3 = start - normal;

            var col  = new Color(1f, 0.2f, 0.95f, 0.9f);
            var fill = new Color(1f, 0.2f, 0.95f, 0.18f);

            GL.Begin(GL.QUADS);
            GL.Color(fill);
            GL.Vertex3(v0.x, v0.y, 0f);
            GL.Vertex3(v1.x, v1.y, 0f);
            GL.Vertex3(v2.x, v2.y, 0f);
            GL.Vertex3(v3.x, v3.y, 0f);
            GL.End();

            GL.Begin(GL.LINE_STRIP);
            GL.Color(col);
            GL.Vertex3(v0.x, v0.y, 0f);
            GL.Vertex3(v1.x, v1.y, 0f);
            GL.Vertex3(v2.x, v2.y, 0f);
            GL.Vertex3(v3.x, v3.y, 0f);
            GL.Vertex3(v0.x, v0.y, 0f);
            GL.End();

            GL.Begin(GL.LINES);
            GL.Color(col);
            GL.Vertex3(start.x, start.y, 0f);
            GL.Vertex3(end.x, end.y, 0f);
            GL.End();
        }

        private bool ProbeInsideIsland(Vector2 world, int tier, out Vector2Int cell)
        {
            if (_island == null) { cell = default; return false; }
            return ProbeInsideIsland(world, tier, _island, out cell);
        }

        private static void DrawDiamondOutline(Vector2 c, float hw, float hh, Color color)
        {
            var v = DiamondVerts(c, hw, hh);
            GL.Begin(GL.LINE_STRIP);
            GL.Color(color);
            for (int i = 0; i <= 4; i++)
                GL.Vertex3(v[i % 4].x, v[i % 4].y, 0f);
            GL.End();
        }

        private static void FillDiamond(Vector2 c, float hw, float hh, Color color)
        {
            var v = DiamondVerts(c, hw, hh);
            GL.Begin(GL.TRIANGLES);
            GL.Color(color);
            GL.Vertex3(c.x, c.y, 0f);
            GL.Vertex3(v[0].x, v[0].y, 0f);
            GL.Vertex3(v[1].x, v[1].y, 0f);
            GL.Vertex3(c.x, c.y, 0f);
            GL.Vertex3(v[1].x, v[1].y, 0f);
            GL.Vertex3(v[2].x, v[2].y, 0f);
            GL.Vertex3(c.x, c.y, 0f);
            GL.Vertex3(v[2].x, v[2].y, 0f);
            GL.Vertex3(v[3].x, v[3].y, 0f);
            GL.Vertex3(c.x, c.y, 0f);
            GL.Vertex3(v[3].x, v[3].y, 0f);
            GL.Vertex3(v[0].x, v[0].y, 0f);
            GL.End();
        }

        private static Vector2[] DiamondVerts(Vector2 c, float hw, float hh) => new[]
        {
            c + new Vector2(0f,  hh),
            c + new Vector2(hw, 0f),
            c + new Vector2(0f, -hh),
            c + new Vector2(-hw, 0f),
        };

        public static Color TerrainColor(TerrainType type, float alpha) => type switch
        {
            TerrainType.FertileValley => new Color(0.25f, 0.78f, 0.30f, alpha),
            TerrainType.RockyPlateau  => new Color(0.58f, 0.52f, 0.46f, alpha),
            TerrainType.CliffEdge       => new Color(0.90f, 0.22f, 0.18f, alpha),
            TerrainType.NaturalSpring   => new Color(0.25f, 0.55f, 0.95f, alpha),
            TerrainType.WindCorridor    => new Color(0.82f, 0.82f, 0.25f, alpha),
            TerrainType.Scaffold        => new Color(0.65f, 0.42f, 0.18f, alpha),
            _                           => new Color(0.5f, 0.5f, 0.5f, alpha),
        };
    }
}
