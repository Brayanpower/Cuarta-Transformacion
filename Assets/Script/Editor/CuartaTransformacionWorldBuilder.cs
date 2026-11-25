#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Generador y Mejorador Automático de Escenarios Nocturnos Colonial-Medievales
/// para "Cuarta Transformación" (Unity 6):
/// - Tools > Cuarta Transformacion > Mejorar Nivel 1 (Fondo Nocturno + Estilo Medieval)
/// - Tools > Cuarta Transformacion > Generar Escenario Nivel 2 (Talleres de Talavera)
/// - Tools > Cuarta Transformacion > Generar Escenario Nivel 3 (Callejones de Guanajuato)
/// - Tools > Cuarta Transformacion > Construir Todos los Niveles y Registrar Build Settings
/// </summary>
public static class CuartaTransformacionWorldBuilder
{
    private const string PathMainMenu = "Assets/Scenes/MainMenu.unity";
    private const string PathNivel1   = "Assets/Scenes/Nivel1.unity";
    private const string PathNivel2   = "Assets/Scenes/Nivel2.unity";
    private const string PathNivel3   = "Assets/Scenes/Nivel3.unity";

    private const string TileTopPath    = "Assets/Tile Palette/1_Mine_Tileset_1_7.asset";
    private const string TileFillPath   = "Assets/Tile Palette/1_Mine_Tileset_1_13.asset";
    private const string TileBottomPath = "Assets/Tile Palette/1_Mine_Tileset_1_19.asset";

    // Fondos pixel-art nocturnos de Dolores Hidalgo y Guanajuato
    private const string BgPathNivel1 = "Assets/Images/Backgrounds/Bg_Nivel1_DoloresNoche.png";
    private const string BgPathNivel2 = "Assets/Images/Backgrounds/Bg_Nivel2_TalaveraNoche.png";
    private const string BgPathNivel3 = "Assets/Images/Backgrounds/Bg_Nivel3_CallejonesNoche.png";

    // Props coloniales-medievales nocturnos y plataformas arquitectónicas
    private const string PathTorch0       = "Assets/Images/Medieval/Medieval_Torch_0.png";
    private const string PathTorch1       = "Assets/Images/Medieval/Medieval_Torch_1.png";
    private const string PathStoneArch    = "Assets/Images/Medieval/Medieval_StoneArch.png";
    private const string PathBannerFiesta = "Assets/Images/Medieval/Medieval_Banner_Fiesta.png";
    private const string PathLanternPost  = "Assets/Images/Medieval/Medieval_LanternPost.png";
    private const string PathBattlement   = "Assets/Images/Medieval/Medieval_Battlement_Wall.png";
    private const string PathPapelPicado  = "Assets/Images/UI/UI_PapelPicado_Garland.png";
    private const string PathPlatCantera    = "Assets/Images/Medieval/Platform_Medieval_Cantera.png";
    private const string PathPlatTalavera   = "Assets/Images/Medieval/Platform_Medieval_Talavera.png";
    private const string PathPlatGuanajuato = "Assets/Images/Medieval/Platform_Medieval_Guanajuato.png";

    private const string PlatformSpriteGuid = "124821cafa18041cfbcec65c57ac7317";
    private const string SpikeSquareGuid    = "311925a002f4447b3a28927169b83ea6";

    // Suelos temáticos pixel-art dedicados por nivel (32x32, PPU 32 -> 1x1 exacto sin huecos)
    private const string GroundImgFolder   = "Assets/Images/GroundTiles";
    private const string GroundAssetFolder = "Assets/Tile Palette/CustomGround";

    private struct GroundTileSet
    {
        public TileBase top;
        public TileBase fill;
        public TileBase bottom;
    }

    private static GroundTileSet LoadOrCreateThemedGroundTiles(int levelNumber)
    {
        AssetDatabase.Refresh();
        Directory.CreateDirectory(GroundAssetFolder);

        string prefix;
        switch (levelNumber)
        {
            case 2: prefix = "Tile_N2_Talavera"; break;
            case 3: prefix = "Tile_N3_Guanajuato"; break;
            default: prefix = "Tile_N1_Cantera"; break;
        }

        TileBase top    = EnsureTileAsset($"{GroundImgFolder}/{prefix}_Top.png",    $"{GroundAssetFolder}/{prefix}_Top.asset");
        TileBase fill   = EnsureTileAsset($"{GroundImgFolder}/{prefix}_Fill.png",   $"{GroundAssetFolder}/{prefix}_Fill.asset");
        TileBase bottom = EnsureTileAsset($"{GroundImgFolder}/{prefix}_Bottom.png", $"{GroundAssetFolder}/{prefix}_Bottom.asset");

        // Fallback a la paleta original si faltara algún PNG
        if (top == null)    top    = AssetDatabase.LoadAssetAtPath<TileBase>(TileTopPath);
        if (fill == null)   fill   = AssetDatabase.LoadAssetAtPath<TileBase>(TileFillPath);
        if (bottom == null) bottom = AssetDatabase.LoadAssetAtPath<TileBase>(TileBottomPath);

        return new GroundTileSet { top = top, fill = fill, bottom = bottom };
    }

    private static TileBase EnsureTileAsset(string pngPath, string tileAssetPath)
    {
        Sprite sp = CuartaTransformacionMenuBuilder.ImportAndLoadCrispSprite(pngPath, null, 32f, new Vector2(0.5f, 0.5f));
        if (sp == null)
            return null;

        Tile existing = AssetDatabase.LoadAssetAtPath<Tile>(tileAssetPath);
        if (existing != null)
        {
            if (existing.sprite != sp || existing.colliderType != Tile.ColliderType.Grid)
            {
                existing.sprite = sp;
                existing.color = Color.white;
                existing.colliderType = Tile.ColliderType.Grid;
                EditorUtility.SetDirty(existing);
            }
            return existing;
        }

        Tile newTile = ScriptableObject.CreateInstance<Tile>();
        newTile.sprite = sp;
        newTile.color = Color.white;
        newTile.colliderType = Tile.ColliderType.Grid;
        AssetDatabase.CreateAsset(newTile, tileAssetPath);
        return newTile;
    }

    /// <summary>
    /// Configura CompositeCollider2D (Merge) sobre el Tilemap del suelo para fundir todos los
    /// bloques 1x1 en una superficie continua y eliminar por completo los atascos en suelo plano.
    /// </summary>
    private static void EnsureSeamlessCompositeGround(GameObject sueloObj)
    {
        if (sueloObj == null)
            return;

        Rigidbody2D rb = sueloObj.GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = sueloObj.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        rb.simulated = true;
        EditorUtility.SetDirty(rb);

        TilemapCollider2D tmCol = sueloObj.GetComponent<TilemapCollider2D>();
        if (tmCol == null)
            tmCol = sueloObj.AddComponent<TilemapCollider2D>();
        tmCol.compositeOperation = Collider2D.CompositeOperation.Merge;
        EditorUtility.SetDirty(tmCol);

        CompositeCollider2D comp = sueloObj.GetComponent<CompositeCollider2D>();
        if (comp == null)
            comp = sueloObj.AddComponent<CompositeCollider2D>();
        comp.geometryType = CompositeCollider2D.GeometryType.Polygons;
        comp.generationType = CompositeCollider2D.GenerationType.Synchronous;
        comp.edgeRadius = 0f;
        comp.GenerateGeometry();
        EditorUtility.SetDirty(comp);
    }

    /// <summary>
    /// Repinta todos los tiles existentes en un Tilemap usando el nuevo tileset temático del nivel
    /// (detectando automáticamente qué celdas son superficie superior, interior o base).
    /// </summary>
    private static void ReskinTilemapGround(Tilemap tm, GroundTileSet tileset)
    {
        if (tm == null || tileset.top == null)
            return;

        tm.CompressBounds();
        BoundsInt bounds = tm.cellBounds;
        List<Vector3Int> occupied = new List<Vector3Int>();

        for (int x = bounds.xMin; x <= bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y <= bounds.yMax; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (tm.HasTile(cell))
                    occupied.Add(cell);
            }
        }

        foreach (Vector3Int cell in occupied)
        {
            bool hasAbove = tm.HasTile(new Vector3Int(cell.x, cell.y + 1, 0));
            bool hasBelow = tm.HasTile(new Vector3Int(cell.x, cell.y - 1, 0));
            TileBase chosen = !hasAbove ? tileset.top : (!hasBelow ? tileset.bottom : tileset.fill);
            tm.SetTile(cell, chosen);
        }

