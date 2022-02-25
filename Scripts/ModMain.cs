
using Language;

namespace DCQueenLoaderMod;

class DCQueenLoader : ModBase , ILocalSettings<Settings>
{
    public static AssetBundle ab;
    public static Texture2D iconTex;
    public static Sprite iconSpr;
    public static GameObject scenePrefab;
    public static GameObject dreamentry;
    public Settings settings = new();
    void ILocalSettings<Settings>.OnLoadLocal(Settings s) => settings = s;
    Settings ILocalSettings<Settings>.OnSaveLocal() => settings;
    public static GameObject HKPrime;
    public static EnemyHitEffectsUninfected hitEffect;
    public override List<(string, string)> GetPreloadNames()
    {
        return new()
        {
            ("GG_Hollow_Knight", "Battle Scene/HK Prime")
        };
    }
    protected override (LanguageCode, string)[] Languages => new (LanguageCode, string)[]
    {
        (LanguageCode.ZH, "Languages.zh"),
        (LanguageCode.EN, "Languages.en")
    };
    protected override LanguageCode DefaultLanguageCode => LanguageCode.EN;
    public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
    {
        I18n.UseLanguageHook = true;

        ab = LoadAssetBundle("DCQueenLoader.ab");
        scenePrefab = ab.LoadAsset<GameObject>("QueenScene");
        iconTex = ab.LoadAsset<Texture2D>("QueenIcon");
        iconSpr = Sprite.Create(iconTex, new Rect(0, 0,iconTex.width, iconTex.height),
            new Vector2(0.5f, 0), 100);
        HKPrime = preloadedObjects["GG_Hollow_Knight"]["Battle Scene/HK Prime"];
        hitEffect = HKPrime.GetComponent<EnemyHitEffectsUninfected>();

        ModHooks.GetPlayerVariableHook += (type, name, orig) =>
        {
            if(type != typeof(BossStatue.Completion) || name != "statueStateDCQueen") return orig;
            return settings.status;
        };
        ModHooks.SetPlayerVariableHook += (type, name, orig) =>
        {
            if (type != typeof(BossStatue.Completion) || name != "statueStateDCQueen") return orig;
            settings.status = (BossStatue.Completion)orig;
            return orig;
        };
        
        UnityEngine.SceneManagement.SceneManager.activeSceneChanged += (currnet, next) =>
        {
            if(next.name == "GG_Workshop") 
            {
                ModifyStatue(next);
                return;
            }
            if (next.name == "GG_Grimm")
            {
                foreach (var v in next.GetRootGameObjects())
                {
                    if (v.name != "Boss Scene Controller" && !v.name.StartsWith("gg_battle_transitions")
                        && v.GetComponent<TransitionPoint>() == null &&
                         v.GetComponentInChildren<TransitionPoint>() == null)
                    {
                        UnityEngine.Object.DestroyImmediate(v);
                        continue;
                    }
                    else
                    {
                        if (v.name == "Boss Scene Controller")
                        {
                            v.transform.Find("Dream Entry").position = new Vector3(11.2072f, 13.5825f, 0);
                            v.transform.Find("door_dreamEnter").position = new Vector3(11.2072f, 13.5825f, 0);
                        }
                    }
                }
                
                UnityEngine.Object.Instantiate(scenePrefab, new Vector3(10, 10, 0), Quaternion.identity);
                var boss = next.FindGameObject("QueenBoss");

                var he = boss.AddComponent<EnemyHitEffectsUninfected>();
                he.slashEffectGhost1 = hitEffect.slashEffectGhost1;
                he.slashEffectGhost2 = hitEffect.slashEffectGhost2;
                he.uninfectedHitPt = hitEffect.uninfectedHitPt;
                GameObject audios = new GameObject();
                audios.transform.parent = boss.transform;
                he.audioPlayerPrefab = audios.AddComponent<AudioSource>();
                var hm = boss.GetComponent<HealthManager>();
                
                hm.hp = 2400 * Mathf.Max(BossSceneController.Instance?.BossLevel ?? 1 + 1, 1);
                ReflectionHelper.SetField<HealthManager, IHitEffectReciever>(hm, "hitEffectReceiver", he);
                var camLock = next.FindGameObject("BArea").AddComponent<CameraLockArea>();
                camLock.cameraXMax = 30;
                camLock.cameraXMin = 14.6f;
                camLock.cameraYMin = 13.5925f;
                camLock.cameraYMax = 14;
                var control = boss.GetComponent<QueenController>();
                control.StartCoroutine(BattelControl());
                control.OnDeath = () => BossSceneController.Instance.EndBossScene();
                PlayMakerFSM.BroadcastEvent("STOP");
                
                //hm.OnDeath +
                
            }
        };
    }
    private static void ModifyStatue(Scene gg)
    {
        var s = gg.FindGameObject("GG_Statue_Grimm").GetComponent<BossStatue>();
        s.statueStatePD = "statueStateDCQueen";
        s.dreamBossScene = null;
        s.dreamStatueStatePD = "";
        var sd = s.bossDetails;
        sd.nameKey = "NAME_DC_QUEEN";
        sd.descriptionKey = "DESC_DC_QUEEN";
        s.bossDetails = sd;
        var statue = gg.FindGameObject("GG_Statue_Grimm/Base/Statue/GG_statues_0006_5")
            .GetComponent<SpriteRenderer>();
        statue.sprite = iconSpr;
        statue.drawMode = SpriteDrawMode.Sliced;
        statue.size = new Vector2(18, 18);
        statue.transform.position = new Vector3(193.069f, 35.68f, 3.07f);

        var plaqueR = gg.FindGameObject("GG_Statue_Grimm/Base/Plaque/Plaque_Trophy_Right");
        plaqueR?.SetActive(false);
        var plaqueL = gg.FindGameObject("GG_Statue_Grimm/Base/Plaque/Plaque_Trophy_Left");
        plaqueL.transform.position = new Vector3(193.359f, 35.1272f, 1.5323f);
        gg.FindGameObject("GG_Statue_Grimm/dream_version_switch")?.SetActive(false);
    }
    private static IEnumerator BattelControl()
    {
        yield return new WaitForFinishedEnteringScene();
        HeroController.instance.transform.position = new Vector3(11.2072f, 13.5825f, 0);
        BossSceneController.Instance.bossesDeadWaitTime = 0;
        yield return null;
        while(true)
        {
            yield return null;
            if(HeroController.instance.transform.position.y < 0)
            {
                yield return HeroController.instance.HazardRespawn();
            }
        }
    }
}
