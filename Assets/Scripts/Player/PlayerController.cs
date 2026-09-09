// Assets/Scripts/Player/PlayerController.cs
// Owned by: world/island agent
// 2D sprite player.  Transform-based movement (no Rigidbody).
// Uses legacy Input (Input.GetAxisRaw).  Speed ~3 world units/s.
// Drives SpriteAnimator with idle/walk/action strips per facing direction.
// Exposes Inventory (convenience), CurrentFacingCell for InteractionSystem.
using UnityEngine;
using SkyHarvest.Core;
using SkyHarvest.Island;

namespace SkyHarvest.Player
{
    public class PlayerController : MonoBehaviour
    {
        // ---- config ----
        [SerializeField] private float _moveSpeed = 3f;

        // ---- references set by Bootstrap ----
        public IslandData? Island { get; set; }

        /// <summary>The elevation tier the player is currently standing on (0 = lower
        /// farm, 1 = raised forge). Movement and cell lookups are resolved against this
        /// tier so the raised platform and the void between tiers are handled correctly.
        /// Crossing tiers only happens across a carved stair edge.</summary>
        public int CurrentTier { get; set; }

        // ---- sprite animator (assigned by Bootstrap or Awake) ----
        private SpriteAnimator? _spriteAnimator;

        // ---- animation strip paths (manifest paths from CONVENTIONS) ----
        // Facing: s=south, n=north, e=east, w=west
        private const int PlayerFrameWidth = 48;   // px per player sprite frame

        private Sprite[]? _idleS, _idleN, _idleE, _idleW;
        private Sprite[]? _walkS, _walkN, _walkE, _walkW;
        private Sprite[]? _actS,  _actN,  _actE,  _actW;

        // ---- state ----
        private enum Facing { S, N, E, W }
        private Facing _facing = Facing.S;
        private bool   _isMoving;
        private bool   _isActing;

        // ---- inventory convenience ----
        public Inventory Inventory => GetComponent<PlayerInventoryComponent>().Inventory;

        // ---- interaction ----
        /// <summary>The grid cell the player is facing (1 cell ahead).</summary>
        public Vector2Int CurrentFacingCell
        {
            get
            {
                Vector2Int gridPos = Core.GridMath.WorldToGrid(transform.position, CurrentTier);
                return gridPos + FacingOffset();
            }
        }

        // -----------------------------------------------------------------------
        // Unity lifecycle
        // -----------------------------------------------------------------------
        private void Awake()
        {
            _spriteAnimator = GetComponent<SpriteAnimator>();
            if (_spriteAnimator == null)
                _spriteAnimator = gameObject.AddComponent<SpriteAnimator>();

            LoadSprites();
        }

        private void Update()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            if (h != 0f || v != 0f)
            {
                CancelWalk();          // keyboard always wins over an auto-walk
                Move(h, v);
                return;
            }

            if (_path != null) { FollowPath(); return; }

            Move(0f, 0f);
        }

        // -----------------------------------------------------------------------
        // Auto-walk (tap-to-move) — drives Move() so tier rules are identical
        // -----------------------------------------------------------------------
        private System.Collections.Generic.List<Vector2Int>? _path;
        private int _pathIndex;
        private System.Action? _onArrive;
        private const float ArriveEpsilon = 0.08f;

        public bool IsAutoWalking => _path != null;

        public void WalkPath(System.Collections.Generic.List<Vector2Int> path, System.Action? onArrive)
        {
            if (path == null || path.Count == 0) { onArrive?.Invoke(); return; }
            _path = path; _pathIndex = 0; _onArrive = onArrive;
        }

        public void CancelWalk()
        {
            _path = null; _onArrive = null;
        }

        private void FollowPath()
        {
            if (_path == null || Island == null) { CancelWalk(); return; }
            if (_pathIndex >= _path.Count)
            {
                var done = _onArrive;
                CancelWalk();
                Move(0f, 0f);
                done?.Invoke();
                return;
            }

            var node = _path[_pathIndex];
            Vector2 target = Core.GridMath.GridToWorld(node, Island.Tier(node));
            Vector2 pos = transform.position;
            Vector2 delta = target - pos;

            if (delta.magnitude <= ArriveEpsilon)
            {
                _pathIndex++;
                return;
            }

            var (h, v) = TapTargeting.WorldDirToAxes(delta);
            Vector2 before = transform.position;
            Move(h, v, maxStep: delta.magnitude);
            if ((Vector2)transform.position == before)
            {
                // Blocked (something changed under us) — give up rather than jitter forever.
                CancelWalk();
                Move(0f, 0f);
            }
        }

