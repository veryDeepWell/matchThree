#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools → FX → Create Basic VFX Prefabs
/// Generates simple ParticleSystem prefabs and optionally assigns them to GameFxCatalog.
/// </summary>
public static class BasicVfxPrefabGenerator
{
    private const string Folder = "Assets/FX/GeneratedVfx";

    private struct VfxSpec
    {
        public string Name;
        public Color StartColor;
        public Color EndColor;
        public int MaxParticles;
        public float Duration;
        public float Lifetime;
        public float Speed;
        public float Size;
        public float Gravity;
        public float Spread; // shape radius / angle feel
        public bool Upward;
        public ParticleSystemShapeType Shape;
    }

    [MenuItem("Tools/FX/Create Basic VFX Prefabs")]
    public static void CreateAll()
    {
        EnsureFolder(Folder);

        var matchDestroy = CreatePrefab(new VfxSpec
        {
            Name = "Vfx_MatchDestroy",
            StartColor = new Color(1f, 0.95f, 0.55f, 1f),
            EndColor = new Color(1f, 0.4f, 0.1f, 0f),
            MaxParticles = 18,
            Duration = 0.25f,
            Lifetime = 0.4f,
            Speed = 2.2f,
            Size = 0.18f,
            Gravity = 0.8f,
            Spread = 0.15f,
            Upward = false,
            Shape = ParticleSystemShapeType.Sphere
        });

        var specialSpawn = CreatePrefab(new VfxSpec
        {
            Name = "Vfx_SpecialSpawn",
            StartColor = new Color(0.55f, 0.85f, 1f, 1f),
            EndColor = new Color(0.3f, 0.5f, 1f, 0f),
            MaxParticles = 24,
            Duration = 0.35f,
            Lifetime = 0.55f,
            Speed = 1.6f,
            Size = 0.22f,
            Gravity = -0.4f,
            Spread = 0.25f,
            Upward = true,
            Shape = ParticleSystemShapeType.Circle
        });

        var specialActivate = CreatePrefab(new VfxSpec
        {
            Name = "Vfx_SpecialActivate",
            StartColor = new Color(1f, 0.45f, 0.95f, 1f),
            EndColor = new Color(0.7f, 0.1f, 1f, 0f),
            MaxParticles = 28,
            Duration = 0.3f,
            Lifetime = 0.5f,
            Speed = 3.0f,
            Size = 0.2f,
            Gravity = 0.2f,
            Spread = 0.35f,
            Upward = false,
            Shape = ParticleSystemShapeType.Sphere
        });

        var cellBreak = CreatePrefab(new VfxSpec
        {
            Name = "Vfx_CellBreak",
            StartColor = new Color(0.75f, 0.9f, 1f, 1f),
            EndColor = new Color(0.5f, 0.7f, 0.9f, 0f),
            MaxParticles = 14,
            Duration = 0.2f,
            Lifetime = 0.45f,
            Speed = 1.4f,
            Size = 0.16f,
            Gravity = 1.2f,
            Spread = 0.12f,
            Upward = false,
            Shape = ParticleSystemShapeType.Box
        });

        var levelWin = CreatePrefab(new VfxSpec
        {
            Name = "Vfx_LevelWin",
            StartColor = new Color(1f, 0.9f, 0.2f, 1f),
            EndColor = new Color(1f, 0.5f, 0.1f, 0f),
            MaxParticles = 40,
            Duration = 0.6f,
            Lifetime = 0.9f,
            Speed = 2.5f,
            Size = 0.25f,
            Gravity = 0.5f,
            Spread = 0.5f,
            Upward = true,
            Shape = ParticleSystemShapeType.Cone
        });

        var levelLose = CreatePrefab(new VfxSpec
        {
            Name = "Vfx_LevelLose",
            StartColor = new Color(0.55f, 0.55f, 0.6f, 1f),
            EndColor = new Color(0.2f, 0.2f, 0.25f, 0f),
            MaxParticles = 20,
            Duration = 0.4f,
            Lifetime = 0.7f,
            Speed = 1.2f,
            Size = 0.2f,
            Gravity = 1.5f,
            Spread = 0.3f,
            Upward = false,
            Shape = ParticleSystemShapeType.Sphere
        });

        // Optional soft ring for magnet (not a catalog slot, but useful)
        CreatePrefab(new VfxSpec
        {
            Name = "Vfx_MagnetPulse",
            StartColor = new Color(0.7f, 0.35f, 1f, 0.9f),
            EndColor = new Color(0.4f, 0.1f, 0.8f, 0f),
            MaxParticles = 16,
            Duration = 0.25f,
            Lifetime = 0.4f,
            Speed = 0.8f,
            Size = 0.28f,
            Gravity = 0f,
            Spread = 0.4f,
            Upward = false,
            Shape = ParticleSystemShapeType.Circle
        });

        int assigned = TryAssignToCatalog(matchDestroy, specialSpawn, specialActivate, cellBreak, levelWin, levelLose);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Basic VFX Prefabs",
            $"Created prefabs in {Folder}\n\n" +
            $"• Vfx_MatchDestroy\n• Vfx_SpecialSpawn\n• Vfx_SpecialActivate\n" +
            $"• Vfx_CellBreak\n• Vfx_LevelWin\n• Vfx_LevelLose\n• Vfx_MagnetPulse\n\n" +
            (assigned > 0
                ? $"Assigned {assigned} slots on GameFxCatalog."
                : "No GameFxCatalog found — assign prefabs in FX Studio manually."),
            "OK");

