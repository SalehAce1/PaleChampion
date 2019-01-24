using System.Collections;
using UnityEngine;
using HutongGames.PlayMaker.Actions;
using Logger = Modding.Logger;
using UnityEngine.SceneManagement;
using UObject = UnityEngine.Object;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using ModCommon.Util;
using ModCommon;
using On;
using System;
using MonoMod.RuntimeDetour;
using System.Reflection;

namespace PaleChampion
{
    internal class LoadGO : MonoBehaviour
    {
        public static GameObject[] platformCol = new GameObject[1];
        public static GameObject sawOrig;
        public static GameObject paleLurk;
        public static GameObject pillarWD;
        public static GameObject paleLurkerBarb;
        Texture _oldTex;
        Sprite oldSawTex;

        void Start()
        {
            StartCoroutine(LoadAllGO());
        }
        void Update()
        {

        }

        IEnumerator LoadAllGO()
        {
            Log("Load colosseum");
            //GameManager.instance.LoadScene("Room_Colosseum_Bronze");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Room_Colosseum_Bronze");
            yield return new WaitWhile(() => GameManager.instance.IsLoadingSceneTransition);
            yield return null;
            On.CameraController.LockToArea += EmptyBoi;
            On.CameraController.ReleaseLock += EmptyBoi2;
            foreach (var i in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (i.name == "Colosseum Platform (1)")
                {
                    platformCol[0] = Instantiate(i);
                }
            }
            if (platformCol[0] == null)
            {
                Log("Platform Not Found.");
            }
            else
            {
                DontDestroyOnLoad(platformCol[0]);
                //ModCommon.GameObjectExtensions.PrintSceneHierarchyTree(sawOrig);
                platformCol[0].SetActive(false);
                Log("Found Platform.");
            }
            On.CameraController.LockToArea -= EmptyBoi;
            On.CameraController.ReleaseLock -= EmptyBoi2;
            
            /*Logger.Log("Loading WD");
            GameManager.instance.LoadScene("GG_White_Defender");
            yield return null;
            foreach (var i in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (i.name == "Dung Pillar (1)")
                {
                    pillarWD = Instantiate(i);
                }
            }
            if (pillarWD == null)
            {
                Modding.Logger.Log("Dung Pillar not found.");
            }
            else
            {
                DontDestroyOnLoad(pillarWD);
                //ModCommon.GameObjectExtensions.PrintSceneHierarchyTree(pillarWD);
                Destroy(pillarWD.LocateMyFSM("Control"));
                _oldTex = pillarWD.GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture;
                pillarWD.GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture = PaleChampion.SPRITES[0].texture;
                pillarWD.SetActive(false);
                Modding.Logger.Log("Found Dung Pillar.");
            }*/

            /*GameManager.instance.LoadScene("White_Palace_05");
            yield return null;

            foreach (var i in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (i.name == "wp_saw (1)")
                {
                    sawOrig = Instantiate(i);
                    oldSawTex = sawOrig.GetComponent<SpriteRenderer>().sprite;
                    sawOrig.GetComponent<SpriteRenderer>().sprite.texture.LoadImage(PaleChampion.SPRITEBYTE[2]);
                }
            }
            if (sawOrig == null)
            {
                Modding.Logger.Log("Saw not found.");
            }
            else
            {
                DontDestroyOnLoad(sawOrig);
                //ModCommon.GameObjectExtensions.PrintSceneHierarchyTree(sawOrig);
                sawOrig.SetActive(false);
                Modding.Logger.Log("Found Saw.");
            }*/

            /*GameManager.instance.LoadScene("GG_Lurker");
            yield return null;

            foreach (var i in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (i.name == "Pale Lurker")
                {
                    paleLurk = Instantiate(i);
                }
                else if (i.name == "Lurker Barb")
                {
                    paleLurkerBarb = Instantiate(i);
                }
            }
            if (paleLurk == null)
            {
                Modding.Logger.Log("Pale Lurker Not Found.");
            }
            else
            {
                DontDestroyOnLoad(paleLurk);
                //ModCommon.GameObjectExtensions.PrintSceneHierarchyTree(sawOrig);
                paleLurk.SetActive(false);
                Modding.Logger.Log("Found Pale Lurker.");
            }
            if (paleLurkerBarb == null)
            {
                Modding.Logger.Log("Pale Lurker Barb Not Found.");
            }
            else
            {
                DontDestroyOnLoad(paleLurkerBarb);
                //ModCommon.GameObjectExtensions.PrintSceneHierarchyTree(sawOrig);
                paleLurkerBarb.SetActive(false);
                Modding.Logger.Log("Found Pale Lurker Barb.");
            }*/
            //GameManager.instance.LoadScene("Menu_Title");
        }

        private void EmptyBoi2(On.CameraController.orig_ReleaseLock orig, CameraController self, CameraLockArea lockarea)
        {
            Log("EMPTY BOI 2 SAYS ROARRRRRRRRRRRRRRRRRRRRRRRRRR");
        }

        private void EmptyBoi(On.CameraController.orig_LockToArea orig, CameraController self, CameraLockArea lockArea)
        {
            Log("EMPTY BOI 1 SAYS I AM DEAD");
        }

        private void OnDestroy()
        {
            Logger.Log("Fix sprites");
            pillarWD.GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture = _oldTex;
            sawOrig.GetComponent<SpriteRenderer>().sprite = oldSawTex;
        }
        private static void Log(object obj)
        {
            Logger.Log("[GO Loader] " + obj);
        }
    }
}