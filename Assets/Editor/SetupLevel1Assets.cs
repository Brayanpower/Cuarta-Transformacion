using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class SetupLevel1Assets
{
    static SetupLevel1Assets()
    {
        EditorApplication.delayCall += ExecuteSetup;
    }

    [MenuItem("Tools/Fix Level 1 Sprites and Platforms")]
    public static void ExecuteSetup()
    {
        string logPath = Path.Combine(Application.dataPath, "../setup_log.txt");
        List<string> logs = new List<string>();
        logs.Add($"[{DateTime.Now}] Iniciando SetupLevel1Assets...");

        try
        {
            // 1. Configurar TextureImporter de esbirro.png
            SetupEsbirroImporter(logs);

            // 2. Configurar TextureImporter de vendedor_garbanos.png
            SetupBossImporter(logs);

            // 3. Actualizar la escena Nivel1.unity
            UpdateScene(logs);

            logs.Add("Setup completado con éxito.");
        }
        catch (Exception ex)
        {
            logs.Add($"ERROR durante el setup: {ex.Message}\n{ex.StackTrace}");
            Debug.LogError($"[SetupLevel1Assets] Error: {ex}");
        }

        File.WriteAllLines(logPath, logs.ToArray());
        Debug.Log("[SetupLevel1Assets] Log guardado en setup_log.txt");
    }

    private static void SetupEsbirroImporter(List<string> logs)
    {
        string path = "Assets/Images/Nivel1/enemigos/esbirro.png";
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            logs.Add($"No se encontró TextureImporter en {path}");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 160f;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = 4096;

        List<SpriteMetaData> metas = new List<SpriteMetaData>();

        // Idle (8 frames)
        metas.Add(new SpriteMetaData { name = "esbirro_idle_0", rect = new Rect(88, 1654, 142, 159), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "esbirro_idle_1", rect = new Rect(326, 1654, 127, 162), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "esbirro_idle_2", rect = new Rect(539, 1649, 141, 163), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "esbirro_idle_3", rect = new Rect(813, 1653, 143, 160), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "esbirro_idle_4", rect = new Rect(1086, 1657, 133, 168), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "esbirro_idle_5", rect = new Rect(1331, 1653, 154, 169), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "esbirro_idle_6", rect = new Rect(1597, 1652, 114, 177), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "esbirro_idle_7", rect = new Rect(1818, 1649, 162, 168), alignment = (int)SpriteAlignment.BottomCenter });

        // Walk (8 frames)
        metas.Add(new SpriteMetaData { name = "esbirro_walk_0", rect = new Rect(76, 1156, 165, 165), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "esbirro_walk_1", rect = new Rect(329, 1157, 142, 143), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "esbirro_walk_2", rect = new Rect(583, 1157, 144, 143), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "esbirro_walk_3", rect = new Rect(829, 1153, 165, 175), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "esbirro_walk_4", rect = new Rect(1082, 1167, 157, 165), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "esbirro_walk_5", rect = new Rect(1338, 1157, 118, 174), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "esbirro_walk_6", rect = new Rect(1587, 1157, 117, 168), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "esbirro_walk_7", rect = new Rect(1822, 1156, 170, 164), alignment = (int)SpriteAlignment.BottomCenter });

        // Defeat (8 frames)
        metas.Add(new SpriteMetaData { name = "esbirro_defeat_0", rect = new Rect(70, 121, 145, 171), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "esbirro_defeat_1", rect = new Rect(337, 123, 165, 163), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "esbirro_defeat_2", rect = new Rect(604, 10, 164, 267), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "esbirro_defeat_3", rect = new Rect(768, 10, 215, 199), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "esbirro_defeat_4", rect = new Rect(1034, 9, 246, 222), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "esbirro_defeat_5", rect = new Rect(1280, 9, 256, 269), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "esbirro_defeat_6", rect = new Rect(1536, 9, 256, 258), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "esbirro_defeat_7", rect = new Rect(1792, 10, 216, 237), alignment = (int)SpriteAlignment.BottomCenter });

        importer.spritesheet = metas.ToArray();
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
        logs.Add($"Esbirro reimportado con {metas.Count} slices.");
    }

    private static void SetupBossImporter(List<string> logs)
    {
        string path = "Assets/Images/Nivel1/Boos/vendedor_garbanos.png";
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            logs.Add($"No se encontró TextureImporter en {path}");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 150f;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = 4096;

        List<SpriteMetaData> metas = new List<SpriteMetaData>();

        // Idle (6 frames)
        metas.Add(new SpriteMetaData { name = "vendedor_idle_0", rect = new Rect(1, 1998, 297, 308), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "vendedor_idle_1", rect = new Rect(298, 1998, 298, 308), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "vendedor_idle_2", rect = new Rect(596, 1998, 282, 308), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "vendedor_idle_3", rect = new Rect(923, 1998, 233, 308), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "vendedor_idle_4", rect = new Rect(1201, 1998, 289, 308), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "vendedor_idle_5", rect = new Rect(1490, 1998, 286, 308), alignment = (int)SpriteAlignment.BottomCenter });

        // Walk Right (6 frames)
        metas.Add(new SpriteMetaData { name = "vendedor_walk_right_0", rect = new Rect(0, 1605, 298, 290), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "vendedor_walk_right_1", rect = new Rect(298, 1605, 298, 292), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "vendedor_walk_right_2", rect = new Rect(596, 1605, 298, 291), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "vendedor_walk_right_3", rect = new Rect(894, 1605, 279, 290), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "vendedor_walk_right_4", rect = new Rect(1195, 1606, 295, 290), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "vendedor_walk_right_5", rect = new Rect(1490, 1605, 242, 290), alignment = (int)SpriteAlignment.BottomCenter });

        // Walk Left (6 frames)
        metas.Add(new SpriteMetaData { name = "vendedor_walk_left_0", rect = new Rect(36, 1215, 262, 287), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "vendedor_walk_left_1", rect = new Rect(298, 1215, 298, 287), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "vendedor_walk_left_2", rect = new Rect(596, 1215, 297, 294), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "vendedor_walk_left_3", rect = new Rect(908, 1215, 284, 287), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "vendedor_walk_left_4", rect = new Rect(1192, 1215, 298, 287), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "vendedor_walk_left_5", rect = new Rect(1490, 1215, 298, 293), alignment = (int)SpriteAlignment.BottomCenter });

        // Cucharazo (5 frames)
        metas.Add(new SpriteMetaData { name = "vendedor_cucharazo_0", rect = new Rect(36, 815, 321, 294), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "vendedor_cucharazo_1", rect = new Rect(364, 815, 352, 294), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "vendedor_cucharazo_2", rect = new Rect(716, 815, 358, 291), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "vendedor_cucharazo_3", rect = new Rect(1074, 815, 358, 288), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "vendedor_cucharazo_4", rect = new Rect(1432, 815, 334, 291), alignment = (int)SpriteAlignment.BottomCenter });

        // Lluvia (5 frames)
        metas.Add(new SpriteMetaData { name = "vendedor_lluvia_0", rect = new Rect(36, 420, 322, 303), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "vendedor_lluvia_1", rect = new Rect(358, 420, 358, 299), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "vendedor_lluvia_2", rect = new Rect(716, 420, 358, 300), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "vendedor_lluvia_3", rect = new Rect(1074, 420, 358, 300), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "vendedor_lluvia_4", rect = new Rect(1432, 426, 354, 269), alignment = (int)SpriteAlignment.BottomCenter });

        // Defeat (5 frames)
        metas.Add(new SpriteMetaData { name = "vendedor_defeat_0", rect = new Rect(36, 30, 321, 302), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "vendedor_defeat_1", rect = new Rect(403, 31, 312, 303), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "vendedor_defeat_2", rect = new Rect(756, 31, 318, 303), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "vendedor_defeat_3", rect = new Rect(1074, 34, 358, 246), alignment = (int)SpriteAlignment.BottomCenter });
        metas.Add(new SpriteMetaData { name = "vendedor_defeat_4", rect = new Rect(1432, 30, 356, 274), alignment = (int)SpriteAlignment.BottomCenter });

        importer.spritesheet = metas.ToArray();
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
        logs.Add($"Vendedor Viral reimportado con {metas.Count} slices.");
    }

    private static void UpdateScene(List<string> logs)
    {
        string scenePath = "Assets/Scenes/Nivel1.unity";
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != scenePath)
        {
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        // Cargar sprites importados
        Dictionary<string, Sprite> esbirroSprites = LoadSpritesFromPath("Assets/Images/Nivel1/enemigos/esbirro.png");
        Dictionary<string, Sprite> bossSprites = LoadSpritesFromPath("Assets/Images/Nivel1/Boos/vendedor_garbanos.png");
        Dictionary<string, Sprite> terrainSprites = LoadSpritesFromPath("Assets/Images/Nivel1/Terrain/mainlev_build.png");

        // 1. Configurar Boss_VendedorViral
        GameObject bossObj = GameObject.Find("Boss_VendedorViral");
        if (bossObj != null)
        {
            var sr = bossObj.GetComponent<SpriteRenderer>();
            var boss = bossObj.GetComponent<ViralVendorBoss>();

            if (sr != null && bossSprites.ContainsKey("vendedor_idle_0"))
            {
                sr.sprite = bossSprites["vendedor_idle_0"];
                sr.sortingOrder = 3;
            }

            if (boss != null)
            {
                boss.idleSprites = GetSpriteList(bossSprites, "vendedor_idle_", 6);
                boss.walkRightSprites = GetSpriteList(bossSprites, "vendedor_walk_right_", 6);
                boss.walkLeftSprites = GetSpriteList(bossSprites, "vendedor_walk_left_", 6);
                boss.cucharazoSprites = GetSpriteList(bossSprites, "vendedor_cucharazo_", 5);
                boss.lluviaSprites = GetSpriteList(bossSprites, "vendedor_lluvia_", 5);
                boss.defeatSprites = GetSpriteList(bossSprites, "vendedor_defeat_", 5);
                EditorUtility.SetDirty(boss);
            }
            logs.Add("Boss_VendedorViral actualizado en escena.");
        }

        // 2. Configurar Esbirros
        SimpleEnemy[] enemies = UnityEngine.Object.FindObjectsByType<SimpleEnemy>(FindObjectsSortMode.None);
        int esbirrosActualizados = 0;
        foreach (var enemy in enemies)
        {
            if (enemy is ViralVendorBoss) continue;

            var sr = enemy.GetComponent<SpriteRenderer>();
            if (sr != null && esbirroSprites.ContainsKey("esbirro_idle_0"))
            {
                sr.sprite = esbirroSprites["esbirro_idle_0"];
                sr.sortingOrder = 2;
            }

            enemy.walkSprites = GetSpriteList(esbirroSprites, "esbirro_walk_", 8);
            enemy.defeatSprites = GetSpriteList(esbirroSprites, "esbirro_defeat_", 8);
            EditorUtility.SetDirty(enemy);
            esbirrosActualizados++;
        }
        logs.Add($"Esbirros actualizados en escena: {esbirrosActualizados}");

        // 3. Configurar Plataformas con sprites limpios de terrain (sin estiramiento)
        Sprite platformSprite = null;
        if (terrainSprites.ContainsKey("mainlev_build_12"))
            platformSprite = terrainSprites["mainlev_build_12"];
        else if (terrainSprites.ContainsKey("mainlev_build_21"))
            platformSprite = terrainSprites["mainlev_build_21"];

        var allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int platformsFixed = 0;
        foreach (var go in allObjects)
        {
            if (go.name.StartsWith("Platform_"))
            {
                var sr = go.GetComponent<SpriteRenderer>();
                var col = go.GetComponent<BoxCollider2D>();

                if (sr != null && platformSprite != null)
                {
                    sr.sprite = platformSprite;
                    sr.drawMode = SpriteDrawMode.Simple;

                    // Escala armónica sin deformación vertical
                    float curScaleX = Mathf.Abs(go.transform.localScale.x);
                    if (curScaleX < 1.5f) curScaleX = 2.0f;
                    go.transform.localScale = new Vector3(curScaleX, 1.0f, 1.0f);

                    if (col != null)
                    {
                        // mainlev_build_12 mide 1.72 de ancho y 0.18 de alto a PPU 100
                        col.size = new Vector2(1.72f, 0.22f);
                        col.offset = new Vector2(0f, 0f);
                    }
                    EditorUtility.SetDirty(go);
                    platformsFixed++;
                }
            }
        }
        logs.Add($"Plataformas actualizadas con mainlev_build: {platformsFixed}");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        logs.Add("Escena guardada correctamente.");
    }

    private static Dictionary<string, Sprite> LoadSpritesFromPath(string path)
    {
        Dictionary<string, Sprite> dict = new Dictionary<string, Sprite>();
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var a in assets)
        {
            if (a is Sprite s)
            {
                dict[s.name] = s;
            }
        }
        return dict;
    }

    private static Sprite[] GetSpriteList(Dictionary<string, Sprite> dict, string prefix, int count)
    {
        List<Sprite> list = new List<Sprite>();
        for (int i = 0; i < count; i++)
        {
            string key = prefix + i;
            if (dict.ContainsKey(key))
                list.Add(dict[key]);
        }
        return list.ToArray();
    }
}

