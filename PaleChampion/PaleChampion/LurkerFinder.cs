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
        private void Start()
        {
            USceneManager.activeSceneChanged += SceneChanged;
        }

        private void SceneChanged(Scene arg0, Scene arg1)
        {
            if (arg1.name == "GG_Workshop") SetStatue();
            
            if (arg1.name != "GG_Oblobbles") return;
            if (arg0.name != "GG_Workshop") return;

            StartCoroutine(AddComponent());
        }

        private static void SetStatue()
        {
                //Used 56's pale prince code here
                GameObject statue = Instantiate(GameObject.Find("GG_Statue_ElderHu"));
                statue.transform.SetPosition2D(201.2f, statue.transform.GetPositionY());
                Log("did it");
                var scene = ScriptableObject.CreateInstance<BossScene>();
                scene.sceneName = "GG_Oblobbles";
                Log("did it2");
                var bs = statue.GetComponent<BossStatue>();
                bs.bossScene = scene;
                Log("did it3");
                bs.statueStatePD = "statueStatePure";
                Log("did it4");
                var details = new BossStatue.BossUIDetails();
                details.nameKey = details.nameSheet = "LURKER_NAME";
                details.descriptionKey = details.descriptionSheet = "LURKER_DESC";
                bs.bossDetails = details;
                Log("did it5");
                bs.statueDisplay.PrintSceneHierarchyTree();
                Log("did it 7");
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

        private static IEnumerator AddComponent()
        {
            yield return null;

            Destroy(GameObject.Find("Mega Fat Bee"));
            Destroy(GameObject.Find("Mega Fat Bee (1)"));

            var xH = HeroController.instance.transform.GetPositionX();
            var yH = HeroController.instance.transform.GetPositionY();
            HeroController.instance.gameObject.transform.SetPosition2D(xH, yH + 5f);

            yield return new WaitForSeconds(0.5f);
            var newGO = Instantiate(PaleChampion.preloadedGO["lurker"]);
            newGO.SetActive(true);
            newGO.transform.SetPosition2D(xH + 8f, yH);
            newGO.AddComponent<PaleLurker>();
            newGO.LocateMyFSM("Lurker Control").SendEvent("START");
        }

        private void OnDestroy()
        {
            USceneManager.activeSceneChanged -= SceneChanged;
        }

        public static void Log(object o)
        {
            Logger.Log($"[{Assembly.GetExecutingAssembly().GetName().Name}]: " + o);
        }
    }
}