﻿using System.Collections;
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
using UnityEngine.SceneManagement;

namespace PaleChampion
{
    internal class PaleLurker : MonoBehaviour
    {
        private readonly Dictionary<string, float> _fpsDict = new Dictionary<string, float> //Use to change animation speed
        {
            //["DigIn 1"] = 60,
            //["DigIn 2"] = 60
            //["DigOut"] = 60
        };
        public HealthManager _hm;
        private tk2dSpriteAnimator _anim;
        private PlayMakerFSM _control;
        private BoxCollider2D _bCol;
        private Texture _oldTex;
        private bool isDigFixed;
        public static int hits = 0;
        public GameObject[] lurkerBarbs = new GameObject[5];
        Texture oldPLTex;
        private PlayMakerFSM dreamNail;
        private string[] HeroAttacks = { "Slash", "DownSlash", "UpSlash", "Fireball2 Spiral(Clone)", "Sharp Shadow",
                                         "Hit R", "Hit L", "Hit U", "Q Fall Damage", "Great Slash", "Dash Slash" };
        public static Vector2[] allDagEndPos = new Vector2[33];
        public static List<Vector2> allDagStartPos = new List<Vector2>();

        private Rigidbody2D _rgbd;
        private GameObject lurker2;

        private void Awake()
        {
            Log("Added PaleLurker Mono");
            _hm = gameObject.GetComponent<HealthManager>();
            _control = gameObject.LocateMyFSM("Lurker Control");
            _anim = gameObject.GetComponent<tk2dSpriteAnimator>();
            _rgbd = gameObject.GetComponent<Rigidbody2D>();
            _bCol = gameObject.GetComponent<BoxCollider2D>();
            dreamNail = HeroController.instance.gameObject.LocateMyFSM("Dream Nail");
        }
        private void Start()
        {
            try
            {
                oldPLTex = gameObject.GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture;
                gameObject.GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture = PaleChampion.SPRITES[3].texture;
                Log("Setting health");
                Destroy(GameObject.Find("Roof"));
                Destroy(gameObject.LocateMyFSM("hp_scaler"));
                _hm.hp = 1800;
            }
            catch (System.Exception e)
            {
                Log(e);
            }

            Log("Changing fps of animation");
            foreach (KeyValuePair<string, float> i in _fpsDict)
            {
                _anim.GetClipByName(i.Key).fps = i.Value;
            }

            try
            {
                USceneManager.activeSceneChanged += SceneChanged;

                Log("Make Lurker follow you");
                _control.InsertMethod(" Hop", 6, VelocitySetter);

                Log("Make Lurker stop throwing for now.");
                _control.ChangeTransition("Hopthrow Antic", "FINISHED", "Hop Air");
                _control.ChangeTransition("Hopland", "FINISHED", "After Land");

                Log("Stop slash attack for now");
                _control.ChangeTransition("After Land", "SLASH", "Hop Antic");
                _control.RemoveAction("Alert", 0);

                Log("Also, stopping her from digging for now");
                _control.RemoveAction("Alert", 3);
                _control.RemoveTransition("Idle", "HERO CAST SPELL");
                _control.RemoveTransition("Alert", "HERO CAST SPELL");
                _control.RemoveTransition("Hop Antic", "HERO CAST SPELL");

                Log("Dig if hit once or if player is too far");
                _control.InsertCoroutine("Hop Antic", 0, Evade);

                Log("Make pillar spikes appear when digging");
                _control.InsertCoroutine("Dig 1", 0, DigAttacks);

                Log("Throw barbs in a spread");
                _control.RemoveAction("Wallbarb", 3);
                _control.InsertCoroutine("Wallbarb", 0, ThrowTrap);

                Log("High Jump State");
                _control.CopyState("Musix", "High Jump");
                _control.RemoveAction("High Jump", 0);
                _control.InsertCoroutine("High Jump", 0, WaitingJump);
                _control.RemoveTransition("High Jump", "FINISHED");
                _control.AddTransition("High Jump", "JUMPDONE", "Idle");

                Log("Fix dig after intial digout");
                StartCoroutine(FixDig());

            }
            catch (Exception e)
            {
                Log(e);
            }
            lurker2 = Instantiate(PaleChampion.preloadedGO["lurker2"]);
            lurker2.SetActive(false);
            lurker2.transform.localScale *= 1.5f;
            lurker2.AddComponent<Rigidbody2D>().gravityScale = 0f;
            var anim = lurker2.GetComponent<CustomAnimator>();
            anim.animations.Add("idle", PaleChampion.SPRITES.GetRange(11, 5));
            anim.animations.Add("dash", PaleChampion.SPRITES.GetRange(16, 6));
            anim.animations.Add("jump", PaleChampion.SPRITES.GetRange(22, 5));
            anim.animations.Add("throw", PaleChampion.SPRITES.GetRange(27, 7));
            StartCoroutine(OblobPhase());
        }

        private void SceneChanged(Scene arg0, Scene arg1)
        {
            if (arg0.name == "GG_Oblobbles" && arg1.name == "GG_Workshop")
            {
                Log("die");
                USceneManager.activeSceneChanged -= SceneChanged;
                gameObject.GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture = oldPLTex;
                Destroy(gameObject.GetComponent<PaleLurker>());
            }
        }

        private void AfterPlayerDied()
        {
            Log("pog");
            Destroy(gameObject);
        }

        void OnTriggerEnter2D(Collider2D col)
        {
            //Needs fluke, elegy, and more
            if (HeroAttacks.Contains(col.name))
            {
                if (!phase2 && !evading)
                {
                    hits++;
                }
                else if (phase2) hits++;
            }
            if (col.name == "Colosseum Spike(Clone)(Clone)")
            {
                StartCoroutine(ReturnSpike(col.gameObject, gameObject.transform.position));
            }
        }

        bool lurkOnPlat = false;
        bool startC = false;
        void OnCollisionEnter2D(Collision2D other)
        {
            if (other.gameObject.name == "Chunk 0 2" && startC)
            {
                lurkOnPlat = true;
            }
            if (other.gameObject.name == "Colosseum Platform (1)(Clone)(Clone)")
            {
                lurkOnPlat = true;
            }
        }

        public IEnumerator FixDig() //Sets first dig
        {
            yield return new WaitWhile(() => _control.ActiveStateName == "Dig Out");
            _control.enabled = false;
            _anim.Play("Slash Antic");
            yield return new WaitForSeconds(0.4f);
            _anim.Play("Slash Antic");
            yield return new WaitForSeconds(0.4f);
            _anim.Play("Slash End");
            yield return new WaitForSeconds(0.6f);
            _control.enabled = true;
            _anim.Play("Idle");
            Log("Start fight");
            _control.InsertMethod("Idle", 0, () => _control.SendEvent("HERO L"));

            yield return null;

            _control.ChangeTransition("Dig 2", "FINISHED", "Teleport");
            _control.RemoveAction("Teleport", 0);
            _control.RemoveAction("Teleport", 0);
            _control.RemoveAction("Teleport", 0);
            _control.InsertMethod("Teleport", 0, TeleportLurker);
            _control.RemoveTransition("Teleport", "FINISHED");
            _control.SetState("Dig 1");

            while (true)
            {
                try
                {
                    Log("Start MUSIC");
                    gameObject.GetComponent<AudioSource>().clip = MusicLoad.LoadAssets.music[1];
                    gameObject.GetComponent<AudioSource>().Play();

                }
                catch (Exception e)
                {
                    Log(e);
                }
                yield return new WaitForSeconds(5f);
            }

            // _control.InsertCoroutine("Teleport", 1, Phase2);
            // phase2 = true;
        }

        public void VelocitySetter() //Set hop power
        {
            try
            {
                var xPHero = HeroController.instance.transform.GetPositionX();
                var xPLurker = gameObject.transform.GetPositionX();
                var scaleL = gameObject.transform.localScale;
                if (throwSpike)
                {
                    throwSpike = false;
                    var dir = Mathf.Sign(xPLurker - xPHero);
                    float xL = 0f;
                    if (dir > 0) xL = (119.5f - xPLurker) * 3.5f;
                    else if (dir < 0) xL = (85.5f - xPLurker) * 3.5f;
                    var yL = 45f;
                    gameObject.transform.localScale = new Vector3(dir * Mathf.Abs(scaleL.x), scaleL.y, scaleL.z);
                    _rgbd.velocity = new Vector2(xL, yL);
                }
                else
                {
                    var xL = _rgbd.velocity.x;
                    var yL = 15f + Math.Abs(xPHero - xPLurker);
                    gameObject.transform.localScale = new Vector3(scaleL.x * -1f, scaleL.y, scaleL.z);
                    _rgbd.velocity = new Vector2(-1f * xL, yL);
                }
            }
            catch(Exception e)
            {
                Log(e);
            }
        }

