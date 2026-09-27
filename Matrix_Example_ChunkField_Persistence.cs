using System;
using System.IO;
using MysticEyeStudios.Matrix.FieldPopulators;
using MysticEyeStudios.Matrix.Fields;
using MysticEyeStudios.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

public class Matrix_Example_ChunkField_Persistence : MonoBehaviour
{
    /*
     * WORLD OBJECT:
     *   MeshFilter
     *   MeshRenderer
     *   MeshCollider
     *   Matrix_Example_ChunkField_Persistence
     *
     * PLAYER OBJECT:
     *   CharacterController
     *   Matrix_Example_ChunkTracking_PlayerMovement
     *   Camera
     *
     * Assign Player and PlayerCamera here.
     *
     * The field is sampled around Player.position, but ApplyMesh targets
     * THIS transform so the generated world never follows the player.
     */
    // ============================================================
    // WORLD
    // ============================================================

    [Header("World")]
    public int Seed = 12345;

    [Range(8.0f, 128.0f)]
    public float NoiseScale = 36.0f;

    [Range(2.0f, 32.0f)]
    public float TerrainHeight = 12.0f;

    [Range(-20.0f, 20.0f)]
    public float GroundHeight = 0.0f;

    [Range(0.01f, 0.25f)]
    public float CaveScale = 0.065f;

    [Range(0.45f, 0.85f)]
    public float CaveThreshold = 0.67f;

    // ============================================================
    // PERSISTENCE
    // ============================================================

    [Header("Persistence")]
    [Range(4, 64)]
    public int PersistenceSegmentSize = 64;

    public string SaveFolder = "Matrix_Example_ChunkField_Persistence";

    // ============================================================
    // RENDERING
    // ============================================================

    [Header("Rendering")]
    [Range(4, 64)]
    public int RenderChunkSize = 16;

    [Range(1, 4)]
    public int RenderChunkRadius = 1;

    [Range(0.25f, 4.0f)]
    public float GridSize = 1.0f;

    public Material RenderMaterial;

    public bool Cubic = true;
    public bool MarchingCubes = false;

    [Header("Auto Save")]
    public bool AutoSave = true;

    [Range(0.25f, 30.0f)]
    public float AutoSaveInterval = 2.0f;

    // ============================================================
    // EDITING
    // ============================================================

    [Header("Voxel Editing")]
    [Range(1.0f, 20.0f)]
    public float EditDistance = 8.0f;

    public Color SelectedColor = Color.red;

    // ============================================================
    // MATRIX / THREADING
    // ============================================================

    private ModificationField<CubicObject?> modificationField;
    private PersistentField<CubicObject?> persistentField;
    private TaskMaster taskMaster;
    private SegmentedFieldInterpreter<CubicObject?> segmentedInterpreter;
    private float nextAutoSaveTime;
    private int lastQueuedRevision = -1;

    // ============================================================
    // DEBUG
    // ============================================================

    private string status = "Ready.";
    private Vector3Int lastEditAddress;
    private bool hasEditAddress;

    public Transform Player;
    public Camera PlayerCamera;

    // ============================================================
    // UNITY
    // ============================================================

    private void Start()
    {
        if (Player == null && PlayerCamera != null)
            Player = PlayerCamera.transform.root;

        if (PlayerCamera == null)
            PlayerCamera = Camera.main;

        if (Player == null && PlayerCamera != null)
            Player = PlayerCamera.transform.root;

        if (Player == null)
        {
            Debug.LogError(
                "Matrix_Example_ChunkField_Persistence requires a Player transform.");
            enabled = false;
            return;
        }

        var generator =
            new FormulaPopulator<Vector3, CubicObject?>(
                GenerateWorld);

        modificationField =
            new ModificationField<CubicObject?>(
                generator,
                CubicObjectsEqual);

        persistentField =
            new PersistentField<CubicObject?>(
                modificationField,
                Vector3.one * PersistenceSegmentSize,
                Path.Combine(
                    Application.persistentDataPath,
                    SaveFolder),
                ReadCubicObject,
                WriteCubicObject);

        taskMaster = new TaskMaster();

        Vector3 renderSegmentSize =
            Vector3.one * RenderChunkSize * GridSize;

        segmentedInterpreter =
            new SegmentedFieldInterpreter<CubicObject?>(
                new AsyncFieldInterpreter(
                    persistentField,
                    taskMaster),
                renderSegmentSize);

        segmentedInterpreter.RenderAroundPoint(
            Player.position,
            RenderChunkRadius,
            GridSize,
            transform,
            RenderMaterial);

        nextAutoSaveTime =
            Time.unscaledTime +
            AutoSaveInterval;
    }