        var folderObj = AssetDatabase.LoadAssetAtPath<Object>(Folder);
        if (folderObj != null)
        {
            EditorGUIUtility.PingObject(folderObj);
            Selection.activeObject = folderObj;
        }
    }

    private static GameObject CreatePrefab(VfxSpec spec)
    {
        string path = $"{Folder}/{spec.Name}.prefab";

        var go = new GameObject(spec.Name);
        var ps = go.AddComponent<ParticleSystem>();
        var renderer = go.GetComponent<ParticleSystemRenderer>();

        // Main
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = spec.Duration;
        main.startLifetime = spec.Lifetime;
        main.startSpeed = spec.Speed;
        main.startSize = spec.Size;
        main.startColor = spec.StartColor;
        main.gravityModifier = spec.Gravity;
        main.maxParticles = spec.MaxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;

        // Emission — one burst
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, (short)spec.MaxParticles)
        });

        // Shape
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = spec.Shape;
        shape.radius = Mathf.Max(0.05f, spec.Spread);
        if (spec.Shape == ParticleSystemShapeType.Cone)
        {
            shape.angle = 25f;
            shape.radius = 0.1f;
        }
        if (spec.Shape == ParticleSystemShapeType.Box)
            shape.scale = new Vector3(0.3f, 0.3f, 0.1f);
        if (spec.Shape == ParticleSystemShapeType.Circle)
            shape.radius = spec.Spread;

        // Color over lifetime → fade out
        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[]
            {
                new GradientColorKey(spec.StartColor, 0f),
                new GradientColorKey(Color.Lerp(spec.StartColor, spec.EndColor, 0.5f), 0.45f),
                new GradientColorKey(spec.EndColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(spec.StartColor.a, 0f),
                new GradientAlphaKey(spec.StartColor.a * 0.7f, 0.4f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLife.color = new ParticleSystem.MinMaxGradient(grad);

        // Size over lifetime → shrink
        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.15f));

        // Velocity feel
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = spec.Upward;
        if (spec.Upward)
        {
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.y = new ParticleSystem.MinMaxCurve(1.2f);
        }

        // Renderer — default particle material
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 40;
            var mat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
            if (mat == null)
            {
                // Fallback for newer Unity versions
                var shader = Shader.Find("Particles/Standard Unlit");
                if (shader == null)
                    shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply");
                if (shader == null)
                    shader = Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    mat = new Material(shader);
                    mat.name = "GeneratedParticleMat";
                    string matPath = $"{Folder}/M_GeneratedParticle.mat";
                    if (!File.Exists(matPath))
                        AssetDatabase.CreateAsset(mat, matPath);
                    else
                        mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                }
            }
            if (mat != null)
                renderer.sharedMaterial = mat;
        }

        // Ensure it stops and gets cleaned: stopAction Destroy + FxPlayer also destroys
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);

        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static int TryAssignToCatalog(
        GameObject matchDestroy,
        GameObject specialSpawn,
        GameObject specialActivate,
        GameObject cellBreak,
        GameObject levelWin,
        GameObject levelLose)
    {
        string[] guids = AssetDatabase.FindAssets("t:GameFxCatalog");
        if (guids == null || guids.Length == 0)
            return 0;

        int total = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var catalog = AssetDatabase.LoadAssetAtPath<GameFxCatalog>(path);
            if (catalog == null)
                continue;

            Undo.RecordObject(catalog, "Assign Basic VFX Prefabs");

            int n = 0;
            if (catalog.matchDestroyVfx == null) { catalog.matchDestroyVfx = matchDestroy; n++; }
            if (catalog.specialSpawnVfx == null) { catalog.specialSpawnVfx = specialSpawn; n++; }
            if (catalog.specialActivateVfx == null) { catalog.specialActivateVfx = specialActivate; n++; }
            if (catalog.cellBreakVfx == null) { catalog.cellBreakVfx = cellBreak; n++; }
            if (catalog.levelWinVfx == null) { catalog.levelWinVfx = levelWin; n++; }
            if (catalog.levelLoseVfx == null) { catalog.levelLoseVfx = levelLose; n++; }

            if (n > 0)
            {
                EditorUtility.SetDirty(catalog);
                total += n;
            }
        }

        return total;
    }

    private static void EnsureFolder(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath))
            return;

        string[] parts = assetPath.Split('/');
        string current = parts[0]; // Assets
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