        /// <summary>Turn toward a cell without moving (used before acting on a tapped tile).</summary>
        public void FaceCell(Vector2Int cell)
        {
            var cur = Core.GridMath.WorldToGrid(transform.position, CurrentTier);
            var d = cell - cur;
            if (d == Vector2Int.zero) return;
            if (Mathf.Abs(d.x) >= Mathf.Abs(d.y)) _facing = d.x > 0 ? Facing.E : Facing.W;
            else                                  _facing = d.y < 0 ? Facing.N : Facing.S;
            if (!_isMoving) SetIdleAnimation();
        }

        // -----------------------------------------------------------------------
        // Movement
        // -----------------------------------------------------------------------
        /// <summary>Move by input axes for this frame; (0,0) settles into idle.</summary>
        public void Move(float h, float v, float maxStep = float.MaxValue)
        {
            if (h == 0f && v == 0f)
            {
                if (_isMoving)
                {
                    _isMoving = false;
                    SetIdleAnimation();
                }
                return;
            }

            // Dimetric mapping: Horizontal input maps to gx-gy direction,
            // Vertical maps to gx+gy direction.  Move in world-space 2D.
            Vector2 dir = new Vector2(h * 0.5f, v * 0.25f).normalized;
            Vector2 from = transform.position;
            float step = Mathf.Min(_moveSpeed * Time.deltaTime, maxStep);
            Vector2 candidate = from + dir * step;

            // Tier-aware clamp: a step is allowed only if the destination cell exists
            // and CanTraverse permits it (same tier, or a carved stair edge). This blocks
            // walking out over the void AND climbing the cliff before the stairs are mined.
            if (Island != null)
            {
                Vector2Int fromCell = Core.GridMath.WorldToGrid(from, CurrentTier);
                if (!TryStep(from, candidate, fromCell, out candidate))
                {
                    // Slide along whichever single axis is still walkable.
                    Vector2 cx = from + new Vector2(dir.x, 0f) * step;
                    Vector2 cy = from + new Vector2(0f, dir.y) * step;
                    if (TryStep(from, cx, fromCell, out var rx))      candidate = rx;
                    else if (TryStep(from, cy, fromCell, out var ry)) candidate = ry;
                    else                                              candidate = from;   // fully blocked
                }
            }

            transform.position = new Vector3(candidate.x, candidate.y, 0f);

            // Update facing and walk animation
            UpdateFacing(h, v);
            if (!_isMoving || _isActing)
            {
                _isMoving  = true;
                _isActing  = false;
                SetWalkAnimation();
            }
        }

        /// <summary>
        /// Resolve a candidate world move against the tier model. Returns false (blocked)
        /// when the destination cell is off-island or the tier change isn't permitted.
        /// When the step crosses a carved stair edge onto another tier, the player snaps
        /// to that cell's centre on the new tier (and CurrentTier updates).
        /// </summary>
        private bool TryStep(Vector2 from, Vector2 candidate, Vector2Int fromCell, out Vector2 result)
        {
            result = candidate;
            if (Island == null) return true;

            Vector2Int toCell = Core.GridMath.WorldToGrid(candidate, CurrentTier);
            if (toCell == fromCell) return true;                 // sub-cell move, same tile
            if (!Island.CanTraverse(fromCell, toCell)) return false;

            int toTier = Island.Tier(toCell);
            if (toTier != CurrentTier)                            // climbed/descended the stairs
            {
                CurrentTier = toTier;
                result = Core.GridMath.GridToWorld(toCell, toTier);
            }
            return true;
        }

        // -----------------------------------------------------------------------
        // Action animation (called by InteractionSystem)
        // -----------------------------------------------------------------------
        public void PlayActionAnimation()
        {
            _isActing = true;
            SetActionAnimation();
        }

        // -----------------------------------------------------------------------
        // Facing helpers
        // -----------------------------------------------------------------------
        private void UpdateFacing(float h, float v)
        {
            if (Mathf.Abs(h) >= Mathf.Abs(v))
                _facing = h > 0f ? Facing.E : Facing.W;
            else
                _facing = v > 0f ? Facing.N : Facing.S;
        }