    private void Update()
    {
        HandleEditing();
        HandleColorSelection();

        if (Player != null)
        {
            segmentedInterpreter.RenderAroundPoint(
                Player.position,
                RenderChunkRadius,
                GridSize,
                transform,
                RenderMaterial);
        }

        if (modificationField != null &&
            modificationField.Revision != lastQueuedRevision)
        {
            segmentedInterpreter.Refresh();

            lastQueuedRevision =
                modificationField.Revision;
        }

        segmentedInterpreter?.RenderNext(GridSize);

        if (AutoSave &&
            persistentField != null &&
            Time.unscaledTime >= nextAutoSaveTime)
        {
            persistentField.Save();

            nextAutoSaveTime =
                Time.unscaledTime +
                AutoSaveInterval;
        }

        taskMaster?.M_MainThreadTick();
    }

    private void OnDisable()
    {
        // Auto-save handles normal play. This is the final synchronous
        // flush when the example is disabled/stopped in the editor.
        persistentField?.Save();
    }

    private void OnApplicationQuit()
    {
        // Flush modifications synchronously before shutting down workers.
        persistentField?.Save();
        taskMaster?.Dispose();
    }

    // ============================================================
    // PROCEDURAL FIELD
    // ============================================================

    private CubicObject? GenerateWorld(Vector3 position)
    {
        float seedX = Seed * 0.137f;
        float seedY = Seed * 0.193f;
        float seedZ = Seed * 0.271f;

        float large =
            Mathf.PerlinNoise(
                (position.x + seedX) / NoiseScale,
                (position.z + seedZ) / NoiseScale);

        float detail =
            Mathf.PerlinNoise(
                (position.x - seedZ) / (NoiseScale * 0.35f),
                (position.z + seedX) / (NoiseScale * 0.35f));

        float terrainSurface =
            GroundHeight +
            ((large - 0.5f) * TerrainHeight) +
            ((detail - 0.5f) * TerrainHeight * 0.25f);

        if (position.y > terrainSurface)
            return null;

        float depth = terrainSurface - position.y;

        // Cheap deterministic 3D-ish noise assembled from three 2D
        // Perlin projections. Good enough for this example and, more
        // importantly, still a pure formula field.
        if (depth > 2.0f)
        {
            float xy =
                Mathf.PerlinNoise(
                    (position.x + seedX) * CaveScale,
                    (position.y + seedY) * CaveScale);

            float yz =
                Mathf.PerlinNoise(
                    (position.y - seedY) * CaveScale,
                    (position.z + seedZ) * CaveScale);

            float xz =
                Mathf.PerlinNoise(
                    (position.x - seedZ) * CaveScale,
                    (position.z - seedX) * CaveScale);

            float cave = (xy + yz + xz) / 3.0f;

            if (cave > CaveThreshold)
                return null;
        }

        Color color;

        if (depth < 1.25f)
            color = new Color(0.20f, 0.55f, 0.18f);
        else if (depth < 3.5f)
            color = new Color(0.35f, 0.22f, 0.12f);
        else
            color = new Color(0.42f, 0.42f, 0.42f);

        return new CubicObject()
        {
            VoxelColor = color,
            RenderType = GetCurrentRenderType()
        };
    }

    // ============================================================
    // EDITING
    // ============================================================

    private void HandleEditing()
    {
        if (PlayerCamera == null)
            return;

        Mouse mouse = Mouse.current;

        if (mouse == null)
            return;

        if (!Physics.Raycast(
                PlayerCamera.transform.position,
                PlayerCamera.transform.forward,
                out RaycastHit hit,
                EditDistance))
        {
            hasEditAddress = false;
            return;
        }

        if (hit.collider == null)
            return;

        Transform hitTransform =
            hit.collider.transform;

        if (hitTransform != transform &&
            !hitTransform.IsChildOf(transform))
        {
            return;
        }

        Vector3 removePoint =
            hit.point -
            hit.normal *
            GridSize *
            0.35f;

        Vector3 addPoint =
            hit.point +
            hit.normal *
            GridSize *
            0.35f;

        Vector3Int removeAddress =
            ToAddress(removePoint);

        Vector3Int addAddress =
            ToAddress(addPoint);

        lastEditAddress = removeAddress;
        hasEditAddress = true;

        // LMB removes.
        if (mouse.leftButton.wasPressedThisFrame)
        {
            persistentField.Set(
                removeAddress,
                null);

            status =
                $"Removed {removeAddress}";

        }

        // RMB adds / replaces with the selected color.
        if (mouse.rightButton.wasPressedThisFrame)
        {
            persistentField.Set(
                addAddress,
                new CubicObject()
                {
                    VoxelColor = SelectedColor,
                    RenderType = GetCurrentRenderType()
                });

            status =
                $"Placed {addAddress}";

        }
    }

    private void HandleColorSelection()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        if (keyboard.digit1Key.wasPressedThisFrame)
            SelectedColor = Color.red;

        if (keyboard.digit2Key.wasPressedThisFrame)
            SelectedColor = Color.green;

