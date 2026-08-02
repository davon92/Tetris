using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class BattleEffectsController : MonoBehaviour
{
    private const float PixelsPerUnit = 16f;
    private const int MaxGarbageIndicators = 20;
    private const float GarbageIndicatorRowGap = 1f;
    private const float GarbageIndicatorClearance = 0.9f;

    private sealed class SpriteEffect
    {
        public GameObject GameObject;
        public SpriteRenderer Renderer;
        public Vector3 Start;
        public Vector3 End;
        public Vector3 StartScale;
        public Vector3 EndScale;
        public Color StartColor;
        public Color EndColor;
        public float Duration;
        public float Delay;
        public float ArcHeight;
        public float RotationSpeed;
        public float Age;
        public Action OnComplete;
    }

    private sealed class ShakeEffect
    {
        public Transform Target;
        public Vector3 RestPosition;
        public float Duration;
        public float Age;
        public int StrengthPixels;
    }

    private readonly List<SpriteEffect> spriteEffects = new();
    private readonly List<ShakeEffect> shakeEffects = new();
    private readonly System.Random random = new(7331);

    private TetrisGameSession playerOne;
    private TetrisGameSession playerTwo;
    private Sprite effectSprite;
    private Material effectMaterial;
    private SpriteRenderer[] playerOneGarbageDots;
    private SpriteRenderer[] playerTwoGarbageDots;

    public void Initialize(
        TetrisGameSession first,
        TetrisGameSession second,
        TetriminoPiece[] piecePrefabs)
    {
        ClearBattle();
        playerOne = first;
        playerTwo = second;
        CacheEffectStyle(piecePrefabs);
        Subscribe(playerOne);
        Subscribe(playerTwo);
        playerOneGarbageDots = CreateGarbageDots(playerOne);
        playerTwoGarbageDots = CreateGarbageDots(playerTwo);
    }

    public void ClearBattle()
    {
        Unsubscribe(playerOne);
        Unsubscribe(playerTwo);
        RestoreShakenBoards();

        for (int i = spriteEffects.Count - 1; i >= 0; i--)
        {
            if (spriteEffects[i].GameObject != null)
                Destroy(spriteEffects[i].GameObject);
        }

        spriteEffects.Clear();
        shakeEffects.Clear();
        DestroyGarbageDots(ref playerOneGarbageDots);
        DestroyGarbageDots(ref playerTwoGarbageDots);
        playerOne = null;
        playerTwo = null;
    }

    private void OnDisable()
    {
        ClearBattle();
    }

    private void Update()
    {
        UpdateSpriteEffects(Time.deltaTime);
        UpdateShakeEffects(Time.deltaTime);
        UpdateGarbageIndicators(playerOne, playerOneGarbageDots);
        UpdateGarbageIndicators(playerTwo, playerTwoGarbageDots);
    }

    private void Subscribe(TetrisGameSession session)
    {
        if (session == null)
            return;

        session.PieceLocked += OnPieceLocked;
        session.LinesCleared += OnLinesCleared;
        session.AttackGenerated += OnAttackGenerated;
        session.GarbageApplied += OnGarbageApplied;
        session.GarbageCancelled += OnGarbageCancelled;
        session.AbilityResolved += OnAbilityResolved;
    }

    private void Unsubscribe(TetrisGameSession session)
    {
        if (session == null)
            return;

        session.PieceLocked -= OnPieceLocked;
        session.LinesCleared -= OnLinesCleared;
        session.AttackGenerated -= OnAttackGenerated;
        session.GarbageApplied -= OnGarbageApplied;
        session.GarbageCancelled -= OnGarbageCancelled;
        session.AbilityResolved -= OnAbilityResolved;
    }

    private void CacheEffectStyle(TetriminoPiece[] piecePrefabs)
    {
        effectSprite = null;
        effectMaterial = null;
        if (piecePrefabs == null)
            return;

        foreach (TetriminoPiece prefab in piecePrefabs)
        {
            if (prefab == null)
                continue;

            SpriteRenderer[] renderers = prefab.GetVisualRenderers();
            if (renderers.Length == 0)
                continue;

            effectSprite = renderers[0].sprite;
            effectMaterial = renderers[0].sharedMaterial;
            return;
        }
    }

    private void OnPieceLocked(TetrisGameSession session, Vector2Int[] cells)
    {
        Color color = Brighten(TetrominoDefinitions.GetColor(session.ActiveType), 0.35f);
        foreach (Vector2Int cell in cells)
        {
            if (cell.y < 0 || cell.y >= session.Model.VisibleHeight)
                continue;

            Vector3 start = session.GetCellWorldPosition(cell);
            Vector3 drift = new Vector3(
                RandomRange(-0.3f, 0.3f),
                RandomRange(0.15f, 0.55f),
                0f);
            CreateSpriteEffect(
                "Lock Spark",
                start,
                start + drift,
                Vector3.one * 0.35f,
                Vector3.one * 0.08f,
                new Color(color.r, color.g, color.b, 0.9f),
                new Color(color.r, color.g, color.b, 0f),
                0.22f,
                rotationSpeed: RandomRange(-240f, 240f));
        }
    }

    private void OnLinesCleared(TetrisGameSession session, int[] rows)
    {
        foreach (int row in rows)
        {
            Vector3 left = session.GetCellWorldPosition(new Vector2Int(0, row));
            Vector3 right = session.GetCellWorldPosition(new Vector2Int(session.Model.Width - 1, row));
            Vector3 center = (left + right) * 0.5f;
            CreateSpriteEffect(
                "Line Clear Flash",
                center,
                center,
                new Vector3(session.Model.Width, 0.85f, 1f),
                new Vector3(session.Model.Width, 0.1f, 1f),
                new Color(1f, 0.95f, 0.58f, 0.9f),
                new Color(0.45f, 0.9f, 1f, 0f),
                0.32f);

            for (int x = 0; x < session.Model.Width; x += 2)
            {
                Vector3 start = session.GetCellWorldPosition(new Vector2Int(x, row));
                Vector3 end = start + new Vector3(
                    RandomRange(-0.65f, 0.65f),
                    RandomRange(0.8f, 1.8f),
                    0f);
                CreateSpriteEffect(
                    "Clear Spark",
                    start,
                    end,
                    Vector3.one * 0.32f,
                    Vector3.one * 0.05f,
                    new Color(1f, 0.75f, 0.25f, 1f),
                    new Color(0.25f, 0.9f, 1f, 0f),
                    0.42f,
                    arcHeight: 0.35f,
                    delay: x * 0.015f,
                    rotationSpeed: RandomRange(-360f, 360f));
            }
        }

        StartShake(session, 0.16f, Mathf.Clamp(rows.Length, 1, 2));
    }

    private void OnAttackGenerated(TetrisGameSession source, int lines)
    {
        TetrisGameSession target = source == playerOne ? playerTwo : playerOne;
        if (target == null)
            return;

        float sourceY = source.Model.VisibleHeight + GarbageIndicatorClearance;
        float targetY = target.Model.VisibleHeight + GarbageIndicatorClearance;
        Vector3 start = source.GetBoardWorldPosition(source.Model.Width * 0.5f, sourceY);
        Vector3 end = target.GetBoardWorldPosition(target.Model.Width * 0.5f, targetY);
        Color attackColor = lines >= 4
            ? new Color(1f, 0.28f, 0.85f, 1f)
            : new Color(0.25f, 0.9f, 1f, 1f);
        float scale = 0.65f + Mathf.Min(lines, 4) * 0.14f;

        // Arc peak must clear the boards but stay below the title banner:
        // IMGUI draws over world sprites, so anything above ~world y 12.4
        // vanishes behind the opaque banner mid-flight.
        CreateSpriteEffect(
            "Magic Attack",
            start,
            end,
            Vector3.one * scale,
            Vector3.one * (scale * 1.25f),
            attackColor,
            new Color(1f, 0.9f, 0.3f, 0.15f),
            0.52f,
            arcHeight: 1.3f,
            rotationSpeed: 540f,
            onComplete: () => SpawnAttackImpact(target, end, attackColor, lines));

        for (int i = 0; i < 4; i++)
        {
            float offset = (i - 1.5f) * 0.28f;
            CreateSpriteEffect(
                "Magic Trail",
                start + Vector3.up * offset,
                end + Vector3.up * offset,
                Vector3.one * 0.28f,
                Vector3.one * 0.08f,
                new Color(attackColor.r, attackColor.g, attackColor.b, 0.7f),
                new Color(attackColor.r, attackColor.g, attackColor.b, 0f),
                0.48f,
                arcHeight: 1.1f + Mathf.Abs(offset) * 0.5f,
                delay: i * 0.035f,
                rotationSpeed: i % 2 == 0 ? 420f : -420f);
        }
    }

    private void OnGarbageApplied(TetrisGameSession session, int lineCount)
    {
        StartShake(session, 0.34f, Mathf.Clamp(1 + lineCount / 2, 2, 4));
        Vector3 center = session.GetBoardWorldPosition(session.Model.Width * 0.5f, 1.2f);
        for (int i = 0; i < Mathf.Min(12, 4 + lineCount * 2); i++)
        {
            Vector3 start = center + new Vector3(RandomRange(-4.5f, 4.5f), 0f, 0f);
            Vector3 end = start + new Vector3(
                RandomRange(-0.8f, 0.8f),
                RandomRange(1.2f, 3.2f),
                0f);
            CreateSpriteEffect(
                "Garbage Impact",
                start,
                end,
                Vector3.one * 0.42f,
                Vector3.one * 0.08f,
                new Color(0.8f, 0.85f, 1f, 0.95f),
                new Color(0.4f, 0.45f, 0.62f, 0f),
                0.48f,
                arcHeight: 0.5f,
                delay: i * 0.015f,
                rotationSpeed: RandomRange(-280f, 280f));
        }
    }

    /// <summary>A spell landed on this board: flash every cell it destroyed.</summary>
    private void OnAbilityResolved(
        TetrisGameSession session,
        MagicAbilityDefinition ability,
        Vector2Int[] destroyed)
    {
        // The flash takes the spell's authored accent, so a designer recolours
        // an effect by recolouring its asset.
        Color spellColor = ability.Accent;
        spellColor.a = 1f;

        bool mending = ability.Effect == MagicEffect.Mend;
        StartShake(session, 0.4f, mending ? 2 : 5);

        if (mending)
        {
            Vector3 healCenter = session.GetBoardWorldPosition(session.Model.Width * 0.5f, 4f);
            for (int i = 0; i < 14; i++)
            {
                Vector3 start = healCenter + new Vector3(RandomRange(-4.5f, 4.5f), RandomRange(-3f, 0f), 0f);
                CreateSpriteEffect(
                    "Heal Mote",
                    start,
                    start + new Vector3(RandomRange(-0.4f, 0.4f), RandomRange(2.5f, 4.5f), 0f),
                    Vector3.one * 0.34f,
                    Vector3.one * 0.06f,
                    spellColor,
                    new Color(spellColor.r, spellColor.g, spellColor.b, 0f),
                    0.6f,
                    delay: i * 0.02f,
                    rotationSpeed: RandomRange(-200f, 200f));
            }

            return;
        }

        foreach (Vector2Int cell in destroyed)
        {
            Vector3 position = session.GetCellWorldPosition(cell);
            CreateSpriteEffect(
                "Spell Burst",
                position,
                position + new Vector3(RandomRange(-0.7f, 0.7f), RandomRange(0.4f, 1.6f), 0f),
                Vector3.one * 0.55f,
                Vector3.one * 0.05f,
                spellColor,
                new Color(1f, 0.95f, 0.6f, 0f),
                0.45f,
                arcHeight: 0.3f,
                delay: RandomRange(0f, 0.12f),
                rotationSpeed: RandomRange(-400f, 400f));
        }
    }

    /// <summary>The player cleared enough to cancel some or all of their incoming garbage.</summary>
    private void OnGarbageCancelled(TetrisGameSession session, int cancelledLines)
    {
        Color shieldColor = new Color(0.4f, 1f, 0.68f, 1f);
        Vector3 center = session.GetBoardWorldPosition(
            session.Model.Width * 0.5f,
            session.Model.VisibleHeight + GarbageIndicatorClearance);

        for (int i = 0; i < 10; i++)
        {
            float angle = i * Mathf.PI * 2f / 10;
            Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * 0.5f, 0f);
            CreateSpriteEffect(
                "Garbage Blocked",
                center,
                center + direction * RandomRange(0.8f, 1.6f),
                Vector3.one * 0.36f,
                Vector3.one * 0.05f,
                shieldColor,
                new Color(shieldColor.r, shieldColor.g, shieldColor.b, 0f),
                0.4f,
                delay: (i % 3) * 0.02f,
                rotationSpeed: i % 2 == 0 ? 360f : -360f);
        }
    }

    private void SpawnAttackImpact(
        TetrisGameSession target,
        Vector3 position,
        Color color,
        int lines)
    {
        StartShake(target, 0.24f, Mathf.Clamp(lines, 1, 4));
        int sparkCount = 8 + Mathf.Min(lines, 4) * 2;
        for (int i = 0; i < sparkCount; i++)
        {
            float angle = i * Mathf.PI * 2f / sparkCount;
            Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            CreateSpriteEffect(
                "Magic Impact",
                position,
                position + direction * RandomRange(1.1f, 2.4f),
                Vector3.one * 0.42f,
                Vector3.one * 0.05f,
                color,
                new Color(1f, 0.85f, 0.2f, 0f),
                0.38f,
                arcHeight: 0.2f,
                delay: (i % 3) * 0.025f,
                rotationSpeed: i % 2 == 0 ? 400f : -400f);
        }
    }

    private void CreateSpriteEffect(
        string effectName,
        Vector3 start,
        Vector3 end,
        Vector3 startScale,
        Vector3 endScale,
        Color startColor,
        Color endColor,
        float duration,
        float arcHeight = 0f,
        float delay = 0f,
        float rotationSpeed = 0f,
        Action onComplete = null)
    {
        GameObject effectObject = new GameObject(effectName);
        effectObject.transform.SetParent(transform, true);
        effectObject.transform.position = start;
        effectObject.transform.localScale = startScale;

        SpriteRenderer renderer = effectObject.AddComponent<SpriteRenderer>();
        renderer.sprite = effectSprite;
        if (effectMaterial != null)
            renderer.sharedMaterial = effectMaterial;
        renderer.sortingOrder = 80;
        renderer.color = startColor;

        spriteEffects.Add(new SpriteEffect
        {
            GameObject = effectObject,
            Renderer = renderer,
            Start = start,
            End = end,
            StartScale = startScale,
            EndScale = endScale,
            StartColor = startColor,
            EndColor = endColor,
            Duration = Mathf.Max(0.01f, duration),
            Delay = Mathf.Max(0f, delay),
            ArcHeight = arcHeight,
            RotationSpeed = rotationSpeed,
            OnComplete = onComplete
        });
    }

    private void UpdateSpriteEffects(float deltaTime)
    {
        for (int i = spriteEffects.Count - 1; i >= 0; i--)
        {
            SpriteEffect effect = spriteEffects[i];
            effect.Age += deltaTime;
            if (effect.Age < effect.Delay)
            {
                effect.Renderer.enabled = false;
                continue;
            }

            effect.Renderer.enabled = true;
            float progress = Mathf.Clamp01((effect.Age - effect.Delay) / effect.Duration);
            Vector3 position = Vector3.Lerp(effect.Start, effect.End, progress);
            position.y += Mathf.Sin(progress * Mathf.PI) * effect.ArcHeight;
            effect.GameObject.transform.position = position;
            effect.GameObject.transform.localScale =
                Vector3.Lerp(effect.StartScale, effect.EndScale, progress);
            effect.GameObject.transform.Rotate(0f, 0f, effect.RotationSpeed * deltaTime);
            effect.Renderer.color = Color.Lerp(effect.StartColor, effect.EndColor, progress);

            if (progress < 1f)
                continue;

            effect.OnComplete?.Invoke();
            if (effect.GameObject != null)
                Destroy(effect.GameObject);
            spriteEffects.RemoveAt(i);
        }
    }

    private void StartShake(TetrisGameSession session, float duration, int strengthPixels)
    {
        if (session == null)
            return;

        foreach (ShakeEffect shake in shakeEffects)
        {
            if (shake.Target != session.transform)
                continue;

            shake.Duration = Mathf.Max(shake.Duration, duration);
            shake.StrengthPixels = Mathf.Max(shake.StrengthPixels, strengthPixels);
            shake.Age = 0f;
            return;
        }

        shakeEffects.Add(new ShakeEffect
        {
            Target = session.transform,
            RestPosition = session.transform.localPosition,
            Duration = duration,
            StrengthPixels = strengthPixels
        });
    }

    private void UpdateShakeEffects(float deltaTime)
    {
        for (int i = shakeEffects.Count - 1; i >= 0; i--)
        {
            ShakeEffect shake = shakeEffects[i];
            if (shake.Target == null)
            {
                shakeEffects.RemoveAt(i);
                continue;
            }

            shake.Age += deltaTime;
            if (shake.Age >= shake.Duration)
            {
                shake.Target.localPosition = shake.RestPosition;
                shakeEffects.RemoveAt(i);
                continue;
            }

            float remaining = 1f - shake.Age / shake.Duration;
            int strength = Mathf.Max(1, Mathf.CeilToInt(shake.StrengthPixels * remaining));
            float x = random.Next(-strength, strength + 1) / PixelsPerUnit;
            float y = random.Next(-strength, strength + 1) / PixelsPerUnit;
            shake.Target.localPosition = shake.RestPosition + new Vector3(x, y, 0f);
        }
    }

    private void RestoreShakenBoards()
    {
        foreach (ShakeEffect shake in shakeEffects)
        {
            if (shake.Target != null)
                shake.Target.localPosition = shake.RestPosition;
        }
    }

    /// <summary>
    /// One dot per queued garbage line, laid out in the empty space above the
    /// board (wrapping to a second row past 10) so the player can see an
    /// attack coming before it lands on their next lock.
    /// </summary>
    private SpriteRenderer[] CreateGarbageDots(TetrisGameSession session)
    {
        if (session == null)
            return null;

        Transform root = new GameObject("Garbage Warning").transform;
        root.SetParent(session.transform, false);

        int width = Mathf.Max(1, session.Model.Width);
        SpriteRenderer[] dots = new SpriteRenderer[MaxGarbageIndicators];
        for (int i = 0; i < MaxGarbageIndicators; i++)
        {
            GameObject dotObject = new GameObject($"Garbage Dot {i}");
            dotObject.transform.SetParent(root, false);

            int row = i / width;
            int column = i % width;
            dotObject.transform.localPosition = new Vector3(
                column + 0.5f,
                session.Model.VisibleHeight + GarbageIndicatorClearance + row * GarbageIndicatorRowGap,
                0f);
            dotObject.transform.localScale = Vector3.one * 0.4f;

            SpriteRenderer renderer = dotObject.AddComponent<SpriteRenderer>();
            renderer.sprite = effectSprite;
            if (effectMaterial != null)
                renderer.sharedMaterial = effectMaterial;
            renderer.sortingOrder = 60;
            renderer.enabled = false;
            dots[i] = renderer;
        }

        return dots;
    }

    private static void DestroyGarbageDots(ref SpriteRenderer[] dots)
    {
        if (dots == null)
            return;

        foreach (SpriteRenderer dot in dots)
        {
            if (dot != null)
                Destroy(dot.gameObject);
        }

        dots = null;
    }

    private void UpdateGarbageIndicators(TetrisGameSession session, SpriteRenderer[] dots)
    {
        if (session == null || dots == null)
            return;

        int pending = session.PendingGarbage;
        if (pending == 0)
        {
            foreach (SpriteRenderer dot in dots)
            {
                if (dot != null)
                    dot.enabled = false;
            }

            return;
        }

        float pulse = 0.55f + Mathf.Sin(Time.time * (5f + pending * 0.4f)) * 0.35f;
        Color warning = Color.Lerp(
            new Color(1f, 0.75f, 0.25f, 0.9f),
            new Color(1f, 0.2f, 0.25f, 0.95f),
            Mathf.Clamp01((pending - 4) / 12f));
        warning.a *= pulse;

        for (int i = 0; i < dots.Length; i++)
        {
            SpriteRenderer dot = dots[i];
            if (dot == null)
                continue;

            bool visible = i < pending;
            dot.enabled = visible;
            if (visible)
                dot.color = warning;
        }
    }

    private float RandomRange(float minimum, float maximum)
    {
        return Mathf.Lerp(minimum, maximum, (float)random.NextDouble());
    }

    private static Color Brighten(Color color, float amount)
    {
        return new Color(
            Mathf.Lerp(color.r, 1f, amount),
            Mathf.Lerp(color.g, 1f, amount),
            Mathf.Lerp(color.b, 1f, amount),
            color.a);
    }
}
