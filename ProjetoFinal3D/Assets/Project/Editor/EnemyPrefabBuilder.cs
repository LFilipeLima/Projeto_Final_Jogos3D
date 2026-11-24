using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>Editor setup for the single enemy and its isolated play test.</summary>
public static class EnemyPrefabBuilder
{
    private const string EnemyPath = "Assets/Project/Prefabs/Enemies/Enemy.prefab";
    private const string PlayerPath = "Assets/Project/Prefabs/Player/Player.prefab";
    private const string MainScenePath = "Assets/Project/Scenes/MainArena.unity";
    private const string TestScenePath = "Assets/Project/Scenes/Tests/EnemyArenaTest.unity";

    [MenuItem("Tools/Arena de Treinamento/Criar prefab Enemy")]
    public static void CreateEnemyPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPath) != null)
        {
            Debug.LogWarning("Enemy.prefab ja existe. Nenhum asset foi substituido.");
            return;
        }

        int enemyLayer = EnsureEnemyLayer();
        GameObject enemy = new GameObject("Enemy");
        enemy.layer = enemyLayer;

        NavMeshAgent agent = enemy.AddComponent<NavMeshAgent>();
        agent.radius = 0.45f;
        agent.height = 2f;
        agent.speed = 1.8f;
        agent.acceleration = 8f;
        agent.angularSpeed = 720f;
        agent.stoppingDistance = 1.2f;

        CapsuleCollider hitbox = enemy.AddComponent<CapsuleCollider>();
        hitbox.radius = 0.5f;
        hitbox.height = 2f;
        hitbox.center = new Vector3(0f, 1f, 0f);
        hitbox.isTrigger = false;

        enemy.AddComponent<EnemyHealth>();
        enemy.AddComponent<EnemyController>();

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "Visual";
        visual.transform.SetParent(enemy.transform, false);
        visual.transform.localPosition = new Vector3(0f, 1f, 0f);
        visual.layer = enemyLayer;
        visual.AddComponent<EnemyHitFeedback>();
        UnityEngine.Object.DestroyImmediate(visual.GetComponent<CapsuleCollider>());
        visual.AddComponent<Animator>();

        PrefabUtility.SaveAsPrefabAsset(enemy, EnemyPath);
        UnityEngine.Object.DestroyImmediate(enemy);
        AssetDatabase.SaveAssets();
        Debug.Log($"Inimigo criado: {EnemyPath}. O filho Visual pode ser substituido.");
    }

    [MenuItem("Tools/Arena de Treinamento/Integrar Player na cena principal")]
    public static void IntegratePlayer()
    {
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath);
        if (playerPrefab == null || AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath) == null)
        {
            Debug.LogError("Crie MainArena e confirme Player.prefab antes da integracao.");
            return;
        }

        int enemyLayer = EnsureEnemyLayer();
        EnsurePlayerVisual();
        playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath);

        Scene scene = LoadScene(MainScenePath);
        ArenaConfiguration arena = FindInScene<ArenaConfiguration>(scene);
        Camera camera = FindInScene<Camera>(scene);
        if (arena == null || arena.PlayerStart == null || camera == null)
        {
            Debug.LogError("MainArena precisa de ArenaConfiguration, PlayerStart e Camera.");
            return;
        }

        PlayerMovement movement = FindInScene<PlayerMovement>(scene);
        GameObject player;
        if (movement == null)
        {
            player = PrefabUtility.InstantiatePrefab(playerPrefab, scene) as GameObject;
            if (player == null)
            {
                throw new InvalidOperationException("Nao foi possivel instanciar Player.prefab.");
            }
            movement = player.GetComponent<PlayerMovement>();
        }
        else
        {
            player = movement.gameObject;
        }

        player.transform.SetPositionAndRotation(arena.PlayerStart.position,
            arena.PlayerStart.rotation);

        PlayerCamera follow = camera.GetComponent<PlayerCamera>();
        if (follow == null)
        {
            follow = camera.gameObject.AddComponent<PlayerCamera>();
        }

        SetObjectReference(movement, "cameraTransform", camera.transform);
        SetObjectReference(follow, "target", player.transform);

        PlayerShooter shooter = player.GetComponent<PlayerShooter>();
        SerializedObject shooterFields = new SerializedObject(shooter);
        SerializedProperty hitMask = shooterFields.FindProperty("hitMask");
        hitMask.intValue |= 1 << enemyLayer;
        shooterFields.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(movement);
        PrefabUtility.RecordPrefabInstancePropertyModifications(shooter);

        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Player integrado em MainArena; camera e Hit Mask configurados.");
    }

    [MenuItem("Tools/Arena de Treinamento/Criar cena de teste Enemy")]
    public static void CreateEnemyTestScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TestScenePath) != null)
        {
            Debug.LogWarning("EnemyArenaTest.unity ja existe. Nenhum asset foi substituido.");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath) == null)
        {
            Debug.LogError("Crie MainArena antes da cena de teste.");
            return;
        }

        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPath);
        Scene source = LoadScene(MainScenePath);
        ArenaConfiguration sourceArena = FindInScene<ArenaConfiguration>(source);
        if (enemyPrefab == null || FindInScene<PlayerHealth>(source) == null ||
            sourceArena == null || sourceArena.NavigationSurface == null ||
            sourceArena.NavigationSurface.navMeshData == null)
        {
            Debug.LogError("Crie Enemy.prefab, integre Player e faca Bake da NavMesh antes da cena de teste.");
            return;
        }

        foreach (SpawnPoint point in sourceArena.EnemySpawnPoints)
        {
            if (point == null || !point.TryGetNavMeshPosition(0.75f, out _))
            {
                Debug.LogError("Corrija todos os spawns sobre a NavMesh antes da cena de teste.");
                return;
            }
        }

        EditorSceneManager.SaveScene(source);
        if (!AssetDatabase.CopyAsset(MainScenePath, TestScenePath))
        {
            throw new InvalidOperationException("Nao foi possivel copiar MainArena para Tests.");
        }

        AssetDatabase.Refresh();
        Scene testScene = LoadScene(TestScenePath);
        ArenaConfiguration arena = FindInScene<ArenaConfiguration>(testScene);
        PlayerHealth player = FindInScene<PlayerHealth>(testScene);
        if (arena == null || arena.EnemySpawnPoints.Count == 0 || player == null)
        {
            throw new InvalidOperationException("Cena de teste sem arena, spawn ou Player.");
        }

        SpawnPoint spawn = arena.EnemySpawnPoints[0];
        if (!spawn.TryGetNavMeshPosition(0.75f, out Vector3 spawnPosition))
        {
            throw new InvalidOperationException("O primeiro spawn nao esta na NavMesh.");
        }
        GameObject enemy = PrefabUtility.InstantiatePrefab(enemyPrefab, testScene) as GameObject;
        if (enemy == null)
        {
            throw new InvalidOperationException("Nao foi possivel instanciar Enemy.prefab.");
        }
        enemy.transform.SetPositionAndRotation(spawnPosition, spawn.Rotation);

        GameObject bootstrap = new GameObject("Enemy Test Bootstrap");
        SceneManager.MoveGameObjectToScene(bootstrap, testScene);
        EnemyArenaTestBootstrap test = bootstrap.AddComponent<EnemyArenaTestBootstrap>();
        SetObjectReference(test, "player", player.transform);
        SetObjectReference(test, "enemy", enemy.GetComponent<EnemyController>());

        EditorSceneManager.SaveScene(testScene);
        Debug.Log($"Cena de teste criada: {TestScenePath}.");
    }

    private static void EnsurePlayerVisual()
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(PlayerPath);
        try
        {
            if (contents.transform.Find("Visual") != null)
            {
                return;
            }

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.layer = contents.layer;
            visual.transform.SetParent(contents.transform, false);
            visual.transform.localPosition = new Vector3(0f, 1f, 0f);
            UnityEngine.Object.DestroyImmediate(visual.GetComponent<CapsuleCollider>());
            visual.AddComponent<Animator>();
            PrefabUtility.SaveAsPrefabAsset(contents, PlayerPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    private static int EnsureEnemyLayer()
    {
        int layer = LayerMask.NameToLayer("Enemy");
        if (layer >= 0)
        {
            return layer;
        }

        UnityEngine.Object tagManager = AssetDatabase.LoadAllAssetsAtPath(
            "ProjectSettings/TagManager.asset")[0];
        SerializedObject settings = new SerializedObject(tagManager);
        SerializedProperty layers = settings.FindProperty("layers");
        for (int index = 8; index < 32; index++)
        {
            SerializedProperty slot = layers.GetArrayElementAtIndex(index);
            if (string.IsNullOrEmpty(slot.stringValue))
            {
                slot.stringValue = "Enemy";
                settings.ApplyModifiedPropertiesWithoutUndo();
                return index;
            }
        }

        throw new InvalidOperationException("Nenhuma camada livre para Enemy.");
    }

    private static Scene LoadScene(string path)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            throw new OperationCanceledException("A abertura da cena foi cancelada.");
        }

        return EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
            {
                return component;
            }
        }
        return null;
    }

    private static void SetObjectReference(UnityEngine.Object target, string field,
        UnityEngine.Object value)
    {
        SerializedObject fields = new SerializedObject(target);
        fields.FindProperty(field).objectReferenceValue = value;
        fields.ApplyModifiedPropertiesWithoutUndo();
    }
}