        public void TeleportLurker() //TPs lurker after dig
        {
            var xH = HeroController.instance.transform.GetPositionX();
            var yH = HeroController.instance.transform.GetPositionY();
            var xL = gameObject.transform.GetPositionX();
            var yL = gameObject.transform.GetPositionY();

            if (phase2)
            {
                Log("tpphase2");
                gameObject.transform.SetPosition2D(119.7f, 16f);
                //_control.RemoveAction("Teleport", 0);
                return;
            }
            if (xH - xL > 0)
            {
                var newX = xH + 5f;
                if (newX > 119f) newX = xH - 5f;
                gameObject.transform.SetPosition2D(newX, yL);
            }
            else
            {
                var newX = xH - 5f;
                if (newX < 85.5f) newX = xH + 5f;
                gameObject.transform.SetPosition2D(newX, yL);
            }
        }
        bool evading;
        int p1Max = 3;
        bool dashed;
        IEnumerator Evade() //Tells lurker to dig/dash when hit and controls parts of phase 1
        {
            if (hits > 0)
            {
                Log("evade");
                evading = true;
                hits = 0;
                p1Max = 5;
                if (_hm.hp < 1400)
                {
                    _control.InsertCoroutine("Teleport", 1, Phase2);
                    phase2 = true;
                }

                if (UnityEngine.Random.Range(0,3)==0)
                {
                    dashed = true;
                    lurker2.SetActive(true);
                    lurker2.transform.SetPosition2D(gameObject.transform.position);
                    StartCoroutine(lurker2.GetComponent<CustomAnimator>().Play("dash", false, 0.05f));
                    var sig = Mathf.Sign(gameObject.transform.GetPositionX() - HeroController.instance.transform.GetPositionX());
                    gameObject.GetComponent<MeshRenderer>().enabled = false;
                    gameObject.transform.localScale.SetX(sig * Mathf.Abs(gameObject.transform.localScale.x));
                    lurker2.transform.localScale.SetX(-1f * sig * Mathf.Abs(lurker2.transform.localScale.x));
                    _control.enabled = false;
                    lurker2.GetComponent<Rigidbody2D>().velocity = new Vector2(sig * 25f, 0f);
                    while (lurker2.GetComponent<CustomAnimator>().playing)
                    {
                        gameObject.transform.SetPosition2D(lurker2.transform.position);
                        yield return null;
                    }
                    lurker2.GetComponent<Rigidbody2D>().velocity = new Vector2(0f, 0f);
                    lurker2.SetActive(false);
                    gameObject.GetComponent<MeshRenderer>().enabled = true;
                    if (p1Max > 3)
                    {
                        gameObject.transform.localScale.SetX(-1f * Mathf.Abs(gameObject.transform.localScale.x));
                        _anim.Play("Throw");
                        yield return new WaitUntil(() => _anim.IsPlaying("Throw"));
                        StartCoroutine(gameObject.AddComponent<ObjectFlinger>().DelayStart(-2, 4));
                        yield return null;
                    }
                    _control.enabled = true;
                }
                _control.SetState("Dig 1");
            }
            yield return new WaitForSeconds(3f);
            evading = false;
        }
        public IEnumerator DigAttacks() //Different digging attacks: WD spikes, high jump, nothing
        {
            int rnd = UnityEngine.Random.Range(0, p1Max);
            _control.ChangeTransition("Dig Out", "FINISHED", "Reset");
            if (rnd == 0)
            {
                _control.enabled = false;
                _anim.Play("Slash Antic");
                yield return new WaitForSeconds(0.1f);
                _anim.Play("Slash Antic");
                yield return new WaitForSeconds(0.1f);
                _anim.Play("Slash Antic");
                yield return new WaitForSeconds(0.1f);
                _anim.Play("Slash End");
                yield return new WaitForSeconds(0.15f);
                _control.enabled = true;
                _control.SetState("Dig 2");
                var xL = gameObject.transform.GetPositionX();
                var yL = gameObject.transform.GetPositionY();
                yield return new WaitForSeconds(_anim.GetClipByName("DigIn 2").Duration);
                for (int i = 0; i < 3; i++)
                {
                    var DungPill1 = Instantiate(PaleChampion.preloadedGO["dung"]);
                    var DungPill2 = Instantiate(PaleChampion.preloadedGO["dung"]);
                    var xSpikeScale = DungPill2.transform.localScale.x;
                    var ySpikeScale = DungPill2.transform.localScale.y;
                    var zSpikeScale = DungPill2.transform.localScale.z;
                    Destroy(DungPill1.LocateMyFSM("Control"));
                    Destroy(DungPill2.LocateMyFSM("Control"));
                    DungPill1.SetActive(true);
                    DungPill2.SetActive(true);
                    DungPill1.AddComponent<DungPillar>();
                    DungPill2.AddComponent<DungPillar>();
                    DungPill1.transform.SetPosition2D(xL + (i * 2.2f) + 1.5f, yL + 4.5f);
                    DungPill2.transform.SetPosition2D(xL - (i * 2.2f) - 1.5f, yL + 4.5f);
                    DungPill2.transform.localScale = new Vector3(-1f * xSpikeScale, ySpikeScale, zSpikeScale);
                    yield return new WaitForSeconds(0.125f);
                }
            }
            else if ((rnd == 4 || rnd==3) && !canThrowSpike)
            {
                throwSpike = true;
            }
            else if ((rnd == 3 && !dashed) || (rnd >= 2 && p1Max > 4))
            {
                _control.ChangeTransition("Dig Out", "FINISHED", "High Jump");
            }
            dashed = false;
        }
        bool throwSpike;
        bool canThrowSpike;
        public IEnumerator WaitingJump() //Jumping into throwing daggers
        {
            _control.enabled = false;
            yield return null;
            _anim.Play("DigOut");
            yield return new WaitUntil(() => _anim.IsPlaying("DigOut"));
            gameObject.GetComponent<BoxCollider2D>().enabled = true;
            _rgbd.isKinematic = false;
            _anim.Play("Wallhop Antic");
            yield return new WaitUntil(() => _anim.IsPlaying("Wallhop Antic"));
            _rgbd.velocity = new Vector2(-20f * gameObject.transform.GetScaleX(), 60f * 0.9f);
            _anim.Play("Hop");
            _rgbd.gravityScale = 2f;
            yield return null;
            yield return new WaitWhile(() => Math.Abs(_rgbd.velocity.y - 1f) > 1f);
            _anim.Play("Throw");
            yield return new WaitUntil(() => _anim.IsPlaying("Throw"));
            StartCoroutine(gameObject.AddComponent<ObjectFlinger>().DelayStart(-2, 4));
            var oldVe = _rgbd.velocity;
            _rgbd.velocity = new Vector2(0f, 0f);
            _rgbd.gravityScale = 0f;
            yield return new WaitForSeconds(0.5f);
            _rgbd.gravityScale = 2f;
            _rgbd.velocity = oldVe;
            yield return new WaitForSeconds(0.1f);
            yield return new WaitWhile(() => _rgbd.velocity.y != 0f);
            _rgbd.velocity = new Vector2(0f, 0f);
            _control.enabled = true;
            _control.SetState("Idle");
            yield return null;

        }
        IEnumerator ThrowTrap() //Throw barbs that can move in later parts of P1
        {
            Log("throw");
            int throwN = 4;
            if (p1Max == 5) throwN = 3;
            for (int i = 1; i < throwN; i++)
            {
                GameObject barb = Instantiate(PaleChampion.preloadedGO["barb"]);
                barb.SetActive(true);
                barb.transform.SetPosition2D(gameObject.transform.GetPositionX(), gameObject.transform.GetPositionY());
                var p1 = barb.transform.position;
                var p2 = HeroController.instance.transform.position;
                Vector3 vectorToTarget = p2 - p1;
                float angle = Mathf.Atan2(vectorToTarget.y, vectorToTarget.x) + i * 10f * Mathf.Deg2Rad;
                barb.GetComponent<Rigidbody2D>().velocity = new Vector2(30f * Mathf.Cos(angle), 30f * Mathf.Sin(angle) + 10f);
                if (p1Max == 5) StartCoroutine(BarbMovement(barb));
                Log("thjrown");
            }
            yield return null;
        }

        IEnumerator BarbMovement(GameObject barb) //Control barb movement
        {
            var barbCtrl = barb.LocateMyFSM("Control");
            Log("wait");
            yield return new WaitWhile(() => barbCtrl.ActiveStateName != "Spike Up");
            var origY = barb.transform.GetPositionY();
            var dir = Mathf.Sign(HeroController.instance.transform.GetPositionX() - barb.transform.GetPositionX());
            barb.GetComponent<Rigidbody2D>().velocity = new Vector2(dir * 13f, 0f);
            while (barb!=null)
            {
                barb.transform.Rotate(Vector3.forward * 15 * dir);
                if (barb.transform.GetPositionX() > 119.2f || barb.transform.GetPositionX() < 85.8f)
                {
                    dir *= -1f;
                    barb.GetComponent<Rigidbody2D>().velocity = new Vector2(dir * 13f, 0f);
                    yield return new WaitForSeconds(0.5f);
                }
                barb.transform.SetPositionY(origY);
                canThrowSpike = true;
                yield return null;
            }
            canThrowSpike = false;
        }

        bool phase2;
        bool retractSpikes1 = true;

        private IEnumerator Phase2() //Start phase 2
        {
            Log("Start Phase 2");
            yield return null;
            _control.enabled = false;
            setUpEnd = false;
            _temp = 0;
            platPhaseEnd = false;
            allDagEndPos = new Vector2[33];
            allDagStartPos = new List<Vector2>();
            
            _allSpikes = new GameObject[25];


            StartCoroutine(GoToWall1());
        }

        List<GameObject> allLowerSpike = new List<GameObject>();
        private IEnumerator GoToWall1() //Digs to right wall
        {
            _rgbd.gravityScale = 0f;
            _rgbd.velocity = new Vector2(0f, 0f);
            _anim.Play("DigOut");
            gameObject.transform.SetRotation2D(90f);
            yield return new WaitWhile(() => _anim.IsPlaying("DigOut"));
            gameObject.transform.SetRotation2D(0f);
            var ls = gameObject.transform.localScale;
            gameObject.transform.localScale = new Vector3(-1f, ls.y);
            gameObject.GetComponent<BoxCollider2D>().enabled = true;
            _rgbd.isKinematic = false;
            _anim.Play("Wall Cling");
            retractSpikes1 = false;
            var temp = 0;
            gameObject.transform.SetPositionZ(0.5f);
            for (int i = 84; i < 122; i += 2)
            {
                StartCoroutine(SpawnPlatform(1, i, 6f, 1));
                yield return null;
                allLowerSpike.Add(currentPlat);
                _allSpikes[temp] = currentPlat;
                temp++;
            }
            StartCoroutine(BombNJump2());
        }

        private IEnumerator BombNJump2() //Throw bombs, spawn platform, jump to platform
        {
            var predPos = gameObject.transform.position;
            var initYV = 30f;
            var initXV = 25f;
            var fakeT = 0f;
            while (predPos.y > 9f)
            {
                initYV += 2f * Physics2D.gravity.y * fakeT;
                var x = predPos.x - initXV * fakeT;
                var y = predPos.y + initYV * fakeT;
                predPos = new Vector3(x, y);
                fakeT += 0.001f;
            }

            yield return new WaitForSeconds(1f);

            for (int t = 0; t < 5; t++)
            {
                for (int i = -1; i < 2; i++) ///////////////////////////////////////////////////////////////////////////////////////////////////
                {
                    var bomb = Instantiate(PaleChampion.preloadedGO["bomb"]);
                    bomb.layer = 12;
                    bomb.GetComponent<ParticleSystem>().Play();
                    bomb.SetActive(true);
                    bomb.AddComponent<SpawnPillar>();
                    bomb.transform.Find("normal").gameObject.AddComponent<BombCollider>();
                    bomb.transform.SetPosition2D(gameObject.transform.GetPositionX() - 1.5f, gameObject.transform.GetPositionY());
                    var diff = HeroController.instance.transform.position - gameObject.transform.position;
                    bomb.GetComponent<Rigidbody2D>().gravityScale = 1f;
                    bomb.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
                    bomb.GetComponent<Rigidbody2D>().velocity = new Vector2(diff.x * 2f + i * 15f, diff.y + i * 2f);
                }
                _anim.Play("Throw");
                yield return new WaitWhile(() => _anim.IsPlaying("Throw"));
                _anim.Play("Wall Cling");
                yield return new WaitWhile(() => _anim.IsPlaying("Wall Cling"));
                yield return new WaitForSeconds(0.5f);
            }

            StartCoroutine(SpawnPlatform(0, predPos.x + 0.5f, predPos.y));
            yield return new WaitForSeconds(1f);

            Log("Going to the middle");
            _anim.Play("Wallhop Antic");
            yield return new WaitWhile(() => _anim.IsPlaying("Wallhop Antic"));
            _anim.Play("Hop");
            var sca = gameObject.transform.localScale;
            gameObject.transform.localScale = new Vector3(-1f * sca.x, sca.y, sca.z);
            _rgbd.velocity = new Vector2(25f * GetSide(), 30f);
            _rgbd.gravityScale = 2f;
            yield return new WaitWhile(() => !lurkOnPlat);
            lurkOnPlat = false;
            _rgbd.velocity = new Vector2(0f, 0f);
            _anim.Play("Idle");
            StartCoroutine(JumpNEnemySpawn3());
        }

