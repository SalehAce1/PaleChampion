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
using On;

namespace PaleChampion
{
    internal class SawHandler : MonoBehaviour
    {
        void Start()
        {

            // = PaleChampion.SPRITES[2].texture;
            gameObject.GetComponent<AudioSource>().volume = 0.2f;
            gameObject.GetComponent<DamageHero>().hazardType = 0;
            gameObject.transform.localScale /= 2.4f;
            gameObject.AddComponent<Rigidbody2D>().velocity = new Vector2(0f, 0f);
            gameObject.GetComponent<Rigidbody2D>().isKinematic = true;
            gameObject.GetComponent<Rigidbody2D>().gravityScale = 0f;
            gameObject.GetComponent<TinkEffect>().enabled = false;
            gameObject.AddComponent<TinkSound>();
        }

        private static void Log(object obj)
        {
            Logger.Log("[Saws] " + obj);
        }
    }
}