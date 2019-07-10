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
    internal class MoveFloorUp : MonoBehaviour
    {
        public bool move = false;
        public float moveTo = 0f;
        public float speed = 0f;

        void Start()
        {
            StartCoroutine(thisisme());   
        }
        IEnumerator thisisme()
        {
            yield return new WaitForSeconds(1f);
            gameObject.AddComponent<Rigidbody2D>().isKinematic = true;
            moveTo = 7.3f;
            speed = 0.2f;
            move = true;
        }

        void FixedUpdate()
        {
            Vector2 currPos = gameObject.transform.position;
            Vector2 finPos = new Vector2(currPos.x, moveTo);
            if (move)
            {
                gameObject.transform.position = Vector2.MoveTowards(currPos, finPos, speed * 100f * Time.deltaTime);
            }
            if (currPos == finPos)
            {
                move = false;
            }
        }
        private static void Log(object obj)
        {
            Logger.Log("[Floor] " + obj);
        }
    }
}