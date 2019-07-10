using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using JetBrains.Annotations;
using Modding;
using UnityEngine;
using UnityEngine.SceneManagement;
using UObject = UnityEngine.Object;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using HutongGames.PlayMaker.Actions;
using ModCommon.Util;
using ModCommon;

namespace PaleChampion
{
    [UsedImplicitly]
    public class PaleChampion : Mod, ITogglableMod
    {
        public static Dictionary<string, GameObject> preloadedGO = new Dictionary<string, GameObject>();
        public static Texture _oldTexAsp;
        public static Texture _oldTexSpit;

        public static PaleChampion Instance;
        private string _lastScene;

        internal bool IsInHall => _lastScene == "GG_Lurker";
        public static readonly List<Sprite> SPRITES = new List<Sprite>();
        public static readonly List<byte[]> SPRITEBYTE = new List<byte[]>();


        public override List<(string, string)> GetPreloadNames()
        {
            return new List<(string, string)>
            {
                ("Room_Colosseum_Bronze", "Super Spitter Col(Clone)"), //Colosseum Manager\Sprite Cache\Buzzer
                ("Room_Colosseum_Bronze", "Colosseum Manager/Sprite Cache/Spitter Shot R"),
                ("Room_Colosseum_Bronze", "Colosseum Manager/Waves/Arena 8/Colosseum Platform (1)"),
                ("Room_Colosseum_Bronze", "Colosseum Manager/Waves/Wave 7/Colosseum Cage Small"),
                ("Room_Colosseum_Bronze", "Colosseum Manager/Ground Spikes/Colosseum Spike"),
                ("GG_White_Defender", "White Defender/Slam Pillars/Dung Pillar (1)"),
                ("White_Palace_07", "wp_saw (30)"),
                ("White_Palace_07", "wp_trap_spikes (2)"),
                ("GG_Lurker", "Lurker Control/Pale Lurker"),
                ("GG_Lurker", "Lurker Control/Lurker Barb"),
                ("Grimm_Nightmare","Grimm_flare_pillar (1)/Pillar"),
                ("GG_Oblobbles","Mega Fat Bee"),
            };
        }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Log("Storing GOs");
            preloadedGO.Add("aspid", preloadedObjects["Room_Colosseum_Bronze"]["Super Spitter Col(Clone)"]);
            preloadedGO.Add("spit", preloadedObjects["Room_Colosseum_Bronze"]["Colosseum Manager/Sprite Cache/Spitter Shot R"]);
            preloadedGO.Add("platform", preloadedObjects["Room_Colosseum_Bronze"]["Colosseum Manager/Waves/Arena 8/Colosseum Platform (1)"]);
            preloadedGO.Add("cage", preloadedObjects["Room_Colosseum_Bronze"]["Colosseum Manager/Waves/Wave 7/Colosseum Cage Small"]);
            preloadedGO.Add("spike", preloadedObjects["Room_Colosseum_Bronze"]["Colosseum Manager/Ground Spikes/Colosseum Spike"]);
            preloadedGO.Add("dung", preloadedObjects["GG_White_Defender"]["White Defender/Slam Pillars/Dung Pillar (1)"]);
            preloadedGO.Add("saw", preloadedObjects["White_Palace_07"]["wp_saw (30)"]);
            preloadedGO.Add("wp spike", preloadedObjects["White_Palace_07"]["wp_trap_spikes (2)"]);
            preloadedGO.Add("lurker", preloadedObjects["GG_Lurker"]["Lurker Control/Pale Lurker"]);
            preloadedGO.Add("barb", preloadedObjects["GG_Lurker"]["Lurker Control/Lurker Barb"]);
            preloadedGO.Add("pillar", preloadedObjects["Grimm_Nightmare"]["Grimm_flare_pillar (1)/Pillar"]);
            preloadedGO.Add("bomb", null);
            preloadedGO.Add("smoke", null);
            preloadedGO.Add("lurker2", null);
            preloadedGO.Add("ob", preloadedObjects["GG_Oblobbles"]["Mega Fat Bee"]);
            Instance = this;
            Log("Initalizing.");
            ModHooks.Instance.AfterSavegameLoadHook += AfterSaveGameLoad;
            ModHooks.Instance.NewGameHook += AddComponent;
            ModHooks.Instance.LanguageGetHook += LangGet;
            USceneManager.activeSceneChanged += LastScene;
            int ind = 0;
            Assembly asm = Assembly.GetExecutingAssembly();
            MusicLoad.LoadAssets.LoadWavFile();
            foreach (string res in asm.GetManifestResourceNames())
            {
                if (!res.EndsWith(".png"))
                {
                    continue;
                }

                using (Stream s = asm.GetManifestResourceStream(res))
                {
                    if (s == null) continue;

                    byte[] buffer = new byte[s.Length];
                    s.Read(buffer, 0, buffer.Length);
                    s.Dispose();

                    // Create texture from bytes
                    var tex = new Texture2D(1, 1);
                    tex.LoadImage(buffer,true);
                    // Create sprite from texture
                    SPRITEBYTE.Add(buffer);
                    SPRITES.Add(Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f)));

                    Log("Created sprite from embedded image: " + res + " at ind " + ++ind);
                }
            }
            GameManager.instance.gameObject.AddComponent<GOLoader>();
        }
        
        private void LastScene(Scene arg0, Scene arg1) => _lastScene = arg0.name;

        private string LangGet(string key, string sheettitle)
        {
            switch (key)
            {
                case "LURKER_1": return "...This place, a construct of memory and dream...?";
                case "LURKER_2": return "...Power... not stronger but distinct...";
                case "LURKER_3": return "...The Blackwyrm's resonance... faint, only memory here... ?";
                case "LURKER_NAME": return "Pale Champion";
                case "LURKER_DESC": return "Lost memory of the King's great champion.";
                case "OBLOBBLES_MAIN": return "Pale Champion";
                case "GODSEEKER_RADIANCE_STATUE": return "Those great Knights challenged the Blackwyrm, and by defeating it revealed its followers as Fools.<br><page>If death can claim such an ancient thing, then what of our King? Though regarded as deity, could he fail us also?";
                default: return Language.Language.GetInternal(key, sheettitle);
            }
        }

        private void AfterSaveGameLoad(SaveGameData data) => AddComponent();

        private void AddComponent()
        {
            GameManager.instance.gameObject.AddComponent<LurkerFinder>();
        }

        public void Unload()
        {
            ModHooks.Instance.AfterSavegameLoadHook -= AfterSaveGameLoad;
            ModHooks.Instance.NewGameHook -= AddComponent;
            ModHooks.Instance.LanguageGetHook -= LangGet;
            USceneManager.activeSceneChanged -= LastScene;

            // ReSharper disable once Unity.NoNullPropogation
            var x = GameManager.instance?.gameObject.GetComponent<LurkerFinder>();
            if (x == null) return;
            UObject.Destroy(x);
        }
    }
}