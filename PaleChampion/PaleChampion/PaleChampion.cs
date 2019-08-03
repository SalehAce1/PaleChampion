using System;
using System.Diagnostics;
using System.Reflection;
using Modding;
using JetBrains.Annotations;
using ModCommon;
using MonoMod.RuntimeDetour;
using UnityEngine.SceneManagement;
using UnityEngine;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using UObject = UnityEngine.Object;
using System.Collections.Generic;
using System.IO;

namespace PaleChampion
{
    [UsedImplicitly]
    public class PaleChampion : Mod<SaveSettings>, ITogglableMod
    {
        public static Dictionary<string, GameObject> preloadedGO = new Dictionary<string, GameObject>();
        public static Texture _oldTexAsp;
        public static Texture _oldTexSpit;

        public static PaleChampion Instance;
        private string _lastScene;

        internal bool IsInHall => _lastScene == "GG_Lurker";
        public static readonly List<Sprite> SPRITES = new List<Sprite>();
        public static readonly List<byte[]> SPRITEBYTE = new List<byte[]>();

        public override string GetVersion()
        {
            return "1.1.0.0";
        }

        public override List<(string, string)> GetPreloadNames()
        {
            return new List<(string, string)>
            {
                ("Room_Colosseum_Bronze", "Super Spitter Col(Clone)"), //Colosseum Manager\Sprite Cache\Buzzer
                ("Room_Colosseum_Bronze", "Colosseum Manager/Sprite Cache/Spitter Shot R"),
                ("Room_Colosseum_Bronze", "Colosseum Manager/Waves/Arena 8/Colosseum Platform (1)"),
                ("Room_Colosseum_Bronze", "Colosseum Manager/Waves/Wave 7/Colosseum Cage Small"),
                ("Room_Colosseum_Bronze", "Colosseum Manager/Ground Spikes/Colosseum Spike"),
                ("Room_Colosseum_Silver", "Colosseum Manager/Waves/Wave 26 Obble/Colosseum Cage Small (1)"),
                ("GG_White_Defender", "White Defender/Slam Pillars/Dung Pillar (1)"),
                ("White_Palace_07", "wp_saw (30)"),
                ("White_Palace_07", "wp_trap_spikes (2)"),
                ("GG_Lurker", "Lurker Control/Pale Lurker"),
                ("GG_Lurker", "Lurker Control/Lurker Barb"),
                ("Grimm_Nightmare","Grimm_flare_pillar (1)/Pillar"),
                ("Grimm_Nightmare","Grimm_flare_pillar (1)"),
                ("GG_Oblobbles","Mega Fat Bee"),
            };
        }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Log("Storing GOs");
            preloadedGO.Add("flame", preloadedObjects["Grimm_Nightmare"]["Grimm_flare_pillar (1)"]);
            preloadedGO.Add("aspid", preloadedObjects["Room_Colosseum_Bronze"]["Super Spitter Col(Clone)"]);
            preloadedGO.Add("spit", preloadedObjects["Room_Colosseum_Bronze"]["Colosseum Manager/Sprite Cache/Spitter Shot R"]);
            preloadedGO.Add("platform", preloadedObjects["Room_Colosseum_Bronze"]["Colosseum Manager/Waves/Arena 8/Colosseum Platform (1)"]);
            preloadedGO.Add("cage", preloadedObjects["Room_Colosseum_Bronze"]["Colosseum Manager/Waves/Wave 7/Colosseum Cage Small"]);
            preloadedGO.Add("spike", preloadedObjects["Room_Colosseum_Bronze"]["Colosseum Manager/Ground Spikes/Colosseum Spike"]);
            preloadedGO.Add("cage obb", preloadedObjects["Room_Colosseum_Silver"]["Colosseum Manager/Waves/Wave 26 Obble/Colosseum Cage Small (1)"]);
            preloadedGO.Add("dung", preloadedObjects["GG_White_Defender"]["White Defender/Slam Pillars/Dung Pillar (1)"]);
            preloadedGO.Add("saw", preloadedObjects["White_Palace_07"]["wp_saw (30)"]);
            preloadedGO.Add("wp spike", preloadedObjects["White_Palace_07"]["wp_trap_spikes (2)"]);
            preloadedGO.Add("lurker", preloadedObjects["GG_Lurker"]["Lurker Control/Pale Lurker"]);
            preloadedGO.Add("barb", preloadedObjects["GG_Lurker"]["Lurker Control/Lurker Barb"]);
            preloadedGO.Add("pillar", preloadedObjects["Grimm_Nightmare"]["Grimm_flare_pillar (1)/Pillar"]);
            preloadedGO.Add("bomb", null);
            preloadedGO.Add("smoke", null);
            preloadedGO.Add("fire", null);
            preloadedGO.Add("lurker2", null);
            preloadedGO.Add("music box", null);
            preloadedGO.Add("ob", preloadedObjects["GG_Oblobbles"]["Mega Fat Bee"]);
            Instance = this;
            Log("Initalizing.");

            Unload();
            ModHooks.Instance.AfterSavegameLoadHook += AfterSaveGameLoad;
            ModHooks.Instance.NewGameHook += AddComponent;
            ModHooks.Instance.LanguageGetHook += LangGet;
            USceneManager.activeSceneChanged += LastScene;
            ModHooks.Instance.SetPlayerVariableHook += SetVariableHook;
            ModHooks.Instance.GetPlayerVariableHook += GetVariableHook;
            int ind = 0;
            Assembly asm = Assembly.GetExecutingAssembly();
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
                case "LURKER_DESC": return "Zealous god of the Colosseum."; //
                case "OBLOBBLES_MAIN": return "Pale Champion";
                case "GODSEEKER_RADIANCE_STATUE": return "Those great Knights challenged the Blackwyrm, and by defeating it revealed its followers as Fools.<br><page>If death can claim such an ancient thing, then what of our King? Though regarded as deity, could he fail us also?";
                default: return Language.Language.GetInternal(key, sheettitle);
            }
        }

        private void AfterSaveGameLoad(SaveGameData data) => AddComponent();

        private void AddComponent()
        {
            GameManager.instance.gameObject.AddComponent<GOLoader>();
            GameManager.instance.gameObject.AddComponent<LurkerFinder>();
        }

        private object SetVariableHook(Type t, string key, object obj)
        {
            if (key == "lurkerDaddy")
                Settings.Completion = (BossStatue.Completion)obj;
            return obj;
        }

        private object GetVariableHook(Type t, string key, object orig)
        {
            return key == "lurkerDaddy"
                ? Settings.Completion
                : orig;
        }

        public void Unload()
        {
            AudioListener.volume = 1f;
            AudioListener.pause = false;
            ModHooks.Instance.AfterSavegameLoadHook -= AfterSaveGameLoad;
            ModHooks.Instance.NewGameHook -= AddComponent;
            ModHooks.Instance.LanguageGetHook -= LangGet;
            USceneManager.activeSceneChanged -= LastScene;
            ModHooks.Instance.SetPlayerVariableHook -= SetVariableHook;
            ModHooks.Instance.GetPlayerVariableHook -= GetVariableHook;

            // ReSharper disable once Unity.NoNullPropogation
            var x = GameManager.instance?.gameObject.GetComponent<LurkerFinder>();
            if (x == null) return;
            UObject.Destroy(x);
        }
    }
}