        private IEnumerator JumpNEnemySpawn3() //Roar, enemy spawn: aspid, homing obble, squit. Go to left wall./////////////////////////////////////////////////////////////////
        {
            Log("Start Spin");
            _anim.Play("Slash Antic");
            var pos = gameObject.transform.position;
            StartCoroutine(EnemyTrial(pos.x, pos.y));
            yield return new WaitForSeconds(0.4f);
            _anim.Play("Slash Antic");
            yield return new WaitForSeconds(0.4f);
            _anim.Play("Slash Antic");
            yield return new WaitForSeconds(0.4f);
            _anim.Play("Slash End");
            yield return new WaitForSeconds(0.6f);

            Log("Go to other wall");
            _anim.Play("Hop");
            _rgbd.velocity = new Vector2(30f * GetSide(), 50f);
            _rgbd.gravityScale = 2f;
            yield return null;
            currentPlat.LocateMyFSM("Control").SendEvent("PLAT RETRACT");

            while (true)
            {
                RaycastHit2D hit = Physics2D.Raycast(gameObject.transform.position, Vector2.left);
                Log("hit " + hit.collider.gameObject.name + " | " + hit.collider.gameObject.layer);
                if (hit.collider != null && hit.collider.gameObject.layer == 8)//hit.collider.name == "Chunk 0 2")
                {
                    float distance = Mathf.Abs(hit.point.x - gameObject.transform.position.x);
                    if (distance < 1f)
                    {
                        break;
                    }
                }
                yield return null;
            }
            Log("Found da wall");
            gameObject.transform.position = new Vector2(gameObject.transform.position.x - 0.4f, gameObject.transform.position.y);
            _rgbd.velocity = new Vector2(0f, 0f);
            _rgbd.gravityScale = 0f;
            _anim.Play("Wall Cling");
            yield return new WaitWhile(() => _anim.IsPlaying("Wall Cling"));
            Vector2 curP = gameObject.transform.position;
            StartCoroutine(BeforeDaggSet35(curP));
        }
        IEnumerator BeforeDaggSet35(Vector2 pos) ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            float leftOrRight = 1f;
            
            _bCol.enabled = true;
            _rgbd.gravityScale = 0f;
            _rgbd.velocity = new Vector2(0f, 0f);
            _rgbd.isKinematic = true;
            while (_hm.hp > 1200)
            {
                for (int t = 0; t < 3; t++)
                {
                    for (int i = -1; i < 2; i++)
                    {
                        var bomb = Instantiate(PaleChampion.preloadedGO["bomb"]);
                        bomb.layer = 12;
                        bomb.GetComponent<ParticleSystem>().Play();
                        bomb.SetActive(true);
                        bomb.AddComponent<SpawnPillar>();
                        bomb.transform.Find("normal").gameObject.AddComponent<BombCollider>();
                        bomb.transform.SetPosition2D(gameObject.transform.GetPositionX() + leftOrRight, gameObject.transform.GetPositionY());
                        var diff = HeroController.instance.transform.position - gameObject.transform.position;
                        bomb.GetComponent<Rigidbody2D>().gravityScale = 1f;
                        bomb.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
                        bomb.GetComponent<Rigidbody2D>().velocity = new Vector2(diff.x * 2f + i * 15f, diff.y + i * 2f);
                    }
                    _anim.Play("Throw");
                    yield return new WaitWhile(() => _anim.IsPlaying("Throw"));
                    _anim.Play("Wall Cling");
                    yield return new WaitWhile(() => _anim.IsPlaying("Wall Cling"));
                    yield return new WaitForSeconds(3f);
                }
                Vector2 newPos = new Vector2();
                if (leftOrRight > 0) newPos = new Vector2(119.5f, 15.5f);
                else newPos = new Vector2(85.5f, 15.5f);
                gameObject.transform.SetRotation2D(-90f * leftOrRight);
                _anim.Play("DigIn 1");
                yield return new WaitWhile(() => _anim.IsPlaying("DigIn 1"));
                _anim.Play("DigIn 2");
                yield return new WaitWhile(() => _anim.IsPlaying("DigIn 2"));
                gameObject.transform.SetRotation2D(0f);
                gameObject.transform.position = newPos;
                var sca = gameObject.transform.localScale;
                gameObject.transform.localScale = new Vector3(-1f * sca.x, sca.y, sca.z);
                gameObject.transform.SetRotation2D(90f * leftOrRight);
                _anim.Play("DigOut");
                yield return new WaitWhile(() => _anim.IsPlaying("DigOut"));
                gameObject.transform.SetRotation2D(0f);
                _anim.Play("Wall Cling");
                yield return new WaitWhile(() => _anim.IsPlaying("Wall Cling"));
                yield return new WaitForSeconds(0.5f);
                leftOrRight *= -1f;
            }
            gameObject.transform.SetRotation2D(-90f * leftOrRight);
            _anim.Play("DigIn 1");
            yield return new WaitWhile(() => _anim.IsPlaying("DigIn 1"));
            _anim.Play("DigIn 2");
            yield return new WaitWhile(() => _anim.IsPlaying("DigIn 2"));
            gameObject.transform.SetRotation2D(0f);
            gameObject.transform.position = pos;
            var sca2 = gameObject.transform.localScale;
            gameObject.transform.localScale = new Vector3(leftOrRight * sca2.x, sca2.y, sca2.z);
            gameObject.transform.SetRotation2D(-90f);
            _anim.Play("DigOut");
            yield return new WaitWhile(() => _anim.IsPlaying("DigOut"));
            gameObject.transform.SetRotation2D(0f);
            _anim.Play("Wall Cling");
            yield return new WaitWhile(() => _anim.IsPlaying("Wall Cling"));
            yield return new WaitForSeconds(0.5f);
            StartCoroutine(WiredDaggThrow4());
        }
        private IEnumerator WiredDaggThrow4() //Set up wire dag on all sides
        {
            Log("Start saw attack");
            _rgbd.isKinematic = false;
            var initY = gameObject.transform.GetPositionY();
            while (gameObject.transform.GetPositionY() >= 7.5f)
            {
                _anim.Play("Throw");
                var a = Instantiate(PaleChampion.preloadedGO["wp spike"]);
                a.transform.SetPosition2D(85.5f, gameObject.transform.GetPositionY());
                a.SetActive(true);
                a.AddComponent<ThrowingSpike>();
                yield return new WaitWhile(() => _anim.IsPlaying("Throw"));
                _anim.Play("Wall Cling");
                yield return new WaitWhile(() => _anim.IsPlaying("Wall Cling"));
                _rgbd.gravityScale = 0.1f;
                yield return new WaitForSeconds(0.8f);
                _rgbd.gravityScale = 0f;
                _rgbd.velocity = new Vector2(0f, 0f);
            }

            yield return new WaitForSeconds(1f);

            Log("Jump to roof");
            _anim.Play("Wallhop Antic");
            yield return new WaitWhile(() => _anim.IsPlaying("Wallhop Antic"));
            _anim.Play("Hop");
            var sca = gameObject.transform.localScale;
            gameObject.transform.localScale = new Vector3(-1f * sca.x, sca.y, sca.z);
            _rgbd.velocity = new Vector2(10f * GetSide(), 70f);
            _rgbd.gravityScale = 2f;
            startC = true;
            yield return new WaitWhile(() => !lurkOnPlat);
            _rgbd.velocity = new Vector2(0f, 0f);
            _rgbd.gravityScale = 0f;
            startC = false;
            lurkOnPlat = false;

            Log("Up daggers");
            gameObject.transform.SetPosition2D(85.5f, gameObject.transform.position.y - 1.5f);
            while (gameObject.transform.GetPositionX() <= 119f)
            {
                if (Mathf.Abs(gameObject.transform.GetPositionX() - 102.5f) > 2f)
                {
                    var a = Instantiate(PaleChampion.preloadedGO["wp spike"]);
                    a.transform.SetPosition2D(gameObject.transform.GetPositionX(), gameObject.transform.GetPositionY());
                    a.transform.SetRotation2D(-90f);
                    a.SetActive(true);
                    a.AddComponent<ThrowingSpike>(); 
                }
                _rgbd.velocity = new Vector2(5f, 0f);
                yield return new WaitForSeconds(0.5f);
                _rgbd.velocity = new Vector2(0f, 0f);
            }
            yield return new WaitForSeconds(1f);
            gameObject.transform.SetPosition2D(102.5f, gameObject.transform.position.y);
            for (float i = 190f, k = 350f; i <= 270f; i += 10f, k -= 10f)
            {
                var a = Instantiate(PaleChampion.preloadedGO["wp spike"]);
                a.transform.SetPosition2D(gameObject.transform.GetPositionX(), gameObject.transform.GetPositionY());
                a.transform.SetRotation2D(i);
                a.SetActive(true);
                a.AddComponent<ThrowingSpike>();

                a = Instantiate(PaleChampion.preloadedGO["wp spike"]);
                a.transform.SetPosition2D(gameObject.transform.GetPositionX(), gameObject.transform.GetPositionY());
                a.transform.SetRotation2D(k);
                a.SetActive(true);
                a.AddComponent<ThrowingSpike>();
                yield return new WaitForSeconds(0.4f);
            }

            yield return new WaitForSeconds(1f);
            StartCoroutine(SetUpSawPhase5());
        }
        public static bool FastApproximately(float a, float b, float threshold)
        {
            return ((a - b) < 0 ? ((a - b) * -1) : (a - b)) <= threshold;
        }
        private List<Vector2> allSawPos = new List<Vector2>();
        private List<GameObject> allUpperPlat = new List<GameObject>();
        private List<GameObject> allUpperSpike = new List<GameObject>();
        private IEnumerator SetUpSawPhase5()
        {
            setUpEnd = true;
            for (float i = 85f; i < 121f; i += 3.3f)
            {
                StartCoroutine(SpawnPlatform(0, i, 3f));
                currentPlat.AddComponent<MoveFloorUp>();
                allUpperPlat.Add(currentPlat);
            }
            yield return new WaitForSeconds(3f);
            Log("Spawn upper spike");
            for (int i = 84; i < 122; i += 2)
            {
                StartCoroutine(SpawnPlatform(1, i, 9f, 1));
                yield return null;
                allUpperSpike.Add(currentPlat);
            }
            for (int i = 5; i < 19; i += 2)
            {
                StartCoroutine(SpawnPlatform(1, 85.5f, i, 1, 270));
                yield return null;
            }
            for (int i = 5; i < 19; i += 2)
            {
                StartCoroutine(SpawnPlatform(1, 119.5f, i, 1, 90f));
                yield return null;
            }
            yield return new WaitForSeconds(1f);
            
            Log("err1");
            int c1 = 0;
            foreach (Vector2 i in allDagEndPos)
            {
                var saw = Instantiate(PaleChampion.preloadedGO["saw"]);
                saw.transform.SetPosition2D(i);
                saw.AddComponent<SawHandler>();
                saw.SetActive(false);
                AllSaws.Add(saw);
                c1++;
                yield return null;
            }
            
            foreach (Vector2 i in allDagStartPos)
            {
                var saw = Instantiate(PaleChampion.preloadedGO["saw"]);
                saw.transform.SetPosition2D(i);
                saw.SetActive(false);
                saw.AddComponent<SawHandler>();
                AllSaws.Add(saw);
                c1++; 
                yield return null;
            }
            Log("err3");
            foreach (GameObject i in AllSaws)
            {
                allSawPos.Add(i.transform.position);
            }
            yield return null;
            foreach (GameObject i in AllSaws)
            {
                Vector2 pos = i.transform.position;
                if (Mathf.Approximately(pos.x, 85.5f) && pos.y < 20.1f) leftSaw.Add(i); //left
                else if (FastApproximately(pos.x, 119.5f, 0.05f) && pos.y < 21f && pos.y > 6f) rightSaw.Add(i); //right
                else if (!Mathf.Approximately(pos.x, 102.5f) && pos.y > 21f) upSaw.Add(i); //up
                else if (!Mathf.Approximately(pos.x, 102.5f) && pos.y < 5.5f) downSaw.Add(i); //down
                else if (Mathf.Approximately(pos.x, 102.5f) && pos.y > 21f) upDSaw.Add(i);
                else downDSaw.Add(i);
            }
            leftSaw.RemoveAt(0);
            rightSaw.RemoveAt(rightSaw.Count - 1);
            upSaw.Add(upDSaw[upDSaw.Count - 1]);

            _hm.hp = 1300;

            //var sca = gameObject.transform.localScale;
            //gameObject.transform.localScale = new Vector3(-1f * sca.x, sca.y, sca.z);
            
            _rgbd.gravityScale = 2f;
            yield return new WaitWhile(() => !lurkOnPlat);
            lurkOnPlat = false;
            _anim.Play("Idle");
            //_rgbd.velocity = new Vector2(0f, 0f);
            //_rgbd.gravityScale = 0f;
            yield return new WaitForSeconds(0.1f);
            round = 0;
            StartCoroutine(SawMoveSelector());
            StartCoroutine(LurkerJumperAI());
        }
        List<GameObject> leftSaw = new List<GameObject>();
        List<GameObject> upSaw = new List<GameObject>();
        List<GameObject> upDSaw = new List<GameObject>();
        List<GameObject> rightSaw = new List<GameObject>();
        List<GameObject> downSaw = new List<GameObject>();
        List<GameObject> downDSaw = new List<GameObject>();
        bool newbatch;
        public IEnumerator LurkerJumperAI(float xSpd = 16f)////////////////////////////////////////////////////20 to 16
        {
            
            gameObject.transform.SetPositionZ(2f);
            float sig = 0f;
            while(!newbatch)
            {
                yield return null;
            }
            newbatch = false;
            while(AllSaws[0].transform.GetPositionX() - gameObject.transform.GetPositionX() > 6f && side >= 0)
            {
                sig = 1f;
                yield return null;
            }
            while (AllSaws[33].transform.GetPositionX() - gameObject.transform.GetPositionX() < -6f && side <= 0)
            {
                sig = -1f;
                yield return null;
            }
            Vector2 currPos = gameObject.transform.position;
            var height = allDagEndPos[rand].y-currPos.y+1.4f;
            _rgbd.gravityScale = 2f;
            float accel = Physics2D.gravity.y * 2f;
            float vel = (float)Math.Sqrt(-2f * height * accel);
            _anim.Play("Hop");
            lurker2.SetActive(true);
            lurker2.transform.SetPosition2D(gameObject.transform.position);
            StartCoroutine(lurker2.GetComponent<CustomAnimator>().Play("jump", false, 0.01f, gameObject));
            gameObject.GetComponent<MeshRenderer>().enabled = false;
            _rgbd.velocity = new Vector2(0f, vel);
            gameObject.GetComponent<BoxCollider2D>().isTrigger = true;
            yield return new WaitForSeconds(((-1f * vel) / accel));
            Log("donk");
            _rgbd.velocity = new Vector2(0f, 0f);
            _rgbd.gravityScale = 0f;
            yield return null;
            if (side != 0)
            {
                lurker2.SetActive(false);
                gameObject.GetComponent<MeshRenderer>().enabled = true;
                yield return null;
                lurker2.SetActive(true);
                lurker2.transform.SetPosition2D(gameObject.transform.position);
                StartCoroutine(lurker2.GetComponent<CustomAnimator>().Play("dash", false, 0.01f,gameObject));
                gameObject.GetComponent<MeshRenderer>().enabled = false;
                gameObject.transform.localScale.SetX(sig * Mathf.Abs(gameObject.transform.localScale.x));
                lurker2.transform.localScale = new Vector2(-1f * sig * Mathf.Abs(lurker2.transform.localScale.x), lurker2.transform.localScale.y);

            }
            _rgbd.velocity = new Vector2(xSpd * side, 0f);
            float waitT = 0f;
            while(AllSaws[0].transform.GetPositionX() - gameObject.transform.GetPositionX() > -4f && side >= 0)
            {
                waitT += Time.fixedDeltaTime;
                yield return null;
            }
            while (AllSaws[33].transform.GetPositionX() - gameObject.transform.GetPositionX() < 4f && side <= 0)
            {
                waitT += Time.fixedDeltaTime;
                yield return null;
            }
            Log("donk 2");
            lurker2.SetActive(false);
            gameObject.GetComponent<MeshRenderer>().enabled = true;
            _anim.Play("Hop");
            _rgbd.gravityScale = 2.5f;
            _rgbd.velocity = new Vector2(0f, 0f);
            gameObject.GetComponent<BoxCollider2D>().isTrigger = false;
            yield return new WaitWhile(() => gameObject.transform.GetPositionY() > currPos.y+0.1f);
            var sca = gameObject.transform.localScale;
            gameObject.transform.localScale = new Vector3(-1f * sca.x, sca.y, sca.z);
            _anim.Play("Idle");
            if (side != 0)
            {
                yield return new WaitForSeconds(0.1f);
                _anim.Play("Hop");
                _rgbd.velocity = new Vector2(0f, vel);
                yield return new WaitForSeconds(0.1f); //0.1
                _rgbd.velocity = new Vector2(0f, 0f);
                _rgbd.gravityScale = 0f;
                yield return null;

                lurker2.SetActive(true);
                lurker2.transform.SetPosition2D(gameObject.transform.position);
                StartCoroutine(lurker2.GetComponent<CustomAnimator>().Play("dash", false, 0.01f, gameObject));
                gameObject.GetComponent<MeshRenderer>().enabled = false;
                lurker2.transform.localScale = new Vector2(-1f * lurker2.transform.localScale.x, lurker2.transform.localScale.y);

                _rgbd.velocity = new Vector2(-1f * xSpd * side, 0f);
                yield return new WaitWhile(() => !FastApproximately(gameObject.transform.GetPositionX(), 102.5f, 0.1f));
                _rgbd.gravityScale = 2f;
                _rgbd.velocity = new Vector2(0f, 0f);

                lurker2.SetActive(false);
                gameObject.GetComponent<MeshRenderer>().enabled = true;
            }
            _anim.Play("Throw");
            StartCoroutine(gameObject.AddComponent<ObjectFlinger>().DelayStart(-1, 2));
            yield return new WaitUntil(() => _anim.IsPlaying("Throw"));
            yield return new WaitWhile(() => gameObject.transform.GetPositionY() > currPos.y + 0.1f);
            sca = gameObject.transform.localScale;
            gameObject.transform.localScale = new Vector3(-1f * sca.x, sca.y, sca.z);
            _anim.Play("Idle");
            if (round < 10) StartCoroutine(LurkerJumperAI());
            else
            {
                _anim.Play("DigIn 1");
                yield return new WaitWhile(() => _anim.IsPlaying("DigIn 1"));
                _anim.Play("DigIn 2");
                yield return new WaitWhile(() => _anim.IsPlaying("DigIn 2"));
                gameObject.transform.SetPosition2D(110f, 22.8f);
                _rgbd.gravityScale = 0f;
                _rgbd.velocity = new Vector2(0f, 0f);
                _rgbd.isKinematic = true;
                gameObject.GetComponent<BoxCollider2D>().enabled = false;
                _anim.Play("Hop");
            }
        }
        int rand = 0;
        float sawSpd = -10f;
        int round = 0;
        int side = 1;
        int level = 2;
        List<GameObject> smokeBomb = new List<GameObject>();
        public IEnumerator SawMoveSelector()
        {
            Log("Pick Saw Move");
            Log("Current HP: " + _hm.hp);
            Log("Current round " + round);
            if (round > 20)
            {
                _hm.hp = 1000;
                StartCoroutine(HallwaySaw());
                foreach (GameObject i in smokeBomb) i.GetComponent<ParticleSystem>().Stop();
                Log("STOPP");
                yield return new WaitWhile(() => true);
            }

            int random = UnityEngine.Random.Range(0, level);
            if (random == 0)
            {
                HeroController.instance.AddMPChargeSpa(33);
                StartCoroutine(BothSideSaw());
            }
            else if (random == 1) StartCoroutine(OneSideSaw());
            else if (random == 2) StartCoroutine(RandomSaw());
            else if (random == 3) StartCoroutine(VerHorSaw());

            Log("check " + _hm.hp + " " + round);
            if (_hm.hp < 1000 && round == 0)
            {
                Log("HP LESS THAN 1000");
                for (int i = 84; i < 122; i += 2)
                {
                    var smoke = Instantiate(PaleChampion.preloadedGO["smoke"]);
                    smoke.SetActive(true);
                    smoke.layer = 18;
                    smoke.transform.SetPosition3D(i, 6f, 0.2f);
                    smokeBomb.Add(smoke);
                    yield return null;
                }
                sawSpd -= 2f;
                level = 4;
                round = 10;
            }
            if (round >= 10) round++;
            Log("Reached end of choice");
        }

