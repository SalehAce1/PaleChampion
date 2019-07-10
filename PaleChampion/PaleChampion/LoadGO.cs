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
        public static GameObject[] platformCol = new GameObject[5];
        public static GameObject[] colCage = new GameObject[5];
        public static GameObject aspid;
        public static GameObject aspidShot;
        public static Texture _oldTexAsp;
        public static Texture _oldTexSpit;



        void Start()
        {
            StartCoroutine(LoadAllGO());
        }

        IEnumerator LoadAllGO()
        {
            Log("Load colosseum");
            On.CameraController.LockToArea += EmptyBoi;
            On.CameraController.ReleaseLock += EmptyBoi2;
            GameManager.instance.LoadScene("Room_Colosseum_Bronze");
            yield return null;
            foreach (var i in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (i.name == "Colosseum Platform (1)")
                {
                    platformCol[0] = Instantiate(i);
                }
                else if (i.name == "Colosseum Spike")
                {
                    platformCol[1] = Instantiate(i);
                }
                else if (i.name == "Colosseum Wall C")
                {
                    platformCol[2] = Instantiate(i);
                }
                else if (i.name == "Colosseum Wall R")
                {
                    platformCol[3] = Instantiate(i);
                }
                else if (i.name == "Colosseum Wall L")
                {
                    platformCol[4] = Instantiate(i);
                }
                else if (i.name == "Colosseum Cage Small")
                {
                    colCage[0] = Instantiate(i);
                }
                else if (i.name == "Super Spitter Col(Clone)" || i.name == "Super Spitter Col")
                {
                    aspid = Instantiate(i);
                }
                else if (i.name == "Spitter Shot R")
                {
                    aspidShot = Instantiate(i);
                }
            }
            foreach (GameObject i in platformCol)
            {
                if (i == null)
                {
                    Log(i.name + " not found!");
                }
                else
                {
                    DontDestroyOnLoad(i);
                    i.SetActive(false);
                    Log("Found " + i.name);
                }
            }
            if (aspid == null)
            {
                Log("Aspid not found!");
            }
            else
            {
                _oldTexAsp = aspid.GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture;
                aspid.GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture = PaleChampion.SPRITES[8].texture;
                aspid.AddComponent<AspidControl>();
                DontDestroyOnLoad(aspid);
                aspid.SetActive(false);
                Log("Found " + aspid.name);
            }
            if (aspidShot == null)
            {
                Log("Shot not found!");
            }
            else
            {
                _oldTexSpit = aspidShot.GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture;
                aspidShot.GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture = PaleChampion.SPRITES[9].texture;
                DontDestroyOnLoad(aspidShot);
                aspidShot.SetActive(false);
                Log("Found " + aspidShot.name);
            }
            if (colCage[0] == null)
            {
                Log(colCage[0].name + " not found!");
            }
            else
            {
                //var fsm = colCage[0].LocateMyFSM("Spawn");
                //fsm.GetAction<ActivateGameObject>("Spawn", 0).gameObject.GameObject = aspid;
                DontDestroyOnLoad(colCage[0]);
                colCage[0].SetActive(false);
                Log("Found " + colCage[0].name);
            }
           
        }
        public static void EmptyBoi2(On.CameraController.orig_ReleaseLock orig, CameraController self, CameraLockArea lockarea)
        {
            
        }

        public static void EmptyBoi(On.CameraController.orig_LockToArea orig, CameraController self, CameraLockArea lockArea)
        {
         
        }

        private static void Log(object obj)
        {
            Logger.Log("[GO Loader] " + obj);
        }
    }
}