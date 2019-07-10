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
    internal class BombCollider : MonoBehaviour
    {
        bool done;
        void Start()
        {
            gameObject.transform.SetPositionZ(3f);
            gameObject.layer = 12;
            //gameObject.AddComponent<DamageHero>().enabled = true;
            //gameObject.AddComponent<DamageHero>().damageDealt = 1;
        }
        private void OnTriggerEnter2D(Collider2D coll)
        {
            if (!done && (coll.gameObject.name.Contains("dank")|| coll.gameObject.layer == 8))
            {
                StartCoroutine(gameObject.transform.parent.gameObject.GetComponent<SpawnPillar>().DestroyBomb());
                done = true;
            }
        }
        private static void Log(object obj)
        {
            Logger.Log("[Bomb Collider] " + obj);
        }
    }
}