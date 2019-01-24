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
    public class PaleChampion : Mod<VoidModSettings>, ITogglableMod
    {
        public static PaleChampion Instance;

        private string _lastScene;

        internal bool IsInHall => _lastScene == "GG_Lurker";
        public static readonly IList<Sprite> SPRITES = new List<Sprite>();
        public static readonly IList<byte[]> SPRITEBYTE = new List<byte[]>();
        
        //public override string GetVersion()
        //{
        //return FileVersionInfo.GetVersionInfo(Assembly.GetAssembly(typeof(PaleChampion)).Location).FileVersion;
        //}

        public override void Initialize()
        {
            Instance = this;

            Log("Initalizing.");
            ModHooks.Instance.AfterSavegameLoadHook += AfterSaveGameLoad;
            ModHooks.Instance.NewGameHook += AddComponent;
            //ModHooks.Instance.LanguageGetHook += LangGet;
            USceneManager.activeSceneChanged += LastScene;
            int ind = 0;
            Assembly asm = Assembly.GetExecutingAssembly();
            MusicLoad.LoadAssets.LoadMusicSound();
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
            GameManager.instance.gameObject.AddComponent<LoadGO>();
            /*Resources.LoadAll<GameObject>("");
            foreach (var i in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (i.name == "Weaverling")
                {
                    weaverPref = i;
                }
            }*/
        }
        
        private void LastScene(Scene arg0, Scene arg1) => _lastScene = arg0.name;
        /*private string LangGet(string key, string sheettitle)
        {
            switch (key)
            {
                case "HORNET_MAIN": return "Daughter";
                case "HORNET_SUB": return "of Hallownest";
                case "NAME_HORNET_2": return "Daughter of Hallownest";
                case "GG_S_HORNET": return "Protector God, birthed, raised, and trained by three great Queens.";
                default: return Language.Language.GetInternal(key, sheettitle);
            }
        }*/

        private void AfterSaveGameLoad(SaveGameData data) => AddComponent();

        private void AddComponent()
        {
            GameManager.instance.gameObject.AddComponent<LurkerFinder>();
        }

        public void Unload()
        {
            ModHooks.Instance.AfterSavegameLoadHook -= AfterSaveGameLoad;
            ModHooks.Instance.NewGameHook -= AddComponent;
            //ModHooks.Instance.LanguageGetHook -= LangGet;
            USceneManager.activeSceneChanged -= LastScene;

            // ReSharper disable once Unity.NoNullPropogation
            var x = GameManager.instance?.gameObject.GetComponent<LurkerFinder>();
            if (x == null) return;
            UObject.Destroy(x);
        }
    }
}