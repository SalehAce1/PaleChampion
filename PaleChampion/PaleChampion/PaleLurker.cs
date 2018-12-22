using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using JetBrains.Annotations;
using ModCommon.Util;
using ModCommon;
using Modding;
using UnityEngine;
using UnityEngine.UI;
using Logger = Modding.Logger;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using System.IO;
using System;

namespace PaleChampion
{
    internal class PaleLurker : MonoBehaviour
    {
        private readonly Dictionary<string, float> _fpsDict = new Dictionary<string, float> //Use to change animation speed
        {
            //["Run"] = 100
        };
        public static HealthManager _hm;
        private tk2dSpriteAnimator _anim;
        private PlayMakerFSM _control;

        PlayMakerFSM _sceneFSM;
        private void Awake()
        {
            Log("Added PaleLurker Mono");

            try
            {
                _hm = gameObject.GetComponent<HealthManager>();
                _control = gameObject.LocateMyFSM("Lurker Control");
            }
            catch(Exception e)
            {
                Log(e);
            }
            
        }
        private void Start()
        {
            _hm.hp = 1800;
            try
            {
                Log("Trying to get Lurker to follow you.");
                //_control.RemoveAction("Hop", 5);
                _control.InsertMethod("Hop", 0, test);
                Log("56 told me so");
            }
            catch (Exception e)
            {
                Log(e);
            }
        }

        public void test()
        {
            Log("Testing");
        }

        private void Update()
        {
            //Log("Stuff: " + _control.ActiveStateName);

        }

        private void OnDestroy()
        {

        }

        private static void Log(object obj)
        {
            Logger.Log("[Pale Champion] " + obj);
        }
    }
}