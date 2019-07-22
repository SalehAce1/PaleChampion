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
using GlobalEnums;

namespace PaleChampion
{
    internal class FlameEnder : MonoBehaviour
    {
        public float duration = 3f;
        Rigidbody2D rg;
        float sign;
        void Start()
        {
            StartCoroutine(DelayedDeath());
            sign = Mathf.Sign(HeroController.instance.transform.GetPositionX() - gameObject.transform.GetPositionX());
            rg = gameObject.transform.parent.gameObject.AddComponent<Rigidbody2D>();
            rg.gravityScale = 0f;
            Log("added v");
        }
        IEnumerator DelayedDeath()
        {
            yield return new WaitForSeconds(1.5f);
            rg.velocity = new Vector2(sign * 10f, 0f);
            yield return new WaitWhile(() => sign > 0 &&  gameObject.transform.GetPositionX() < 119.5f);
            yield return new WaitWhile(() => sign < 0 && gameObject.transform.GetPositionX() > 85.5f);
            gameObject.GetComponent<ParticleSystem>().Stop();
            gameObject.transform.parent.GetChild(1).GetComponent<ParticleSystem>().Stop();
        }
        private void OnParticleCollision(GameObject other)
        {
            if (other != HeroController.instance.gameObject) return;
            HeroController.instance.TakeDamage(gameObject, CollisionSide.other, 1, 1);
        }
        private static void Log(object obj)
        {
            Logger.Log("[Flame Ender] " + obj);
        }
    }
}