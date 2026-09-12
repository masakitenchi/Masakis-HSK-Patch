namespace Core_SK_Patch;

// The original complete sprite remains the UI/minified/placement fallback.
// Only spawned buildings use the layered renderer; no shared material is edited.
public sealed class Graphic_CompressedOcean : Graphic_Single
{
    public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
    {
        if (thing is Building_CompressedOcean ocean && ocean.Spawned)
            CompressedOceanRenderer.Draw(ocean, loc, rot, drawSize, extraRotation);
        else
            base.DrawWorker(loc, rot, thingDef, thing, extraRotation);
    }
}

internal static class CompressedOceanRenderer
{
    internal const string BasePath = "Things/Building/CoreSK/CompressedOceanBase";
    internal const string WaterPath = "Things/Building/CoreSK/CompressedOceanWater";
    internal const string FishPath = "Things/Building/CoreSK/CompressedOceanFish";
    private static Material platform, water, fish;
    private static Mesh[] waterFrames, fishFrames;
    private static MaterialPropertyBlock tint;

    private static void Initialize()
    {
        if (platform != null) return;
        // Called from drawing on the main thread, never a static loading worker.
        platform = MaterialPool.MatFrom(BasePath, ShaderDatabase.Cutout);
        water = MaterialPool.MatFrom(WaterPath, ShaderDatabase.Cutout);
        fish = MaterialPool.MatFrom(FishPath, ShaderDatabase.Transparent);
        tint = new MaterialPropertyBlock();
        waterFrames = new Mesh[OceanAnimation.Frames];
        fishFrames = new Mesh[OceanAnimation.Frames];
        for (int frame = 0; frame < OceanAnimation.Frames; frame++)
        {
            waterFrames[frame] = MakeGrid(frame, true);
            fishFrames[frame] = MakeGrid(frame, false);
        }
    }

    // Precompute and share a bounded set of meshes. Drawing multiple oceans does
    // not allocate meshes/materials or modify another ocean's animation state.
    private static Mesh MakeGrid(int frame, bool isWater)
    {
        int columns = isWater ? 16 : 10;
        int rows = isWater ? 16 : 1;
        var vertices = new Vector3[(columns + 1) * (rows + 1)];
        var uv = new Vector2[vertices.Length];
        var triangles = new int[columns * rows * 6];
        for (int row = 0; row <= rows; row++)
            for (int col = 0; col <= columns; col++)
            {
                int index = row * (columns + 1) + col;
                float u = (float)col / columns, v = (float)row / rows;
                vertices[index] = new Vector3(u - 0.5f, 0, v - 0.5f + (isWater ? 0 : OceanAnimation.TailBend(u, frame)));
                float wu = u, wv = v;
                if (isWater) OceanAnimation.WaterUV(u, v, frame, out wu, out wv);
                uv[index] = new Vector2(wu, wv);
            }
        int t = 0;
        for (int row = 0; row < rows; row++)
            for (int col = 0; col < columns; col++)
            {
                int a = row * (columns + 1) + col, b = a + columns + 1;
                triangles[t++] = a; triangles[t++] = b; triangles[t++] = a + 1;
                triangles[t++] = a + 1; triangles[t++] = b; triangles[t++] = b + 1;
            }
        var mesh = new Mesh { name = "CoreSK_Ocean_" + (isWater ? "Water_" : "Fish_") + frame,
            vertices = vertices, uv = uv, triangles = triangles };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    internal static void Draw(Building_CompressedOcean ocean, Vector3 loc, Rot4 rot, Vector2 size, float extraRotation)
    {
        Initialize();
        double time = OceanAnimation.Time(Find.TickManager?.TicksGame ?? 0, ocean.thingIDNumber);
        bool powered = ocean.Operational;
        float brightness = powered ? 1f : 0.55f;
        tint.SetColor("_Color", new Color(brightness, brightness, brightness, 1));
        DrawLayer(MeshPool.plane10, platform, loc, rot.AsAngle + extraRotation, size.x, size.y);

        Vector3 center = loc + Quaternion.Euler(0, rot.AsAngle + extraRotation, 0) * new Vector3(0, 0, 0.08f)
            + new Vector3(0, 0.015f, (float)Math.Sin(time * 0.8) * 0.025f);
        float diameter = 1.55f + (float)Math.Sin(time * 0.55) * 0.008f;
        DrawLayer(waterFrames[OceanAnimation.Frame(time, 5)], water, center, 0, diameter, diameter);

        int count = OceanAnimation.FishCount(ocean.Population, ocean.Properties.capacity);
        for (int i = 0; i < count; i++)
        {
            OceanAnimation.Swim(time, i, out float x, out float z, out float heading);
            // Keep every fish within the sphere including its tail and nose.
            float scale = 0.30f + (i % 3) * 0.025f;
            tint.SetColor("_Color", new Color(0.65f * brightness, 0.9f * brightness, brightness, 0.7f + z * 0.4f));
            DrawLayer(fishFrames[OceanAnimation.Frame(time + i, 22)], fish,
                center + new Vector3(x, 0.008f + i * 0.001f, z), heading, scale, scale);
        }
    }

    private static void DrawLayer(Mesh mesh, Material material, Vector3 position, float angle, float x, float z) =>
        Graphics.DrawMesh(mesh, Matrix4x4.TRS(position, Quaternion.Euler(0, angle, 0), new Vector3(x, 1, z)),
            material, 0, null, 0, tint);
}
