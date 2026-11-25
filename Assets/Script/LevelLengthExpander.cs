using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Extiende automáticamente los Niveles 1, 2 y 3 (+85 metros jugables por nivel, ~230–245 m totales)
/// al cargar cada escena, desplazando la arena del jefe final hacia la derecha e insertando
/// un nuevo sector completo con plataformas, fosos de lava, resortes Sonic, enemigos, coleccionables
/// y arquitectura colonial-medieval sin necesidad de reconstruir manualmente las escenas.
/// </summary>
public class LevelLengthExpander : MonoBehaviour
{
    private const float ExtensionShiftX = 85f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void HookSceneLoaded()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryExpandScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryExpandScene(scene);
    }

    private static void TryExpandScene(Scene scene)
    {
        if (scene.name != "Nivel1" && scene.name != "Nivel2" && scene.name != "Nivel3")
            return;

        if (GameObject.Find("_LevelExtendedMarker") != null)
            return;

        if (scene.name == "Nivel1")
        {
            ViralVendorBoss boss = Object.FindAnyObjectByType<ViralVendorBoss>();
            if (boss == null || boss.arenaTriggerX >= 180f)
                return;
            ExpandNivel1(scene, boss);
        }
        else if (scene.name == "Nivel2")
        {
            CharismaticMayorBoss boss = Object.FindAnyObjectByType<CharismaticMayorBoss>();
            if (boss == null || boss.arenaTriggerX >= 180f)
                return;
            ExpandNivel2(scene, boss);
        }
        else if (scene.name == "Nivel3")
        {
            AlleyInfluencerBoss boss = Object.FindAnyObjectByType<AlleyInfluencerBoss>();
            if (boss == null || boss.arenaTriggerX >= 180f)
                return;
            ExpandNivel3(scene, boss);
        }

        GameObject marker = new GameObject("_LevelExtendedMarker");
        marker.hideFlags = HideFlags.HideInHierarchy;

        CameraFollow cf = Object.FindAnyObjectByType<CameraFollow>();
        if (cf != null)
            cf.maxBounds = new Vector2(Mathf.Max(cf.maxBounds.x, 265f), cf.maxBounds.y);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // NIVEL 1 — SECTOR EXTRA: CALZADA DE LOS ARCOS Y PLAZA DEL MERCADO (+85m)
    // ═══════════════════════════════════════════════════════════════════════════

    private static void ExpandNivel1(Scene scene, ViralVendorBoss boss)
    {
        const float splitX = 130.0f;
        ShiftArenaObjects(scene, splitX, ExtensionShiftX);

        boss.arenaTriggerX += ExtensionShiftX;
        boss.arenaLeftBound += ExtensionShiftX;
        boss.arenaRightBound += ExtensionShiftX;
        boss.arenaCameraPosition += new Vector3(ExtensionShiftX, 0f, 0f);

        GameObject sueloObj = GameObject.Find("suelo");
        if (sueloObj != null)
        {
            Tilemap tm = sueloObj.GetComponent<Tilemap>();
            if (tm != null && ExtractGroundTiles(tm, out TileBase top, out TileBase fill, out TileBase bottom))
            {
                ClearTileRect(tm, 128, 252, -9, 12);
                PaintFloorBlock(tm, 128, 154, -9, -5, top, fill, bottom); // yTop = -4
                PaintFloorBlock(tm, 155, 171, -9, -7, top, fill, bottom); // foso lava yTop = -6
                PaintFloorBlock(tm, 162, 164, -9, -5, top, fill, bottom); // isla piedra yTop = -4
                PaintFloorBlock(tm, 172, 196, -9, -4, top, fill, bottom); // plaza elevada yTop = -3
                PaintFloorBlock(tm, 197, 215, -9, -5, top, fill, bottom); // antesala arena yTop = -4
                PaintFloorBlock(tm, 216, 246, -9, -5, top, fill, bottom); // arena desplazada yTop = -4
                PaintFloorBlock(tm, 246, 250, -9, 6,  top, fill, bottom); // muro derecho cierre
                RefreshTilemapCollider(sueloObj, tm);
            }
        }

        Material mat = GetSceneSpriteMaterial();
        Sprite platSprite = LoadMedievalSprite("Platform_Medieval_Cantera.png", 32f, new Vector2(0.5f, 0.82f));
        UpgradeLegacyNivel1Platforms(scene, platSprite, mat);
        Sprite archSprite = LoadMedievalSprite("Medieval_StoneArch.png", 18f, new Vector2(0.5f, 0f));
        Sprite batSprite  = LoadMedievalSprite("Medieval_Battlement_Wall.png", 20f, new Vector2(0.5f, 0f));
        Sprite lanternSp  = LoadMedievalSprite("Medieval_LanternPost.png", 22f, new Vector2(0.5f, 0f));
        Sprite torch0     = LoadMedievalSprite("Medieval_Torch_0.png", 20f, new Vector2(0.5f, 0f));
        Sprite torch1     = LoadMedievalSprite("Medieval_Torch_1.png", 20f, new Vector2(0.5f, 0f));
        Sprite bannerSp   = LoadMedievalSprite("Medieval_Banner_Fiesta.png", 20f, new Vector2(0.5f, 1f));

        // Lava en el nuevo foso profundo
        SpawnLavaPit("Hazard_Ext_N1_1", new Vector3(158.5f, -5.82f, 0f), 7.0f, mat);
        SpawnLavaPit("Hazard_Ext_N1_2", new Vector3(168.5f, -5.82f, 0f), 7.0f, mat);

        // Puentes medievales de cantera
        SpawnPlatform("Plat_Ext_N1_1", new Vector3(140.0f, -1.6f, 0f), 1.05f, platSprite, mat);
        SpawnPlatform("Plat_Ext_N1_2", new Vector3(150.0f, -0.8f, 0f), 1.05f, platSprite, mat);
        SpawnPlatform("Plat_Ext_N1_3", new Vector3(158.5f, -2.5f, 0f), 0.95f, platSprite, mat);
        SpawnPlatform("Plat_Ext_N1_4", new Vector3(168.0f, -2.2f, 0f), 0.95f, platSprite, mat);
        SpawnPlatform("Plat_Ext_N1_5", new Vector3(184.0f, -0.4f, 0f), 1.15f, platSprite, mat);
        SpawnPlatform("Plat_Ext_N1_6", new Vector3(205.0f, -1.6f, 0f), 1.05f, platSprite, mat);

        // Resortes Sonic
        SpawnSpring("Spring_Ext_N1_1", new Vector3(163.0f, -3.95f, 0f), new Vector2(0f, 14.0f), mat);
        SpawnSpring("Spring_Ext_N1_2", new Vector3(170.5f, -5.95f, 0f), new Vector2(0f, 14.5f), mat);

        // Esbirros Garbanzos adicionales
        SpawnGarbanzoEnemy("Esbirro_Ext_N1_1", new Vector3(138.0f, -4.0f, 0f), mat);
        SpawnGarbanzoEnemy("Esbirro_Ext_N1_2", new Vector3(148.0f, -4.0f, 0f), mat);
        SpawnGarbanzoEnemy("Esbirro_Ext_N1_3", new Vector3(180.0f, -3.0f, 0f), mat);
        SpawnGarbanzoEnemy("Esbirro_Ext_N1_4", new Vector3(191.0f, -3.0f, 0f), mat);
        SpawnGarbanzoEnemy("Esbirro_Ext_N1_5", new Vector3(206.0f, -4.0f, 0f), mat);

        // Coleccionables
        SpawnCollectible("Coin_Ext_N1_1", new Vector3(140.0f, -0.7f, 0f), CollectibleItem.ItemType.Coin, mat);
        SpawnCollectible("Coin_Ext_N1_2", new Vector3(150.0f, 0.1f, 0f), CollectibleItem.ItemType.Coin, mat);
        SpawnCollectible("Coin_Ext_N1_3", new Vector3(158.5f, -1.6f, 0f), CollectibleItem.ItemType.Coin, mat);
        SpawnCollectible("Coin_Ext_N1_4", new Vector3(168.0f, -1.3f, 0f), CollectibleItem.ItemType.Coin, mat);
        SpawnCollectible("Coin_Ext_N1_5", new Vector3(184.0f, 0.6f, 0f), CollectibleItem.ItemType.Coin, mat);
        SpawnCollectible("Coin_Ext_N1_6", new Vector3(205.0f, -0.7f, 0f), CollectibleItem.ItemType.Coin, mat);
        SpawnCollectible("Heart_Ext_N1_1", new Vector3(163.0f, -2.6f, 0f), CollectibleItem.ItemType.Heart, mat);
        SpawnCollectible("Heart_Ext_N1_2", new Vector3(210.0f, -3.0f, 0f), CollectibleItem.ItemType.Heart, mat);

        // Decoración colonial-medieval nocturna del tramo nuevo
        Transform decor = EnsureDecorContainer("MedievalDecor_Nivel1");
        SpawnArch(decor, "Arch_Ext_N1_1", new Vector3(134f, -4f, 0f), 1.05f, archSprite, mat);
        SpawnArch(decor, "Arch_Ext_N1_2", new Vector3(174f, -3f, 0f), 1.08f, archSprite, mat);
        SpawnArch(decor, "Arch_Ext_N1_3", new Vector3(199f, -4f, 0f), 1.05f, archSprite, mat);

        SpawnBattlement(decor, "Almena_Ext_N1_1", new Vector3(144f, -4f, 0f), batSprite, mat);
        SpawnBattlement(decor, "Almena_Ext_N1_2", new Vector3(188f, -3f, 0f), batSprite, mat);
        SpawnBattlement(decor, "Almena_Ext_N1_3", new Vector3(211f, -4f, 0f), batSprite, mat);

        SpawnLantern(decor, "Farol_Ext_N1_1", new Vector3(136f, -4f, 0f), lanternSp, mat);
        SpawnLantern(decor, "Farol_Ext_N1_2", new Vector3(178f, -3f, 0f), lanternSp, mat);
        SpawnLantern(decor, "Farol_Ext_N1_3", new Vector3(202f, -4f, 0f), lanternSp, mat);

        SpawnTorch(decor, "Antorcha_Ext_N1_1", new Vector3(147f, -4f, 0f), torch0, torch1, mat);
        SpawnTorch(decor, "Antorcha_Ext_N1_2", new Vector3(162.5f, -4f, 0f), torch0, torch1, mat);
        SpawnTorch(decor, "Antorcha_Ext_N1_3", new Vector3(194f, -3f, 0f), torch0, torch1, mat);
        SpawnTorch(decor, "Antorcha_Ext_N1_4", new Vector3(209f, -4f, 0f), torch0, torch1, mat);

        SpawnBanner(decor, "Estandarte_Ext_N1_1", new Vector3(140f, -1.78f, 0f), bannerSp, mat);
        SpawnBanner(decor, "Estandarte_Ext_N1_2", new Vector3(184f, -0.58f, 0f), bannerSp, mat);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // NIVEL 2 — SECTOR EXTRA: GALERÍA DE LOS GRANDES HORNOS (+85m)
    // ═══════════════════════════════════════════════════════════════════════════

    private static void ExpandNivel2(Scene scene, CharismaticMayorBoss boss)
    {
        const float splitX = 115.5f;
        ShiftArenaObjects(scene, splitX, ExtensionShiftX);

        boss.arenaTriggerX += ExtensionShiftX;
        boss.arenaLeftBound += ExtensionShiftX;
        boss.arenaRightBound += ExtensionShiftX;
        boss.arenaCameraPosition += new Vector3(ExtensionShiftX, 0f, 0f);

        GameObject sueloObj = GameObject.Find("suelo");
        if (sueloObj != null)
        {
            Tilemap tm = sueloObj.GetComponent<Tilemap>();
            if (tm != null && ExtractGroundTiles(tm, out TileBase top, out TileBase fill, out TileBase bottom))
            {
                ClearTileRect(tm, 115, 238, -9, 12);
                PaintFloorBlock(tm, 115, 133, -8, -4, top, fill, bottom); // terraza yTop = -3
                PaintFloorBlock(tm, 134, 173, -8, -6, top, fill, bottom); // gran foso hornos yTop = -5
                PaintFloorBlock(tm, 152, 155, -8, -4, top, fill, bottom); // isla central Talavera yTop = -3
                PaintFloorBlock(tm, 174, 199, -8, -3, top, fill, bottom); // galería alta yTop = -2
                PaintFloorBlock(tm, 200, 233, -8, -4, top, fill, bottom); // arena desplazada yTop = -3
                PaintFloorBlock(tm, 231, 235, -8, 6,  top, fill, bottom); // muro cierre
                RefreshTilemapCollider(sueloObj, tm);
            }
        }

        Material mat = GetSceneSpriteMaterial();
        Sprite platSprite = LoadMedievalSprite("Platform_Medieval_Talavera.png", 32f, new Vector2(0.5f, 0.82f));
        Sprite archSprite = LoadMedievalSprite("Medieval_StoneArch.png", 18f, new Vector2(0.5f, 0f));
        Sprite batSprite  = LoadMedievalSprite("Medieval_Battlement_Wall.png", 20f, new Vector2(0.5f, 0f));
        Sprite lanternSp  = LoadMedievalSprite("Medieval_LanternPost.png", 22f, new Vector2(0.5f, 0f));
        Sprite torch0     = LoadMedievalSprite("Medieval_Torch_0.png", 20f, new Vector2(0.5f, 0f));
        Sprite torch1     = LoadMedievalSprite("Medieval_Torch_1.png", 20f, new Vector2(0.5f, 0f));

        // Lava completa pared-a-pared en el segundo gran foso de hornos
        SpawnLavaPit("Hazard_Ext_N2_1", new Vector3(143.0f, -4.82f, 0f), 18.0f, mat);
        SpawnLavaPit("Hazard_Ext_N2_2", new Vector3(165.0f, -4.82f, 0f), 18.0f, mat);

        // Plataformas de Talavera sólidas y quebradizas
        SpawnPlatform("Plat_Ext_N2_1", new Vector3(124.0f, -0.4f, 0f), 1.15f, platSprite, mat);
        SpawnBreakableTalavera("Talavera_Ext_1", new Vector3(137.5f, -1.6f, 0f), mat);
        SpawnBreakableTalavera("Talavera_Ext_2", new Vector3(143.5f, -0.9f, 0f), mat);
        SpawnBreakableTalavera("Talavera_Ext_3", new Vector3(149.0f, -1.4f, 0f), mat);
        SpawnBreakableTalavera("Talavera_Ext_4", new Vector3(158.5f, -1.5f, 0f), mat);
        SpawnBreakableTalavera("Talavera_Ext_5", new Vector3(164.5f, -0.7f, 0f), mat);
        SpawnBreakableTalavera("Talavera_Ext_6", new Vector3(170.0f, -1.2f, 0f), mat);
        SpawnPlatform("Plat_Ext_N2_2", new Vector3(182.0f, 0.5f, 0f), 1.15f, platSprite, mat);
        SpawnPlatform("Plat_Ext_N2_3", new Vector3(193.0f, 0.2f, 0f), 1.10f, platSprite, mat);

        // Resortes Sonic
        SpawnSpring("Spring_Ext_N2_1", new Vector3(153.5f, -2.95f, 0f), new Vector2(0f, 14.5f), mat);
        SpawnSpring("Spring_Ext_N2_2", new Vector3(197.5f, -1.95f, 0f), new Vector2(0f, 13.0f), mat);

        // Jarrones explosivos y guerreros de Talavera
        SpawnExplosivePot("Jarron_Ext_N2_1", new Vector3(121.0f, -3.0f, 0f), mat);
        SpawnExplosivePot("Jarron_Ext_N2_2", new Vector3(129.0f, -3.0f, 0f), mat);
        SpawnExplosivePot("Jarron_Ext_N2_3", new Vector3(179.0f, -2.0f, 0f), mat);
        SpawnExplosivePot("Jarron_Ext_N2_4", new Vector3(190.0f, -2.0f, 0f), mat);

        SpawnTalaveraEnemy("Esbirro_Talavera_Ext1", new Vector3(124.0f, -3.0f, 0f), mat);
        SpawnTalaveraEnemy("Esbirro_Talavera_Ext2", new Vector3(131.0f, -3.0f, 0f), mat);
        SpawnTalaveraEnemy("Esbirro_Talavera_Ext3", new Vector3(177.0f, -2.0f, 0f), mat);
        SpawnTalaveraEnemy("Esbirro_Talavera_Ext4", new Vector3(186.0f, -2.0f, 0f), mat);
        SpawnTalaveraEnemy("Esbirro_Talavera_Ext5", new Vector3(195.0f, -2.0f, 0f), mat);

        // Coleccionables
        SpawnCollectible("Coin_Ext_N2_1", new Vector3(124.0f, 0.5f, 0f), CollectibleItem.ItemType.Coin, mat);
        SpawnCollectible("Coin_Ext_N2_2", new Vector3(143.5f, 0.2f, 0f), CollectibleItem.ItemType.Coin, mat);
        SpawnCollectible("Coin_Ext_N2_3", new Vector3(164.5f, 0.4f, 0f), CollectibleItem.ItemType.Coin, mat);
        SpawnCollectible("Coin_Ext_N2_4", new Vector3(182.0f, 1.4f, 0f), CollectibleItem.ItemType.Coin, mat);
        SpawnCollectible("Coin_Ext_N2_5", new Vector3(193.0f, 1.1f, 0f), CollectibleItem.ItemType.Coin, mat);
        SpawnCollectible("Heart_Ext_N2_1", new Vector3(153.5f, -1.7f, 0f), CollectibleItem.ItemType.Heart, mat);
        SpawnCollectible("Heart_Ext_N2_2", new Vector3(198.5f, -1.0f, 0f), CollectibleItem.ItemType.Heart, mat);

        Transform decor = EnsureDecorContainer("MedievalDecor_Nivel2");
        SpawnArch(decor, "Arch_Ext_N2_1", new Vector3(119f, -3f, 0f), 1.05f, archSprite, mat);
        SpawnArch(decor, "Arch_Ext_N2_2", new Vector3(175f, -2f, 0f), 1.08f, archSprite, mat);
        SpawnBattlement(decor, "Almena_Ext_N2_1", new Vector3(127f, -3f, 0f), batSprite, mat);
        SpawnBattlement(decor, "Almena_Ext_N2_2", new Vector3(188f, -2f, 0f), batSprite, mat);
        SpawnLantern(decor, "Farol_Ext_N2_1", new Vector3(132f, -3f, 0f), lanternSp, mat);
        SpawnLantern(decor, "Farol_Ext_N2_2", new Vector3(184f, -2f, 0f), lanternSp, mat);
        SpawnTorch(decor, "Antorcha_Ext_N2_1", new Vector3(154f, -3f, 0f), torch0, torch1, mat);
        SpawnTorch(decor, "Antorcha_Ext_N2_2", new Vector3(194f, -2f, 0f), torch0, torch1, mat);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // NIVEL 3 — SECTOR EXTRA: ESCALINATAS DEL CALLEJÓN Y MIRADOR NOCTURNO (+85m)
    // ═══════════════════════════════════════════════════════════════════════════

    private static void ExpandNivel3(Scene scene, AlleyInfluencerBoss boss)
    {
        const float splitX = 117.5f;
        ShiftArenaObjects(scene, splitX, ExtensionShiftX);

        boss.arenaTriggerX += ExtensionShiftX;
        boss.arenaLeftBound += ExtensionShiftX;
        boss.arenaRightBound += ExtensionShiftX;
        boss.arenaCameraPosition += new Vector3(ExtensionShiftX, 0f, 0f);

        GameObject sueloObj = GameObject.Find("suelo");
        if (sueloObj != null)
        {
            Tilemap tm = sueloObj.GetComponent<Tilemap>();
            if (tm != null && ExtractGroundTiles(tm, out TileBase top, out TileBase fill, out TileBase bottom))
            {
                ClearTileRect(tm, 118, 240, -9, 12);
                PaintFloorBlock(tm, 118, 127, -8, -1, top, fill, bottom); // subida yTop = 0
                PaintFloorBlock(tm, 128, 137, -8, 0,  top, fill, bottom); // escalinata yTop = 1
                PaintFloorBlock(tm, 138, 146, -8, -7, top, fill, bottom); // foso 1 yTop = -6
                PaintFloorBlock(tm, 147, 161, -8, 1,  top, fill, bottom); // mirador alto yTop = 2
                PaintFloorBlock(tm, 162, 170, -8, -7, top, fill, bottom); // foso 2 yTop = -6
                PaintFloorBlock(tm, 171, 181, -8, -4, top, fill, bottom); // túnel bajo yTop = -3
                PaintFloorBlock(tm, 182, 189, -8, -7, top, fill, bottom); // foso 3 yTop = -6
                PaintFloorBlock(tm, 190, 202, -8, -1, top, fill, bottom); // acceso arena yTop = 0
                PaintFloorBlock(tm, 203, 236, -8, -1, top, fill, bottom); // arena desplazada yTop = 0
                PaintFloorBlock(tm, 233, 237, -8, 10, top, fill, bottom); // muro cierre
                RefreshTilemapCollider(sueloObj, tm);
            }
        }

        Material mat = GetSceneSpriteMaterial();
        Sprite platSprite = LoadMedievalSprite("Platform_Medieval_Guanajuato.png", 32f, new Vector2(0.5f, 0.82f));
        Sprite archSprite = LoadMedievalSprite("Medieval_StoneArch.png", 18f, new Vector2(0.5f, 0f));
        Sprite batSprite  = LoadMedievalSprite("Medieval_Battlement_Wall.png", 20f, new Vector2(0.5f, 0f));
        Sprite lanternSp  = LoadMedievalSprite("Medieval_LanternPost.png", 22f, new Vector2(0.5f, 0f));
        Sprite torch0     = LoadMedievalSprite("Medieval_Torch_0.png", 20f, new Vector2(0.5f, 0f));
        Sprite torch1     = LoadMedievalSprite("Medieval_Torch_1.png", 20f, new Vector2(0.5f, 0f));

        // Fosos con lava
        SpawnLavaPit("Hazard_Ext_N3_1", new Vector3(142.5f, -5.82f, 0f), 9.0f, mat);
        SpawnLavaPit("Hazard_Ext_N3_2", new Vector3(166.5f, -5.82f, 0f), 9.0f, mat);
        SpawnLavaPit("Hazard_Ext_N3_3", new Vector3(186.0f, -5.82f, 0f), 8.0f, mat);

        // Balcones coloniales + tejas frágiles sobre los fosos
        SpawnPlatform("Balcon_Ext_N3_1", new Vector3(124.0f, 2.2f, 0f), 1.05f, platSprite, mat);
        SpawnPlatform("Balcon_Ext_N3_2", new Vector3(133.0f, 3.3f, 0f), 1.05f, platSprite, mat);
        SpawnBreakableTalavera("Azotea_Ext_N3_1", new Vector3(142.0f, 1.5f, 0f), mat);
        SpawnPlatform("Balcon_Ext_N3_3", new Vector3(154.0f, 4.4f, 0f), 1.15f, platSprite, mat);
        SpawnBreakableTalavera("Azotea_Ext_N3_2", new Vector3(166.0f, 1.2f, 0f), mat);
        SpawnPlatform("Balcon_Ext_N3_4", new Vector3(176.0f, 0.2f, 0f), 1.10f, platSprite, mat);
        SpawnBreakableTalavera("Azotea_Ext_N3_3", new Vector3(185.5f, 0.8f, 0f), mat);
        SpawnPlatform("Balcon_Ext_N3_5", new Vector3(195.0f, 2.3f, 0f), 1.10f, platSprite, mat);

        // Resortes Sonic y Reflectores
        SpawnSpring("Spring_Ext_N3_1", new Vector3(135.5f, 1.05f, 0f), new Vector2(0f, 13.5f), mat);
        SpawnSpring("Spring_Ext_N3_2", new Vector3(173.5f, -2.95f, 0f), new Vector2(0f, 15.5f), mat);
        SpawnSpring("Spring_Ext_N3_3", new Vector3(179.5f, -2.95f, 0f), new Vector2(0f, 15.5f), mat);

        SpawnSpotlight("Reflector_Ext_N3_1", new Vector3(130.0f, 6.2f, 0f), 0.3f, mat);
        SpawnSpotlight("Reflector_Ext_N3_2", new Vector3(154.0f, 7.6f, 0f), 1.6f, mat);
        SpawnSpotlight("Reflector_Ext_N3_3", new Vector3(176.0f, 4.8f, 0f), 2.4f, mat);
        SpawnSpotlight("Reflector_Ext_N3_4", new Vector3(196.0f, 5.4f, 0f), 0.9f, mat);

        // Drones paparazzi + Fans de selfies
        SpawnDroneEnemy("Dron_Ext_N3_1", new Vector3(127.0f, 4.2f, 0f), mat);
        SpawnDroneEnemy("Dron_Ext_N3_2", new Vector3(145.0f, 4.5f, 0f), mat);
        SpawnDroneEnemy("Dron_Ext_N3_3", new Vector3(165.0f, 4.0f, 0f), mat);
        SpawnDroneEnemy("Dron_Ext_N3_4", new Vector3(183.0f, 3.6f, 0f), mat);
        SpawnDroneEnemy("Dron_Ext_N3_5", new Vector3(199.0f, 3.8f, 0f), mat);

        SpawnSelfieFan("Fan_Ext_N3_1", new Vector3(122.0f, 0.0f, 0f), mat);
        SpawnSelfieFan("Fan_Ext_N3_2", new Vector3(152.0f, 2.0f, 0f), mat);
        SpawnSelfieFan("Fan_Ext_N3_3", new Vector3(158.0f, 2.0f, 0f), mat);
        SpawnSelfieFan("Fan_Ext_N3_4", new Vector3(194.0f, 0.0f, 0f), mat);

        // Coleccionables
        SpawnCollectible("Coin_Ext_N3_1", new Vector3(124.0f, 3.1f, 0f), CollectibleItem.ItemType.Coin, mat);
        SpawnCollectible("Coin_Ext_N3_2", new Vector3(133.0f, 4.2f, 0f), CollectibleItem.ItemType.Coin, mat);
        SpawnCollectible("Coin_Ext_N3_3", new Vector3(142.0f, 2.5f, 0f), CollectibleItem.ItemType.Coin, mat);
        SpawnCollectible("Coin_Ext_N3_4", new Vector3(154.0f, 5.3f, 0f), CollectibleItem.ItemType.Coin, mat);
        SpawnCollectible("Coin_Ext_N3_5", new Vector3(166.0f, 2.2f, 0f), CollectibleItem.ItemType.Coin, mat);
        SpawnCollectible("Coin_Ext_N3_6", new Vector3(185.5f, 1.8f, 0f), CollectibleItem.ItemType.Coin, mat);
        SpawnCollectible("Coin_Ext_N3_7", new Vector3(195.0f, 3.2f, 0f), CollectibleItem.ItemType.Coin, mat);
        SpawnCollectible("Heart_Ext_N3_1", new Vector3(154.0f, 3.1f, 0f), CollectibleItem.ItemType.Heart, mat);
        SpawnCollectible("Heart_Ext_N3_2", new Vector3(199.5f, 1.0f, 0f), CollectibleItem.ItemType.Heart, mat);

        Transform decor = EnsureDecorContainer("MedievalDecor_Nivel3");
        SpawnArch(decor, "Arch_Ext_N3_1", new Vector3(120f, 0f, 0f), 1.05f, archSprite, mat);
        SpawnArch(decor, "Arch_Ext_N3_2", new Vector3(149f, 2f, 0f), 1.08f, archSprite, mat);
        SpawnArch(decor, "Arch_Ext_N3_3", new Vector3(192f, 0f, 0f), 1.08f, archSprite, mat);
        SpawnBattlement(decor, "Almena_Ext_N3_1", new Vector3(132f, 1f, 0f), batSprite, mat);
        SpawnBattlement(decor, "Almena_Ext_N3_2", new Vector3(158f, 2f, 0f), batSprite, mat);
        SpawnLantern(decor, "Farol_Ext_N3_1", new Vector3(126f, 0f, 0f), lanternSp, mat);
        SpawnLantern(decor, "Farol_Ext_N3_2", new Vector3(196f, 0f, 0f), lanternSp, mat);
        SpawnTorch(decor, "Antorcha_Ext_N3_1", new Vector3(151f, 2f, 0f), torch0, torch1, mat);
        SpawnTorch(decor, "Antorcha_Ext_N3_2", new Vector3(176f, -3f, 0f), torch0, torch1, mat);
        SpawnTorch(decor, "Antorcha_Ext_N3_3", new Vector3(200f, 0f, 0f), torch0, torch1, mat);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UTILIDADES DE DESPLAZAMIENTO, TILEMAPS Y SPAWNERS
    // ═══════════════════════════════════════════════════════════════════════════

    private static void UpgradeLegacyNivel1Platforms(Scene scene, Sprite platSprite, Material mat)
    {
        if (platSprite == null)
            return;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            GameObject go = roots[i];
            if (go == null || !go.name.StartsWith("Platform_"))
                continue;

            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr != null && (sr.sprite == null || !sr.sprite.name.StartsWith("Platform_Medieval_")))
            {
                sr.sprite = platSprite;
                sr.drawMode = SpriteDrawMode.Simple;
                sr.color = Color.white;
                sr.sortingOrder = 1;
                if (mat != null) sr.sharedMaterial = mat;

                Vector3 sc = go.transform.localScale;
                go.transform.localScale = new Vector3(0.96052f, 1f, sc.z != 0f ? sc.z : 1f);

                BoxCollider2D col = go.GetComponent<BoxCollider2D>();
                if (col != null)
                {
                    col.size = new Vector2(3.46f, 0.40f);
                    col.offset = new Vector2(0f, -0.02f);
                }
            }
        }
    }

    private static void ShiftArenaObjects(Scene scene, float splitX, float shiftX)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            GameObject go = roots[i];
            if (go == null)
                continue;

            string n = go.name;
            if (n == "Grid" || n == "Background" || n == "Main Camera" || n == "Player" ||
                n == "Global Light 2D" || n == "IntroCutscene_Nivel1" ||
                n.StartsWith("_") || n.StartsWith("MedievalDecor_"))
            {
                if (n.StartsWith("MedievalDecor_"))
                {
                    foreach (Transform child in go.transform)
                    {
                        if (child.position.x >= splitX)
                            child.position += new Vector3(shiftX, 0f, 0f);
                    }
                }
                continue;
            }

            if (go.transform.position.x >= splitX)
                go.transform.position += new Vector3(shiftX, 0f, 0f);
        }
    }

    private static bool ExtractGroundTiles(Tilemap tm, out TileBase top, out TileBase fill, out TileBase bottom)
    {
        top = null;
        fill = null;
        bottom = null;

        tm.CompressBounds();
        BoundsInt b = tm.cellBounds;
        for (int x = Mathf.Max(b.xMin, 0); x <= Mathf.Min(b.xMax, 24); x++)
        {
            List<int> ys = new List<int>();
            for (int y = b.yMin; y <= b.yMax; y++)
            {
                if (tm.HasTile(new Vector3Int(x, y, 0)))
                    ys.Add(y);
            }
            if (ys.Count >= 3)
            {
                top = tm.GetTile(new Vector3Int(x, ys[ys.Count - 1], 0));
                fill = tm.GetTile(new Vector3Int(x, ys[ys.Count - 2], 0));
                bottom = tm.GetTile(new Vector3Int(x, ys[0], 0));
                if (top != null)
                    return true;
            }
        }
        return false;
    }

    private static void ClearTileRect(Tilemap tm, int xMin, int xMax, int yMin, int yMax)
    {
        for (int x = xMin; x <= xMax; x++)
            for (int y = yMin; y <= yMax; y++)
                tm.SetTile(new Vector3Int(x, y, 0), null);
    }

    private static void PaintFloorBlock(Tilemap tm, int xMin, int xMax, int yMin, int yMax, TileBase top, TileBase fill, TileBase bottom)
    {
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

    private static void RefreshTilemapCollider(GameObject sueloObj, Tilemap tm)
    {
        tm.RefreshAllTiles();
        TilemapCollider2D tmCol = sueloObj.GetComponent<TilemapCollider2D>();
        if (tmCol != null)
            tmCol.compositeOperation = Collider2D.CompositeOperation.Merge;

        CompositeCollider2D comp = sueloObj.GetComponent<CompositeCollider2D>();
        if (comp != null)
        {
            comp.geometryType = CompositeCollider2D.GeometryType.Polygons;
            comp.generationType = CompositeCollider2D.GenerationType.Synchronous;
            comp.GenerateGeometry();
        }
    }

    private static Material GetSceneSpriteMaterial()
    {
        PlayerMovement pm = Object.FindAnyObjectByType<PlayerMovement>();
        if (pm != null)
        {
            SpriteRenderer sr = pm.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sharedMaterial != null)
                return sr.sharedMaterial;
        }
        return null;
    }

    private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    private static Sprite LoadMedievalSprite(string fileName, float ppu, Vector2 pivot)
    {
        string key = $"{fileName}_{ppu}_{pivot.x}_{pivot.y}";
        if (spriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        string path = Path.Combine(Application.dataPath, "Images/Medieval/" + fileName);
        if (File.Exists(path))
        {
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            if (tex.LoadImage(File.ReadAllBytes(path)))
            {
                Sprite sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, ppu);
                sp.name = Path.GetFileNameWithoutExtension(fileName);
                spriteCache[key] = sp;
                return sp;
            }
        }

        string resName = Path.GetFileNameWithoutExtension(fileName);
        Texture2D resTex = Resources.Load<Texture2D>("Medieval/" + resName);
        if (resTex == null)
            return null;

        resTex.filterMode = FilterMode.Point;
        resTex.wrapMode = TextureWrapMode.Clamp;
        Sprite resSp = Sprite.Create(resTex, new Rect(0, 0, resTex.width, resTex.height), pivot, ppu);
        resSp.name = resName;
        spriteCache[key] = resSp;
        return resSp;
    }

    private static Transform EnsureDecorContainer(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null)
            return existing.transform;
        return new GameObject(name).transform;
    }

    private static void SpawnPlatform(string name, Vector3 pos, float scaleX, Sprite sprite, Material mat)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(scaleX, 1f, 1f);
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0) go.layer = groundLayer;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite != null ? sprite : ProceduralPixelArt.GetSolidTileSprite(
            new Color(0.55f, 0.45f, 0.35f), new Color(0.32f, 0.25f, 0.22f), new Color(0.85f, 0.70f, 0.32f), "ExtPlat");
        sr.sortingOrder = 1;
        if (mat != null) sr.sharedMaterial = mat;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        if (sprite != null)
        {
            col.size = new Vector2(3.46f, 0.40f);
            col.offset = new Vector2(0f, -0.02f);
        }
        else
        {
            col.size = new Vector2(2.0f, 0.45f);
        }
    }

    private static void SpawnBreakableTalavera(string name, Vector3 pos, Material mat)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(1.1f, 0.55f, 1f);
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0) go.layer = groundLayer;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 2;
        if (mat != null) sr.sharedMaterial = mat;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(3.0f, 1.0f);
        go.AddComponent<BreakableTalaveraPlatform>();
    }

    private static void SpawnLavaPit(string name, Vector3 centerPos, float width, Material mat)
    {
        GameObject go = new GameObject(name);
        go.transform.position = centerPos;
        go.transform.localScale = new Vector3(width, 0.35f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.color = new Color(1.0f, 0.42f, 0.08f, 1f);
        sr.sortingOrder = 1;
        if (mat != null) sr.sharedMaterial = mat;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = Vector2.one;

        HazardSpikes hz = go.AddComponent<HazardSpikes>();
        hz.renderAsLava = true;
    }

    private static void SpawnSpring(string name, Vector3 pos, Vector2 force, Material mat)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 3;
        if (mat != null) sr.sharedMaterial = mat;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.95f, 0.5f);
        col.offset = new Vector2(0f, 0.25f);

        SpeedSpring sp = go.AddComponent<SpeedSpring>();
        sp.springForce = force;
    }

    private static void SpawnExplosivePot(string name, Vector3 pos, Material mat)
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
    }

    private static void SpawnSpotlight(string name, Vector3 pos, float phaseOffset, Material mat)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 2;
        if (mat != null) sr.sharedMaterial = mat;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.4f, 4.4f);
        col.offset = new Vector2(0f, -2.2f);

        SpotlightHazard sh = go.AddComponent<SpotlightHazard>();
        sh.phaseOffset = phaseOffset;
    }

    private static void SpawnGarbanzoEnemy(string name, Vector3 pos, Material mat)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(0.82f, 0.82f, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 3;
        if (mat != null) sr.sharedMaterial = mat;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.85f, 0.90f);
        col.offset = new Vector2(0f, 0.45f);

        SimpleEnemy en = go.AddComponent<SimpleEnemy>();
        en.maxHealth = 3;
        en.patrolSpeed = 2.8f;
        en.detectionRange = 12f;
        en.canRespawn = false;
    }

    private static void SpawnTalaveraEnemy(string name, Vector3 pos, Material mat)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 3;
        if (mat != null) sr.sharedMaterial = mat;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.85f, 1.0f);
        col.offset = new Vector2(0f, 0.5f);

        Level2TalaveraEnemy en = go.AddComponent<Level2TalaveraEnemy>();
        en.maxHealth = 3;
        en.canRespawn = false;
    }

    private static void SpawnDroneEnemy(string name, Vector3 pos, Material mat)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 3;
        if (mat != null) sr.sharedMaterial = mat;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.95f, 0.70f);
        col.offset = Vector2.zero;

        Level3DroneEnemy en = go.AddComponent<Level3DroneEnemy>();
        en.maxHealth = 3;
        en.canRespawn = false;
    }

    private static void SpawnSelfieFan(string name, Vector3 pos, Material mat)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 3;
        if (mat != null) sr.sharedMaterial = mat;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.82f, 1.08f);
        col.offset = new Vector2(0f, 0.54f);

        Level3SelfieFanEnemy en = go.AddComponent<Level3SelfieFanEnemy>();
        en.maxHealth = 4;
        en.canRespawn = false;
    }

    private static void SpawnCollectible(string name, Vector3 pos, CollectibleItem.ItemType type, Material mat)
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
    }

    private static void SpawnArch(Transform parent, string name, Vector3 pos, float scale, Sprite sp, Material mat)
    {
        if (sp == null) return;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.sortingOrder = -2;
        if (mat != null) sr.sharedMaterial = mat;
    }

    private static void SpawnBattlement(Transform parent, string name, Vector3 pos, Sprite sp, Material mat)
    {
        if (sp == null) return;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.sortingOrder = -3;
        sr.color = new Color(0.88f, 0.85f, 0.94f, 0.95f);
        if (mat != null) sr.sharedMaterial = mat;
    }

    private static void SpawnLantern(Transform parent, string name, Vector3 pos, Sprite sp, Material mat)
    {
        if (sp == null) return;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.sortingOrder = -1;
        if (mat != null) sr.sharedMaterial = mat;
        MedievalPropAnimator anim = go.AddComponent<MedievalPropAnimator>();
        anim.propStyle = MedievalPropAnimator.PropStyle.LanternGlowPulse;
    }

    private static void SpawnTorch(Transform parent, string name, Vector3 pos, Sprite t0, Sprite t1, Material mat)
    {
        if (t0 == null) return;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = t0;
        sr.sortingOrder = -1;
        if (mat != null) sr.sharedMaterial = mat;
        MedievalPropAnimator anim = go.AddComponent<MedievalPropAnimator>();
        anim.propStyle = MedievalPropAnimator.PropStyle.TorchFlicker;
        anim.frames = t1 != null ? new[] { t0, t1 } : new[] { t0 };
        anim.animationFps = 8f;
    }

    private static void SpawnBanner(Transform parent, string name, Vector3 pos, Sprite sp, Material mat)
    {
        if (sp == null) return;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.sortingOrder = -1;
        if (mat != null) sr.sharedMaterial = mat;
        MedievalPropAnimator anim = go.AddComponent<MedievalPropAnimator>();
        anim.propStyle = MedievalPropAnimator.PropStyle.BannerSway;
    }
}
