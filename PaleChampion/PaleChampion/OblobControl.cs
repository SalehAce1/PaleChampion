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
    internal class OblobControl : MonoBehaviour
    {
        Vector3 _oldPos;
        public bool attacking;
        public bool end;
        tk2dSpriteAnimator _anim;
        Recoil _recoil;
        void Start()
        {
            _oldPos = gameObject.transform.position;
            _anim = gameObject.GetComponent<tk2dSpriteAnimator>();
            _recoil = gameObject.GetComponent<Recoil>();
            StartCoroutine(BasicMove());
        }
        IEnumerator BasicMove()
        {
            while (true)
            {
                Vector2 randomPos = new Vector2(UnityEngine.Random.Range(90f, 114f), UnityEngine.Random.Range(7f, 18f));
                if (PaleLurker.platPhaseEnd) randomPos = new Vector2(105f, 30f);
                float speed = 12f;
                var lc = gameObject.transform.localScale;
                if (gameObject.transform.GetPositionX() - randomPos.x > 0)
                {
                    gameObject.transform.localScale = new Vector2(Mathf.Abs(lc.x), lc.y);
                }
                else if (gameObject.transform.GetPositionX() - randomPos.x < 0)
                {
                    gameObject.transform.localScale = new Vector2(-1f * Mathf.Abs(lc.x), lc.y);
                }
                while ((Vector2)gameObject.transform.position != randomPos || speed <= 0)
                {
                    gameObject.transform.position = Vector2.MoveTowards(gameObject.transform.position, randomPos, speed * Time.deltaTime);
                    gameObject.transform.SetPositionZ(_oldPos.z);
                    if (Vector2.Distance(gameObject.transform.position,randomPos) % 2 == 0) speed -= 0.1f;
                    yield return null;
                }
                if (gameObject.transform.position.y > 28f)
                {
                    break;
                }
                yield return new WaitForSeconds(1.5f);
            }
            StartCoroutine(BasicMove2());
        }
        IEnumerator BasicMove2()
        {
            gameObject.GetComponent<HealthManager>().hp = 900;
            while (!end)
            {
                _recoil.enabled = true;
                Vector2 randomPos = new Vector2(UnityEngine.Random.Range(90f, 114f), UnityEngine.Random.Range(PaleLurker.lastPhasePlat[0].transform.GetPositionY() + 0.5f, 19f));
                if (PaleLurker.stringSawTack) randomPos = new Vector2(105f, 20f);
                float speed = 12f;
                var lc = gameObject.transform.localScale;
                if (gameObject.transform.GetPositionX() - randomPos.x > 0)
                {
                    gameObject.transform.localScale = new Vector2(Mathf.Abs(lc.x), lc.y);
                }
                else if (gameObject.transform.GetPositionX() - randomPos.x < 0)
                {
                    gameObject.transform.localScale = new Vector2(-1f * Mathf.Abs(lc.x), lc.y);
                }
                while ((Vector2)gameObject.transform.position != randomPos || speed <= 0)
                {
                    gameObject.transform.position = Vector2.MoveTowards(gameObject.transform.position, randomPos, speed * Time.deltaTime);
                    if (Vector2.Distance(gameObject.transform.position, randomPos) % 2 == 0) speed -= 0.1f;
                    yield return null;
                }
                if (PaleLurker.stringSawTack)
                {
                    yield return new WaitWhile(() => PaleLurker.stringSawTack);
                    continue;
                }
                yield return new WaitForSeconds(1f);
                attacking = true;
                _recoil.enabled = false;
                yield return new WaitForSeconds(0.5f);
                _anim.Play("Shoot Antic");
                yield return new WaitUntil(() => _anim.IsPlaying("Shoot Antic"));
                _anim.Play("Shoot Antic");
                yield return new WaitUntil(() => _anim.IsPlaying("Shoot Antic"));
                _anim.Play("Shoot");
                for (float i = 0, j = 360; i < 360; i+=20f, j -= 20f)
                {
                    var spit = Instantiate(PaleChampion.preloadedGO["spit"]);
                    spit.SetActive(true);
                    spit.transform.SetPosition2D(gameObject.transform.position);
                    spit.AddComponent<Rigidbody2D>().velocity = new Vector2(16f * Mathf.Cos(i * Mathf.Deg2Rad), 16f * Mathf.Sin(i * Mathf.Deg2Rad));
                    spit.GetComponent<Rigidbody2D>().gravityScale = 0f;
                    spit.GetComponent<Rigidbody2D>().isKinematic= true;
                    spit.layer = 12;
                    spit.AddComponent<DamageHero>().enabled = true;
                    spit.GetComponent<DamageHero>().damageDealt = 1;
                    spit.GetComponent<BoxCollider2D>().isTrigger = true;

                    spit = Instantiate(PaleChampion.preloadedGO["spit"]);
                    spit.SetActive(true);
                    spit.transform.SetPosition2D(gameObject.transform.position);
                    spit.AddComponent<Rigidbody2D>().velocity = new Vector2(16f * Mathf.Cos(j * Mathf.Deg2Rad), 16f * Mathf.Sin(j * Mathf.Deg2Rad));
                    spit.GetComponent<Rigidbody2D>().gravityScale = 0f;
                    spit.GetComponent<Rigidbody2D>().isKinematic = true;
                    spit.layer = 12;
                    spit.AddComponent<DamageHero>().enabled = true;
                    spit.GetComponent<DamageHero>().damageDealt = 1;
                    spit.GetComponent<BoxCollider2D>().isTrigger = true;

                    yield return new WaitForSeconds(0.1f);
                }
                yield return new WaitForSeconds(0.2f);
                attacking = false;
                _anim.Play("Shoot CD");
                yield return new WaitForSeconds(0.5f);
                _anim.Play("Fly");
            }
        }
        private static void Log(object obj)
        {
            Logger.Log("[Obloblboble Control] " + obj);
        }
    }
}