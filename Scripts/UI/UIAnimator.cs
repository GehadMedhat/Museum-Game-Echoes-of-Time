/*
 ═══════════════════════════════════════════════════════════════
 UIAnimator.cs
 ───────────────────────────────────────────────────────────────
 Reusable coroutine-based animation helpers for UI Toolkit
 VisualElements.  All methods return IEnumerator so callers
 can chain them with yield return.

 No external dependencies — pure Unity coroutines.
 ═══════════════════════════════════════════════════════════════
*/

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace EchoesOfTime.UI
{
    public static class UIAnimator
    {
        // ─────────────────────────────────────────────────────────
        //  EASING
        // ─────────────────────────────────────────────────────────

        public static float EaseInOut(float t) =>
            t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;

        public static float EaseOut(float t) => 1f - (1f - t) * (1f - t);

        public static float EaseIn(float t) => t * t;

        // ─────────────────────────────────────────────────────────
        //  OPACITY
        // ─────────────────────────────────────────────────────────

        /// <summary>Animate opacity from → to over duration seconds.</summary>
        public static IEnumerator Fade(VisualElement el, float from, float to,
                                       float duration, Func<float, float> ease = null)
        {
            ease ??= EaseInOut;
            el.style.opacity = from;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                el.style.opacity = Mathf.Lerp(from, to, ease(Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }

            el.style.opacity = to;
        }

        // ─────────────────────────────────────────────────────────
        //  SLIDE (translate Y)
        // ─────────────────────────────────────────────────────────

        /// <summary>Slide element vertically (pixels) from → to.</summary>
        public static IEnumerator SlideY(VisualElement el, float fromPx, float toPx,
                                          float duration, Func<float, float> ease = null)
        {
            ease ??= EaseOut;
            float elapsed = 0f;
            el.style.translate = new Translate(0, fromPx, 0);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float y = Mathf.Lerp(fromPx, toPx, ease(Mathf.Clamp01(elapsed / duration)));
                el.style.translate = new Translate(0, y, 0);
                yield return null;
            }

            el.style.translate = new Translate(0, toPx, 0);
        }

        // ─────────────────────────────────────────────────────────
        //  FADE + SLIDE combined (replicates CSS fadeDown / fadeUp)
        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// CSS fadeDown: fade in while sliding from -offsetPx → 0.
        /// CSS fadeUp  : fade in while sliding from +offsetPx → 0.
        /// </summary>
        public static IEnumerator FadeSlide(VisualElement el, float offsetPx,
                                             float duration, float delay = 0f,
                                             Func<float, float> ease = null)
        {
            el.style.opacity   = 0;
            el.style.translate = new Translate(0, offsetPx, 0);

            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

            ease ??= EaseOut;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = ease(Mathf.Clamp01(elapsed / duration));
                el.style.opacity   = t;
                el.style.translate = new Translate(0, Mathf.Lerp(offsetPx, 0f, t), 0);
                yield return null;
            }

            el.style.opacity   = 1f;
            el.style.translate = new Translate(0, 0, 0);
        }

        // ─────────────────────────────────────────────────────────
        //  GLOW PULSE (animates opacity to simulate text glow)
        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// Loops forever, breathing opacity between minOpacity and 1.
        /// Mirror of CSS glowPulse keyframe (3s period).
        /// Stop by setting the returned token's Cancelled = true.
        /// </summary>
        public static IEnumerator GlowPulse(VisualElement el,
                                             float minOpacity = 0.75f,
                                             float period     = 3f)
        {
            float half = period / 2f;

            while (true)
            {
                // Rise
                for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
                {
                    if (el == null) yield break;
                    el.style.opacity = Mathf.Lerp(minOpacity, 1f, t / half);
                    yield return null;
                }
                // Fall
                for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
                {
                    if (el == null) yield break;
                    el.style.opacity = Mathf.Lerp(1f, minOpacity, t / half);
                    yield return null;
                }
            }
        }

        // ─────────────────────────────────────────────────────────
        //  PROGRESS BAR WIDTH
        // ─────────────────────────────────────────────────────────

        /// <summary>Animate a fill bar's width (percent 0–100) from current → target.</summary>
        public static IEnumerator AnimateProgressBar(VisualElement fill,
                                                      float targetPct,
                                                      float duration = 0.6f)
        {
            float startPct = fill.resolvedStyle.width /
                             (fill.parent?.resolvedStyle.width ?? 1f) * 100f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float pct = Mathf.Lerp(startPct, targetPct, EaseOut(Mathf.Clamp01(elapsed / duration)));
                fill.style.width = Length.Percent(pct);
                yield return null;
            }
            fill.style.width = Length.Percent(targetPct);
        }

        // ─────────────────────────────────────────────────────────
        //  CARD HOVER (translate Y lift)
        // ─────────────────────────────────────────────────────────

        /// <summary>Registers hover-lift callbacks on a card element (-8px lift).</summary>
        public static void RegisterCardHover(VisualElement card,
                                              MonoBehaviour host,
                                              float liftPx   = -8f,
                                              float duration = 0.35f)
        {
            Coroutine active = null;

            card.RegisterCallback<MouseEnterEvent>(_ =>
            {
                if (active != null) host.StopCoroutine(active);
                active = host.StartCoroutine(AnimateLift(card, liftPx, duration));
            });

            card.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                if (active != null) host.StopCoroutine(active);
                active = host.StartCoroutine(AnimateLift(card, 0f, duration));
            });
        }

        private static IEnumerator AnimateLift(VisualElement el, float targetY, float duration)
        {
            // Read current Y
            float startY  = el.resolvedStyle.translate.y;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float y = Mathf.Lerp(startY, targetY, EaseOut(Mathf.Clamp01(elapsed / duration)));
                el.style.translate = new Translate(0, y, 0);
                yield return null;
            }

            el.style.translate = new Translate(0, targetY, 0);
        }

        // ─────────────────────────────────────────────────────────
        //  FLOAT ANIMATION (orb / idle element bob)
        // ─────────────────────────────────────────────────────────

        /// <summary>Loops a gentle vertical bob (mirrors CSS @keyframes float).</summary>
        public static IEnumerator FloatBob(VisualElement el,
                                            float amplitude = 6f,
                                            float period    = 6f,
                                            float phaseOffset = 0f)
        {
            float timer = phaseOffset;
            while (true)
            {
                if (el == null) yield break;
                timer += Time.unscaledDeltaTime;
                float y = Mathf.Sin(timer / period * Mathf.PI * 2f) * amplitude;
                el.style.translate = new Translate(0, y, 0);
                yield return null;
            }
        }
    }
}