        public IEnumerator OneSideSaw()
        {
            side = negToPos();
            rand = UnityEngine.Random.Range(1, 4);
            newbatch = true;
            ResetSawPos();
            for (int i = 0; i < 5; i++)
            {
                if (i == rand)
                {
                    if (side == 1) AllSaws[rand].SetActive(false);
                    else AllSaws[rand+16].SetActive(false);
                    continue;
                }
                if (side == 1) AllSaws[i].SetActive(true);
                else AllSaws[i+33].SetActive(true);
            }
            yield return null;
            for (int i = 0; i < 5; i++)
            {
                if (i == rand) continue;
                if (side == 1) AllSaws[i].GetComponent<Rigidbody2D>().velocity = new Vector2(sawSpd * side, 0f);
                else AllSaws[i+33].GetComponent<Rigidbody2D>().velocity = new Vector2(sawSpd * side, 0f);
            }
            Log("wait");
            if (side == 1) yield return new WaitWhile(() => AllSaws[0].transform.GetPositionX() > 80f);
            else yield return new WaitWhile(() => AllSaws[33].transform.GetPositionX() < 120.5f);

            Log("wait end");
            for (int i = 0; i < 5; i++)
            {
                if (i == rand) continue;
                if (side == 1)
                {
                    AllSaws[i].transform.SetPosition2D(allDagEndPos[i]);
                    AllSaws[i].GetComponent<Rigidbody2D>().velocity = new Vector2(0f, 0f);
                    AllSaws[i].SetActive(false);
                }
                else
                {
                    AllSaws[i + 33].SetActive(false);
                    AllSaws[i + 33].transform.SetPosition2D(85.5f, allDagEndPos[i].y);
                    AllSaws[i + 33].GetComponent<Rigidbody2D>().velocity = new Vector2(0f, 0f);
                }
            }

            StartCoroutine(SawMoveSelector());
            Log("Move down");

        }
        IEnumerator BothSideSaw()
        {
            side = 0;
            rand = UnityEngine.Random.Range(1, 4);
            newbatch = true;
            ResetSawPos();
            for (int i = 0; i < 5; i++)
            {
                if (i == rand)
                {
                    AllSaws[rand].SetActive(false);
                    AllSaws[rand + 16].SetActive(false);
                    continue;
                }
                AllSaws[i].SetActive(true);
                AllSaws[i + 33].SetActive(true);
            }
            yield return null;
            for (int i = 0; i < 5; i++)
            {
                if (i == rand) continue;
                AllSaws[i].GetComponent<Rigidbody2D>().velocity = new Vector2(sawSpd, 0f);
                AllSaws[i + 33].GetComponent<Rigidbody2D>().velocity = new Vector2(sawSpd * -1f, 0f);
            }
            yield return new WaitWhile(() => AllSaws[0].transform.GetPositionX() > 80f);
            yield return new WaitWhile(() => AllSaws[33].transform.GetPositionX() < 120.5f);

            for (int i = 0; i < 5; i++)
            {
                if (i == rand) continue;
                
                AllSaws[i].transform.SetPosition2D(allDagEndPos[i]);
                AllSaws[i].GetComponent<Rigidbody2D>().velocity = new Vector2(0f, 0f);
                AllSaws[i].SetActive(false);
                AllSaws[i + 33].SetActive(false);
                AllSaws[i + 33].transform.SetPosition2D(85.5f, allDagEndPos[i].y);
                AllSaws[i + 33].GetComponent<Rigidbody2D>().velocity = new Vector2(0f, 0f);
                
            }

            Log("get new saws");
            StartCoroutine(SawMoveSelector());
        }
        IEnumerator VerHorSaw()
        {
            ResetSawPos();
            int sawNum = 12;
            float timeToWait = 0;
            int r = UnityEngine.Random.Range(8, 10);
            int r2 = UnityEngine.Random.Range(3,4);
            float plS = Mathf.Sign(HeroController.instance.transform.GetPositionX() - 102f);

            List<GameObject> listToUse;
            if (plS == 1) listToUse = new List<GameObject>(leftSaw);
            else listToUse = new List<GameObject>(rightSaw);
            yield return null;
            for (int i = 0; i < upSaw.Count; i++)
            {
                if (i == r || i == r - 6) continue;
                GameObject saw2 = Instantiate(upSaw[i]);
                upSaw[i].SetActive(true);
                saw2.SetActive(true);
                yield return null;
                GameObject lastSaw = AddSaw(upSaw[i], saw2, new Vector2(0f, 1f), timeToWait); 

                for (int j = 0; j < sawNum; j++)
                {
                    GameObject newSaw = Instantiate(lastSaw);
                    newSaw.SetActive(true);
                    yield return null;
                    lastSaw = AddSaw(lastSaw, newSaw, new Vector2(0f, 1f), timeToWait);
                }
            }
            yield return null;

            foreach (GameObject saw in upSaw)
            {
                if (saw.activeSelf) saw.GetComponent<Rigidbody2D>().velocity = new Vector2(0f, -8f);
            }
            yield return new WaitForSeconds(0.1f);

            GameObject lS = new GameObject();

            for (int i = 0; i < listToUse.Count; i++)
            {
                Log(listToUse[i].name);
                if (i == r2 || i == r2 - 1) continue;
                GameObject saw2 = Instantiate(listToUse[i]);
                listToUse[i].SetActive(true);
                saw2.SetActive(true);
                yield return null;
                GameObject lastSaw = AddSaw(listToUse[i], saw2, new Vector2(-1f * plS, 0f), timeToWait);

                for (int j = 0; j < sawNum; j++)
                {
                    GameObject newSaw = Instantiate(lastSaw);
                    newSaw.SetActive(true);
                    yield return null;
                    lastSaw = AddSaw(lastSaw, newSaw, new Vector2(-1f * plS, 0f), timeToWait);
                    if ((i == listToUse.Count - 3 || i == listToUse.Count - 1) && j == sawNum - 1) lS = newSaw;
                }
            }
            
            yield return null;

            foreach (GameObject saw in listToUse)
            {
                if (saw.activeSelf) saw.GetComponent<Rigidbody2D>().velocity = new Vector2(plS * 10f, 0f);
            }

            while (true)
            {
                if (plS < 0 && lS.transform.GetPositionX() < 85f) break;
                if (plS > 0 && lS.transform.GetPositionX() > 120f) break;
                yield return null;
            }
            yield return new WaitForSeconds(1f);
            foreach (var i in FindObjectsOfType<GameObject>().Where(x => x.activeSelf && x.name.Contains("saw"))) i.SetActive(false);
            StartCoroutine(SawMoveSelector());
        }

