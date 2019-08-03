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
    internal class DaggerStuck : MonoBehaviour
    {
        float t = 0f;
        void FixedUpdate()
        {
            t += Time.deltaTime;
        }
        private void OnTriggerEnter2D(Collider2D coll)
        {
            if (coll.gameObject.layer == 8 && t > 0.15f && !coll.name.Contains("Plat"))
            {
                gameObject.GetComponent<Rigidbody2D>().velocity = new Vector2(0f, 0f);
                gameObject.GetComponent<BoxCollider2D>().enabled = false;
                StartCoroutine(DestroyMe());
                return;
            }

            if (!coll.CompareTag("Nail Attack"))
            {
                return;
            }

            float degrees = 0f;
            PlayMakerFSM damagesEnemy = PlayMakerFSM.FindFsmOnGameObject(coll.gameObject, "damages_enemy");
            if (damagesEnemy != null)
            {
                degrees = damagesEnemy.FsmVariables.FindFsmFloat("direction").Value * Mathf.Deg2Rad;
            }
            else return;
            gameObject.GetComponent<Rigidbody2D>().velocity = new Vector2(30f * Mathf.Cos(degrees), 30f * Mathf.Sin(degrees));
            gameObject.transform.SetRotation2D(degrees * Mathf.Rad2Deg);
        }
        IEnumerator DestroyMe()
        {
            yield return new WaitForSeconds(3f);
            Destroy(gameObject);
        }
        private void OnDestroy()
        {

        }

        private static void Log(object obj)
        {
            Logger.Log("[Object Flinger] " + obj);
        }
    }
}