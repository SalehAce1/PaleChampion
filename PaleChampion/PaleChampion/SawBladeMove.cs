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

namespace PaleChampion
{
    internal class SawBladeMove : MonoBehaviour
    {
        bool right;
        void Start()
        {
            
        }
        void Update()
        {
            try
            {
                if (gameObject.GetComponent<Rigidbody2D>().velocity.x == 0 || right)
                {
                    right = true;
                    gameObject.GetComponent<Rigidbody2D>().velocity += new Vector2(0.5f,0f);
                }
                if (gameObject.GetComponent<Rigidbody2D>().velocity.x >= 15f || !right)
                {
                    right = false;
                    gameObject.GetComponent<Rigidbody2D>().velocity -= new Vector2(0.5f, 0f);
                }
            }
            catch(System.Exception e)
            { Log(e); }
        }
        private static void Log(object obj)
        {
            Logger.Log("[Saw Blade] " + obj);
        }
    }
}