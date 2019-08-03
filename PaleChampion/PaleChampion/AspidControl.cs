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
    internal class AspidControl : MonoBehaviour
    {
        private tk2dSpriteAnimator _anim;
        private PlayMakerFSM _oldCtrl;
        private AudioSource _aud;
        public Vector2 _pos;
        public bool firing;
        bool amAspid;
        GameObject otherAspid;

        void Awake()
        {
            Log("Apis start");
            _anim = gameObject.GetComponent<tk2dSpriteAnimator>();
            _aud = gameObject.GetComponent<AudioSource>();
            if (gameObject.name.Contains("aspid"))
            {
                _oldCtrl = gameObject.LocateMyFSM("spitter");
                _oldCtrl.InsertMethod("Distance Fly", 0, KillFSM);
                amAspid = true;
            }
            else if (gameObject.name.Contains("obb"))
            {
                Destroy(gameObject.LocateMyFSM("Fatty Fly Attack"));
                Destroy(gameObject.LocateMyFSM("fat fly bounce"));
                StartCoroutine(ObbBrain());
                StartCoroutine(Dodge());
            }
        }
        void Start()
        {
            gameObject.GetComponent<BoxCollider2D>().isTrigger = true;
            _pos = gameObject.transform.position;
            if (amAspid)
            {
                otherAspid = GameObject.Find("enemyT aspid" + (int.Parse(gameObject.name[gameObject.name.Length - 1].ToString()) ^ 1)); ;
            }
            else gameObject.GetComponent<HealthManager>().hp = 200;
        }
        
        void FixedUpdate()
        {
            gameObject.transform.position = Vector2.MoveTowards(gameObject.transform.position, _pos, 35f * Time.deltaTime);
            if ( amAspid && otherAspid == null && GameObject.Find("enemyT aspid" + (int.Parse(gameObject.name[gameObject.name.Length - 1].ToString()) ^ 1)) != null)
            {
                otherAspid = GameObject.Find("enemyT aspid" + (int.Parse(gameObject.name[gameObject.name.Length - 1].ToString()) ^ 1));
                Log("fixed otherasp");
            }
        }
        IEnumerator Dodge()
        {
            Log("Dodoge");
            float dodgeBy = 5f;
            while (true)
            {
                if (HeroController.instance.spellControl.ActiveStateName == "Fireball Antic")
                {
                    Log("Hero attack");
                    float distance = Mathf.Abs(HeroController.instance.transform.GetPositionY() - gameObject.transform.position.y);
                    if (distance < 1.5f)
                    {
                        if (_pos.y + dodgeBy > 22f) _pos.y -= dodgeBy;
                        else _pos.y += dodgeBy;
                        yield return new WaitForSeconds(1.2f);
                    }
                }
                yield return null;
            }
        }
        public static List<GameObject> liveOrbs = new List<GameObject>();
        IEnumerator ObbBrain()
        {
            yield return null;
            float speed = 18f;
            List<GameObject> bull = new List<GameObject>();
            gameObject.GetComponent<Rigidbody2D>().isKinematic = true;
            while (true)
            {
                if (liveOrbs.Count < 12)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        _anim.Play("Attack");
                        var spit = Instantiate(PaleChampion.preloadedGO["spit"]);
                        spit.SetActive(true);
                        spit.transform.SetPosition2D(gameObject.transform.GetPositionX(), gameObject.transform.GetPositionY());
                        spit.AddComponent<Rigidbody2D>().gravityScale = 0f;
                        spit.GetComponent<Rigidbody2D>().isKinematic = true;
                        spit.layer = 12;
                        spit.AddComponent<DamageHero>().enabled = true;
                        spit.GetComponent<DamageHero>().damageDealt = 1;
                        spit.GetComponent<BoxCollider2D>().isTrigger = true;
                        bull.Add(spit);
                        liveOrbs.Add(spit);
                    }
                    _anim.Play("Attack");
                    yield return new WaitForSeconds(0.1f);
                    float step = 45f;
                    foreach (var i in bull)
                    {
                        float angle = step * Mathf.Deg2Rad;
                        i.GetComponent<Rigidbody2D>().velocity = new Vector2(speed * Mathf.Cos(angle), speed * Mathf.Sin(angle));
                        StartCoroutine(BlobBulletBrain(i));
                        step += 90f;
                        _aud.clip = GOLoader.spitAud;
                        _aud.volume = GameManager.instance.gameSettings.soundVolume;
                        _aud.Play();
                        yield return new WaitForSeconds(0.1f);
                    }
                    yield return new WaitWhile(() => _anim.IsPlaying("Attack"));
                    bull.Clear();
                    _anim.Play("Fly");
                }
                yield return new WaitForSeconds(1.5f);
            }
        }

        IEnumerator BlobBulletBrain(GameObject go)
        {
            yield return new WaitForSeconds(0.2f);
            go.AddComponent<HomingInfection>().centre = gameObject.transform;
        }


        void KillFSM()
        {
            Log("Killing");
            _oldCtrl.enabled = false;
            StartCoroutine(AspidBrain());
            StartCoroutine(Dodge());
            StartCoroutine(AspidDeath());
            Log("Killed");
        }
        IEnumerator AspidDeath()
        {
            Log("lmao I cant die");
            while (!PaleLurker.aspidsDie) yield return null;
            _pos.y += 10f;
            Log("lmao I am dying");
            yield return new WaitForSeconds(0.5f);
            Destroy(gameObject);
            Log("lmao I feel betrayed");
        }
        IEnumerator AspidBrain()
        {
            Log("Aspid mind begins");
            yield return new WaitWhile(() => _oldCtrl.enabled == true);
            Log("FSM is dead");
            float speed = 18f;
            int num = 0;
            while (true)
            {
                while (num < 2 && (otherAspid == null || !otherAspid.GetComponent<AspidControl>().firing))//!PaleLurker.pillarOn)
                {
                    firing = true;
                    yield return new WaitWhile(() => PaleLurker.flameUp);
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
                    _aud.clip = GOLoader.spitAud;
                    _aud.volume = GameManager.instance.gameSettings.soundVolume;
                    _aud.Play();
                    num++;
                    yield return null;
                    _anim.Play("Idle");
                    if (!PaleLurker.setUpEnd) yield return new WaitForSeconds(0.1f);
                    else yield return new WaitForSeconds(1.75f);
                }
                if (firing == true && !PaleLurker.setUpEnd) yield return new WaitForSeconds(2f);
                else if (firing == true) yield return new WaitForSeconds(0.75f);
                firing = false;
                num = 0;
                _anim.Play("Idle");
                yield return null;
            }
        }
        void OnDestroy()
        {
            if (amAspid) firing = false;
            else
            {
                foreach (var i in liveOrbs)
                {
                    Destroy(i);
                }
                liveOrbs.Clear();
            }
        }
        private static void Log(object obj)
        {
            Logger.Log("[Aspid Control] " + obj);
        }
    }
}