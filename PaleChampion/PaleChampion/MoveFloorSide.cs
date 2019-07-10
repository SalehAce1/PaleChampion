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
    internal class MoveFloorSide : MonoBehaviour
    {
        void Start()
        {
            StartCoroutine(thisisme());
        }
        public int side = 0;
        public float moveTo = 0;
        public bool move = false;
        public float speed = 0f;
        IEnumerator thisisme()
        {
            yield return new WaitForSeconds(1f);
            gameObject.transform.SetPositionZ(1f);
            if (side == 1)
            {
                moveTo = 94f;
            }
            else
            {
                moveTo = 111f;
            }
            //gameObject.AddComponent<Rigidbody2D>().velocity = new Vector2(10f * side, 0f);
            speed = 0.2f;
            gameObject.AddComponent<Rigidbody2D>().isKinematic = true;
            move = true;
        }

        void Update()
        {
            Vector2 currPos = gameObject.transform.position;
            Vector2 finPos = new Vector2(moveTo, currPos.y);
            if (move)
            {
                gameObject.transform.position = Vector2.MoveTowards(currPos, finPos, speed * Time.deltaTime * 100f);
            }
            if (currPos == finPos)
            {
                move = false;
            }
        }
        public static bool FastApproximately(float a, float b, float threshold)
        {
            return ((a - b) < 0 ? ((a - b) * -1) : (a - b)) <= threshold;
        }
        private static void Log(object obj)
        {
            Logger.Log("[Floor] " + obj);
        }
    }
}