        if (keyboard.digit3Key.wasPressedThisFrame)
            SelectedColor = Color.blue;

        if (keyboard.digit4Key.wasPressedThisFrame)
            SelectedColor = Color.yellow;

        if (keyboard.digit5Key.wasPressedThisFrame)
            SelectedColor = Color.white;

        if (keyboard.digit6Key.wasPressedThisFrame)
            SelectedColor =
                new Color(
                    0.65f,
                    0.25f,
                    0.80f);
    }

    private Vector3Int ToAddress(
        Vector3 position)
    {
        return new Vector3Int(
            Mathf.RoundToInt(position.x),
            Mathf.RoundToInt(position.y),
            Mathf.RoundToInt(position.z));
    }

    // ============================================================
    // RENDERING
    // ============================================================

    private RenderType GetCurrentRenderType()
    {
        RenderType renderType =
            RenderType.None;

        if (MarchingCubes)
            renderType |=
                RenderType.MarchingCubes;

        if (Cubic)
            renderType |=
                RenderType.Cubic;

        return renderType;
    }

    // ============================================================
    // DEBUG GUI
    // ============================================================

    private void OnGUI()
    {
        GUILayout.BeginArea(
            new Rect(
                10,
                10,
                430,
                330),
            GUI.skin.box);

        GUILayout.Label(
            "MATRIX CHUNK FIELD PERSISTENCE");

        GUILayout.Label(
            $"Stored modifications: {modificationField?.ModificationCount ?? 0}");

        GUILayout.Label(
            $"Revision: {modificationField?.Revision ?? 0}");

        GUILayout.Label(
            $"Persistence segment: {PersistenceSegmentSize}");

        GUILayout.Label(
            $"Render segments: {segmentedInterpreter?.SegmentCount ?? 0}");

        GUILayout.Label(
            $"Render segment: {RenderChunkSize} cells | radius {RenderChunkRadius}");

        GUILayout.Label(
            $"Auto save: {(AutoSave ? $"{AutoSaveInterval:0.##}s" : "OFF")}");

        GUILayout.Label(
            $"Player: {(Player != null ? Player.position.ToString() : "NONE")}");

        if (hasEditAddress)
        {
            GUILayout.Label(
                $"Target: {lastEditAddress}");

        }

        GUILayout.Space(8);

        GUILayout.Label(
            "LMB remove | RMB place");

        GUILayout.Label(
            "1 Red | 2 Green | 3 Blue | 4 Yellow | 5 White | 6 Purple");

        GUILayout.Label(
            $"Selected: {SelectedColor}");

        GUILayout.Space(8);

        if (GUILayout.Button(
                "SAVE ALL MODIFICATIONS"))
        {
            persistentField.Save();
            status = "Saved all modifications.";
        }

        GUILayout.Space(8);
        GUILayout.Label(status);

        GUILayout.EndArea();
    }

    // ============================================================
    // PERSISTENCE VALUE SERIALIZATION
    // ============================================================

    private static void WriteCubicObject(
        BinaryWriter writer,
        CubicObject? value)
    {
        bool hasValue =
            value.HasValue;

        writer.Write(
            hasValue);

        if (!hasValue)
            return;

        CubicObject voxel =
            value.Value;

        writer.Write(
            voxel.VoxelColor.r);

        writer.Write(
            voxel.VoxelColor.g);

        writer.Write(
            voxel.VoxelColor.b);

        writer.Write(
            voxel.VoxelColor.a);

        writer.Write(
            (int)voxel.RenderType);
    }

    private static CubicObject? ReadCubicObject(
        BinaryReader reader)
    {
        bool hasValue =
            reader.ReadBoolean();

        if (!hasValue)
            return null;

        Color color =
            new Color(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle());

        RenderType renderType =
            (RenderType)
            reader.ReadInt32();

        return new CubicObject()
        {
            VoxelColor = color,
            RenderType = renderType
        };
    }

    private static bool CubicObjectsEqual(
        CubicObject? a,
        CubicObject? b)
    {
        if (!a.HasValue &&
            !b.HasValue)
        {
            return true;
        }

        if (a.HasValue !=
            b.HasValue)
        {
            return false;
        }

        CubicObject av =
            a.Value;

        CubicObject bv =
            b.Value;

        return
            Mathf.Abs(
                av.VoxelColor.r -
                bv.VoxelColor.r) <
            0.0001f &&

            Mathf.Abs(
                av.VoxelColor.g -
                bv.VoxelColor.g) <
            0.0001f &&

            Mathf.Abs(
                av.VoxelColor.b -
                bv.VoxelColor.b) <
            0.0001f &&

            Mathf.Abs(
                av.VoxelColor.a -
                bv.VoxelColor.a) <
            0.0001f &&

            av.RenderType ==
            bv.RenderType;
    }
}