        IEnumerator RandomSaw()
        {
            ResetSawPos();
            int count1 = 0;
            float ang1 = 190f;
            float ang2 = 350f;
            foreach (GameObject i in AllSaws)
            {
                if (UnityEngine.Random.Range(0, 5) == 1)
                {
                    
                    i.SetActive(true);
                    yield return null;
                    Vector2 pos = i.transform.position;
                    var rb = i.GetComponent<Rigidbody2D>();
                    
                    if (Mathf.Approximately(pos.x,85.5f) && pos.y < 20.1f) rb.velocity = new Vector2(-1f * sawSpd, 0f); //left
                    else if (Mathf.Approximately(pos.x, 119.5f) && pos.y < 21f) rb.velocity = new Vector2(sawSpd, 0f); //right
                    else if (!Mathf.Approximately(pos.x, 102.5f) && pos.y > 21f) rb.velocity = new Vector2(0f, sawSpd); //up
                    else if (!Mathf.Approximately(pos.x,102.5f) && pos.y < 5.5f) rb.velocity = new Vector2(0f, -1f * sawSpd); //down
                    else if (Mathf.Approximately(pos.x, 102.5f) && pos.y > 21f) //Diag up 
                    {
                        if (count1 % 2 == 0)
                        {
                            float ang = Mathf.Deg2Rad * ang1;
                            rb.velocity = new Vector2(-1f * sawSpd * Mathf.Cos(ang), -1f * sawSpd * Mathf.Sin(ang));
                            ang1 += 10f;
                        }
                        else
                        {
                            float ang = Mathf.Deg2Rad * ang2;
                            rb.velocity = new Vector2(-1f * sawSpd * Mathf.Cos(ang), -1f * sawSpd * Mathf.Sin(ang));
                            ang2 -= 10f;
                        }
                        count1++;
                    }
                    else
                    {
                        i.SetActive(false);
                    }
                    yield return new WaitForSeconds(0.1f);
                }
            }

            yield return new WaitForSeconds(3.5f);
            StartCoroutine(SawMoveSelector());
        }

        void ResetSawPos()
        {
            for (int i = 0; i < allSawPos.Count; i++)
            {
                AllSaws[i].transform.position = allSawPos[i];
                AllSaws[i].SetActive(false);
            }
            foreach (var i in FindObjectsOfType<GameObject>().Where(x => x.activeSelf && x.name.Contains("saw"))) i.SetActive(false);
        }

        int negToPos()
        {
            int num = UnityEngine.Random.Range(0, 2);
            if (num == 0) num = -1;
            return num;
        }

        GameObject AddSaw(GameObject realSaw, GameObject cloned, Vector2 direction, float delay)
        {
            cloned.transform.SetScaleX(realSaw.transform.GetScaleX());
            cloned.transform.SetScaleY(realSaw.transform.GetScaleY());
            cloned.transform.SetScaleZ(realSaw.transform.GetScaleZ());
            StartCoroutine(FollowGO(cloned, realSaw, direction, delay));
            return cloned;
        }

        IEnumerator FollowGO(GameObject follower, GameObject follow, Vector2 direction, float delay)
        {
            float t = 0;
            follower.transform.position = follow.transform.position;
            while (t < delay || delay == 0)
            {
                yield return null;
                if (stopFollowGO) continue;
                Vector2 fPos = follow.transform.position;
                follower.transform.position = new Vector2(direction.x * 2.3f + fPos.x, direction.y * 2.3f + fPos.y);
                t += Time.fixedDeltaTime;
            }
            Destroy(follower);
        }

