using System.Collections;
using UnityEngine;
using HutongGames.PlayMaker.Actions;
using Logger = Modding.Logger;
using UnityEngine.SceneManagement;
using UObject = UnityEngine.Object;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace PaleChampion
{
    internal class LurkerFinder : MonoBehaviour
    {
        private GameObject Lurker;
        public static GameObject sawOrig;
        Texture _oldTex;
        public static GameObject pillarWD;
        private void Start()
        { 
            Logger.Log("[Pale Champion] Added PaleLurker MonoBehaviour");
            PlayerData.instance.killedPaleLurker = false;

            StartCoroutine(LoadingGameObjects());
        }
        private void Update()
        {
            if (Lurker != null) return;
            if (HeroController.instance.transform.GetPositionX() >= 109.3f && HeroController.instance.transform.GetPositionY() >= 79.4f)
            {
                Lurker = GameObject.Find("Pale Lurker");
            }
            if (Lurker == null) return;
            Lurker.AddComponent<PaleLurker>();
            
        }

        private void OnDestroy()
        {
            Logger.Log("Fix sprites");
            pillarWD.GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture = _oldTex;
        }
        IEnumerator LoadingGameObjects()
        {
            Logger.Log("Loading WD");
            GameManager.instance.LoadScene("GG_White_Defender");
            yield return null;
            foreach (var i in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (i.name == "Dung Pillar (1)")
                {
                    Logger.Log("error1");
                    pillarWD = Instantiate(i);
                }
            }
            if (pillarWD == null)
            {
                Modding.Logger.Log("Dung Pillar not found.");
            }
            else
            {
                Logger.Log("error3");
                DontDestroyOnLoad(pillarWD);
                //ModCommon.GameObjectExtensions.PrintSceneHierarchyTree(pillarWD);
                Destroy(pillarWD.LocateMyFSM("Control"));
                _oldTex = pillarWD.GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture;
                pillarWD.GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture = PaleChampion.SPRITES[0].texture;
                pillarWD.SetActive(false);
                Modding.Logger.Log("Found Dung Pillar.");
            }

            GameManager.instance.LoadScene("White_Palace_05");
            yield return null;

            foreach (var i in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (i.name == "wp_saw (1)")
                {
                    Logger.Log("error2");
                    sawOrig = Instantiate(i);
                }
            }
            if (sawOrig == null)
            {
                Modding.Logger.Log("Saw not found.");
            }
            else
            {
                Logger.Log("error4");
                DontDestroyOnLoad(sawOrig);
                //ModCommon.GameObjectExtensions.PrintSceneHierarchyTree(sawOrig);
                sawOrig.SetActive(false);
                Modding.Logger.Log("Found Saw.");
            }
        }
    }
}