        private Vector2Int FacingOffset() => _facing switch
        {
            // In this dimetric projection worldY = (gx+gy)*-0.25 + elev, so pressing
            // UP on-screen moves toward lower gy (the forge/back tier). N must therefore
            // offset to lower gy (-1) and S to higher gy (+1) — the opposite of an abstract
            // cardinal grid where north = +y.
            Facing.N => new Vector2Int( 0, -1),
            Facing.S => new Vector2Int( 0,  1),
            Facing.E => new Vector2Int( 1,  0),
            Facing.W => new Vector2Int(-1,  0),
            _        => Vector2Int.zero
        };

        // -----------------------------------------------------------------------
        // Animation helpers
        // -----------------------------------------------------------------------
        private void SetIdleAnimation()
        {
            if (_spriteAnimator == null) return;
            _spriteAnimator.Frames = FacingIdleStrip();
            _spriteAnimator.Fps    = 4f;
            _spriteAnimator.Loop   = true;
        }

        private void SetWalkAnimation()
        {
            if (_spriteAnimator == null) return;
            _spriteAnimator.Frames = FacingWalkStrip();
            _spriteAnimator.Fps    = 8f;
            _spriteAnimator.Loop   = true;
        }

        private void SetActionAnimation()
        {
            if (_spriteAnimator == null) return;
            _spriteAnimator.Frames = FacingActionStrip();
            _spriteAnimator.Fps    = 8f;
            _spriteAnimator.Loop   = false;
        }

        private Sprite[]? FacingIdleStrip() => _facing switch
        {
            Facing.S => _idleS, Facing.N => _idleN,
            Facing.E => _idleE, Facing.W => _idleW,
            _        => _idleS
        };

        private Sprite[]? FacingWalkStrip() => _facing switch
        {
            Facing.S => _walkS, Facing.N => _walkN,
            Facing.E => _walkE, Facing.W => _walkW,
            _        => _walkS
        };

        private Sprite[]? FacingActionStrip() => _facing switch
        {
            Facing.S => _actS, Facing.N => _actN,
            Facing.E => _actE, Facing.W => _actW,
            _        => _actS
        };

        // -----------------------------------------------------------------------
        // Sprite loading
        // -----------------------------------------------------------------------
        private void LoadSprites()
        {
            _idleS = TryLoadStrip("Sprites/player/player_idle_s", PlayerFrameWidth);
            _idleN = TryLoadStrip("Sprites/player/player_idle_n", PlayerFrameWidth);
            _idleE = TryLoadStrip("Sprites/player/player_idle_e", PlayerFrameWidth);
            _idleW = TryLoadStrip("Sprites/player/player_idle_w", PlayerFrameWidth);

            _walkS = TryLoadStrip("Sprites/player/player_walk_s", PlayerFrameWidth);
            _walkN = TryLoadStrip("Sprites/player/player_walk_n", PlayerFrameWidth);
            _walkE = TryLoadStrip("Sprites/player/player_walk_e", PlayerFrameWidth);
            _walkW = TryLoadStrip("Sprites/player/player_walk_w", PlayerFrameWidth);

            _actS  = TryLoadStrip("Sprites/player/player_action_s", PlayerFrameWidth);
            _actN  = TryLoadStrip("Sprites/player/player_action_n", PlayerFrameWidth);
            _actE  = TryLoadStrip("Sprites/player/player_action_e", PlayerFrameWidth);
            _actW  = TryLoadStrip("Sprites/player/player_action_w", PlayerFrameWidth);

            // Set initial idle-south strip
            SetIdleAnimation();
        }

        private static Sprite[]? TryLoadStrip(string path, int frameW)
        {
            try   { return SpriteLoader.LoadStrip(path, frameW); }
            catch { return null; }
        }

        // ---- UI wiring (called by Bootstrap after player is spawned) ----
        private UI.InventoryUI? _invUI;
        private UI.WorkshopUI?  _wsUI;
        private UI.StorageUI?   _stUI;
        private UI.BuildMenuUI? _bmUI;
        private UI.PauseMenuUI? _pmUI;

        public void SetUIRefs(UI.InventoryUI? inv, UI.WorkshopUI? ws,
                              UI.StorageUI? st, UI.BuildMenuUI? bm, UI.PauseMenuUI? pm)
        {
            _invUI = inv; _wsUI = ws; _stUI = st; _bmUI = bm; _pmUI = pm;
        }

        public UI.InventoryUI?  InventoryUI  => _invUI;
        public UI.WorkshopUI?   WorkshopUI   => _wsUI;
        public UI.StorageUI?    StorageUIRef => _stUI;
        public UI.BuildMenuUI?  BuildMenuUI  => _bmUI;
        public UI.PauseMenuUI?  PauseMenuUI  => _pmUI;
    }
}