        IEnumerator LurkerHallWay()
        {
            yield return new WaitForSeconds(2f);
            hits = 0;
            gameObject.GetComponent<BoxCollider2D>().enabled = true;
            _rgbd.gravityScale = 0f;
            _rgbd.velocity = new Vector2(0f, 0f);
            _rgbd.isKinematic = true;
            while (_hm.hp > 800)
            {
                gameObject.transform.position = new Vector2(85.5f, 15.5f);
                var sca = gameObject.transform.localScale;
                gameObject.transform.localScale = new Vector3(-1f * sca.x, sca.y, sca.z);
                gameObject.transform.SetRotation2D(-90f);
                _anim.Play("DigOut");
                yield return new WaitWhile(() => _anim.IsPlaying("DigOut"));
                gameObject.transform.SetRotation2D(0f);
                _anim.Play("Wall Cling");
                yield return new WaitWhile(() => _anim.IsPlaying("Wall Cling"));
                yield return new WaitForSeconds(0.5f);

                while (hits < 3)
                {
                    for (int i = -1; i < 2; i++)
                    {
                        var bomb = Instantiate(PaleChampion.preloadedGO["bomb"]);
                        bomb.layer = 12;
                        bomb.GetComponent<ParticleSystem>().Play();
                        bomb.SetActive(true);
                        bomb.AddComponent<SpawnPillar>();
                        bomb.transform.Find("normal").gameObject.AddComponent<BombCollider>();
                        bomb.transform.SetPosition2D(gameObject.transform.position.x + 0.5f, gameObject.transform.position.y);
                        bomb.GetComponent<Rigidbody2D>().gravityScale = 1f;
                        bomb.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
                        var p1 = bomb.transform.position;
                        var p2 = HeroController.instance.transform.position;
                        bomb.GetComponent<Rigidbody2D>().velocity = new Vector2((p2 - p1).x * 2.5f + i * 4, (p2 - p1).y * 1.5f);
                        /*var bomb = Instantiate(PaleChampion.preloadedGO["bomb"]);
                        bomb.GetComponent<ParticleSystem>().Play();
                        bomb.SetActive(true);
                        bomb.AddComponent<SpawnPillar>();
                        bomb.transform.SetPosition2D(gameObject.transform.GetPositionX() - 1.5f, gameObject.transform.GetPositionY());
                        var diff = HeroController.instance.transform.position - gameObject.transform.position;
                        bomb.GetComponent<Rigidbody2D>().gravityScale = 1f;
                        bomb.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
                        bomb.GetComponent<Rigidbody2D>().velocity = new Vector2(diff.x * 2f + i * 15f, diff.y + i * 2f);*/
                    }
                    _anim.Play("Throw");
                    yield return new WaitWhile(() => _anim.IsPlaying("Throw"));
                    _anim.Play("Wall Cling");
                    yield return new WaitWhile(() => _anim.IsPlaying("Wall Cling"));
                    yield return new WaitForSeconds(3f);
                }
                hits = 0;
                gameObject.transform.SetRotation2D(-90f);
                _anim.Play("DigIn 1");
                yield return new WaitWhile(() => _anim.IsPlaying("DigIn 1"));
                _anim.Play("DigIn 2");
                yield return new WaitWhile(() => _anim.IsPlaying("DigIn 2"));
                gameObject.transform.SetRotation2D(0f);
                gameObject.transform.position = new Vector2(119.5f, 15.5f);
                sca = gameObject.transform.localScale;
                gameObject.transform.localScale = new Vector3(-1f * sca.x, sca.y, sca.z);
                gameObject.transform.SetRotation2D(90f);
                _anim.Play("DigOut");
                yield return new WaitWhile(() => _anim.IsPlaying("DigOut"));
                gameObject.transform.SetRotation2D(0f);
                _anim.Play("Wall Cling");
                yield return new WaitWhile(() => _anim.IsPlaying("Wall Cling"));
                yield return new WaitForSeconds(0.5f);

                while (hits < 3)
                {
                    for (int i = -1; i < 2; i++)
                    {
                        var bomb = Instantiate(PaleChampion.preloadedGO["bomb"]);
                        bomb.layer = 12;
                        bomb.GetComponent<ParticleSystem>().Play();
                        bomb.SetActive(true);
                        bomb.AddComponent<SpawnPillar>();
                        bomb.transform.Find("normal").gameObject.AddComponent<BombCollider>();
                        bomb.transform.SetPosition2D(gameObject.transform.position.x - 0.5f, gameObject.transform.position.y);
                        bomb.GetComponent<Rigidbody2D>().gravityScale = 1f;
                        bomb.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
                        var p1 = bomb.transform.position;
                        var p2 = HeroController.instance.transform.position;
                        bomb.GetComponent<Rigidbody2D>().velocity = new Vector2((p2 - p1).x*2.5f + i * 4, (p2 - p1).y*1.5f);
                        /*var bomb = Instantiate(PaleChampion.preloadedGO["bomb"]);
                        bomb.GetComponent<ParticleSystem>().Play();
                        bomb.SetActive(true);
                        bomb.AddComponent<SpawnPillar>();
                        bomb.transform.SetPosition2D(gameObject.transform.GetPositionX() - 1.5f, gameObject.transform.GetPositionY());
                        var diff = HeroController.instance.transform.position - gameObject.transform.position;
                        bomb.GetComponent<Rigidbody2D>().gravityScale = 1f;
                        bomb.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
                        bomb.GetComponent<Rigidbody2D>().velocity = new Vector2(diff.x * 2f + i * 15f, diff.y + i * 2f);*/
                    }
                    _anim.Play("Throw");
                    yield return new WaitWhile(() => _anim.IsPlaying("Throw"));
                    _anim.Play("Wall Cling");
                    yield return new WaitWhile(() => _anim.IsPlaying("Wall Cling"));
                    yield return new WaitForSeconds(3f);
                }
                hits = 0;
                //hallWayRounds++;
                gameObject.transform.SetRotation2D(90f);
                _anim.Play("DigIn 1");
                yield return new WaitWhile(() => _anim.IsPlaying("DigIn 1"));
                _anim.Play("DigIn 2");
                yield return new WaitWhile(() => _anim.IsPlaying("DigIn 2"));
                gameObject.transform.SetRotation2D(0f);
                gameObject.transform.position = new Vector3(22f, 112f);
                gameObject.GetComponent<BoxCollider2D>().enabled = true;
                _rgbd.gravityScale = 0f;
                _rgbd.velocity = new Vector2(0f, 0f);
                _rgbd.isKinematic = true;
                yield return null;
            }
        }
        bool stopFollowGO = false;
        //int hallWayRounds = 0;
        IEnumerator HallwaySaw()
        {
            ResetSawPos();
            yield return null;
            List<GameObject> allHallway = new List<GameObject>();
            int sawNum = 15;
            float timeToWait = 0f;
            for (int i = 4; i < 5; i++)//////////////////////////////////////////////
            {
                GameObject saw2 = Instantiate(leftSaw[i]);
                leftSaw[i].SetActive(true);
                saw2.SetActive(true);
                yield return null;
                GameObject lastSaw = AddSaw(leftSaw[i], saw2, new Vector2(-1f, 0f), timeToWait);
                if (i == 4)
                {
                    allHallway.Add(leftSaw[i]);
                    allHallway.Add(saw2);
                }
                for (int j = 0; j < sawNum; j++)
                {
                    GameObject newSaw = Instantiate(lastSaw);
                    newSaw.SetActive(true);
                    yield return null;
                    if (i == 4) allHallway.Add(newSaw);
                    lastSaw = AddSaw(lastSaw, newSaw, new Vector2(-1f, 0f), timeToWait);
                }
            }
            for (int i = 0; i < 1; i++)
            {
                GameObject saw2 = Instantiate(rightSaw[i]);
                rightSaw[i].SetActive(true);
                saw2.SetActive(true);
                yield return null;
                GameObject lastSaw = AddSaw(rightSaw[i], saw2, new Vector2(1f, 0f), timeToWait);
                allHallway.Add(rightSaw[i]);
                allHallway.Add(saw2);
                for (int j = 0; j < sawNum; j++)
                {
                    GameObject newSaw = Instantiate(lastSaw);
                    newSaw.SetActive(true);
                    yield return null;
                    allHallway.Add(newSaw);
                    lastSaw = AddSaw(lastSaw, newSaw, new Vector2(1f, 0f), timeToWait);
                }
            }
            yield return null;

            for (int i = 0; i < leftSaw.Count; i++)
            {
                if (leftSaw[i].activeSelf) leftSaw[i].GetComponent<Rigidbody2D>().velocity = new Vector2(10f, 0f);
                if (rightSaw[i].activeSelf) rightSaw[i].GetComponent<Rigidbody2D>().velocity = new Vector2(-10f, 0f);
            }
            StartCoroutine(LurkerHallWay());
            while (rightSaw[0].transform.GetPositionX() > 84f) yield return null;
            for (int i = 0; i < leftSaw.Count; i++)
            {
                if (leftSaw[i].activeSelf) leftSaw[i].GetComponent<Rigidbody2D>().velocity = new Vector2(0f, 0f);
                if (rightSaw[i].activeSelf) rightSaw[i].GetComponent<Rigidbody2D>().velocity = new Vector2(0f, 0f);
            }
            yield return null;
            stopFollowGO = true;

            while (_hm.hp > 800) 
            {
                int ra = UnityEngine.Random.Range(0, 4);//6
                if (ra  == 0)
                {
                    for (int i = (allHallway.Count + 1) / 2, j = allHallway.Count / 2; i < allHallway.Count; i++, j--)
                    {
                        allHallway[i].AddComponent<HallWaySaw>();
                        allHallway[j].AddComponent<HallWaySaw>();
                        yield return new WaitForSeconds(1.1f);
                    }
                }
                else if (ra == 1)
                {
                    for (int i = (allHallway.Count + 1) / 2, j = 0; i < allHallway.Count; i++, j++)
                    {
                        allHallway[i].AddComponent<HallWaySaw>();
                        allHallway[j].AddComponent<HallWaySaw>();
                        yield return new WaitForSeconds(1.1f);
                    }
                }
                else if (ra == 2)
                {
                    for (int i = 0; i < allHallway.Count / 2; i++)
                    {
                        allHallway[i].AddComponent<HallWaySaw>();
                        allHallway[allHallway.Count - i - 1].AddComponent<HallWaySaw>();
                        yield return new WaitForSeconds(1.2f);
                    }
                }
                else if (ra == 30)
                {
                    for (int i = allHallway.Count - 1; i >= 0; i--)
                    {
                        allHallway[i].AddComponent<HallWaySaw>();
                        yield return new WaitForSeconds(1.2f);
                    }
                }
                else if (ra == 40)
                {
                    foreach (GameObject i in allHallway)
                    {
                        i.AddComponent<HallWaySaw>();
                        yield return new WaitForSeconds(1f);
                    }
                }
                else
                {
                    foreach (GameObject i in allHallway)
                    {
                        if (UnityEngine.Random.Range(0, 5) == 0) i.AddComponent<HallWaySaw>();
                        yield return new WaitForSeconds(1f);
                    }
                }
                yield return new WaitForSeconds(1f);
            }
            yield return new WaitForSeconds(1.5f);
            StartCoroutine(FinalSawPhase());
        }
        IEnumerator SpikeFollowX(GameObject follower, GameObject follow, int side)
        {
            while (true)
            {
                var fPos = follow.transform.position;
                var oPos = follower.transform.position;
                follower.transform.position = new Vector3(fPos.x + 1.7f * side, oPos.y, 1.5f);
                yield return null;
            }
        }
        /*IEnumerator SpikeFollowY(GameObject follower, GameObject follow)
        {
            while (true)
            {
                var fPos = follow.transform.position;
                var oPos = follower.transform.position;
                follower.transform.position = new Vector3(oPos.x, fPos.y + 1.7f, 1.5f);
                yield return null;
            }
        }*/
        IEnumerator FinalSawPhase()
        {
            foreach (GameObject i in allUpperSpike)
            {
                i.LocateMyFSM("Control").SendEvent("RETRACT");
            }
            yield return new WaitForSeconds(0.1f);
            StartCoroutine(UpperPlatPuzz());
            StartCoroutine(BubbleBath());
            yield return new WaitForSeconds(1.5f);
            while (!platPhaseEnd)
            {
                if (GameObject.Find("aspid0") == null)
                {
                    StartCoroutine(EnemyTrial2(87f, 17.5f, 0));
                }
                if (GameObject.Find("aspid1") == null)
                {
                    StartCoroutine(EnemyTrial2(117f, 17.5f, 1));
                }
                yield return new WaitForSeconds(1.5f);
            }
            GameObject.Find("aspid0").GetComponent<AspidControl>()._pos.y += 20f;
            GameObject.Find("aspid1").GetComponent<AspidControl>()._pos.y += 20f;
            yield return new WaitForSeconds(3f);
            Destroy(GameObject.Find("aspid0"));
            Destroy(GameObject.Find("aspid1"));
        }
        public static bool platPhaseEnd;
        IEnumerator UpperPlatPuzz()
        {
            yield return null;
            float upBy = 3.5f;
            float interval = 2f;
            int pillarRound = 0;
            int index = 1;
            //GameObject closePlat = allUpperPlat[0];
            var comp0 = allUpperPlat[index].GetComponent<MoveFloorUp>();
            comp0.GetComponent<MoveFloorUp>().moveTo += upBy;
            comp0.GetComponent<MoveFloorUp>().move = true;
            comp0.GetComponent<MoveFloorUp>().speed = 0.3f;
            while (true)
            {
                if (!allUpperPlat[index].GetComponent<MoveFloorUp>().move && pillarRound < 10)
                {
                    var a = Instantiate(PaleChampion.preloadedGO["pillar"]);
                    a.transform.SetPosition2D(allUpperPlat[index].transform.GetPositionX(), allUpperPlat[index].transform.GetPositionY() + upBy * 2f - 0.5f);
                    a.SetActive(true);
                    yield return new WaitForSeconds(1f);
                    var comp = allUpperPlat[index].GetComponent<MoveFloorUp>();
                    comp.moveTo -= upBy;
                    comp.speed = 0.5f;
                    comp.move = true;
                    yield return new WaitForSeconds(interval);
                    if (pillarRound % 2 == 0 && interval > 0.05f) interval -= 0.39f;
                    Vector2 pl = HeroController.instance.transform.position;
                    index = 1;
                    float minDist = Vector2.Distance(allUpperPlat[index].transform.position, pl);
                    for (int i = 0; i < allUpperPlat.Count; i++)
                    {
                        float temp = Vector2.Distance(allUpperPlat[i].transform.position, pl);
                        if (temp < minDist)
                        {
                            minDist = temp;
                            index = i;
                        }
                    }
                    var comp2 = allUpperPlat[index].GetComponent<MoveFloorUp>();
                    comp2.moveTo += upBy;
                    comp2.speed = 0.3f;
                    comp2.move = true;
                    pillarRound++;
                }
                else if (!allUpperPlat[index].GetComponent<MoveFloorUp>().move && pillarRound == 10)
                {
                    var a = Instantiate(PaleChampion.preloadedGO["pillar"]);
                    a.transform.SetPosition2D(allUpperPlat[index].transform.GetPositionX(), allUpperPlat[index].transform.GetPositionY() + upBy * 2f - 0.5f);
                    a.SetActive(true);
                    yield return new WaitForSeconds(1f);
                    var comp = allUpperPlat[index].GetComponent<MoveFloorUp>();
                    comp.moveTo -= upBy;
                    comp.speed = 0.5f;
                    comp.move = true;
                    yield return new WaitForSeconds(interval);
                    Vector2 pl = HeroController.instance.transform.position;
                    float minDist = Vector2.Distance(allUpperPlat[1].transform.position, pl);
                    index = 1;
                    for (int i = 0; i < allUpperPlat.Count; i++)
                    {
                        float temp = Vector2.Distance(allUpperPlat[i].transform.position, pl);
                        if (temp < minDist)
                        {
                            minDist = temp;
                            index = i;
                        }
                    }
                    if (index < 2) index = 2;
                    if (index > allUpperPlat.Count - 3) index = allUpperPlat.Count - 3;
                    for (int j = -1; j < 2; j++)
                    {
                        var comp2 = allUpperPlat[index + j].GetComponent<MoveFloorUp>();
                        comp2.moveTo += upBy;
                        comp2.speed = 0.2f;
                        comp2.move = true;
                    }
                    pillarRound++;
                }
                else if (!allUpperPlat[index].GetComponent<MoveFloorUp>().move && !allUpperPlat[index + 1].GetComponent<MoveFloorUp>().move && !allUpperPlat[index - 1].GetComponent<MoveFloorUp>().move && pillarRound > 10 && pillarRound < 20)
                {
                    for (int j = -1; j < 2; j++)
                    {
                        var a = Instantiate(PaleChampion.preloadedGO["pillar"]);
                        a.transform.SetPosition2D(allUpperPlat[index + j].transform.GetPositionX(), allUpperPlat[index + j].transform.GetPositionY() + upBy * 2f - 0.5f);
                        a.SetActive(true);
                    }
                    yield return new WaitForSeconds(1f);
                    for (int j = -1; j < 2; j++)
                    {
                        var comp = allUpperPlat[index + j].GetComponent<MoveFloorUp>();
                        comp.moveTo -= upBy;
                        comp.speed = 0.5f;
                        comp.move = true;
                    }
                    yield return new WaitForSeconds(interval);
                    Vector2 pl = HeroController.instance.transform.position;
                    float minDist = Vector2.Distance(allUpperPlat[1].transform.position, pl);
                    index = 1;
                    for (int i = 0; i < allUpperPlat.Count; i++)
                    {
                        float temp = Vector2.Distance(allUpperPlat[i].transform.position, pl);
                        if (temp < minDist)
                        {
                            minDist = temp;
                            index = i;
                        }
                    }
                    if (index < 2) index = 2;
                    if (index > allUpperPlat.Count - 3) index = allUpperPlat.Count - 3;
                    for (int j = -1; j < 2; j++)
                    {
                        var comp = allUpperPlat[index + j].GetComponent<MoveFloorUp>();
                        comp.moveTo += upBy;
                        comp.speed = 0.2f;
                        comp.move = true;
                    }
                    pillarRound++;
                }
                else if (!allUpperPlat[index].GetComponent<MoveFloorUp>().move && !allUpperPlat[index + 1].GetComponent<MoveFloorUp>().move && !allUpperPlat[index - 1].GetComponent<MoveFloorUp>().move && pillarRound == 20)
                {
                    interval = 1.5f;
                    for (int j = -1; j < 2; j++)
                    {
                        var a = Instantiate(PaleChampion.preloadedGO["pillar"]);
                        a.transform.SetPosition2D(allUpperPlat[index + j].transform.GetPositionX(), allUpperPlat[index + j].transform.GetPositionY() + upBy * 2f - 0.5f);
                        a.SetActive(true);
                    }
                    yield return new WaitForSeconds(1f);
                    for (int j = -1; j < 2; j++)
                    {
                        var comp = allUpperPlat[index + j].GetComponent<MoveFloorUp>();
                        comp.moveTo -= upBy;
                        comp.speed = 0.5f;
                        comp.move = true;
                    }
                    yield return new WaitForSeconds(interval);
                    index = UnityEngine.Random.Range(1, allUpperPlat.Count - 1);
                    for (int j = 0; j < allUpperPlat.Count; j++)
                    {
                        if (j == index || j == allUpperPlat.Count - index - 1) continue;
                        var comp2 = allUpperPlat[j].GetComponent<MoveFloorUp>();
                        comp2.moveTo += upBy;
                        comp2.speed = 0.2f;
                        comp2.move = true;
                    }
                    pillarRound++;
                }
                else if (!allUpperPlat[index + 1].GetComponent<MoveFloorUp>().move && pillarRound > 20 && pillarRound < 30) //index+1 might cause problems but im lazy ;)
                {
                    yield return new WaitForSeconds(0.75f);
                    for (int j = 0; j < allUpperPlat.Count; j++)
                    {
                        if (j == index || j == allUpperPlat.Count - index - 1) continue;
                        var a = Instantiate(PaleChampion.preloadedGO["pillar"]);
                        a.transform.SetPosition2D(allUpperPlat[j].transform.GetPositionX(), allUpperPlat[j].transform.GetPositionY() + upBy * 2f - 0.5f);
                        a.SetActive(true);
                    }
                    yield return new WaitForSeconds(1f);
                    for (int j = 0; j < allUpperPlat.Count; j++)
                    {
                        if (j == index || j == allUpperPlat.Count - index - 1) continue;
                        var comp = allUpperPlat[j].GetComponent<MoveFloorUp>();
                        comp.moveTo -= upBy;
                        comp.speed = 0.5f;
                        comp.move = true;
                    }
                    yield return new WaitForSeconds(interval);
                    if (pillarRound % 2 == 0) interval -= 0.2f;
                    index = UnityEngine.Random.Range(1, allUpperPlat.Count / 2 - 1);
                    for (int j = 0; j < allUpperPlat.Count; j++)
                    {
                        if (j == index || j == allUpperPlat.Count - index - 1) continue;
                        var comp2 = allUpperPlat[j].GetComponent<MoveFloorUp>();
                        comp2.moveTo += upBy;
                        comp2.speed = 0.2f;
                        comp2.move = true;
                    }
                    pillarRound++;
                }
                else if (!allUpperPlat[index + 1].GetComponent<MoveFloorUp>().move && pillarRound >= 30) //index+1 might cause problems but im lazy ;)
                {
                    yield return new WaitForSeconds(0.75f);
                    for (int j = 0; j < allUpperPlat.Count; j++)
                    {
                        if (j == index || j == allUpperPlat.Count - index - 1) continue;
                        var a = Instantiate(PaleChampion.preloadedGO["pillar"]);
                        a.transform.SetPosition2D(allUpperPlat[j].transform.GetPositionX(), allUpperPlat[j].transform.GetPositionY() + upBy * 2f - 0.5f);
                        a.SetActive(true);
                    }
                    yield return new WaitForSeconds(1f);
                    for (int j = 0; j < allUpperPlat.Count; j++)
                    {
                        if (j == index || j == allUpperPlat.Count - index - 1) continue;
                        var comp = allUpperPlat[j].GetComponent<MoveFloorUp>();
                        comp.moveTo -= upBy;
                        comp.speed = 0.5f;
                        comp.move = true;
                    }
                    yield return new WaitForSeconds(1.5f);
                    stopFollowGO = false;
                    yield return null;
                    for (int i = 0; i < leftSaw.Count; i++)
                    {
                        if (leftSaw[i].activeSelf) leftSaw[i].GetComponent<Rigidbody2D>().velocity = new Vector2(10f, 0f);
                        if (rightSaw[i].activeSelf) rightSaw[i].GetComponent<Rigidbody2D>().velocity = new Vector2(-10f, 0f);
                    }
                    while (rightSaw[0].transform.GetPositionX() > 45f) yield return null;
                    yield return new WaitForSeconds(5f);
                    Log("wait for plat");
                    platPhaseEnd = true;
                    foreach (var i in allUpperPlat)
                    {
                        i.LocateMyFSM("Control").SendEvent("PLAT RETRACT");
                    }
                    Log("die plat");
                    foreach (var i in FindObjectsOfType<GameObject>().Where(x => x.activeSelf && x.name.Contains("saw"))) i.SetActive(false);
                    Log("die again");
                    break;
                }
                yield return null;
            }
            Log("hmmmmm");
        }
        IEnumerator BubbleBath()
        {
            var spit = Instantiate(PaleChampion.preloadedGO["spit"]);
            spit.SetActive(true);
            yield return null;
            spit.transform.SetPosition2D(86f, 6f);
            spit.AddComponent<Rigidbody2D>().velocity = new Vector2(10f, 0f);
            spit.AddComponent<DamageAdder>();
            spit.name += "dank";
            int ballRounds = 0;
            float bubRate = 2f;
            while (!end)
            {
                spit = Instantiate(PaleChampion.preloadedGO["spit"]);
                spit.SetActive(true);
                yield return null;
                spit.transform.SetPosition2D(86f, 6f);
                spit.AddComponent<Rigidbody2D>().velocity = new Vector2(10f, 0f);
                spit.AddComponent<DamageAdder>();

                var spit2 = Instantiate(PaleChampion.preloadedGO["spit"]);
                spit2.SetActive(true);
                yield return null;
                spit2.transform.SetPosition2D(119f, 6f);
                spit2.AddComponent<Rigidbody2D>().velocity = new Vector2(-10f, 0f);
                spit2.AddComponent<DamageAdder>();
                if (platPhaseEnd)
                {
                    spit.transform.localScale *= 1.6f;
                    spit2.transform.localScale *= 1.6f;
                }
                spit.name += "dank";
                spit2.name += "dank";
                if (ballRounds % 15 == 0 && bubRate > 1f) bubRate -= 0.3f;
                if (ballRounds == 70 && platPhaseEnd) StartCoroutine(BallBouce());
                ballRounds++;
                yield return new WaitForSeconds(bubRate);
            }
        }
        IEnumerator BallBouce()
        {
            Log("Ball bouncer");
            var spit3 = Instantiate(PaleChampion.preloadedGO["spit"]);
            spit3.SetActive(true);
            yield return null;
            spit3.transform.SetPosition2D(115f, 21f);
            spit3.transform.localScale *= 2f;
            spit3.AddComponent<Rigidbody2D>();
            spit3.AddComponent<DamageAdder>().bigBalls = true;
            spit3.name += "dank";

            var spit4 = Instantiate(PaleChampion.preloadedGO["spit"]);
            spit4.SetActive(true);
            yield return null;
            spit4.transform.SetPosition2D(90f, 21f);
            spit4.transform.localScale *= 2f;
            spit4.AddComponent<Rigidbody2D>();
            spit4.AddComponent<DamageAdder>().bigBalls = true;
            spit4.name += "dank";
        }
        bool end;
        IEnumerator OblobPhase()
        {
            var a = Instantiate(PaleChampion.preloadedGO["ob"]);
            a.SetActive(true);
            yield return null;
            a.LocateMyFSM("Fatty Fly Attack").SendEvent("START");
            a.LocateMyFSM("Fatty Fly Attack").ChangeTransition("Wait", "FINISHED", "Wait");
            a.transform.SetPosition2D(110f, 14f);
            a.AddComponent<OblobControl>(); //give balls another GO that has layer ground
            a.GetComponent<BoxCollider2D>().enabled = false;
            //yield return new WaitForSeconds(1f);
            //platPhaseEnd = true;
            yield return new WaitWhile(() => a.transform.position.y < 28f);
            lurker2.SetActive(true);
            a.GetComponent<BoxCollider2D>().enabled = true;
            a.GetComponent<DamageHero>().enabled = true;
            a.GetComponent<DamageHero>().damageDealt = 1;
            var hp = a.GetComponent<HealthManager>();
            StartCoroutine(lurker2.GetComponent<CustomAnimator>().Play("idle", true));
            float wave = 0f;
            StartCoroutine(Lurker2Attack(a));
            while (hp.hp > 100 || a.GetComponent<OblobControl>().attacking)
            {
                Vector3 offset = new Vector3(0f, 2.5f, -1f);
                if (lurker2.GetComponent<CustomAnimator>().animCurr == "throw") offset = new Vector3(0.1f, 2.3f, -1f);
                if (lurker2.GetComponent<CustomAnimator>().animCurr == "jump") offset = new Vector3(0.1f, 2.5f, -1f);
                var lc = lurker2.transform.localScale;
                var hSide = Mathf.Sign(HeroController.instance.transform.GetPositionX() - lurker2.transform.GetPositionX());
                lurker2.transform.SetPosition3D(a.transform.position.x + offset.x, a.transform.position.y + 0.05f * Mathf.Cos(wave) + offset.y, a.transform.position.z + offset.z);
                lurker2.transform.localScale = new Vector2(Mathf.Abs(lc.x) * hSide, lc.y);
                wave += 0.65f;
                Log("hp " + hp.hp);
                if (hp.hp <= 100)
                {
                    a.GetComponent<OblobControl>().end = true;
                    end = true;
                }
                yield return null;
            }
            //hp.hp = 0;
            a.GetComponent<OblobControl>().end = true;
            end = true;
            yield return new WaitForSeconds(1f);
            StartCoroutine(lurker2.GetComponent<CustomAnimator>().Play("jump", false));
            yield return new WaitWhile(() => lurker2.GetComponent<CustomAnimator>().animCurr != "jump");
            lurker2.transform.SetPosition3D(a.transform.position.x + 0.1f, a.transform.position.y + 0.05f * Mathf.Cos(wave) + 2.5f, a.transform.position.z + -1f);
            yield return new WaitForSeconds(4f * 0.1f);
            Log("hi");
            lurker2.GetComponent<Rigidbody2D>().velocity = new Vector2(0f, 20f);
            while (GameObject.Find("Spitter Shot R(Clone)(Clone)dank") != null)
            {
                Destroy(GameObject.Find("Spitter Shot R(Clone)(Clone)dank"));
                yield return new WaitForSeconds(0.01f);
            }
            Log("kill");
            gameObject.transform.SetPosition2D(105f, 21.5f);
            _anim.Play("Hop");
            gameObject.GetComponent<BoxCollider2D>().enabled = true;
            _rgbd.isKinematic = true;
            _rgbd.gravityScale = 0f;
            _rgbd.velocity = new Vector2(0f, 0f);
            foreach (var i in allLowerSpike) i.LocateMyFSM("Control").SendEvent("RETRACT");
            yield return new WaitForSeconds(3f);
            _rgbd.isKinematic = false;
            _rgbd.gravityScale = 2f;
            yield return new WaitForSeconds(0.5f);
            _hm.hp = 0;
            //yield return new WaitWhile(() => _rgbd.velocity != new Vector2(0f, 0f));
            //_rgbd.velocity = new Vector2(0f, 0f);
            //_anim.Play("Idle");
            Log("back");
            //hm.transform.SetPosition2D(HeroController.instance.transform.position);
            yield return new WaitForSeconds(3f);
            GameCameras.instance.cameraFadeFSM.Fsm.Event("START FADE");
           

           // GameCameras.instance.cameraFadeFSM.Fsm.SetState("FadeIn");
             
        }
        IEnumerator Lurker2Attack(GameObject oblob)
        {
            var hp = oblob.GetComponent<HealthManager>();
            while (hp.hp > 130)
            {
                if (!oblob.GetComponent<OblobControl>().attacking)
                {
                    StartCoroutine(lurker2.GetComponent<CustomAnimator>().Play("throw", false, 0.05f));
                    yield return new WaitWhile(() => lurker2.GetComponent<CustomAnimator>().animCurr != "throw");
                    yield return new WaitForSeconds(0.05f * 8f);
                    var bomb = Instantiate(PaleChampion.preloadedGO["bomb"]);
                    bomb.layer = 12;
                    bomb.GetComponent<ParticleSystem>().Play();
                    bomb.SetActive(true);
                    bomb.AddComponent<SpawnPillar>();
                    bomb.transform.Find("normal").gameObject.AddComponent<BombCollider>();
                    bomb.transform.SetPosition2D(lurker2.transform.position.x, lurker2.transform.position.y + 0.2f);
                    bomb.GetComponent<Rigidbody2D>().gravityScale = 1f;
                    bomb.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
                    var p1 = bomb.transform.position;
                    var p2 = HeroController.instance.transform.position;
                    bomb.GetComponent<Rigidbody2D>().velocity = (p2 - p1) * 2f;
                    yield return new WaitWhile(() => lurker2.GetComponent<CustomAnimator>().playing);
                    StartCoroutine(lurker2.GetComponent<CustomAnimator>().Play("idle", true));
                    yield return new WaitForSeconds(1.7f);
                }
                yield return null;
            }
        }

