using System;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>Creates the first interchangeable arena without changing existing scenes.</summary>
public static class TrainingArenaBuilder
{
    private const string ScenePath = "Assets/Project/Scenes/MainArena.unity";
    private const string PrefabPath = "Assets/Project/Prefabs/Arena/TrainingArena.prefab";

    [MenuItem("Tools/Arena de Treinamento/Criar arena principal")]
    public static void CreateArena()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null ||
            AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            Debug.LogWarning("A arena ou a cena principal ja existe. Nenhum asset foi substituido.");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SceneManager.SetActiveScene(scene);

        GameObject arena = new GameObject("TrainingArena");
        NavMeshSurface surface = arena.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.defaultArea = 0;
        surface.agentTypeID = 0;

        CreateCube("Floor", arena.transform, new Vector3(0f, -0.25f, 0f),
            new Vector3(20f, 0.5f, 20f));
        CreateCube("Wall North", arena.transform, new Vector3(0f, 1.5f, 10.25f),
            new Vector3(20.5f, 3f, 0.5f));
        CreateCube("Wall South", arena.transform, new Vector3(0f, 1.5f, -10.25f),
            new Vector3(20.5f, 3f, 0.5f));
        CreateCube("Wall West", arena.transform, new Vector3(-10.25f, 1.5f, 0f),
            new Vector3(0.5f, 3f, 20.5f));
        CreateCube("Wall East", arena.transform, new Vector3(10.25f, 1.5f, 0f),
            new Vector3(0.5f, 3f, 20.5f));
        CreateCube("Obstacle West", arena.transform, new Vector3(-3f, 1f, 0f),
            new Vector3(2f, 2f, 2f));
        CreateCube("Obstacle East", arena.transform, new Vector3(3f, 1f, 0f),
            new Vector3(2f, 2f, 2f));

        Transform playerStart = CreateMarker("PlayerStart", arena.transform,
            new Vector3(0f, 0f, -5f));
        SpawnPoint[] spawns = new SpawnPoint[4];
        Vector3[] positions =
        {
            new Vector3(-7f, 0f, -7f),
            new Vector3(7f, 0f, -7f),
            new Vector3(-7f, 0f, 7f),
            new Vector3(7f, 0f, 7f)
        };

        for (int i = 0; i < spawns.Length; i++)
        {
            Transform marker = CreateMarker($"EnemySpawn_{i + 1:00}", arena.transform,
                positions[i]);
            spawns[i] = marker.gameObject.AddComponent<SpawnPoint>();
            SerializedObject point = new SerializedObject(spawns[i]);
            point.FindProperty("pointId").stringValue = $"enemy_{i + 1:00}";
            point.ApplyModifiedPropertiesWithoutUndo();
        }

        ArenaConfiguration configuration = arena.AddComponent<ArenaConfiguration>();
        SerializedObject arenaFields = new SerializedObject(configuration);
        arenaFields.FindProperty("playerStart").objectReferenceValue = playerStart;
        arenaFields.FindProperty("navigationSurface").objectReferenceValue = surface;
        SerializedProperty spawnArray = arenaFields.FindProperty("enemySpawnPoints");
        spawnArray.arraySize = spawns.Length;
        for (int i = 0; i < spawns.Length; i++)
        {
            spawnArray.GetArrayElementAtIndex(i).objectReferenceValue = spawns[i];
        }
        arenaFields.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAssetAndConnect(arena, PrefabPath,
            InteractionMode.AutomatedAction);

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.transform.position = new Vector3(0f, 6f, -8f);
        camera.transform.rotation = Quaternion.LookRotation(new Vector3(0f, -6f, 8f));
        cameraObject.AddComponent<AudioListener>();

        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            throw new InvalidOperationException("Nao foi possivel salvar MainArena.unity.");
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Arena criada: {PrefabPath} e {ScenePath}. Faça Bake no NavMeshSurface.");
    }

    private static GameObject CreateCube(string name, Transform parent, Vector3 position,
        Vector3 scale)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = position;
        cube.transform.localScale = scale;
        return cube;
    }

    private static Transform CreateMarker(string name, Transform parent, Vector3 position)
    {
        GameObject marker = new GameObject(name);
        marker.transform.SetParent(parent, false);
        marker.transform.localPosition = position;
        return marker.transform;
    }
}
