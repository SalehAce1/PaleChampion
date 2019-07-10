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
    internal class HallWaySaw : MonoBehaviour
    {
        float direction = 0f;
        Rigidbody2D rb;
        Vector2 origPos;

        void Start()
        {
            rb = gameObject.GetComponent<Rigidbody2D>();
            origPos = gameObject.transform.position;
            direction = Mathf.Sign(15.5f - origPos.y);
            StartCoroutine(SawRun());
        }
        IEnumerator SawRun()
        {
            rb.velocity = new Vector2(0f, -1f * direction * 12f);
            /* while (true)
             {
                 UnityEngine.Random.InitState((int)(origPos.x + origPos.y));
                 int rand = UnityEngine.Random.Range(0, 10);
                 if (rand == 0)
                 {
                     rb.velocity = new Vector2(0f, -1f * direction * 10f);
                     break;
                 }
                 yield return new WaitForSeconds(1f);
             }
             */
            while (true)
            {
                Vector2 pos = gameObject.transform.position;
                if (rb.velocity.y != 0f)
                {
                    rb.velocity += new Vector2(0f, direction + 0.01f);
                }
                if (direction > 0 && pos.y > 20f && rb.velocity.y != 0f)
                {
                    rb.velocity = new Vector2(0f, 0f);
                }
                else if (direction < 0 && pos.y < 5f && rb.velocity.y != 0f)
                {
                    rb.velocity = new Vector2(0f, 0f);
                }
                else if (rb.velocity.y == 0f && pos.y != origPos.y)
                {
                    gameObject.transform.position = Vector2.MoveTowards(gameObject.transform.position, origPos, 0.5f);
                }
                else if (rb.velocity.y == 0f && pos.y == origPos.y)
                {
                    break;
                }
                yield return null;
            }
            Destroy(gameObject.GetComponent<HallWaySaw>());
        }
        private static void Log(object obj)
        {
            Logger.Log("[Hallway] " + obj);
        }
    }
}