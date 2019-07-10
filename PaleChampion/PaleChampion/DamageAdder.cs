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
    internal class DamageAdder : MonoBehaviour
    {
        public bool bigBalls;
        Rigidbody2D rb;
        void Start()
        {
            rb = gameObject.GetComponent<Rigidbody2D>();
            gameObject.layer = 17;
        }
        void OnCollisionEnter2D(Collision2D coll)
        {
            if (bigBalls && (coll.gameObject.layer == 17 || coll.gameObject.layer == 8))
            {
                rb.velocity = new Vector2(rb.velocity.x / 2f, 35f);
            }
        }
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.name == "HeroBox")
            {
                HeroController.instance.TakeDamage(HeroController.instance.gameObject, GlobalEnums.CollisionSide.other, 1, 1);
                return;
            }
            if (!bigBalls)//!collision.CompareTag("Nail Attack")
            {
                return;
            }

            float degrees = 0f;
            PlayMakerFSM damagesEnemy = PlayMakerFSM.FindFsmOnGameObject(collision.gameObject, "damages_enemy");
            if (damagesEnemy != null)
            {
                degrees = damagesEnemy.FsmVariables.FindFsmFloat("direction").Value * Mathf.Deg2Rad;
            }
            else return;
            gameObject.GetComponent<Rigidbody2D>().velocity = new Vector2(20f * Mathf.Cos(degrees), 20f * Mathf.Sin(degrees));
            Log("deg " + degrees);
            Log("card " + DirectionUtils.GetCardinalDirection(degrees));
        }
        private static void Log(object obj)
        {
            Logger.Log("[Damage Adder] " + obj);
        }
    }
}