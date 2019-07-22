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
    internal class CustomAnimator : MonoBehaviour
    {
        public Dictionary<string, List<Sprite>> animations = new Dictionary<string, List<Sprite>>();
        public bool playing;
        public string animCurr;
        public bool looping;
        private bool queue;
        public IEnumerator Play(string name, bool loop = false, float delay = 0.1f, GameObject follow = null)
        {
            if (playing)
            {
                queue = true;
                yield return new WaitWhile(() => playing);
                queue = false;
                //StopCoroutine(Play(animCurr,looping));
            }
            animCurr = name;
            playing = true;
            looping = loop;
            if (follow != null)
            {
                StartCoroutine(FollowObject(follow));
            }
            do
            {
                foreach (var i in animations[name])
                {
                    gameObject.GetComponent<SpriteRenderer>().sprite = i;
                    yield return new WaitForSeconds(delay);
                }
                yield return null;
            }
            while (loop && !queue);
            animCurr = "";
            playing = false;
        }
        IEnumerator FollowObject(GameObject go)
        {
            while (gameObject.activeSelf)
            {
                gameObject.transform.SetPosition2D(go.transform.position.x, go.transform.position.y-0.5f);
                yield return null;
            }
        }
        private static void Log(object obj)
        {
            Logger.Log("[Custom Animator] " + obj);
        }
    }
}