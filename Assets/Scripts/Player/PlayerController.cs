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
                Vector2Int gridPos = Core.GridMath.WorldToGrid(transform.position);
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
            HandleMovement();
            HandleToolInput();
        }

        // -----------------------------------------------------------------------
        // Movement
        // -----------------------------------------------------------------------
        private void HandleMovement()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

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
            Vector2 candidate = (Vector2)transform.position + dir * _moveSpeed * Time.deltaTime;

            // Clamp to valid island cell
            if (Island != null)
            {
                Vector2Int targetCell = Core.GridMath.WorldToGrid(candidate);
                if (!Island.IsWalkable(targetCell))
                {
                    // Try each axis independently (slide along walls)
                    Vector2 candidateX = (Vector2)transform.position + new Vector2(dir.x, 0f) * _moveSpeed * Time.deltaTime;
                    Vector2 candidateY = (Vector2)transform.position + new Vector2(0f, dir.y) * _moveSpeed * Time.deltaTime;

                    if (Island.IsWalkable(Core.GridMath.WorldToGrid(candidateX)))
                        candidate = candidateX;
                    else if (Island.IsWalkable(Core.GridMath.WorldToGrid(candidateY)))
                        candidate = candidateY;
                    else
                        candidate = transform.position;   // fully blocked
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

        // -----------------------------------------------------------------------
        // Tool input (1-4 hotbar)
        // -----------------------------------------------------------------------
        private void HandleToolInput()
        {
            if (!TryGetComponent<ToolSystem>(out var tools)) return;

            for (int i = 0; i < 4; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    tools.EquipBySlot(i);
                    return;
                }
            }
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
            Facing.S => new Vector2Int( 0, -1),
            Facing.N => new Vector2Int( 0,  1),
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
