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
    internal class ObjectFlinger : MonoBehaviour
    {
        public IEnumerator DelayStart(int min, int max)
        {
            for (int i = min; i < max; i++)
            {
                GameObject spike = Instantiate(PaleChampion.preloadedGO["wp spike"]);
                spike.SetActive(true);
                spike.transform.SetPosition2D(gameObject.transform.GetPositionX(), gameObject.transform.GetPositionY());
                var p1 = spike.transform.position;
                var p2 = HeroController.instance.transform.position;
                Vector3 vectorToTarget = p2 - p1;
                float angle2 = Mathf.Atan2(vectorToTarget.y, vectorToTarget.x) * Mathf.Rad2Deg + i * 10f;
                float angle = angle2 * Mathf.Deg2Rad;
                Quaternion q = Quaternion.AngleAxis(angle2, Vector3.forward);
                spike.transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle2));//Quaternion.Slerp(spike.transform.rotation, q, Time.deltaTime * 1000f);
                spike.GetComponent<Rigidbody2D>().velocity = new Vector2(30f * Mathf.Cos(angle), 30f * Mathf.Sin(angle));
                spike.AddComponent<DaggerStuck>();
            }
            yield return null;
        }
        private static void Log(object obj)
        {
            Logger.Log("[Object Flinger] " + obj);
        }
    }
}