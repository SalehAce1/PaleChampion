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
    internal class SpecialAspid : MonoBehaviour
    {
        private tk2dSpriteAnimator _anim;
        private PlayMakerFSM _oldCtrl;
        public Vector2 _pos;
        public int mode;
        public bool firing;
        GameObject otherAspid;

        void Awake()
        {
            Log("Apis start");
            _anim = gameObject.GetComponent<tk2dSpriteAnimator>();
            _oldCtrl = gameObject.LocateMyFSM("spitter");
            _oldCtrl.InsertMethod("Distance Fly", 0, KillFSM);
        }
        void Start()
        {
            gameObject.GetComponent<BoxCollider2D>().isTrigger = true;
            otherAspid = GameObject.Find("aspid" + (int.Parse(gameObject.name[gameObject.name.Length - 1].ToString()) ^ 1));
            _pos = gameObject.transform.position;
        }

        void FixedUpdate()
        {
            /*gameObject.transform.position = Vector2.MoveTowards(gameObject.transform.position, _pos, 35f * Time.deltaTime);
            if (otherAspid == null && GameObject.Find("aspid" + (int.Parse(gameObject.name[gameObject.name.Length - 1].ToString()) ^ 1)) != null)
            {
                otherAspid = GameObject.Find("aspid" + (int.Parse(gameObject.name[gameObject.name.Length - 1].ToString()) ^ 1));
                Log("fixed otherasp");
            }*/
        }
        void KillFSM()
        {
            Log("Killing");
            gameObject.LocateMyFSM("spitter").enabled = false;
            StartCoroutine(FixedBrain());
            Log("Killed");
        }
        IEnumerator FixedBrain()
        {
            Log("Aspid mind begins");
            yield return new WaitWhile(() => _oldCtrl.enabled == true);
            Log("FSM is dead");
            _anim.Play("Idle");
            while (true)
            {
                Vector2 pl = new Vector2();
                if (mode == 0) pl = new Vector2(HeroController.instance.transform.position.x + 3f, HeroController.instance.transform.position.y + 3f);
                if (mode == 1) pl = new Vector2(HeroController.instance.transform.position.x - 3f, HeroController.instance.transform.position.y - 3f);
                if (mode == 2) pl = new Vector2(HeroController.instance.transform.position.x + 3f, HeroController.instance.transform.position.y - 3f);
                if (mode == 3) pl = new Vector2(HeroController.instance.transform.position.x - 3f, HeroController.instance.transform.position.y + 3f);
                gameObject.transform.position = Vector2.MoveTowards(gameObject.transform.position, pl, 12f * Time.deltaTime);
                yield return null;
            }
            /*float speed = 18f;
            int num = 0;
            while (true)
            {
                while (num < 3 && (otherAspid == null || !otherAspid.GetComponent<AspidControl>().firing))//!PaleLurker.pillarOn)
                {
                    firing = true;
                    _anim.Play("Idle");
                    yield return new WaitForSeconds(0.05f);
                    _anim.Play("Attack Antic");
                    yield return new WaitWhile(() => _anim.IsPlaying("Attack Antic"));
                    var spit = Instantiate(PaleChampion.preloadedGO["spit"]);
                    spit.SetActive(true);
                    yield return null;
                    spit.transform.SetPosition2D(gameObject.transform.GetPositionX(), gameObject.transform.GetPositionY());
                    var p1 = gameObject.transform.position;
                    var p2 = HeroController.instance.transform.position;
                    Vector3 dir = HeroController.instance.transform.position - gameObject.transform.position;
                    dir = HeroController.instance.transform.InverseTransformDirection(dir);
                    float angle = Mathf.Atan2(dir.y, dir.x);
                    spit.AddComponent<Rigidbody2D>().velocity = new Vector2(speed * Mathf.Cos(angle), speed * Mathf.Sin(angle));
                    spit.GetComponent<Rigidbody2D>().gravityScale = 0f;
                    spit.GetComponent<Rigidbody2D>().isKinematic = true;
                    spit.layer = 12;
                    spit.AddComponent<DamageHero>().enabled = true;
                    spit.GetComponent<DamageHero>().damageDealt = 1;
                    spit.GetComponent<BoxCollider2D>().isTrigger = true;
                    _anim.Play("Attack");
                    num++;
                    yield return new WaitForSeconds(0.05f);
                    _anim.Play("Idle");
                    yield return new WaitForSeconds(1.75f);
                }
                firing = false;
                num = 0;
                _anim.Play("Idle");
                yield return null;
            }*/
        }
        void OnDestroy()
        {
            firing = false;
        }
        private static void Log(object obj)
        {
            Logger.Log("[Aspid Control] " + obj);
        }
    }
}