        private static GameObject[] _allSpikes = new GameObject[25];
        public static bool setUpEnd = false;
        public static int _temp = 0;
        List<GameObject> AllSaws = new List<GameObject>();
        IEnumerator EnemyTrial(float x, float y)
        {
            StartCoroutine(SpawnEnemies(x - 14f, y + 6f,0));
            yield return null;
            StartCoroutine(SpawnEnemies(x + 14f, y + 6f,1));
            yield return null;
            yield return new WaitForSeconds(3.5f);
            while (true)
            {
                bool isBreak = true;
                foreach (var i in FindObjectsOfType<GameObject>().Where(asp => asp.activeSelf && asp.name.Contains("aspid")))
                {
                    isBreak = false;
                }
                if (isBreak) break;
                yield return null;
            }
            if (!setUpEnd)
            {
                StartCoroutine(EnemyTrial(x, y));
            }
        }
        IEnumerator EnemyTrial2(float x, float y, int aspidN)
        {
            StartCoroutine(SpawnEnemies(x, y, aspidN));
            yield return new WaitForSeconds(2.5f);
        }

        public float GetSide()
        {
            return gameObject.transform.GetScaleX() * -1f;
        }

        IEnumerator ReturnSpike(GameObject go, Vector2 pos)
        {
            go.LocateMyFSM("Control").SendEvent("RETRACT");
            yield return new WaitWhile(() => gameObject.transform.GetPositionY() <= pos.y);
            go.LocateMyFSM("Control").SetState("Init");
            yield return null;
            go.LocateMyFSM("Control").SendEvent("EXPAND");
        }