        tm.color = Color.white;
        tm.RefreshAllTiles();
        EditorUtility.SetDirty(tm);
    }

    private struct SharedSceneRefs
    {
        public GameObject player;
        public Camera mainCamera;
        public Sprite platformSprite;
        public Material spriteLitMaterial;
    }

    private struct MedievalPropSprites
    {
        public Sprite torch0;
        public Sprite torch1;
        public Sprite stoneArch;
        public Sprite banner;
        public Sprite lanternPost;
        public Sprite battlement;
        public Sprite papelPicado;
        public Sprite platCantera;
        public Sprite platTalavera;
        public Sprite platGuanajuato;
    }

    private static MedievalPropSprites LoadMedievalProps()
    {
        AssetDatabase.Refresh();
        Vector2 bottomPivot   = new Vector2(0.5f, 0f);
        Vector2 topPivot      = new Vector2(0.5f, 1f);
        Vector2 platformPivot = new Vector2(0.5f, 0.82f);
        return new MedievalPropSprites
        {
            torch0         = CuartaTransformacionMenuBuilder.ImportAndLoadCrispSprite(PathTorch0, null, 20f, bottomPivot),
            torch1         = CuartaTransformacionMenuBuilder.ImportAndLoadCrispSprite(PathTorch1, null, 20f, bottomPivot),
            stoneArch      = CuartaTransformacionMenuBuilder.ImportAndLoadCrispSprite(PathStoneArch, null, 18f, bottomPivot),
            banner         = CuartaTransformacionMenuBuilder.ImportAndLoadCrispSprite(PathBannerFiesta, null, 20f, topPivot),
            lanternPost    = CuartaTransformacionMenuBuilder.ImportAndLoadCrispSprite(PathLanternPost, null, 22f, bottomPivot),
            battlement     = CuartaTransformacionMenuBuilder.ImportAndLoadCrispSprite(PathBattlement, null, 20f, bottomPivot),
            papelPicado    = CuartaTransformacionMenuBuilder.ImportAndLoadCrispSprite(PathPapelPicado, null, 48f, topPivot),
            platCantera    = CuartaTransformacionMenuBuilder.ImportAndLoadCrispSprite(PathPlatCantera, null, 32f, platformPivot),
            platTalavera   = CuartaTransformacionMenuBuilder.ImportAndLoadCrispSprite(PathPlatTalavera, null, 32f, platformPivot),
            platGuanajuato = CuartaTransformacionMenuBuilder.ImportAndLoadCrispSprite(PathPlatGuanajuato, null, 32f, platformPivot)
        };
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // MENÚ 1: MEJORAR NIVEL 1 (FONDO NOCTURNO PIXEL-ART + ESTILO MEDIEVAL)
    // ═══════════════════════════════════════════════════════════════════════════

    [MenuItem("Tools/Cuarta Transformacion/Mejorar Nivel 1 (Fondo Nocturno + Estilo Medieval)", false, 9)]
    public static void UpgradeLevel1Menu()
    {
        UpgradeLevel1MedievalAndNight(true);
    }

    public static void UpgradeLevel1MedievalAndNight(bool showDialog)
    {
        if (!File.Exists(PathNivel1))
            return;

        MedievalPropSprites props = LoadMedievalProps();
        Sprite nightBgN1 = CuartaTransformacionMenuBuilder.ImportAndLoadCrispSprite(BgPathNivel1, null, 100f);

        Scene s1 = EditorSceneManager.OpenScene(PathNivel1, OpenSceneMode.Single);

        // 1. Configurar fondo nocturno pixel-art de Dolores Hidalgo con ParallaxBackground
        GameObject bgObj = GameObject.Find("Background");
        Camera mainCam = Camera.main != null ? Camera.main : UnityEngine.Object.FindAnyObjectByType<Camera>();
        Material litMat = null;

        PlayerMovement pm = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
        if (pm != null)
        {
            SpriteRenderer psr = pm.GetComponent<SpriteRenderer>();
            if (psr != null) litMat = psr.sharedMaterial;
        }

        if (mainCam != null)
        {
            mainCam.backgroundColor = new Color(0.07f, 0.05f, 0.14f, 1f);
            CameraFollow camFollow = mainCam.GetComponent<CameraFollow>();
            if (camFollow != null)
            {
                camFollow.maxBounds = new Vector2(Mathf.Max(camFollow.maxBounds.x, 265f), camFollow.maxBounds.y);
                EditorUtility.SetDirty(camFollow);
            }
            EditorUtility.SetDirty(mainCam);
        }

        if (bgObj != null)
        {
            SpriteRenderer sr = bgObj.GetComponent<SpriteRenderer>();
            if (sr != null && nightBgN1 != null)
            {
                sr.sprite = nightBgN1;
                sr.color = Color.white;
                sr.sortingOrder = -50;
                EditorUtility.SetDirty(sr);
            }

            ParallaxBackground pb = bgObj.GetComponent<ParallaxBackground>();
            if (pb == null) pb = bgObj.AddComponent<ParallaxBackground>();
            pb.targetCamera = mainCam;
            pb.followFactorX = 0.98f;
            pb.followFactorY = 0.95f;
            pb.smoothSpeed = 14f;
            pb.autoFitCamera = true;
            pb.scaleMargin = 1.25f;
            pb.SnapToCameraImmediate();
            EditorUtility.SetDirty(pb);
        }

        // 2. Aplicar nuevo suelo temático de Cantera Colonial de Dolores Hidalgo + CompositeCollider2D sin costuras (0 atascos!)
        GameObject sueloObj = GameObject.Find("suelo");
        if (sueloObj != null)
        {
            Tilemap tm = sueloObj.GetComponent<Tilemap>();
            if (tm != null)
            {
                GroundTileSet n1Ground = LoadOrCreateThemedGroundTiles(1);
                ReskinTilemapGround(tm, n1Ground);
            }
            EnsureSeamlessCompositeGround(sueloObj);
        }

        // 2.5. ELIMINAR ÁRBOLES, ARBUSTOS Y ROCAS ANTIGUAS (Prop_*) Y MEJORAR TODAS LAS BASES DE PALO (Platform_*)
        int removedOldProps = 0;
        int upgradedPlatforms = 0;
        GameObject[] rootObjs = s1.GetRootGameObjects();
        List<GameObject> oldPropsToDelete = new List<GameObject>();

        foreach (GameObject root in rootObjs)
        {
            if (root == null) continue;

            // Quitar todos los árboles, arbustos y rocas antiguas (Prop_*)
            if (root.name.StartsWith("Prop_"))
            {
                oldPropsToDelete.Add(root);
                continue;
            }

            // Reemplazar las bases delgadas de palo (Platform_*) por balcones/puentes medievales de cantera y roble (idempotente)
            if (root.name.StartsWith("Platform_") && props.platCantera != null)
            {
                SpriteRenderer psr = root.GetComponent<SpriteRenderer>();
                BoxCollider2D pcol = root.GetComponent<BoxCollider2D>();

                bool alreadyMedieval = psr != null && psr.sprite == props.platCantera;

                if (alreadyMedieval)
                {
                    psr.drawMode = SpriteDrawMode.Simple;
                    psr.color = Color.white;
                    psr.sortingOrder = 1;
                    if (litMat != null) psr.sharedMaterial = litMat;
                    EditorUtility.SetDirty(psr);

                    float curScaleX = root.transform.localScale.x;
                    if (curScaleX < 0.85f || curScaleX > 1.55f || Mathf.Abs(root.transform.localScale.y - 1f) > 0.001f)
                    {
                        float clampedX = Mathf.Clamp(curScaleX, 0.85f, 1.55f);
                        root.transform.localScale = new Vector3(clampedX, 1.0f, 1f);
                        EditorUtility.SetDirty(root.transform);
                    }

                    if (pcol != null)
                    {
                        pcol.size = new Vector2(3.46f, 0.40f);
                        pcol.offset = new Vector2(0f, -0.02f);
                        EditorUtility.SetDirty(pcol);
                    }
                }
                else
                {
                    float desiredTopY = pcol != null ? pcol.bounds.max.y : root.transform.position.y + 0.09f;
                    float currentWidth = pcol != null ? pcol.bounds.size.x : 3.5f;
                    float scaleX = Mathf.Clamp(currentWidth / 3.5f, 0.85f, 1.55f);

                    if (psr != null)
                    {
                        psr.sprite = props.platCantera;
                        psr.drawMode = SpriteDrawMode.Simple;
                        psr.color = Color.white;
                        psr.sortingOrder = 1;
                        if (litMat != null) psr.sharedMaterial = litMat;
                        EditorUtility.SetDirty(psr);
                    }

                    root.transform.localScale = new Vector3(scaleX, 1.0f, 1f);
                    // Con pivot = (0.5, 0.82) en 32px / 32ppu, el borde superior está a +0.18 del transform.position.y
                    root.transform.position = new Vector3(root.transform.position.x, desiredTopY - 0.18f, root.transform.position.z);
                    EditorUtility.SetDirty(root.transform);

                    if (pcol != null)
                    {
                        // Colisionador ajustado solo al tablero transitable superior (permite caminar bajo los arcos de piedra)
                        pcol.size = new Vector2(3.46f, 0.40f);
                        pcol.offset = new Vector2(0f, -0.02f);
                        EditorUtility.SetDirty(pcol);
                    }
                }

                upgradedPlatforms++;
            }
        }

        foreach (GameObject oldProp in oldPropsToDelete)
        {
            UnityEngine.Object.DestroyImmediate(oldProp);
            removedOldProps++;
        }

        // Actualizar estadísticas cómodas de combate en Player (alcance de espada, vida e invulnerabilidad)
        if (pm != null)
        {
            pm.maxHealth = Mathf.Max(pm.maxHealth, 6);
            pm.attackRange = Mathf.Max(pm.attackRange, 1.15f);
            pm.invulnerabilityDuration = Mathf.Max(pm.invulnerabilityDuration, 1.45f);
            EditorUtility.SetDirty(pm);
        }

        // 3. Reemplazar capa decorativa colonial-medieval idempotente (evita duplicados y sin encimar faroles en plataformas)
        GameObject oldDecor = GameObject.Find("MedievalDecor_Nivel1");
        if (oldDecor != null)
            UnityEngine.Object.DestroyImmediate(oldDecor);

        GameObject decorRoot = new GameObject("MedievalDecor_Nivel1");

        // Arcos medievales de cantera cálida en hitos clave (entrada, plaza media, acceso a la arena)
        PlaceStoneArch(decorRoot.transform, "Arch_N1_Entrada",   new Vector3(-1.5f, -2.0f, 0f), 1.05f, props.stoneArch, litMat);
        PlaceStoneArch(decorRoot.transform, "Arch_N1_Centro",    new Vector3(47.0f, -2.0f, 0f), 1.05f, props.stoneArch, litMat);
        PlaceStoneArch(decorRoot.transform, "Arch_N1_Fortaleza", new Vector3(86.0f, -4.0f, 0f), 1.10f, props.stoneArch, litMat);
        PlaceStoneArch(decorRoot.transform, "Arch_N1_Arena",     new Vector3(130.5f, -4.0f, 0f), 1.20f, props.stoneArch, litMat);

        // Muros con almenas medievales (battlements) flanqueando la ruta y la arena
        float[] battlementXs = { 4.2f, 22.5f, 62f, 78f, 98f, 118f, 134f, 148f };
        foreach (float bx in battlementXs)
        {
            float gy = bx >= 85f ? -4.0f : -2.0f;
            PlaceBattlement(decorRoot.transform, $"Almena_N1_{bx:0}", new Vector3(bx, gy, 0f), props.battlement, litMat);
        }

        // Antorchas medievales animadas (fuego vivo)
        float[] torchXs = { 1.8f, 13.5f, 25.5f, 35.5f, 51f, 66f, 82f, 94f, 112f, 126f, 133f, 142f, 152f };
        foreach (float tx in torchXs)
        {
            float gy = tx >= 85f ? -4.0f : -2.0f;
            PlaceAnimatedTorch(decorRoot.transform, $"Antorcha_N1_{tx:0}", new Vector3(tx, gy, 0f), props, litMat);
        }

        // Faroles coloniales nocturnos (espaciados limpiamente fuera de las plataformas)
        float[] lanternXs = { 11.5f, 32.5f, 59f, 74f, 103.5f, 122f, 138f, 154f };
        foreach (float lx in lanternXs)
        {
            float gy = lx >= 85f ? -4.0f : -2.0f;
            PlaceLanternPost(decorRoot.transform, $"Farol_N1_{lx:0}", new Vector3(lx, gy, 0f), props.lanternPost, litMat);
        }

        // Estandartes medievales de fiesta patronal colgando bajo los balcones de cantera + papel picado en la arena
        PlaceSwayingBanner(decorRoot.transform, "Estandarte_N1_1", new Vector3(18f, 0.82f, 0f), props.banner, litMat);
        PlaceSwayingBanner(decorRoot.transform, "Estandarte_N1_2", new Vector3(68f, 0.22f, 0f), props.banner, litMat);
        PlaceSwayingBanner(decorRoot.transform, "Estandarte_N1_3", new Vector3(132.5f, -1.98f, 0f), props.banner, litMat);
        PlaceSwayingBanner(decorRoot.transform, "Estandarte_N1_4", new Vector3(152.5f, -1.98f, 0f), props.banner, litMat);
        PlacePapelPicadoOverhead(decorRoot.transform, "PapelPicado_Arena_N1", new Vector3(142.5f, 2.9f, 0f), 2.8f, props.papelPicado, litMat);

        Debug.Log($"✅ [WorldBuilder] Nivel 1 limpio: {removedOldProps} árboles/rocas eliminados, {upgradedPlatforms} bases de palo reemplazadas por puentes medievales de cantera.");

        // Tope físico izquierdo en el borde inicial del Nivel 1 (x = -6)
        GameObject oldLeftWall = GameObject.Find("Wall_LeftLimit_Nivel1");
        if (oldLeftWall != null)
            UnityEngine.Object.DestroyImmediate(oldLeftWall);
        CreateInvisibleWall("Wall_LeftLimit_Nivel1", new Vector3(-6.0f, 1.0f, 0f), new Vector2(1.2f, 16f));

        // 4. Asegurar Portal de salida conectado a El Vendedor Viral
        ViralVendorBoss boss = UnityEngine.Object.FindAnyObjectByType<ViralVendorBoss>();
        if (boss != null && boss.levelPortal == null)
        {
            LevelPortalTrigger existing = UnityEngine.Object.FindAnyObjectByType<LevelPortalTrigger>();
            if (existing == null)
            {
                existing = CreateLevelPortal(
                    "Portal_FinNivel1",
                    new Vector3(154.5f, -4.0f, 0f),
                    1,
                    "Nivel2",
                    boss.bossTitle,
                    "¡El Jardín Principal de Dolores Hidalgo está en paz!",
                    litMat
                );
            }
            boss.levelPortal = existing;
            EditorUtility.SetDirty(boss);
        }

        // 4.5. Colocar Cinemática Automática EXCLUSIVA del Nivel 1 con todas sus ilustraciones empaquetadas
        GameObject oldIntro = GameObject.Find("IntroCutscene_Nivel1");
        if (oldIntro != null)
            UnityEngine.Object.DestroyImmediate(oldIntro);

        GameObject introObj = new GameObject("IntroCutscene_Nivel1");
        Level1IntroCutsceneTrigger introTrigger = introObj.AddComponent<Level1IntroCutsceneTrigger>();
        introTrigger.slide1Art = CuartaTransformacionMenuBuilder.ImportAndLoadCrispSprite("Assets/Images/Cutscenes/Cutscene_01_FiestaPaz.png", null, 100f);
        introTrigger.slide2Art = CuartaTransformacionMenuBuilder.ImportAndLoadCrispSprite("Assets/Images/UI/Menu_KeyArt_3Jefes.png", null, 100f);
        introTrigger.slide3Art = CuartaTransformacionMenuBuilder.ImportAndLoadCrispSprite(BgPathNivel2, null, 100f);
        introTrigger.slide4Art = CuartaTransformacionMenuBuilder.ImportAndLoadCrispSprite("Assets/Images/Cutscenes/Cutscene_02_HeroeDespierta.png", null, 100f);
        introTrigger.ornateFrameSprite         = CuartaTransformacionMenuBuilder.ImportAndLoadCrispSprite("Assets/Images/UI/UI_PlankPanel_Formal.png", new Vector4(18f, 18f, 18f, 18f), 100f);
        introTrigger.bannerButtonSprite        = CuartaTransformacionMenuBuilder.ImportAndLoadCrispSprite("Assets/Images/UI/UI_PlankButton_Normal.png", new Vector4(14f, 8f, 14f, 8f), 100f);
        introTrigger.bannerButtonHoverSprite   = CuartaTransformacionMenuBuilder.ImportAndLoadCrispSprite("Assets/Images/UI/UI_PlankButton_Hover.png", new Vector4(14f, 8f, 14f, 8f), 100f);
        introTrigger.bannerButtonPressedSprite = CuartaTransformacionMenuBuilder.ImportAndLoadCrispSprite("Assets/Images/UI/UI_PlankButton_Pressed.png", new Vector4(14f, 8f, 14f, 8f), 100f);
        EditorUtility.SetDirty(introTrigger);

        EditorSceneManager.MarkSceneDirty(s1);
        EditorSceneManager.SaveScene(s1);
        AssetDatabase.SaveAssets();

        Debug.Log("✅ [WorldBuilder] Nivel 1 actualizado con Fondo Nocturno Pixel-Art de Dolores Hidalgo + Arquitectura Colonial-Medieval.");
        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Nivel 1 Mejorado",
                "Nivel1.unity ahora cuenta con:\n\n" +
                "• Fondo pixel-art nocturno de la Parroquia de Dolores Hidalgo\n" +
                "• Arcos de piedra con almenas, antorchas animadas, faroles coloniales y estandartes medievales\n" +
                "• Portal de salida hacia el Nivel 2 conectado a Don Chava",
                "¡Perfecto!"
            );
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // MENÚ 2: GENERAR ESCENARIO NIVEL 2 — TALLERES DE TALAVERA (NOCHE MEDIEVAL)
    // ═══════════════════════════════════════════════════════════════════════════

    [MenuItem("Tools/Cuarta Transformacion/Generar Escenario Nivel 2 (Talleres de Talavera)", false, 10)]
    public static void GenerateLevel2Menu()
    {
        GenerateLevel2Scene(true);
    }

    public static void GenerateLevel2Scene(bool showDialog)
    {
        MedievalPropSprites props = LoadMedievalProps();
        Sprite nightBgN2 = CuartaTransformacionMenuBuilder.ImportAndLoadCrispSprite(BgPathNivel2, null, 100f);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SharedSceneRefs refs = CloneCoreObjectsFromNivel1(scene, new Vector3(0f, 1.5f, 0f));

        // 1. Fondo exclusivo pixel-art nocturno de los Talleres de Talavera
        CreateThemedBackground(
            refs,
            nightBgN2,
            Color.white,
            new Color(0.08f, 0.06f, 0.14f, 1f)
        );

        // 2. Grid + Tilemap ("suelo" en capa Ground) con suelo exclusivo de Talavera/Terracota + CompositeCollider2D
        Tilemap suelo = CreateGroundTilemap(Color.white);
        GroundTileSet n2Ground = LoadOrCreateThemedGroundTiles(2);
        TileBase tileTop    = n2Ground.top;
        TileBase tileFill   = n2Ground.fill;
        TileBase tileBottom = n2Ground.bottom;

        PaintFloorBlock(suelo, -10, -9, -8, 5, tileTop, tileFill, tileBottom);   // Muro izquierdo
        PaintFloorBlock(suelo, -8, 28, -8, -3, tileTop, tileFill, tileBottom);   // Sector 1: top y = -2
        PaintFloorBlock(suelo, 29, 63, -8, -6, tileTop, tileFill, tileBottom);   // Sector 2: foso hornos y = -5
        PaintFloorBlock(suelo, 45, 48, -8, -4, tileTop, tileFill, tileBottom);   // Isla intermedia y = -3
        PaintFloorBlock(suelo, 64, 86, -8, -3, tileTop, tileFill, tileBottom);   // Sector 3A: top y = -2
        PaintFloorBlock(suelo, 87, 114, -8, -4, tileTop, tileFill, tileBottom);  // Sector 3B: top y = -3
        PaintFloorBlock(suelo, 115, 148, -8, -4, tileTop, tileFill, tileBottom); // Sector 4: Arena Jefe 2 y = -3
        PaintFloorBlock(suelo, 146, 150, -8, 6, tileTop, tileFill, tileBottom);  // Muro derecho cierre
        suelo.RefreshAllTiles();
        EnsureSeamlessCompositeGround(suelo.gameObject);

        // 3. Decoración Colonial-Medieval Nocturna de los Talleres de Talavera
        GameObject decorRoot = new GameObject("MedievalDecor_Nivel2");
        PlaceStoneArch(decorRoot.transform, "Arch_N2_Taller",   new Vector3(-1f, -2f, 0f), 1.05f, props.stoneArch, refs.spriteLitMaterial);
        PlaceStoneArch(decorRoot.transform, "Arch_N2_Hornos",   new Vector3(66f, -2f, 0f), 1.08f, props.stoneArch, refs.spriteLitMaterial);
        PlaceStoneArch(decorRoot.transform, "Arch_N2_Arena",    new Vector3(116.5f, -3f, 0f), 1.20f, props.stoneArch, refs.spriteLitMaterial);

        float[] n2Battlements = { 7f, 22f, 74f, 94f, 109f, 121f, 139f };
        foreach (float bx in n2Battlements)
        {
            float gy = bx >= 87f ? -3f : -2f;
            PlaceBattlement(decorRoot.transform, $"Almena_N2_{bx:0}", new Vector3(bx, gy, 0f), props.battlement, refs.spriteLitMaterial);
        }

        float[] n2Torches = { 3f, 17f, 27f, 46.5f, 69f, 83f, 99f, 113f, 120f, 130f, 140f };
        foreach (float tx in n2Torches)
        {
            float gy = (tx > 44f && tx < 50f) || tx >= 87f ? -3f : -2f;
            PlaceAnimatedTorch(decorRoot.transform, $"Antorcha_N2_{tx:0}", new Vector3(tx, gy, 0f), props, refs.spriteLitMaterial);
        }

        float[] n2Lanterns = { 12f, 78f, 104f, 125f, 135f };
        foreach (float lx in n2Lanterns)
        {
            float gy = lx >= 87f ? -3f : -2f;
            PlaceLanternPost(decorRoot.transform, $"Farol_N2_{lx:0}", new Vector3(lx, gy, 0f), props.lanternPost, refs.spriteLitMaterial);
        }

        PlaceSwayingBanner(decorRoot.transform, "Estandarte_N2_1", new Vector3(123f, -0.35f, 0f), props.banner, refs.spriteLitMaterial);
        PlaceSwayingBanner(decorRoot.transform, "Estandarte_N2_2", new Vector3(137f, -0.35f, 0f), props.banner, refs.spriteLitMaterial);
        PlacePapelPicadoOverhead(decorRoot.transform, "PapelPicado_Arena_N2", new Vector3(130f, 3.6f, 0f), 2.8f, props.papelPicado, refs.spriteLitMaterial);

        // 4. Plataformas arquitectónicas medievales de Talavera y Plataformas Quebradizas
        if (props.platTalavera != null)
            refs.platformSprite = props.platTalavera;

        CreateSolidPlatform("Platform_Taller_1", new Vector3(10f, 0.4f, 0f), new Vector3(1.1f, 1f, 1f), refs, Color.white);
        CreateSolidPlatform("Platform_Taller_2", new Vector3(20f, 1.5f, 0f), new Vector3(1.1f, 1f, 1f), refs, Color.white);

        CreateBreakableTalaveraPlatform("Talavera_Fragil_1", new Vector3(32.5f, -1.2f, 0f), refs.spriteLitMaterial);
        CreateBreakableTalaveraPlatform("Talavera_Fragil_2", new Vector3(38.0f, -0.4f, 0f), refs.spriteLitMaterial);
        CreateBreakableTalaveraPlatform("Talavera_Fragil_3", new Vector3(43.0f, -1.0f, 0f), refs.spriteLitMaterial);
        CreateBreakableTalaveraPlatform("Talavera_Fragil_4", new Vector3(52.5f, -0.8f, 0f), refs.spriteLitMaterial);
        CreateBreakableTalaveraPlatform("Talavera_Fragil_5", new Vector3(58.5f, -0.2f, 0f), refs.spriteLitMaterial);

        CreateSolidPlatform("Platform_Horno_1", new Vector3(72f, 0.4f, 0f), new Vector3(1.2f, 1f, 1f), refs, Color.white);
        CreateBreakableTalaveraPlatform("Talavera_Atajo_1", new Vector3(79f, 1.8f, 0f), refs.spriteLitMaterial);
        CreateSolidPlatform("Platform_Horno_2", new Vector3(95f, -0.4f, 0f), new Vector3(1.2f, 1f, 1f), refs, Color.white);
        CreateBreakableTalaveraPlatform("Talavera_Atajo_2", new Vector3(103f, 0.9f, 0f), refs.spriteLitMaterial);

        CreateSolidPlatform("Platform_ArenaN2_Left",  new Vector3(123f, -0.3f, 0f), new Vector3(1.1f, 1f, 1f), refs, Color.white);
        CreateSolidPlatform("Platform_ArenaN2_Right", new Vector3(137f, -0.3f, 0f), new Vector3(1.1f, 1f, 1f), refs, Color.white);

        // 5. Trampas de Lava, Trampolines y Jarrones Explosivos
        CreateSpikeHazard("Hazard_Horno_1", new Vector3(37.0f, -4.82f, 0f), new Vector3(16f, 0.35f, 1f), refs.spriteLitMaterial);
        CreateSpikeHazard("Hazard_Horno_2", new Vector3(56.5f, -4.82f, 0f), new Vector3(15f, 0.35f, 1f), refs.spriteLitMaterial);
        CreateSpeedSpring("Spring_Rescate_Horno", new Vector3(46.5f, -2.95f, 0f), new Vector2(0f, 14.5f), refs.spriteLitMaterial);
        CreateSpeedSpring("Spring_Taller_Alto",   new Vector3(84.5f, -1.95f, 0f), new Vector2(0f, 13.5f), refs.spriteLitMaterial);

        CreateExplosivePot("Jarron_Explosivo_1", new Vector3(15f, -2.0f, 0f), refs.spriteLitMaterial);
        CreateExplosivePot("Jarron_Explosivo_2", new Vector3(68f, -2.0f, 0f), refs.spriteLitMaterial);
        CreateExplosivePot("Jarron_Explosivo_3", new Vector3(92f, -3.0f, 0f), refs.spriteLitMaterial);
        CreateExplosivePot("Jarron_Explosivo_4", new Vector3(108f, -3.0f, 0f), refs.spriteLitMaterial);

        // 6. Esbirros de Talavera y Coleccionables
        CreateTalaveraEnemy("Esbirro_Talavera_1", new Vector3(12f, -2.0f, 0f), refs.spriteLitMaterial);
        CreateTalaveraEnemy("Esbirro_Talavera_2", new Vector3(24f, -2.0f, 0f), refs.spriteLitMaterial);
        CreateTalaveraEnemy("Esbirro_Talavera_3", new Vector3(72f, 0.8f, 0f), refs.spriteLitMaterial);
        CreateTalaveraEnemy("Esbirro_Talavera_4", new Vector3(80f, -2.0f, 0f), refs.spriteLitMaterial);
        CreateTalaveraEnemy("Esbirro_Talavera_5", new Vector3(97f, -3.0f, 0f), refs.spriteLitMaterial);
        CreateTalaveraEnemy("Esbirro_Talavera_6", new Vector3(105f, -3.0f, 0f), refs.spriteLitMaterial);

        CreateCollectible("Coin_N2_1", new Vector3(10f, 1.3f, 0f), CollectibleItem.ItemType.Coin, refs.spriteLitMaterial);
        CreateCollectible("Coin_N2_2", new Vector3(20f, 2.4f, 0f), CollectibleItem.ItemType.Coin, refs.spriteLitMaterial);
        CreateCollectible("Coin_N2_3", new Vector3(38f, 0.7f, 0f), CollectibleItem.ItemType.Coin, refs.spriteLitMaterial);
        CreateCollectible("Coin_N2_4", new Vector3(58.5f, 0.9f, 0f), CollectibleItem.ItemType.Coin, refs.spriteLitMaterial);
        CreateCollectible("Coin_N2_5", new Vector3(79f, 2.8f, 0f), CollectibleItem.ItemType.Coin, refs.spriteLitMaterial);
        CreateCollectible("Coin_N2_6", new Vector3(103f, 1.9f, 0f), CollectibleItem.ItemType.Coin, refs.spriteLitMaterial);
        CreateCollectible("Heart_N2_Mid",     new Vector3(46.5f, -1.8f, 0f), CollectibleItem.ItemType.Heart, refs.spriteLitMaterial);
        CreateCollectible("Heart_N2_PreBoss", new Vector3(112.5f, -2.1f, 0f), CollectibleItem.ItemType.Heart, refs.spriteLitMaterial);

        // 7. Arena del Jefe 2: El Munícipe Carismático
        GameObject entranceGate = CreateInvisibleWall("Arena_Entrance_Gate", new Vector3(116.2f, 1.0f, 0f), new Vector2(1.2f, 14f));
        entranceGate.SetActive(false);
        CreateInvisibleWall("Arena_Back_Wall", new Vector3(145.5f, 1.0f, 0f), new Vector2(1.2f, 14f));

        LevelPortalTrigger portal = CreateLevelPortal(
            "Portal_FinNivel2",
            new Vector3(143.0f, -3.0f, 0f),
            2,
            "Nivel3",
            "EL MUNÍCIPE CARISMÁTICO",
            "¡Los Talleres de Talavera han sido liberados!",
            refs.spriteLitMaterial
        );

        GameObject bossObj = new GameObject("Boss_MunicipeCarismatico");
        bossObj.transform.position = new Vector3(136f, -3.0f, 0f);
        SpriteRenderer bossSr = bossObj.AddComponent<SpriteRenderer>();
        bossSr.sortingOrder = 4;
        if (refs.spriteLitMaterial != null) bossSr.sharedMaterial = refs.spriteLitMaterial;

        CharismaticMayorBoss mayorBoss = bossObj.AddComponent<CharismaticMayorBoss>();
        mayorBoss.playerTransform = refs.player != null ? refs.player.transform : null;
        mayorBoss.arenaGate = entranceGate;
        mayorBoss.levelPortal = portal;
        mayorBoss.arenaTriggerX = 118f;
        mayorBoss.arenaLeftBound = 117f;
        mayorBoss.arenaRightBound = 143f;
        mayorBoss.arenaCameraPosition = new Vector3(130f, -0.5f, -10f);
        mayorBoss.arenaCameraOrthoSize = 6.5f;

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, PathNivel2);
        CuartaTransformacionMenuBuilder.RegisterScenesInBuildSettings();
        AssetDatabase.SaveAssets();

        Debug.Log("✅ [WorldBuilder] Escenario Nivel 2 Nocturno Colonial-Medieval generado en " + PathNivel2);
        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Nivel 2 Generado con Éxito",
                "Se creó Assets/Scenes/Nivel2.unity (Talleres de Talavera Nocturnos) con:\n\n" +
                "• Fondo pixel-art nocturno exclusivo de hornos y cerámica de Talavera\n" +
                "• Arcos medievales, antorchas animadas, almenas, plataformas quebradizas y jarrones explosivos\n" +
                "• Arena final con El Munícipe Carismático y Portal al Nivel 3",
                "¡Excelente!"
            );
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // MENÚ 3: GENERAR ESCENARIO NIVEL 3 — CALLEJONES DE GUANAJUATO (NOCHE MEDIEVAL)
    // ═══════════════════════════════════════════════════════════════════════════

    [MenuItem("Tools/Cuarta Transformacion/Generar Escenario Nivel 3 (Callejones de Guanajuato)", false, 11)]
    public static void GenerateLevel3Menu()
    {
        GenerateLevel3Scene(true);
    }

    public static void GenerateLevel3Scene(bool showDialog)
    {
        MedievalPropSprites props = LoadMedievalProps();
        Sprite nightBgN3 = CuartaTransformacionMenuBuilder.ImportAndLoadCrispSprite(BgPathNivel3, null, 100f);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SharedSceneRefs refs = CloneCoreObjectsFromNivel1(scene, new Vector3(0f, 1.5f, 0f));

        // 1. Fondo exclusivo pixel-art nocturno de los Callejones de Guanajuato
        CreateThemedBackground(
            refs,
            nightBgN3,
            Color.white,
            new Color(0.06f, 0.04f, 0.14f, 1f)
        );

        // 2. Grid + Tilemap escalonado con suelo exclusivo de Adoquín de Guanajuato + CompositeCollider2D
        Tilemap suelo = CreateGroundTilemap(Color.white);
        GroundTileSet n3Ground = LoadOrCreateThemedGroundTiles(3);
        TileBase tileTop    = n3Ground.top;
        TileBase tileFill   = n3Ground.fill;
        TileBase tileBottom = n3Ground.bottom;

        PaintFloorBlock(suelo, -10, -9, -8, 8, tileTop, tileFill, tileBottom);
        PaintFloorBlock(suelo, -8, 10, -8, -3, tileTop, tileFill, tileBottom);  // top y = -2
        PaintFloorBlock(suelo, 11, 19, -8, -2, tileTop, tileFill, tileBottom);  // top y = -1
        PaintFloorBlock(suelo, 20, 28, -8, -1, tileTop, tileFill, tileBottom);  // top y = 0
        PaintFloorBlock(suelo, 29, 72, -8, -4, tileTop, tileFill, tileBottom);  // callejón bajo top y = -3
        PaintFloorBlock(suelo, 73, 84, -8, 0, tileTop, tileFill, tileBottom);   // azotea 1 top y = 1
        PaintFloorBlock(suelo, 85, 89, -8, -6, tileTop, tileFill, tileBottom);  // foso 1
        PaintFloorBlock(suelo, 90, 101, -8, 1, tileTop, tileFill, tileBottom);  // azotea 2 top y = 2
        PaintFloorBlock(suelo, 102, 106, -8, -6, tileTop, tileFill, tileBottom);// foso 2
        PaintFloorBlock(suelo, 107, 117, -8, -1, tileTop, tileFill, tileBottom);// acceso arena top y = 0
        PaintFloorBlock(suelo, 118, 151, -8, -1, tileTop, tileFill, tileBottom);// arena final top y = 0
        PaintFloorBlock(suelo, 148, 152, -8, 10, tileTop, tileFill, tileBottom);// muro cierre
        suelo.RefreshAllTiles();
        EnsureSeamlessCompositeGround(suelo.gameObject);

        // 3. Decoración Colonial-Medieval Nocturna de Callejones de Guanajuato
        GameObject decorRoot = new GameObject("MedievalDecor_Nivel3");
        PlaceStoneArch(decorRoot.transform, "Arch_N3_Entrada",   new Vector3(-1f, -2f, 0f), 1.05f, props.stoneArch, refs.spriteLitMaterial);
        PlaceStoneArch(decorRoot.transform, "Arch_N3_Callejon",  new Vector3(22f, 0f, 0f), 1.10f, props.stoneArch, refs.spriteLitMaterial);
        PlaceStoneArch(decorRoot.transform, "Arch_N3_Azotea",    new Vector3(75f, 1f, 0f), 1.05f, props.stoneArch, refs.spriteLitMaterial);
        PlaceStoneArch(decorRoot.transform, "Arch_N3_Arena",     new Vector3(118.5f, 0f, 0f), 1.20f, props.stoneArch, refs.spriteLitMaterial);

        PlaceBattlement(decorRoot.transform, "Almena_N3_1", new Vector3(7f, -2f, 0f), props.battlement, refs.spriteLitMaterial);
        PlaceBattlement(decorRoot.transform, "Almena_N3_2", new Vector3(81f, 1f, 0f), props.battlement, refs.spriteLitMaterial);
        PlaceBattlement(decorRoot.transform, "Almena_N3_3", new Vector3(98f, 2f, 0f), props.battlement, refs.spriteLitMaterial);
        PlaceBattlement(decorRoot.transform, "Almena_N3_4", new Vector3(123f, 0f, 0f), props.battlement, refs.spriteLitMaterial);
        PlaceBattlement(decorRoot.transform, "Almena_N3_5", new Vector3(141f, 0f, 0f), props.battlement, refs.spriteLitMaterial);

        PlaceAnimatedTorch(decorRoot.transform, "Antorcha_N3_1", new Vector3(4f, -2f, 0f), props, refs.spriteLitMaterial);
        PlaceAnimatedTorch(decorRoot.transform, "Antorcha_N3_2", new Vector3(15f, -1f, 0f), props, refs.spriteLitMaterial);
        PlaceAnimatedTorch(decorRoot.transform, "Antorcha_N3_3", new Vector3(26f, 0f, 0f), props, refs.spriteLitMaterial);
        PlaceAnimatedTorch(decorRoot.transform, "Antorcha_N3_4", new Vector3(50f, 4.7f, 0f), props, refs.spriteLitMaterial);
        PlaceAnimatedTorch(decorRoot.transform, "Antorcha_N3_5", new Vector3(78f, 1f, 0f), props, refs.spriteLitMaterial);
        PlaceAnimatedTorch(decorRoot.transform, "Antorcha_N3_6", new Vector3(95f, 2f, 0f), props, refs.spriteLitMaterial);
        PlaceAnimatedTorch(decorRoot.transform, "Antorcha_N3_7", new Vector3(121f, 0f, 0f), props, refs.spriteLitMaterial);
        PlaceAnimatedTorch(decorRoot.transform, "Antorcha_N3_8", new Vector3(132f, 0f, 0f), props, refs.spriteLitMaterial);
        PlaceAnimatedTorch(decorRoot.transform, "Antorcha_N3_9", new Vector3(143f, 0f, 0f), props, refs.spriteLitMaterial);

        PlaceLanternPost(decorRoot.transform, "Farol_N3_1", new Vector3(12f, -1f, 0f), props.lanternPost, refs.spriteLitMaterial);
        PlaceLanternPost(decorRoot.transform, "Farol_N3_2", new Vector3(110f, 0f, 0f), props.lanternPost, refs.spriteLitMaterial);
        PlaceLanternPost(decorRoot.transform, "Farol_N3_3", new Vector3(127f, 0f, 0f), props.lanternPost, refs.spriteLitMaterial);
        PlaceLanternPost(decorRoot.transform, "Farol_N3_4", new Vector3(137f, 0f, 0f), props.lanternPost, refs.spriteLitMaterial);

        PlaceSwayingBanner(decorRoot.transform, "Estandarte_N3_1", new Vector3(124.5f, 2.55f, 0f), props.banner, refs.spriteLitMaterial);
        PlaceSwayingBanner(decorRoot.transform, "Estandarte_N3_2", new Vector3(139.5f, 2.55f, 0f), props.banner, refs.spriteLitMaterial);
        PlacePapelPicadoOverhead(decorRoot.transform, "PapelPicado_Arena_N3", new Vector3(132f, 6.8f, 0f), 2.9f, props.papelPicado, refs.spriteLitMaterial);

        // 4. Balcones coloniales de piedra y herrería de Guanajuato, azoteas frágiles y arena final
        if (props.platGuanajuato != null)
            refs.platformSprite = props.platGuanajuato;

        CreateSolidPlatform("Balcon_Callejon_1", new Vector3(15f, 1.2f, 0f), new Vector3(1.0f, 1f, 1f), refs, Color.white);
        CreateSolidPlatform("Balcon_Callejon_2", new Vector3(33f, 0.2f, 0f), new Vector3(1.1f, 1f, 1f), refs, Color.white);
        CreateSolidPlatform("Balcon_Callejon_3", new Vector3(41f, 2.5f, 0f), new Vector3(1.15f, 1f, 1f), refs, Color.white);
        CreateSolidPlatform("Balcon_Callejon_4", new Vector3(50f, 4.6f, 0f), new Vector3(1.25f, 1f, 1f), refs, Color.white);
        CreateSolidPlatform("Balcon_Callejon_5", new Vector3(60f, 3.0f, 0f), new Vector3(1.15f, 1f, 1f), refs, Color.white);
        CreateSolidPlatform("Balcon_Callejon_6", new Vector3(68f, 1.5f, 0f), new Vector3(1.1f, 1f, 1f), refs, Color.white);

        CreateBreakableTalaveraPlatform("Azotea_TejaFragil_1", new Vector3(87f, 1.4f, 0f), refs.spriteLitMaterial);
        CreateBreakableTalaveraPlatform("Azotea_TejaFragil_2", new Vector3(104f, 1.8f, 0f), refs.spriteLitMaterial);

        CreateSolidPlatform("Platform_ArenaN3_Left",  new Vector3(124.5f, 2.6f, 0f), new Vector3(1.15f, 1f, 1f), refs, Color.white);
        CreateSolidPlatform("Platform_ArenaN3_Right", new Vector3(139.5f, 2.6f, 0f), new Vector3(1.15f, 1f, 1f), refs, Color.white);

        // 5. Trampolines, Trampas y Reflectores
        CreateSpeedSpring("Spring_Callejon_1",     new Vector3(31f, -2.95f, 0f), new Vector2(0f, 15.0f), refs.spriteLitMaterial);
        CreateSpeedSpring("Spring_Callejon_2",     new Vector3(44f, -2.95f, 0f), new Vector2(0f, 16.5f), refs.spriteLitMaterial);
        CreateSpeedSpring("Spring_Callejon_3",     new Vector3(65f, -2.95f, 0f), new Vector2(0f, 15.5f), refs.spriteLitMaterial);
        CreateSpeedSpring("Spring_ArenaN3_Centro", new Vector3(132f, 0.05f, 0f), new Vector2(0f, 13.5f), refs.spriteLitMaterial);

        CreateSpikeHazard("Hazard_Callejon_Bajo1", new Vector3(37.5f, -2.82f, 0f), new Vector3(6f, 0.35f, 1f), refs.spriteLitMaterial);
        CreateSpikeHazard("Hazard_Callejon_Bajo2", new Vector3(54f, -2.82f, 0f), new Vector3(9f, 0.35f, 1f), refs.spriteLitMaterial);
        CreateSpikeHazard("Hazard_Azotea_Foso1",   new Vector3(87.5f, -4.82f, 0f), new Vector3(5f, 0.35f, 1f), refs.spriteLitMaterial);
        CreateSpikeHazard("Hazard_Azotea_Foso2",   new Vector3(104.5f, -4.82f, 0f), new Vector3(5f, 0.35f, 1f), refs.spriteLitMaterial);

        CreateSpotlight("Reflector_Callejon_1", new Vector3(24f, 3.2f, 0f), 0f, refs.spriteLitMaterial);
        CreateSpotlight("Reflector_Callejon_2", new Vector3(50f, 7.6f, 0f), 1.4f, refs.spriteLitMaterial);
        CreateSpotlight("Reflector_Azotea_1",   new Vector3(79f, 4.2f, 0f), 2.5f, refs.spriteLitMaterial);
        CreateSpotlight("Reflector_Azotea_2",   new Vector3(96f, 5.2f, 0f), 0.7f, refs.spriteLitMaterial);

        // 6. Enemigos exclusivos del Nivel 3 (Mini-Drones Paparazzi + Fans de Selfies) y Coleccionables
        CreateDroneEnemy("Dron_Paparazzi_1", new Vector3(16f, 2.5f, 0f), refs.spriteLitMaterial);
        CreateDroneEnemy("Dron_Paparazzi_2", new Vector3(38f, 4.5f, 0f), refs.spriteLitMaterial);
        CreateDroneEnemy("Dron_Paparazzi_3", new Vector3(56f, 6.2f, 0f), refs.spriteLitMaterial);
        CreateDroneEnemy("Dron_Paparazzi_4", new Vector3(81f, 4.2f, 0f), refs.spriteLitMaterial);
        CreateDroneEnemy("Dron_Paparazzi_5", new Vector3(98f, 5.0f, 0f), refs.spriteLitMaterial);
        CreateDroneEnemy("Dron_Paparazzi_6", new Vector3(111f, 3.2f, 0f), refs.spriteLitMaterial);
        CreateSelfieFanEnemy("Fan_Callejon_1", new Vector3(14f, -1.0f, 0f), refs.spriteLitMaterial);
        CreateSelfieFanEnemy("Fan_Callejon_2", new Vector3(77f, 1.0f, 0f), refs.spriteLitMaterial);
        CreateSelfieFanEnemy("Fan_Callejon_3", new Vector3(94f, 2.0f, 0f), refs.spriteLitMaterial);
        CreateSelfieFanEnemy("Fan_Callejon_4", new Vector3(111f, 0.0f, 0f), refs.spriteLitMaterial);

        CreateCollectible("Coin_N3_1", new Vector3(15f, 2.1f, 0f), CollectibleItem.ItemType.Coin, refs.spriteLitMaterial);
        CreateCollectible("Coin_N3_2", new Vector3(41f, 3.4f, 0f), CollectibleItem.ItemType.Coin, refs.spriteLitMaterial);
        CreateCollectible("Coin_N3_3", new Vector3(50f, 5.6f, 0f), CollectibleItem.ItemType.Coin, refs.spriteLitMaterial);
        CreateCollectible("Coin_N3_4", new Vector3(60f, 3.9f, 0f), CollectibleItem.ItemType.Coin, refs.spriteLitMaterial);
        CreateCollectible("Coin_N3_5", new Vector3(87f, 2.5f, 0f), CollectibleItem.ItemType.Coin, refs.spriteLitMaterial);
        CreateCollectible("Coin_N3_6", new Vector3(104f, 2.9f, 0f), CollectibleItem.ItemType.Coin, refs.spriteLitMaterial);
        CreateCollectible("Heart_N3_Mid",     new Vector3(50f, 6.2f, 0f), CollectibleItem.ItemType.Heart, refs.spriteLitMaterial);
        CreateCollectible("Heart_N3_PreBoss", new Vector3(114.5f, 0.9f, 0f), CollectibleItem.ItemType.Heart, refs.spriteLitMaterial);

        // 7. Arena Final: El Influencer del Callejón
        GameObject entranceGate = CreateInvisibleWall("Arena_Entrance_Gate", new Vector3(118.2f, 4.0f, 0f), new Vector2(1.2f, 16f));
        entranceGate.SetActive(false);
        CreateInvisibleWall("Arena_Back_Wall", new Vector3(147.5f, 4.0f, 0f), new Vector2(1.2f, 16f));

        LevelPortalTrigger portal = CreateLevelPortal(
            "Portal_FinNivel3",
            new Vector3(144.8f, 0.0f, 0f),
            3,
            "MainMenu",
            "EL INFLUENCER DEL CALLEJÓN",
            "¡VICTORIA TOTAL! Salvaste las Fiestas Patronales de Dolores Hidalgo y Guanajuato.",
            refs.spriteLitMaterial
        );

        GameObject bossObj = new GameObject("Boss_InfluencerCallejon");
        bossObj.transform.position = new Vector3(139f, 0.0f, 0f);
        SpriteRenderer bossSr = bossObj.AddComponent<SpriteRenderer>();
        bossSr.sortingOrder = 4;
        if (refs.spriteLitMaterial != null) bossSr.sharedMaterial = refs.spriteLitMaterial;

        AlleyInfluencerBoss influencerBoss = bossObj.AddComponent<AlleyInfluencerBoss>();
        influencerBoss.playerTransform = refs.player != null ? refs.player.transform : null;
        influencerBoss.arenaGate = entranceGate;
        influencerBoss.levelPortal = portal;
        influencerBoss.arenaTriggerX = 120f;
        influencerBoss.arenaLeftBound = 119f;
        influencerBoss.arenaRightBound = 145f;
        influencerBoss.arenaCameraPosition = new Vector3(132f, 2.5f, -10f);
        influencerBoss.arenaCameraOrthoSize = 6.8f;

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, PathNivel3);
        CuartaTransformacionMenuBuilder.RegisterScenesInBuildSettings();
        AssetDatabase.SaveAssets();

        Debug.Log("✅ [WorldBuilder] Escenario Nivel 3 Nocturno Colonial-Medieval generado en " + PathNivel3);
        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Nivel 3 Generado con Éxito",
                "Se creó Assets/Scenes/Nivel3.unity (Callejones de Guanajuato Nocturnos) con:\n\n" +
                "• Fondo pixel-art nocturno exclusivo del Callejón del Beso y túneles de Guanajuato\n" +
                "• Arcos medievales, antorchas, balcones verticales, trampolines y reflectores\n" +
                "• Arena final con El Influencer del Callejón y Portal de Victoria Final",
                "¡A jugar!"
            );
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // MENÚ 4: CONSTRUIR TODOS LOS NIVELES Y REGISTRAR BUILD SETTINGS (1 CLIC)
    // ═══════════════════════════════════════════════════════════════════════════

    [MenuItem("Tools/Cuarta Transformacion/Construir Todos los Niveles y Registrar Build Settings", false, 20)]
    public static void BuildAllLevelsAndRegisterBuildSettings()
    {
        AssetDatabase.Refresh();

        // 1. Construir Menú Temático No Cuadrado + Arte 3 Jefes + Cinemática de Historia
        CuartaTransformacionMenuBuilder.BuildThematicMainMenuSilent();

        // 2. Mejorar Nivel 1 con Fondo Nocturno Pixel-Art + Decoración Colonial-Medieval + Portal
        UpgradeLevel1MedievalAndNight(false);

        // 3. Generar Nivel 2 (Talleres de Talavera Nocturno Colonial-Medieval)
        GenerateLevel2Scene(false);

        // 4. Generar Nivel 3 (Callejones de Guanajuato Nocturno Colonial-Medieval)
        GenerateLevel3Scene(false);

        // 5. Registrar las 4 escenas en orden en Build Settings
        CuartaTransformacionMenuBuilder.RegisterScenesInBuildSettings();
        AssetDatabase.SaveAssets();

        if (File.Exists(PathMainMenu))
            EditorSceneManager.OpenScene(PathMainMenu, OpenSceneMode.Single);

        Debug.Log("🎉 [WorldBuilder] ¡Juego completo (MainMenu + Cinemática + Niveles 1, 2 y 3 nocturnos medievales) construido y registrado!");
        EditorUtility.DisplayDialog(
            "¡Juego Completo Construido!",
            "Se construyeron y registraron en Build Settings todas las escenas con arte nocturno pixel-art y ambientación colonial-medieval:\n\n" +
            "0. MainMenu.unity (Arte 3 Jefes + Menú NO cuadrado + Cinemática de Historia 4 Actos)\n" +
            "1. Nivel1.unity (Parroquia de Dolores Nocturna + Arcos, Antorchas y El Vendedor Viral)\n" +
            "2. Nivel2.unity (Talleres de Talavera Nocturnos + El Munícipe Carismático)\n" +
            "3. Nivel3.unity (Callejones de Guanajuato Nocturnos + El Influencer del Callejón)\n\n" +
            "¡Presiona Play en MainMenu para ver la cinemática y jugar todo el juego!",
            "¡A darle!"
        );
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PROPS COLONIALES-MEDIEVALES (ARCOS, ANTORCHAS, FAROLES, ALMENAS, ESTANDARTES)
    // ═══════════════════════════════════════════════════════════════════════════

    private static void PlaceStoneArch(Transform parent, string name, Vector3 pos, float scale, Sprite sprite, Material mat)
    {
        if (sprite == null) return;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = -2;
        if (mat != null) sr.sharedMaterial = mat;
    }

    private static void PlaceBattlement(Transform parent, string name, Vector3 pos, Sprite sprite, Material mat)
    {
        if (sprite == null) return;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = -3;
        sr.color = new Color(0.88f, 0.85f, 0.94f, 0.95f);
        if (mat != null) sr.sharedMaterial = mat;
    }

    private static void PlaceAnimatedTorch(Transform parent, string name, Vector3 pos, MedievalPropSprites props, Material mat)
    {
        if (props.torch0 == null) return;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = props.torch0;
        sr.sortingOrder = -1;
        if (mat != null) sr.sharedMaterial = mat;

        MedievalPropAnimator anim = go.AddComponent<MedievalPropAnimator>();
        anim.propStyle = MedievalPropAnimator.PropStyle.TorchFlicker;
        anim.frames = props.torch1 != null ? new[] { props.torch0, props.torch1 } : new[] { props.torch0 };
        anim.animationFps = 8f;
    }

    private static void PlaceLanternPost(Transform parent, string name, Vector3 pos, Sprite sprite, Material mat)
    {
        if (sprite == null) return;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = -1;
        if (mat != null) sr.sharedMaterial = mat;

        MedievalPropAnimator anim = go.AddComponent<MedievalPropAnimator>();
        anim.propStyle = MedievalPropAnimator.PropStyle.LanternGlowPulse;
    }

    private static void PlaceSwayingBanner(Transform parent, string name, Vector3 pos, Sprite sprite, Material mat)
    {
        if (sprite == null) return;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = -1;
        if (mat != null) sr.sharedMaterial = mat;

        MedievalPropAnimator anim = go.AddComponent<MedievalPropAnimator>();
        anim.propStyle = MedievalPropAnimator.PropStyle.BannerSway;
    }

    private static void PlacePapelPicadoOverhead(Transform parent, string name, Vector3 pos, float scaleX, Sprite sprite, Material mat)
    {
        if (sprite == null) return;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(scaleX, 1.4f, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = -1;
        if (mat != null) sr.sharedMaterial = mat;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UTILIDADES DE CLONADO Y CONSTRUCCIÓN DE ENTIDADES
    // ═══════════════════════════════════════════════════════════════════════════

    private static SharedSceneRefs CloneCoreObjectsFromNivel1(Scene targetScene, Vector3 playerSpawnPos)
    {
        SharedSceneRefs refs = new SharedSceneRefs();

        if (File.Exists(PathNivel1))
        {
            Scene srcScene = EditorSceneManager.OpenScene(PathNivel1, OpenSceneMode.Additive);
            GameObject[] roots = srcScene.GetRootGameObjects();

            foreach (GameObject root in roots)
            {
                if (root == null) continue;

                if (root.name == "Player" && refs.player == null)
                {
                    GameObject clone = UnityEngine.Object.Instantiate(root);
                    clone.name = "Player";
                    clone.transform.position = playerSpawnPos;
                    EditorSceneManager.MoveGameObjectToScene(clone, targetScene);
                    refs.player = clone;

                    SpriteRenderer sr = clone.GetComponent<SpriteRenderer>();
                    if (sr != null && sr.sharedMaterial != null)
                        refs.spriteLitMaterial = sr.sharedMaterial;
                }
                else if (root.name == "Main Camera" && refs.mainCamera == null)
                {
                    GameObject clone = UnityEngine.Object.Instantiate(root);
                    clone.name = "Main Camera";
                    clone.transform.position = new Vector3(playerSpawnPos.x, playerSpawnPos.y, -10f);
                    EditorSceneManager.MoveGameObjectToScene(clone, targetScene);
                    refs.mainCamera = clone.GetComponent<Camera>();
                }
                else if (root.name == "Global Light 2D")
                {
                    GameObject clone = UnityEngine.Object.Instantiate(root);
                    clone.name = "Global Light 2D";
                    EditorSceneManager.MoveGameObjectToScene(clone, targetScene);
                }
                else if (root.name == "Platform_Arena_Left" && refs.platformSprite == null)
                {
                    SpriteRenderer psr = root.GetComponent<SpriteRenderer>();
                    if (psr != null) refs.platformSprite = psr.sprite;
                }
            }

            EditorSceneManager.CloseScene(srcScene, true);
        }

        if (refs.mainCamera == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            camObj.transform.position = new Vector3(playerSpawnPos.x, playerSpawnPos.y, -10f);
            refs.mainCamera = camObj.AddComponent<Camera>();
            refs.mainCamera.orthographic = true;
            refs.mainCamera.orthographicSize = 4.2f;
            camObj.AddComponent<AudioListener>();
            camObj.AddComponent<CameraFollow>();
        }

        if (refs.player == null)
            refs.player = CreateFallbackPlayer(playerSpawnPos);

        CameraFollow cf = refs.mainCamera.GetComponent<CameraFollow>() ?? refs.mainCamera.gameObject.AddComponent<CameraFollow>();
        cf.target = refs.player.transform;
        cf.isBossLocked = false;
        cf.maxBounds = new Vector2(Mathf.Max(cf.maxBounds.x, 265f), cf.maxBounds.y);

        if (refs.platformSprite == null)
            refs.platformSprite = LoadSpriteByGuidOrPath(PlatformSpriteGuid, "Assets/Images/Nivel1/Terrain");

        return refs;
    }

    private static GameObject CreateFallbackPlayer(Vector3 spawnPos)
    {
        GameObject player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = spawnPos;

        SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
        Sprite finnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/Nivel1/FinnSprite.png");
        if (finnSprite != null) sr.sprite = finnSprite;

        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        BoxCollider2D col = player.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.4f, 0.55f);

        GameObject gc = new GameObject("GroundCheck");
        gc.transform.SetParent(player.transform, false);
        gc.transform.localPosition = new Vector3(0f, -0.28f, 0f);

        PlayerMovement pm = player.AddComponent<PlayerMovement>();
        pm.groundCheck = gc.transform;
        int groundLayer = LayerMask.NameToLayer("Ground");
        pm.groundLayer = groundLayer >= 0 ? (1 << groundLayer) : ~0;

        Animator anim = player.AddComponent<Animator>();
        RuntimeAnimatorController ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animation/AnimatorController.controller");
        if (ctrl != null) anim.runtimeAnimatorController = ctrl;

        return player;
    }

    private static void CreateThemedBackground(SharedSceneRefs refs, Sprite bgSprite, Color spriteTint, Color cameraBgColor)
    {
        if (refs.mainCamera != null)
            refs.mainCamera.backgroundColor = cameraBgColor;

        GameObject bgObj = new GameObject("Background");
        bgObj.transform.position = new Vector3(0f, 0f, 10f);

        SpriteRenderer sr = bgObj.AddComponent<SpriteRenderer>();
        sr.sprite = bgSprite;
        sr.color = spriteTint;
        sr.sortingOrder = -50;
        if (refs.spriteLitMaterial != null)
            sr.sharedMaterial = refs.spriteLitMaterial;

        ParallaxBackground parallax = bgObj.AddComponent<ParallaxBackground>();
        parallax.targetCamera = refs.mainCamera;
        parallax.followFactorX = 0.98f;
        parallax.followFactorY = 0.95f;
        parallax.smoothSpeed = 14f;
        parallax.autoFitCamera = true;
        parallax.scaleMargin = 1.25f;
        parallax.SnapToCameraImmediate();
    }

    private static Tilemap CreateGroundTilemap(Color tilemapTint)
    {
        GameObject gridObj = new GameObject("Grid");
        gridObj.AddComponent<Grid>();

        GameObject sueloObj = new GameObject("suelo");
        sueloObj.transform.SetParent(gridObj.transform, false);

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0)
            sueloObj.layer = groundLayer;

        Tilemap tm = sueloObj.AddComponent<Tilemap>();
        tm.color = tilemapTint;

        TilemapRenderer tr = sueloObj.AddComponent<TilemapRenderer>();
        tr.sortingOrder = 0;

        EnsureSeamlessCompositeGround(sueloObj);
        return tm;
    }

    private static void PaintFloorBlock(Tilemap tm, int xMin, int xMax, int yMin, int yMax, TileBase top, TileBase fill, TileBase bottom)
    {
        if (tm == null || top == null)
            return;

        TileBase f = fill != null ? fill : top;
        TileBase b = bottom != null ? bottom : f;

        for (int x = xMin; x <= xMax; x++)
        {
            for (int y = yMin; y <= yMax; y++)
            {
                TileBase chosen = (y == yMax) ? top : (y == yMin ? b : f);
                tm.SetTile(new Vector3Int(x, y, 0), chosen);
            }
        }
    }

    private static GameObject CreateSolidPlatform(string name, Vector3 pos, Vector3 scale, SharedSceneRefs refs, Color tint)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = scale;

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0)
            go.layer = groundLayer;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = refs.platformSprite;
        sr.color = tint;
        sr.sortingOrder = 1;
        if (refs.spriteLitMaterial != null)
            sr.sharedMaterial = refs.spriteLitMaterial;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        if (sr.sprite != null && sr.sprite.name.StartsWith("Platform_Medieval_"))
        {
            col.size = new Vector2(3.46f, 0.40f);
            col.offset = new Vector2(0f, -0.02f);
        }
        else if (sr.sprite == null)
        {
            col.size = new Vector2(1.8f, 0.25f);
        }

        return go;
    }

    private static GameObject CreateBreakableTalaveraPlatform(string name, Vector3 pos, Material mat)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(1.1f, 0.55f, 1f);

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0)
            go.layer = groundLayer;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 2;
        if (mat != null) sr.sharedMaterial = mat;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(3.0f, 1.0f);

        go.AddComponent<BreakableTalaveraPlatform>();
        return go;
    }

    private static GameObject CreateExplosivePot(string name, Vector3 pos, Material mat)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 3;
        if (mat != null) sr.sharedMaterial = mat;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.85f, 1.0f);
        col.offset = new Vector2(0f, 0.5f);

        go.AddComponent<ExplosivePotHazard>();
        return go;
    }

    private static GameObject CreateSpikeHazard(string name, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = scale;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSpriteByGuidOrPath(SpikeSquareGuid, "Assets");
        sr.color = new Color(1.0f, 0.42f, 0.08f, 1f);
        sr.sortingOrder = 1;
        if (mat != null) sr.sharedMaterial = mat;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = Vector2.one;

        HazardSpikes hazard = go.AddComponent<HazardSpikes>();
        hazard.renderAsLava = true;
        return go;
    }

    private static GameObject CreateSpeedSpring(string name, Vector3 pos, Vector2 force, Material mat)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 3;
        if (mat != null) sr.sharedMaterial = mat;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.0f, 0.7f);
        col.offset = new Vector2(0f, 0.35f);

        SpeedSpring sp = go.AddComponent<SpeedSpring>();
        sp.springForce = force;
        return go;
    }

    private static GameObject CreateSpotlight(string name, Vector3 pos, float phaseOffset, Material mat)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 2;
        if (mat != null) sr.sharedMaterial = mat;

        SpotlightHazard hazard = go.AddComponent<SpotlightHazard>();
        hazard.phaseOffset = phaseOffset;
        return go;
    }

    private static GameObject CreateTalaveraEnemy(string name, Vector3 pos, Material mat)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 3;
        if (mat != null) sr.sharedMaterial = mat;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 2.5f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.75f, 0.95f);
        col.offset = new Vector2(0f, 0.48f);

        go.AddComponent<Level2TalaveraEnemy>();
        return go;
    }

    private static GameObject CreateSelfieFanEnemy(string name, Vector3 pos, Material mat)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 3;
        if (mat != null) sr.sharedMaterial = mat;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 2.5f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 1.2f);
        col.offset = new Vector2(0f, 0.6f);

        go.AddComponent<Level3SelfieFanEnemy>();
        return go;
    }

    private static GameObject CreateDroneEnemy(string name, Vector3 pos, Material mat)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 4;
        if (mat != null) sr.sharedMaterial = mat;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.9f, 0.65f);

        go.AddComponent<Level3DroneEnemy>();
        return go;
    }

    private static GameObject CreateCollectible(string name, Vector3 pos, CollectibleItem.ItemType type, Material mat)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 3;
        if (mat != null) sr.sharedMaterial = mat;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.35f;

        CollectibleItem item = go.AddComponent<CollectibleItem>();
        item.itemType = type;
        return go;
    }

    private static GameObject CreateInvisibleWall(string name, Vector3 pos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0)
            go.layer = groundLayer;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = size;
        return go;
    }

    private static LevelPortalTrigger CreateLevelPortal(
        string name,
        Vector3 pos,
        int completedLevel,
        string nextScene,
        string bossName,
        string message,
        Material mat)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 2;
        if (mat != null) sr.sharedMaterial = mat;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.6f, 2.6f);
        col.offset = new Vector2(0f, 1.3f);

        LevelPortalTrigger portal = go.AddComponent<LevelPortalTrigger>();
        portal.completedLevelNumber = completedLevel;
        portal.nextSceneName = nextScene;
        portal.defeatedBossName = bossName;
        portal.victoryMessage = message;
        portal.startUnlocked = false;
        portal.loadNextSceneDirectly = completedLevel < 3;
        return portal;
    }

    private static Sprite LoadSpriteByGuidOrPath(string guid, string fallbackFolder)
    {
        if (!string.IsNullOrEmpty(guid))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path))
            {
                Sprite direct = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (direct != null) return direct;

                UnityEngine.Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (UnityEngine.Object obj in all)
                {
                    if (obj is Sprite sp) return sp;
                }
            }
        }

        if (!string.IsNullOrEmpty(fallbackFolder) && AssetDatabase.IsValidFolder(fallbackFolder))
        {
            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { fallbackFolder });
            if (guids != null && guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
        }

        return null;
    }
}
#endif
