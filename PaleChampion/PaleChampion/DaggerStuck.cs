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
            if (coll.gameObject.layer == 8 && t > 0.15f)
            {
                gameObject.GetComponent<Rigidbody2D>().velocity = new Vector2(0f, 0f);
                gameObject.GetComponent<BoxCollider2D>().enabled = false;
                StartCoroutine(DestroyMe());
            }
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