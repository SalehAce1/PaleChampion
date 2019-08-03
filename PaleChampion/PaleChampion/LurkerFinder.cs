using System.Collections;
using System.Reflection;
using HutongGames.PlayMaker.Actions;
using ModCommon;
using ModCommon.Util;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = Modding.Logger;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace PaleChampion
{
    internal class LurkerFinder : MonoBehaviour
    {
        private GameObject newPaleLurk;
        private Texture oldPLTex;
        private void Start()
        {
            USceneManager.activeSceneChanged += SceneChanged;
        }

        private void SceneChanged(Scene arg0, Scene arg1)
        {
            if (arg0.name == "GG_Oblobbles" && arg1.name == "GG_Workshop")
            {
                AudioListener.pause = false;
                newPaleLurk.GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture = oldPLTex;
                GameCameras.instance.cameraFadeFSM.Fsm.SetState("FadeIn");
                Destroy(newPaleLurk.GetComponent<PaleLurker>());
            }

            if (arg1.name == "GG_Workshop") SetStatue();
            
            if (arg1.name != "GG_Oblobbles")
            {
                AudioSource _audMain = PaleChampion.preloadedGO["music box"].GetComponent<AudioSource>();
                GameObject bg = PaleChampion.preloadedGO["music box"].transform.Find("bg music").gameObject;
                AudioSource _audBG = bg.GetComponent<AudioSource>();
                _audMain.Stop();
                _audBG.Stop();
                return;
            }
            if (arg0.name != "GG_Workshop") return;

            StartCoroutine(AddComponent());
        }

        private void SetStatue()
        {
            //Used 56's pale prince code here
            GameObject statue = Instantiate(GameObject.Find("GG_Statue_ElderHu"));
            statue.transform.SetPosition2D(201.2f, statue.transform.GetPositionY());
            var scene = ScriptableObject.CreateInstance<BossScene>();
            scene.sceneName = "GG_Oblobbles";
            var bs = statue.GetComponent<BossStatue>();
            bs.bossScene = scene;
            bs.statueStatePD = "lurkerDaddy";
            //if (tier1) bs.StatueState = 
            var gg = new BossStatue.Completion
            {
                completedTier1 = true,
                seenTier3Unlock = true,
                completedTier2 = true,
                completedTier3 = true,
                isUnlocked = true,
                hasBeenSeen = true,
                usingAltVersion = false
            };
            bs.StatueState = gg;
            var details = new BossStatue.BossUIDetails();
            details.nameKey = details.nameSheet = "LURKER_NAME";
            details.descriptionKey = details.descriptionSheet = "LURKER_DESC";
            bs.bossDetails = details;
            foreach(var i in bs.statueDisplay.GetComponentsInChildren<SpriteRenderer>(true))
            {
                i.sprite = PaleChampion.SPRITES[1];
                var scaleX = i.transform.GetScaleX();
                var scaleY = i.transform.GetScaleY();
                i.transform.SetScaleX(scaleX * 1.5f);
                i.transform.SetScaleY(scaleY * 1.5f);
                i.transform.SetPosition2D(i.transform.GetPositionX()-0.1f, i.transform.GetPositionY()-0.7f);
            }

        }

        private IEnumerator AddComponent()
        {
            yield return null;

            GameObject.Find("_SceneManager").LocateMyFSM("FSM").enabled = false;

            Destroy(GameObject.Find("Mega Fat Bee"));
            Destroy(GameObject.Find("Mega Fat Bee (1)"));

            var xH = HeroController.instance.transform.GetPositionX();
            var yH = HeroController.instance.transform.GetPositionY();
            HeroController.instance.gameObject.transform.SetPosition2D(xH, yH + 5f);

            yield return new WaitForSeconds(0.5f);
            newPaleLurk = Instantiate(PaleChampion.preloadedGO["lurker"]);
            oldPLTex = newPaleLurk.GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture;
            newPaleLurk.SetActive(true);
            newPaleLurk.transform.SetPosition2D(xH + 8f, yH);
            newPaleLurk.AddComponent<PaleLurker>();
            newPaleLurk.LocateMyFSM("Lurker Control").SendEvent("START");
        }

        private void OnDestroy()
        {
            USceneManager.activeSceneChanged -= SceneChanged;
        }

        public static void Log(object o)
        {
            Logger.Log("Lurker Finder" + o);
        }
    }
}