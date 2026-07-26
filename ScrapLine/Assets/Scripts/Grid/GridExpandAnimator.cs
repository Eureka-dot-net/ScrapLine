using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Handles visual animations for grid expansion.
/// Provides coroutines for row/column insertion animations with slide effects.
/// Does NOT modify data - only provides visual feedback.
/// </summary>
public class GridExpandAnimator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Grid whose newly-created cells should be animated")]
    public UIGridManager uiGridManager;

    [Header("Animation Settings")]
    [Tooltip("Duration of slide animation in seconds")]
    public float slideDuration = 0.15f;

    [Tooltip("Animation curve for slide effect (ease out recommended)")]
    public AnimationCurve slideEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Audio")]
    [Tooltip("Sound effect played when expansion completes")]
    public AudioClip buildSfx;

    [Header("Debug")]
    [Tooltip("Enable debug logs for animation operations")]
    public bool enableAnimationLogs = true;

    private string ComponentId => $"GridExpandAnimator_{GetEntityId()}";

    /// <summary>
    /// Play row insertion animation
    /// </summary>
    /// <param name="rowIndex">Index where row is being inserted</param>
    /// <returns>Coroutine that completes when animation finishes</returns>
    public IEnumerator PlayInsertRow(int rowIndex)
    {
        if (enableAnimationLogs)
            GameLogger.LogGrid($"Starting row insertion animation at index {rowIndex}", ComponentId);

        if (uiGridManager != null)
            yield return AnimateCells(index => uiGridManager.GetCell(index, rowIndex));

        // Play sound effect
        PlayBuildSound();

        if (enableAnimationLogs)
            GameLogger.LogGrid($"Row insertion animation completed", ComponentId);
    }

    /// <summary>
    /// Play column insertion animation
    /// </summary>
    /// <param name="colIndex">Index where column is being inserted</param>
    /// <returns>Coroutine that completes when animation finishes</returns>
    public IEnumerator PlayInsertColumn(int colIndex)
    {
        if (enableAnimationLogs)
            GameLogger.LogGrid($"Starting column insertion animation at index {colIndex}", ComponentId);

        if (uiGridManager != null)
            yield return AnimateCells(index => uiGridManager.GetCell(colIndex, index));

        // Play sound effect
        PlayBuildSound();

        if (enableAnimationLogs)
            GameLogger.LogGrid($"Column insertion animation completed", ComponentId);
    }

    /// <summary>
    /// Play edge column insertion animation
    /// </summary>
    /// <param name="edge">Which edge the column is being inserted at</param>
    /// <returns>Coroutine that completes when animation finishes</returns>
    public IEnumerator PlayInsertEdgeColumn(GridExpansionService.Edge edge)
    {
        if (enableAnimationLogs)
            GameLogger.LogGrid($"Starting edge column insertion animation at {edge} edge", ComponentId);

        int columnIndex = 0;
        if (edge == GridExpansionService.Edge.Right && uiGridManager != null)
        {
            while (uiGridManager.GetCell(columnIndex + 1, 0) != null)
                columnIndex++;
        }

        yield return PlayInsertColumn(columnIndex);
    }

    private IEnumerator AnimateCells(System.Func<int, UICell> cellAtIndex)
    {
        var animated = new System.Collections.Generic.List<RectTransform>();
        for (int i = 0; ; i++)
        {
            UICell cell = cellAtIndex(i);
            if (cell == null)
                break;

            RectTransform rect = cell.transform as RectTransform;
            if (rect != null)
            {
                rect.localScale = Vector3.zero;
                animated.Add(rect);
            }
        }

        if (animated.Count == 0)
            yield break;

        float duration = Mathf.Max(0.01f, slideDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float eased = slideEase.Evaluate(Mathf.Clamp01(elapsed / duration));
            foreach (RectTransform rect in animated)
                if (rect != null) rect.localScale = Vector3.one * eased;
            yield return null;
        }

        foreach (RectTransform rect in animated)
            if (rect != null) rect.localScale = Vector3.one;
    }

    /// <summary>
    /// Play the build sound effect if available
    /// </summary>
    private void PlayBuildSound()
    {
        if (buildSfx != null)
        {
            // Check if AudioSource exists on this GameObject
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.PlayOneShot(buildSfx);

            if (enableAnimationLogs)
                GameLogger.LogGrid("Played build sound effect", ComponentId);
        }
    }

    /// <summary>
    /// Utility: Animate a RectTransform's position
    /// </summary>
    /// <param name="rectTransform">RectTransform to animate</param>
    /// <param name="startPos">Starting position</param>
    /// <param name="endPos">Ending position</param>
    /// <param name="duration">Animation duration</param>
    /// <returns>Coroutine</returns>
    private IEnumerator AnimatePosition(RectTransform rectTransform, Vector3 startPos, Vector3 endPos, float duration)
    {
        if (rectTransform == null) yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (rectTransform == null) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curveValue = slideEase.Evaluate(t);
            
            rectTransform.position = Vector3.Lerp(startPos, endPos, curveValue);
            yield return null;
        }

        if (rectTransform != null)
            rectTransform.position = endPos;
    }

    /// <summary>
    /// Utility: Fade in a CanvasGroup
    /// </summary>
    /// <param name="canvasGroup">CanvasGroup to fade</param>
    /// <param name="duration">Fade duration</param>
    /// <returns>Coroutine</returns>
    private IEnumerator FadeIn(CanvasGroup canvasGroup, float duration)
    {
        if (canvasGroup == null) yield break;

        float elapsed = 0f;
        canvasGroup.alpha = 0f;

        while (elapsed < duration)
        {
            if (canvasGroup == null) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = t;
            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }
}

/*
 * UNITY WIRING INSTRUCTIONS:
 * 
 * 1. Add this component to the "ExpandModeSystem" GameObject
 * 2. Configure animation settings:
 *    - Slide Duration: 0.15 seconds (fast, snappy animation)
 *    - Slide Ease: Click the curve, set to EaseOutQuad or similar smooth curve
 *      (Start flat, accelerate smoothly, end sharp for snap-in effect)
 * 3. (Optional) Assign audio:
 *    - Drag a build/placement sound effect AudioClip into "Build Sfx" field
 *    - Example sounds: pop, click, build confirmation
 *    - Leave empty if no sound desired
 * 4. Configure debug:
 *    - Enable Animation Logs: true (for debugging)
 * 5. This animator is called by the expansion orchestrator after data changes
 * 
 * ANIMATION CURVE SETUP:
 * - In Inspector, click the "Slide Ease" curve
 * - Use preset: "EaseInOut" or create custom curve
 * - For snappy feel: Start slow, end fast (EaseOutQuad)
 * - For smooth feel: Equal acceleration/deceleration (EaseInOut)
 * 
 * AUDIO SETUP:
 * - Create/import a short sound effect (<0.5s)
 * - Formats: .wav, .ogg, .mp3
 * - Place in Assets/Resources/Audio/ or similar
 * - Drag into "Build Sfx" field in Inspector
 */