        GameObject currentPlat;
        IEnumerator SpawnPlatform(int type = 0, float x = 102.6f, float y = 8f, float time = 1f, float rotation = 0f)
        {
            string transition = "PLAT EXPAND";
            string retract = "PLAT RETRACT";
            GameObject plat = Instantiate(PaleChampion.preloadedGO["platform"]); ;
            if (type == 1) //Spike
            {
                plat = Instantiate(PaleChampion.preloadedGO["spike"]);
                transition = "EXPAND";
                retract = "RETRACT";
            }
            currentPlat = plat;
            plat.SetActive(true);
            yield return null;
            if (type != 1) plat.transform.SetPosition2D(x, y);
            else plat.transform.SetPosition3D(x, y, 0.5f);
            plat.transform.SetPosition2D(x, y);
            if (type == 1)
            {
                plat.transform.SetRotation2D(rotation);
                plat.LocateMyFSM("Control").GetAction<Wait>("Antic", 2).time = 0.8f;
            }
            plat.LocateMyFSM("Control").SetState("Init");
            yield return null;
            plat.LocateMyFSM("Control").SendEvent(transition);
            //plat.transform.SetPositionZ(HeroController.instance.transform.GetPositionZ());
            yield return null;
            if (type == 0)
            {
                plat.GetComponent<BoxCollider2D>().size = new Vector2(3.4f, 1f);
                plat.GetComponent<BoxCollider2D>().offset = new Vector2(0f, 0.7f);
            }
            if (type == 1)
            {
                plat.AddComponent<SpikeCollider>();
            }
            if (!retractSpikes1) plat.LocateMyFSM("Control").SendEvent(retract);
        }
        IEnumerator SpawnEnemies(float x = 102.6f, float y = 8f, int aspidN = 0)
        {
            var enem = Instantiate(PaleChampion.preloadedGO["cage"]);
            var enemFsm = enem.LocateMyFSM("Spawn");
            enem.SetActive(true);
            enem.transform.SetPosition2D(x, y);

            enemFsm.SetState("Init");
            yield return null;
            enemFsm.SendEvent("SUMMON");
            yield return null;
            enemFsm.SendEvent("SPAWN");
            yield return null;
            enemFsm.SendEvent("SUMMON");

            if (true) /////////////////////////////////////////////////////////
            {
                GameObject aspid = enemFsm.GetAction<ActivateGameObject>("Spawn", 0).gameObject.GameObject.Value;
                aspid.AddComponent<AspidControl>();
                aspid.name = "aspid" + aspidN;
            }
            else
            {
                GameObject aspid = enemFsm.GetAction<ActivateGameObject>("Spawn", 0).gameObject.GameObject.Value;
                aspid.AddComponent<SpecialAspid>().mode = aspidN;
                aspid.name = "aspid" + aspidN;
            }
        }

        private void OnDestroy()
        {
            USceneManager.activeSceneChanged -= SceneChanged;
            gameObject.GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture = oldPLTex;
            Destroy(gameObject);
        }

        private static void Log(object obj)
        {
            Logger.Log("[Pale Champion] " + obj);
        }
    }
}
 