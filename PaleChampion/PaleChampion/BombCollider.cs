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
            if (!done && (coll.gameObject.name.Contains("dank") || coll.gameObject.layer == 8) && !coll.gameObject.name.Contains("not"))
            {
                float sign = Mathf.Sign(105f - gameObject.transform.GetPositionX());
                if (gameObject.transform.GetPositionY() > 6.3f && coll.gameObject.layer == 8 && !coll.gameObject.name.ToLower().Contains("plat"))
                {
                    Log("Bounce " + coll.gameObject.name);
                    Destroy(gameObject);
                    //gameObject.transform.parent.gameObject.GetComponent<Rigidbody2D>().velocity = new Vector2(sign * 50f, gameObject.GetComponent<Rigidbody2D>().velocity.y);
                    return;
                }
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