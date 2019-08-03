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
            yield return null;
            if (moveTo == 0)
            {
                Log("HEREO");
                yield return new WaitForSeconds(1f);
                gameObject.AddComponent<Rigidbody2D>().isKinematic = true;
                if (moveTo == 0) moveTo = 7.3f;
                speed = 0.2f;
                move = true;
            }
        }

        Vector2 currPos;
        Vector2 finPos;
        void FixedUpdate()
        {
            if (gameObject.transform.GetRotation2D() == 180f || gameObject.transform.GetRotation2D() == 0f)
            {
                currPos = gameObject.transform.position;
                finPos = new Vector2(currPos.x, moveTo);
                if (move)
                {
                    gameObject.transform.position = Vector2.MoveTowards(currPos, finPos, speed * 100f * Time.deltaTime);
                }
                if (currPos == finPos)
                {
                    move = false;
                }
            }
            else
            {
                currPos = gameObject.transform.position;
                finPos = new Vector2(moveTo, currPos.y);
                if (move)
                {
                    gameObject.transform.position = Vector2.MoveTowards(currPos, finPos, speed * 100f * Time.deltaTime);
                }
                if (currPos == finPos)
                {
                    move = false;
                }
            }
        }
        private static void Log(object obj)
        {
            Logger.Log("[Floor] " + obj);
        }
    }
}