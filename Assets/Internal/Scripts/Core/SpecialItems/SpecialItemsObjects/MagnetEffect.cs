using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MagnetEffect", menuName = "Special Effects/Magnet")]
public class MagnetEffect : SpecialItemEffect
{
    public override void Execute(Board board, int column, int row)
    {
        if (board == null || board.Data == null)
            return;

        // Magnet alone: pick a random normal colour present on the board.
        string targetColor = FindRandomNormalColor(board);
        if (string.IsNullOrEmpty(targetColor))
            return;

        ClearColor(board, column, row, targetColor);
    }

    /// <summary>
    /// Called when the magnet is swapped with a coloured item.
    /// </summary>
    public void ExecuteWithColor(Board board, int column, int row, string colorId)
    {
        if (board == null || string.IsNullOrEmpty(colorId))
            return;

        ClearColor(board, column, row, colorId);
    }

    private void ClearColor(Board board, int originColumn, int originRow, string colorId)
    {
        var itemsToRemove = new HashSet<Item>();
        var cellsToRemove = new HashSet<SpecialCell>();

        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                if (x == originColumn && y == originRow)
                    continue;

                var item = board.Items[x, y];
                if (item == null)
                    continue;

                if (!string.IsNullOrEmpty(item.SpecialItemId))
                    continue;

                if (item.ItemId == colorId)
                    AddTarget(board, x, y, itemsToRemove, cellsToRemove);
            }
        }

        var self = board.Items[originColumn, originRow];
        if (self != null)
            itemsToRemove.Remove(self);

        Vector3 origin = board.GetWorldPosition(originColumn, originRow);
        if (self != null)
            origin = self.transform.position;

        // Draw pull lines to every affected item before they are destroyed.
        SpawnMagnetLines(board, origin, itemsToRemove);

        RemoveTargets(board, itemsToRemove, cellsToRemove);
    }

    private static void SpawnMagnetLines(Board board, Vector3 origin, HashSet<Item> targets)
    {
        if (targets == null || targets.Count == 0)
            return;

        var catalog = SoundManager.GetCatalog()
                      ?? Object.FindObjectOfType<MatchesHandler>()?.FxCatalog;

        Color color = catalog != null ? catalog.magnetLineColor : new Color(0.7f, 0.3f, 1f, 0.95f);
        float width = catalog != null ? catalog.magnetLineWidth : 0.08f;
        float duration = catalog != null ? catalog.magnetLineDuration : 0.35f;
        Material mat = catalog != null ? catalog.magnetLineMaterial : null;

        // Runner lives on a temporary GO so lines can animate after Execute returns.
        var runnerGo = new GameObject("MagnetLines");
        var runner = runnerGo.AddComponent<MagnetLineRunner>();
        runner.Play(origin, targets, color, width, duration, mat);
    }

    private static string FindRandomNormalColor(Board board)
    {
        var colors = new List<string>();
        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                var item = board.Items[x, y];
                if (item == null || !string.IsNullOrEmpty(item.SpecialItemId))
                    continue;
                if (!string.IsNullOrEmpty(item.ItemId) && !colors.Contains(item.ItemId))
                    colors.Add(item.ItemId);
            }
        }

        if (colors.Count == 0)
            return null;

        return colors[Random.Range(0, colors.Count)];
    }
}

/// <summary>
/// Temporary component that draws and fades LineRenderers from magnet origin to targets.
/// </summary>
public sealed class MagnetLineRunner : MonoBehaviour
{
    public void Play(
        Vector3 origin,
        HashSet<Item> targets,
        Color color,
        float width,
        float duration,
        Material sharedMaterial)
    {
        StartCoroutine(Run(origin, targets, color, width, duration, sharedMaterial));
    }

    private IEnumerator Run(
        Vector3 origin,
        HashSet<Item> targets,
        Color color,
        float width,
        float duration,
        Material sharedMaterial)
    {
        var lines = new List<LineRenderer>();
        Material mat = sharedMaterial;
        if (mat == null)
        {
            // Default unlit-ish line material (Sprites/Default works in most 2D setups).
            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply");
            mat = new Material(shader != null ? shader : Shader.Find("Hidden/InternalErrorShader"));
            mat.color = color;
        }

        foreach (var item in targets)
        {
            if (item == null)
                continue;

            var lrGo = new GameObject("MagnetBeam");
            lrGo.transform.SetParent(transform, false);
            var lr = lrGo.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.startWidth = width;
            lr.endWidth = width * 0.35f;
            lr.numCapVertices = 4;
            lr.material = mat;
            lr.startColor = color;
            lr.endColor = new Color(color.r, color.g, color.b, 0.15f);
            lr.sortingOrder = 50;
            lr.SetPosition(0, origin);
            lr.SetPosition(1, item.transform.position);
            lines.Add(lr);
        }

        float t = 0f;
        duration = Mathf.Max(0.05f, duration);
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            // Shrink toward origin + fade
            float w = Mathf.Lerp(width, 0f, k);
            Color c = color;
            c.a = Mathf.Lerp(color.a, 0f, k);

            for (int i = 0; i < lines.Count; i++)
            {
                var lr = lines[i];
                if (lr == null)
                    continue;
                lr.startWidth = w;
                lr.endWidth = w * 0.35f;
                lr.startColor = c;
                lr.endColor = new Color(c.r, c.g, c.b, c.a * 0.2f);

                // Keep end attached if item still alive this frame
                // (may already be destroyed — keep last position)
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
