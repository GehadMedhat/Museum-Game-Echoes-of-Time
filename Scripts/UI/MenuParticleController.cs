/*
 ═══════════════════════════════════════════════════════════════
 MenuParticleController.cs
 ───────────────────────────────────────────────────────────────
 Pure UI Toolkit particle system — no 3D, no shaders, no lag.
 Spawns tiny gold/parchment dots directly inside the UIDocument
 using VisualElements + IVisualElementScheduler.

 SETUP:
   1. Attach to the same GameObject as your UIDocument.
   2. Remove (or disable) the old ParticleSystem component.
   3. No other setup needed — runs automatically on Start.
 ═══════════════════════════════════════════════════════════════
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

namespace EchoesOfTime.UI
{
    public class MenuParticleController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIDocument uiDocument;

        [Header("Particle Settings")]
        [SerializeField] private int   count        = 55;
        [SerializeField] private float minSize      = 2f;
        [SerializeField] private float maxSize      = 5f;
        [SerializeField] private float minSpeed     = 18f;   // px per second
        [SerializeField] private float maxSpeed     = 55f;
        [SerializeField] private float minDrift     = -12f;  // horizontal wander px/s
        [SerializeField] private float maxDrift     = 12f;

        // ── Runtime ──────────────────────────────────────────────
        private VisualElement        _canvas;
        private readonly List<Dot>   _dots   = new();
        private float                _width, _height;
        private bool                 _running;
private VisualElement _fireworksCanvas;

        private static readonly Color32[] Colors =
        {
            new(201, 162,  39, 140),   // gold
            new(201, 162,  39,  90),   // gold dim
            new(240, 208,  96, 110),   // gold light
            new(245, 237, 214,  55),   // parchment
            new(245, 237, 214,  35),   // parchment faint
        };

        // ── Dot state ────────────────────────────────────────────
        private class Dot
        {
            public VisualElement El;
            public float X, Y;       // current position (px)
            public float SpeedY;     // upward speed px/s
            public float DriftX;     // horizontal wander px/s
            public float Life;       // 0→1 normalised age
            public float LifeSpeed;  // how fast it ages
            public float Size;
        }

        // ─────────────────────────────────────────────────────────
        private void Start()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            // Wait one frame for UIDocument to build its hierarchy
            uiDocument.rootVisualElement
                      .schedule
                      .Execute(Init)
                      .ExecuteLater(50);
        }

        private void Init()
        {
            var root = uiDocument.rootVisualElement;

            _width  = root.resolvedStyle.width;
            _height = root.resolvedStyle.height;
            Debug.Log($"[Particles] Init — root size: {_width}x{_height}");

            // Create a full-screen RED canvas first to confirm visibility
            _canvas = new VisualElement
            {
                name = "particle-canvas",
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    left     = 0, top = 0, right = 0, bottom = 0,
                    overflow = Overflow.Hidden,
                }
            };

            // Add to root — sits on top of bg but behind screens
            root.Add(_canvas);
            Debug.Log($"[Particles] Canvas inserted. Parent: {_canvas.parent?.name}");

            // Read screen size from root geometry
            root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            // Spawn all dots
            for (int i = 0; i < count; i++)
                SpawnDot(randomY: true);
            Debug.Log($"[Particles] Spawned {_dots.Count} dots. Canvas children: {_canvas.childCount}");

            _running = true;
        }

        // ─────────────────────────────────────────────────────────
        private void SpawnDot(bool randomY = false)
        {
            float size  = Random.Range(minSize, maxSize);
            var   color = Colors[Random.Range(0, Colors.Length)];

            var el = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position     = Position.Absolute,
                    width        = size,
                    height       = size,
                    borderTopLeftRadius     = size,
                    borderTopRightRadius    = size,
                    borderBottomLeftRadius  = size,
                    borderBottomRightRadius = size,
                    backgroundColor = new StyleColor(color),
                }
            };
            _canvas.Add(el);

            var dot = new Dot
            {
                El         = el,
                Size       = size,
                X          = Random.Range(0f, _width > 0 ? _width  : 1920f),
                Y          = randomY
                             ? Random.Range(0f, _height > 0 ? _height : 1080f)
                             : (_height > 0 ? _height : 1080f) + size,
                SpeedY     = Random.Range(minSpeed, maxSpeed),
                DriftX     = Random.Range(minDrift, maxDrift),
                Life       = randomY ? Random.Range(0f, 1f) : 0f,
                LifeSpeed  = Random.Range(0.025f, 0.06f),
            };

            PlaceDot(dot);
            _dots.Add(dot);
        }

        // ─────────────────────────────────────────────────────────
        private void Update()
        {
            if (!_running || _canvas == null) return;

            float dt = Time.deltaTime;
            float w  = _width  > 0 ? _width  : 1920f;
            float h  = _height > 0 ? _height : 1080f;

            foreach (var dot in _dots)
            {
                // Move upward + drift
                dot.Y    -= dot.SpeedY * dt;   // Y increases downward in UI Toolkit
                dot.X    += dot.DriftX * dt;
                dot.Life += dot.LifeSpeed * dt;

                // Wrap horizontally
                if (dot.X < -dot.Size)   dot.X = w + dot.Size;
                if (dot.X > w + dot.Size) dot.X = -dot.Size;

                // Reset when off top of screen
                if (dot.Y < -dot.Size)
                {
                    dot.Y    = h + dot.Size;
                    dot.X    = Random.Range(0f, w);
                    dot.Life = 0f;
                    dot.SpeedY = Random.Range(minSpeed, maxSpeed);
                    dot.DriftX = Random.Range(minDrift, maxDrift);
                }

                // Opacity: fade in (0→0.15), hold (0.15→0.85), fade out (0.85→1)
                float alpha;
                if      (dot.Life < 0.15f) alpha = dot.Life / 0.15f;
                else if (dot.Life < 0.85f) alpha = 1f;
                else                       alpha = 1f - (dot.Life - 0.85f) / 0.15f;
                alpha = Mathf.Clamp01(alpha);

                dot.El.style.opacity = alpha;
                PlaceDot(dot);
            }
        }

        // ─────────────────────────────────────────────────────────
        private static void PlaceDot(Dot dot)
        {
            dot.El.style.left = dot.X - dot.Size * 0.5f;
            dot.El.style.top  = dot.Y - dot.Size * 0.5f;
        }

        private void OnGeometryChanged(GeometryChangedEvent e)
        {
            var root = uiDocument.rootVisualElement;
            _width  = root.resolvedStyle.width;
            _height = root.resolvedStyle.height;
        }

        private void OnDestroy()
        {
            _running = false;
            _canvas?.RemoveFromHierarchy();
        }
        
        
        // ─────────────────────────────────────────────────────────
// FIREWORKS — call this when all 3 levels are complete
// ─────────────────────────────────────────────────────────

private static readonly Color32[] FireworkColors =
{
    new(255, 80,  80,  255),   // red
    new(255, 200, 50,  255),   // gold
    new(80,  220, 120, 255),   // green
    new(80,  160, 255, 255),   // blue
    new(255, 100, 220, 255),   // pink
    new(255, 255, 255, 255),   // white
};

public void TriggerFireworks()
{
    if (_canvas == null) return;
    StartCoroutine(FireworksBurst());
}

private IEnumerator FireworksBurst()
{
    var target = _fireworksCanvas ?? _canvas;
    
    // Get actual size from the target element
    float w = target.resolvedStyle.width;
    float h = target.resolvedStyle.height;
    if (w <= 0) w = _width  > 0 ? _width  : 1920f;
    if (h <= 0) h = _height > 0 ? _height : 1080f;

    for (int wave = 0; wave < 8; wave++)
    {
        float cx = w * Random.Range(0.2f, 0.8f);
        float cy = h * Random.Range(0.1f, 0.6f);
    SpawnBurst(cx, cy, 60);              // was 30-45
    yield return new WaitForSeconds(0.3f); // was 0.35-0.4
    }
}

private void SpawnBurst(float cx, float cy, int particleCount)
{
    var target = _fireworksCanvas ?? _canvas;
    for (int i = 0; i < particleCount; i++)
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
float speed = Random.Range(200f, 500f);  // was 80-280
float size  = Random.Range(4f, 10f);     // was 3-7
        var   color = FireworkColors[Random.Range(0, FireworkColors.Length)];

        var el = new VisualElement
        {
            pickingMode = PickingMode.Ignore,
            style =
            {
                position                = Position.Absolute,
                width                   = size,
                height                  = size,
                borderTopLeftRadius     = size,
                borderTopRightRadius    = size,
                borderBottomLeftRadius  = size,
                borderBottomRightRadius = size,
                backgroundColor         = new StyleColor(color),
            }
        };
        target.Add(el);
        StartCoroutine(AnimateFireworkParticle(el, cx, cy, angle, speed, size));
    }
}

private IEnumerator AnimateFireworkParticle(
    VisualElement el, float cx, float cy,
    float angle, float speed, float size)
{
    float vx      = Mathf.Cos(angle) * speed;
    float vy      = Mathf.Sin(angle) * speed;
    float gravity = 180f;
    float life    = 0f;
    float maxLife = Random.Range(0.7f, 1.4f);
    float x = cx, y = cy;

    while (life < maxLife)
    {
        float dt = Time.deltaTime;
        life += dt;
        vy   += gravity * dt;   // gravity pulls down
        x    += vx * dt;
        y    += vy * dt;

        float t     = life / maxLife;
        float alpha = 1f - t;   // fade out over lifetime

        el.style.left    = x - size * 0.5f;
        el.style.top     = y - size * 0.5f;
        el.style.opacity = alpha;

        yield return null;
    }

    el.RemoveFromHierarchy();
}
public void SetFireworksTarget(VisualElement target)
{
    _fireworksCanvas = target;
}

    }
}
