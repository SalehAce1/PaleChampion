using System.Collections;
using UnityEngine;
using HutongGames.PlayMaker.Actions;
using Logger = Modding.Logger;
using UnityEngine.SceneManagement;
using UObject = UnityEngine.Object;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using ModCommon.Util;
using ModCommon;

namespace PaleChampion
{
    internal class LurkerFinder : MonoBehaviour
    {
        //private GameObject Lurker;
        
        GameObject LurkerStatue;
        GameObject statueBase;
        
        
        GameObject newStatueBase;
        string _currentScene = "";
        PlayMakerFSM dreamNail;
        GameObject cameraSt;
        PlayMakerFSM camFSM;

        private void CurrentScne(Scene arg0, Scene arg1)
        {
            _currentScene = arg1.name;
            Logger.Log(_currentScene);
            if (_currentScene == "GG_Workshop")
            {
                Logger.Log("Set base");
                var grimm = GameObject.Find("GG_Statue_Grimm");
                var statueBase = grimm.FindGameObjectInChildren("GG_statues_0001_plinth_02");
                Sprite def = statueBase.GetComponent<SpriteRenderer>().sprite;
                newStatueBase = new GameObject("PCBase");
                newStatueBase.AddComponent<SpriteRenderer>().sprite = def;
                newStatueBase.transform.position = new Vector3(201.3f, statueBase.transform.GetPositionY(), 3f);
                newStatueBase.SetActive(true);

                Logger.Log("Setting up new lurker staue");
                LurkerStatue = new GameObject("LurkerStatue");
                LurkerStatue.AddComponent<SpriteRenderer>().sprite = PaleChampion.SPRITES[1];
                var scaleX = LurkerStatue.transform.GetScaleX();
                var scaleY = LurkerStatue.transform.GetScaleY();
                var scaleZ = LurkerStatue.transform.GetScaleZ();
                LurkerStatue.transform.SetScaleX(scaleX * 1.5f);
                LurkerStatue.transform.SetScaleY(scaleY * 1.5f);
                LurkerStatue.transform.position = new Vector3(201.2f, 37.2f, 3f);
                LurkerStatue.SetActive(true);
            }
        }

        IEnumerator LoadingGameObjects()
        {
            yield return new WaitForSeconds(12f);
            Logger.Log("Setting up Dnail");
            dreamNail = HeroController.instance.gameObject.LocateMyFSM("Dream Nail");
            dreamNail.InsertMethod("End", 0, IsCorrectSpot);
        }
        private void Start()
        { 
            Logger.Log("[Pale Champion] Added PaleLurker MonoBehaviour");
            PlayerData.instance.killedPaleLurker = false;
            //USceneManager.activeSceneChanged += CurrentScne;
            USceneManager.activeSceneChanged += CurrentScne;
            try
            {
                cameraSt = GameObject.Find("tk2dCamera");
                camFSM = cameraSt.LocateMyFSM("CameraFade");
            }
            catch(System.Exception e)
            {
                Logger.Log(e);
            }
            StartCoroutine(LoadingGameObjects());
        }
        string strRep = "";
        void Update()
        {
            
        }
        public void IsCorrectSpot()
        {
            var xH = HeroController.instance.transform.GetPositionX();
            var yH = HeroController.instance.transform.GetPositionY();
            if (_currentScene != "GG_Workshop") return;
            if (xH >= 200f && xH <= 202f && yH >= 34f && yH <= 39f)
            {
                Logger.Log("Dnail go");
                StartCoroutine(SceneDel());
            }
        }

        IEnumerator SceneDel()
        {
            GameCameras.instance.cameraFadeFSM.Fsm.Event("FADE INSTANT");
            GameManager.instance.LoadScene("GG_Oblobbles");
            yield return null; 

            Destroy(GameObject.Find("Mega Fat Bee"));
            Destroy(GameObject.Find("Mega Fat Bee (1)"));

            var xH = HeroController.instance.transform.GetPositionX();
            var yH = HeroController.instance.transform.GetPositionY();
            HeroController.instance.gameObject.transform.SetPosition2D(xH, yH + 5f);

            GameCameras.instance.cameraFadeFSM.Fsm.SetState("FadeIn");
            yield return new WaitForSeconds(0.5f);
            LoadGO.paleLurk.SetActive(true);
            LoadGO.paleLurk.transform.SetPosition2D(xH + 8f, yH);
            LoadGO.paleLurk.AddComponent<PaleLurker>();
            LoadGO.paleLurk.LocateMyFSM("Lurker Control").SendEvent("START");
        }

        private void OnDestroy()
        {
           
        }
    }
}