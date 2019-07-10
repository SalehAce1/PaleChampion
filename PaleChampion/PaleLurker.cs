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
    internal class PaleLurker : MonoBehaviour
    {
        private readonly Dictionary<string, float> _fpsDict = new Dictionary<string, float> //Use to change animation speed
        {
            //["DigIn 1"] = 60,
            //["DigIn 2"] = 60
            //["DigOut"] = 60
        };
        public static HealthManager _hm;
        private tk2dSpriteAnimator _anim;
        private PlayMakerFSM _control;
        private static bool _lurkerChanged;
        private Texture _oldTex;
        private bool isDigFixed;
        public static int hits = 0;
        public static GameObject[] sawBlades = new GameObject[4];
        public GameObject[] lurkerBarbs = new GameObject[5];
        Texture oldPLTex;
        private PlayMakerFSM dreamNail;
        private string[] HeroAttacks = { "Slash", "DownSlash", "UpSlash", "Fireball2 Spiral(Clone)", "Sharp Shadow",
                                         "Hit R", "Hit L", "Hit U", "Q Fall Damage", "Great Slash", "Dash Slash" };
        private Rigidbody2D _rgbd;

        public int TakeDamage(int damage)
        {
            //hits++;
            return damage;
        }

        public void velocitySetter()
        {
            var xPHero = HeroController.instance.transform.GetPositionX();
            var xPLurker = gameObject.transform.GetPositionX();
            var xL = gameObject.GetComponent<Rigidbody2D>().velocity.x;
            var yL = 15f + Math.Abs(xPHero - xPLurker);
            var scaleL = gameObject.transform.localScale;
            gameObject.transform.localScale = new Vector3(scaleL.x * -1f, scaleL.y, scaleL.z);
            gameObject.GetComponent<Rigidbody2D>().velocity = new Vector2(-1f * xL, yL);

        }

        public void teleportLurker()
        {
            var xH = HeroController.instance.transform.GetPositionX();
            var yH = HeroController.instance.transform.GetPositionY();
            var xL = gameObject.transform.GetPositionX();
            var yL = gameObject.transform.GetPositionY();

            if (phase2)
            {
                gameObject.transform.SetPosition2D(119.7f, 16f);
                //gameObject.GetComponent<Rigidbody2D>().velocity = new Vector2(10f, 0f);
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
        bool jumpingPlat = false;
        int hitsTotal;
        public void Evade()
        {
            var xH = HeroController.instance.transform.GetPositionX();
            var yH = HeroController.instance.transform.GetPositionY();
            var xL = gameObject.transform.GetPositionX();
            var yL = gameObject.transform.GetPositionY();

            if (hits > 0)//|| Math.Abs(xL-xH) > 10f)
            {
                hits = 0;
                _control.SetState("Dig 1");
            }
        }

        private void Awake()
        {
            Log("Added PaleLurker Mono");
            _hm = gameObject.GetComponent<HealthManager>();
            _control = gameObject.LocateMyFSM("Lurker Control");
            _anim = gameObject.GetComponent<tk2dSpriteAnimator>();
            _rgbd = gameObject.GetComponent<Rigidbody2D>();
            dreamNail = HeroController.instance.gameObject.LocateMyFSM("Dream Nail");
        }
        private void Start()
        {
            try
            {
                //HeroController.instance.spellControl.gameObject.GetComponent<AudioSource>().PlayOneShot(MusicLoad.LoadAssets.PLMusic);
                oldPLTex = gameObject.GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture;
                gameObject.GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture = PaleChampion.SPRITES[3].texture;
                Log("Setting health");
                Destroy(GameObject.Find("Roof"));
                //_hm.hp = 1800;
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
                Log("Lurker runs away if she hurts player");
                ModHooks.Instance.TakeHealthHook += TakeDamage;

                Log("Make Lurker follow you");
                _control.InsertMethod(" Hop", 6, velocitySetter);

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
                _control.InsertMethod("Hop Antic", 0, Evade);

                Log("Make pillar spikes appear when digging");
                _control.InsertMethod("Dig 1", 0, PillarOrSaw);
                /*_control.RemoveAction("Dig 2", 0);
                _control.InsertAction("Dig 2", new Wait
                {
                    time = 0.1f,
                    finishEvent = FsmEvent.Finished,
                    realTime = false
                },0);*/

                Log("Throw barbs in a spread");
                _control.InsertMethod("Wallbarb", 0, ThrowTrap);
                _control.InsertMethod("Wallthrow", 0, RemoveTrap);

                Log("Spike placement on floor and walls");
                //_control.InsertMethod("Wall Cling", 0, PlaceSpike);
                //_control.InsertMethod("Hop Antic", 0, PlaceSpike);

                Log("Start fight");
                _control.InsertMethod("Idle", 0, () => _control.SendEvent("HERO L"));

                Log("Fix dig after intial digout");
                StartCoroutine(FixDig());
                //Log("High Jump State");
                //_control.CopyState("Musix", "High Jump");
                //_control.RemoveAction("High Jump", 0);
                //_control.InsertCoroutine("High Jump", 0, WaitingJump);
                //_control.RemoveTransition("High Jump", "FINISHED");
                //_control.AddTransition("High Jump", "JUMPDONE", "Idle");

            }
            catch (Exception e)
            {
                Log(e);
            }
        }

        public IEnumerator FixDig()
        {
            yield return new WaitWhile(() => _control.ActiveStateName == "Dig Out");
            _control.ChangeTransition("Dig 2", "FINISHED", "Teleport");
            _control.RemoveAction("Teleport", 0);
            _control.RemoveAction("Teleport", 0);
            _control.RemoveAction("Teleport", 0);
            _control.InsertMethod("Teleport", 0, teleportLurker);

            _control.RemoveTransition("Teleport", "FINISHED");
            _control.InsertCoroutine("Teleport", 1, Phase2);
        }


        public IEnumerator WaitingJump()
        {
            yield return null;
            _anim.Play("DigOut");
            yield return new WaitUntil(() => _anim.IsPlaying("DigOut"));
            gameObject.GetComponent<Collider2D>().enabled = true;
            gameObject.GetComponent<Rigidbody2D>().isKinematic = false;
            _anim.Play("Wallhop Antic");
            yield return new WaitUntil(() => _anim.IsPlaying("Wallhop Antic"));
            gameObject.GetComponent<Rigidbody2D>().velocity = new Vector2(-20f * gameObject.transform.GetScaleX(), 60f * 0.9f);
            _anim.Play("Hop");
            yield return null;
            gameObject.GetComponent<Rigidbody2D>().gravityScale = 2f;
            yield return null;
            yield return new WaitWhile(() => Math.Abs(gameObject.GetComponent<Rigidbody2D>().velocity.y - 1f) > 1f);
            _anim.Play("Throw");
            yield return new WaitUntil(() => _anim.IsPlaying("Throw"));
            gameObject.AddComponent<ObjectFlinger>();
            var oldVe = gameObject.GetComponent<Rigidbody2D>().velocity;
            gameObject.GetComponent<Rigidbody2D>().velocity = new Vector2(0f, 0f);
            gameObject.GetComponent<Rigidbody2D>().gravityScale = 0f;
            yield return new WaitForSeconds(0.5f);
            gameObject.GetComponent<Rigidbody2D>().gravityScale = 2f;
            gameObject.GetComponent<Rigidbody2D>().velocity = oldVe;
            Log("dungo");
            yield return new WaitWhile(() => gameObject.GetComponent<Rigidbody2D>().velocity.y != 0f);
            gameObject.GetComponent<Rigidbody2D>().velocity = new Vector2(0f, 0f);
            _control.SetState("Idle");

        }
        public void ThrowTrap()
        {
            var throwing = _control.GetAction<FlingObjectsFromGlobalPoolVel>("Wallbarb", 4);
            for (int i = 0; i < 5; i++)
            {
                _control.AddAction("Wallbarb", new FlingObjectsFromGlobalPoolVel
                {
                    gameObject = throwing.gameObject,
                    spawnPoint = throwing.spawnPoint,
                    position = throwing.position,
                    spawnMin = throwing.spawnMin,
                    spawnMax = throwing.spawnMax,
                    speedMinX = (throwing.speedMaxX.Value + 10f * i * GetSide()),
                    speedMaxX = (throwing.speedMaxX.Value + 10f * i * GetSide()),
                    speedMinY = throwing.speedMaxY.Value + 20f,
                    speedMaxY = throwing.speedMaxY.Value + 20f,
                    originVariationX = 0,
                    originVariationY = 0
                });
            }
        }

        public void RemoveTrap()
        {
            for (int i = 0; i < 4; i++)
            {
                _control.RemoveAction("Wallbarb", 5 + i);
            }
        }

        public float GetSide()
        {
            return gameObject.transform.GetScaleX() * -1f;
        }

        int ree = 0;
        void OnTriggerEnter2D(Collider2D col)
        {
            //Needs fluke, elegy, and more
            if (HeroAttacks.Contains(col.name))
            {
                hits++;
            }
            if (col.name == "Colosseum Spike(Clone)(Clone)")
            {
                //while (GameObject.Find(col.name) != null) GameObject.Find(col.name).LocateMyFSM("Control").SendEvent("RETRACT");
                //col.gameObject.LocateMyFSM("Control").SendEvent("RETRACT");
                if (ree > 0)
                {
                    foreach (GameObject i in _allSpikes)
                    {
                        i.LocateMyFSM("Control").SendEvent("RETRACT");
                    }
                }
                ree++;
            }
        }
        public int digCount = 0;
        public void PillarOrSaw()
        {
            return;
            if (digCount < 3)
            {
                digCount++;
                _control.GetAction<Wait>("Dig 2", 0).time = 0.1f;
                StartCoroutine(SpawnDungPillar());
            }
            else
            {
                digCount = 0;
                _control.GetAction<Wait>("Dig 2", 0).time = 5f;
                StartCoroutine(SpawnSawsHorizontal());
            }
        }
        public IEnumerator SpawnSawsHorizontal()
        {
            for (int i = 0; i < sawBlades.Length; i++)
            {
                sawBlades[i] = Instantiate(LurkerFinder.sawOrig);
                sawBlades[i].SetActive(true);
                sawBlades[i].GetComponent<DamageHero>().hazardType = 0;
                sawBlades[i].AddComponent<Rigidbody2D>();
                sawBlades[i].GetComponent<Rigidbody2D>().gravityScale = 0f;
                sawBlades[i].GetComponent<Rigidbody2D>().isKinematic = true;
                sawBlades[i].transform.SetPosition2D(119.7f, 6.4f + i * 2.5f);
                sawBlades[i].GetComponent<Rigidbody2D>().velocity = new Vector2(-15f, 0f);
                yield return new WaitForSeconds(0.8f);
            }
        }

        public void PlaceSpike()
        {
            var xH = HeroController.instance.transform.GetPositionX();
            var yH = HeroController.instance.transform.GetPositionY();
            var xL = gameObject.transform.GetPositionX();
            var yL = gameObject.transform.GetPositionY();

            /*if (_control.ActiveStateName == "Hop Antic")
            {
                StartCoroutine(SpawnPlatform(1, xL, yL - 0.5f, 2f));
            }
            else if (_control.ActiveStateName == "Wall Cling")
            {
                StartCoroutine(SpawnPlatform(1, xL, yL - 0.5f, 2f));
            }*/
        }

        public IEnumerator SpawnDungPillar()
        {
            var xL = gameObject.transform.GetPositionX();
            var yL = gameObject.transform.GetPositionY();
            yield return new WaitForSeconds(0.4f);
            for (int i = 0; i < 3; i++)
            {
                var DungPill1 = Instantiate(LurkerFinder.pillarWD);
                var DungPill2 = Instantiate(LurkerFinder.pillarWD);
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
        bool goToWall = true;
        private void Update()
        {
            try
            {
                ///var xH = HeroController.instance.transform.GetPositionX();
                //var yH = HeroController.instance.transform.GetPositionY();
                //var xL = gameObject.transform.GetPositionX();
                //var yL = gameObject.transform.GetPositionY();
            }
            catch (System.Exception e)
            {
                Log(e);
            }

        }
        GameObject currentPlat;
        IEnumerator SpawnPlatform(int type = 0, float x = 102.6f, float y = 8f, float time = 1f, float rotation = 0f)
        {
            string transition = "PLAT EXPAND";
            string retract = "PLAT RETRACT";
            if (type == 1) //Spike
            {
                transition = "EXPAND";
                retract = "RETRACT";
            }
            if (type != 0 && type != 1) //All walls
            {
                transition = "MOVE";
                retract = "RESET";
            }
            if (type == 2) //Ceiling
            {
                x = 102.6f;
                y = 8f;
            }
            else if (type == 3) //Right wall
            {
                x = 119.7f;
                y = 4f;
            }
            else if (type == 4) //Left wall
            {
                x = 85.5f;
                y = 4f;
            }

            var plat = Instantiate(LoadGO.platformCol[type]);
            currentPlat = plat;
            plat.SetActive(true);
            if (type != 1) plat.transform.SetPosition2D(x, y);
            else plat.transform.SetPosition3D(x, y, 0.5f);
            plat.transform.SetPosition2D(x, y);
            if (type == 1 && _control.ActiveStateName == "Wall Cling")
            {
                rotation = 90f * gameObject.transform.GetScaleX() * -1f;
            }
            if (type == 2)
            {
                try
                {
                    plat.transform.SetRotation2D(270);
                    Destroy(GameObject.Find("cage_walls_FG_silhouette"));
                    plat.LocateMyFSM("Control").GetAction<SetPosition>("Init", 2).vector = new Vector3(x, y);
                    plat.LocateMyFSM("Control").ChangeTransition("Check Dir", "OUT", "Move In");
                    plat.LocateMyFSM("Control").ChangeTransition("Check Dir", "CANCEL", "Move In");
                    plat.LocateMyFSM("Control").ChangeTransition("Check Dir", "IN", "Move In");
                    plat.LocateMyFSM("Control").GetAction<iTweenMoveTo>("Move In", 1).vectorPosition = new Vector3(90f, 12f, plat.transform.position.z);
                    plat.LocateMyFSM("Control").RemoveAction("Move In", 4);
                }
                catch (Exception e)
                {
                    Log(e);
                }
            }
            if (type != 0 && type != 2)
            {
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

        IEnumerator SpawnEnemies(float x = 102.6f, float y = 8f)
        {
            var enem = Instantiate(LoadGO.colCage[0]);
            enem.SetActive(true);
            enem.transform.SetPosition2D(x, y);

            enem.LocateMyFSM("Spawn").SetState("Init");
            yield return null;
            enem.LocateMyFSM("Spawn").SendEvent("SUMMON");
            yield return null;
            enem.LocateMyFSM("Spawn").SendEvent("SPAWN");
            yield return null;
            enem.LocateMyFSM("Spawn").SendEvent("SUMMON");
        }

        private void OnDestroy()
        {
            gameObject.GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture = oldPLTex;
            ModHooks.Instance.TakeHealthHook -= TakeDamage;
            Destroy(gameObject);
        }

        private static void Log(object obj)
        {
            Logger.Log("[Pale Champion] " + obj);
        }
        bool phase2 = true;
        bool retractSpikes1 = true;
        bool isGravityOn = false;
        PlayMakerFSM _controlCopy;

        private IEnumerator Phase2()
        {
            Log("Start Phase 2");
            yield return null;
            _control.enabled = false;
            StartCoroutine(GoToWall1());
        }

        private IEnumerator GoToWall1()
        {
            gameObject.GetComponent<Rigidbody2D>().gravityScale = 0f;
            gameObject.GetComponent<Rigidbody2D>().velocity = new Vector2(0f, 0f);
            _anim.Play("DigOut");
            gameObject.transform.SetRotation2D(90f);
            yield return new WaitWhile(() => _anim.IsPlaying("DigOut"));
            gameObject.transform.SetRotation2D(0f);
            var ls = gameObject.transform.localScale;
            gameObject.transform.localScale = new Vector3(-1f, ls.y);
            gameObject.GetComponent<BoxCollider2D>().enabled = true;
            gameObject.GetComponent<Rigidbody2D>().isKinematic = false;
            _anim.Play("Wall Cling");
            retractSpikes1 = false;
            var temp = 0;
            for (int i = 84; i < 122; i += 2)
            {
                StartCoroutine(SpawnPlatform(1, i, 6.1f, 1));
                yield return null;
                _allSpikes[temp] = currentPlat;
                temp++;
            }
            StartCoroutine(BombNJump2());
        }

        private IEnumerator BombNJump2()
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

            for (int t = 0; t < 0; t++) /////////////////////////////////////////////////////////////
            {
                for (int i = -1; i < 2; i++)
                {
                    var bomb = Instantiate(LurkerFinder.bombFire);
                    bomb.GetComponent<ParticleSystem>().Play();
                    bomb.SetActive(true);
                    bomb.AddComponent<SpawnPillar>();
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
            gameObject.GetComponent<Rigidbody2D>().velocity = new Vector2(25f * GetSide(), 30f);
            gameObject.GetComponent<Rigidbody2D>().gravityScale = 2f;
            yield return new WaitWhile(() => !lurkOnPlat);
            lurkOnPlat = false;
            gameObject.GetComponent<Rigidbody2D>().velocity = new Vector2(0f, 0f);
            _anim.Play("Idle");
            StartCoroutine(JumpNEnemySpawn3());
        }

        private IEnumerator JumpNEnemySpawn3()
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
            gameObject.GetComponent<Rigidbody2D>().velocity = new Vector2(30f * GetSide(), 50f);
            gameObject.GetComponent<Rigidbody2D>().gravityScale = 2f;
            yield return null;
            currentPlat.LocateMyFSM("Control").SendEvent("PLAT RETRACT");

            while (true)
            {
                RaycastHit2D hit = Physics2D.Raycast(gameObject.transform.position, Vector2.left);

                if (hit.collider != null && hit.collider.name == "Chunk 0 2")
                {
                    float distance = Mathf.Abs(hit.point.x - gameObject.transform.position.x);
                    if (distance < 0.8f)
                    {
                        break;
                    }
                }
                yield return null;
            }
            Log("Found da wall");
            gameObject.GetComponent<Rigidbody2D>().velocity = new Vector2(0f, 0f);
            gameObject.GetComponent<Rigidbody2D>().gravityScale = 0f;
            _anim.Play("Wall Cling");
            yield return new WaitWhile(() => _anim.IsPlaying("Wall Cling"));
            StartCoroutine(WiredDaggThrow4()); //////////////////////////////////////////////////////
        }
        private IEnumerator WiredDaggThrow4()
        {
            Log("Start saw attack");
            var initY = gameObject.transform.GetPositionY();
            while (gameObject.transform.GetPositionY() >= 7.5f)
            {
                _anim.Play("Throw");
                var a = Instantiate(LurkerFinder.fakeWPSpike);
                a.transform.SetPosition2D(85.5f, gameObject.transform.GetPositionY());
                a.SetActive(true);
                a.AddComponent<ThrowingSpike>(); ///////////////////////////////////////////////Changed
                yield return new WaitWhile(() => _anim.IsPlaying("Throw"));
                _anim.Play("Wall Cling");
                yield return new WaitWhile(() => _anim.IsPlaying("Wall Cling"));
                gameObject.GetComponent<Rigidbody2D>().gravityScale = 0.1f;
                yield return new WaitForSeconds(0.8f);
                gameObject.GetComponent<Rigidbody2D>().gravityScale = 0f;
                gameObject.GetComponent<Rigidbody2D>().velocity = new Vector2(0f, 0f);
            }

            yield return new WaitForSeconds(1f);

            Log("Jump to roof");
            _anim.Play("Wallhop Antic");
            yield return new WaitWhile(() => _anim.IsPlaying("Wallhop Antic"));
            _anim.Play("Hop");
            var sca = gameObject.transform.localScale;
            gameObject.transform.localScale = new Vector3(-1f * sca.x, sca.y, sca.z);
            gameObject.GetComponent<Rigidbody2D>().velocity = new Vector2(10f * GetSide(), 70f);
            gameObject.GetComponent<Rigidbody2D>().gravityScale = 2f;
            startC = true;
            yield return new WaitWhile(() => !lurkOnPlat);
            gameObject.GetComponent<Rigidbody2D>().velocity = new Vector2(0f, 0f);
            gameObject.GetComponent<Rigidbody2D>().gravityScale = 0f;
            startC = false;
            lurkOnPlat = false;

            Log("Up daggers");
            gameObject.transform.SetPosition2D(85.5f, gameObject.transform.position.y - 1.5f);
            while (gameObject.transform.GetPositionX() <= 119f)
            {
                if (Mathf.Abs(gameObject.transform.GetPositionX() - 102.5f) > 2f)
                {
                    var a = Instantiate(LurkerFinder.fakeWPSpike);
                    a.transform.SetPosition2D(gameObject.transform.GetPositionX(), gameObject.transform.GetPositionY());
                    a.transform.SetRotation2D(-90f);
                    a.SetActive(true);
                    a.AddComponent<ThrowingSpike>(); 
                }
                gameObject.GetComponent<Rigidbody2D>().velocity = new Vector2(5f, 0f);
                yield return new WaitForSeconds(0.5f);
                gameObject.GetComponent<Rigidbody2D>().velocity = new Vector2(0f, 0f);
            }
            yield return new WaitForSeconds(1f);
            Log("Up daggers");
            gameObject.transform.SetPosition2D(102.5f, gameObject.transform.position.y);
            for (float i = 190f, k = 350f; i <= 270f; i += 10f, k -= 10f)
            {
                var a = Instantiate(LurkerFinder.fakeWPSpike);
                a.transform.SetPosition2D(gameObject.transform.GetPositionX(), gameObject.transform.GetPositionY());
                a.transform.SetRotation2D(i);
                a.SetActive(true);
                a.AddComponent<ThrowingSpike>();

                a = Instantiate(LurkerFinder.fakeWPSpike);
                a.transform.SetPosition2D(gameObject.transform.GetPositionX(), gameObject.transform.GetPositionY());
                a.transform.SetRotation2D(k);
                a.SetActive(true);
                a.AddComponent<ThrowingSpike>();
                yield return new WaitForSeconds(0.4f);
            }
            StartCoroutine(SpawnPlatform(0, gameObject.transform.GetPositionX(), 10f));
            sca = gameObject.transform.localScale;
            gameObject.transform.localScale = new Vector3(-1f * sca.x, sca.y, sca.z);
            yield return new WaitForSeconds(0.5f);
            gameObject.GetComponent<Rigidbody2D>().gravityScale = 2f;
            yield return new WaitWhile(() => !lurkOnPlat);
            lurkOnPlat = false;
            _anim.Play("Idle");
            gameObject.GetComponent<Rigidbody2D>().velocity = new Vector2(0f, 0f);
            gameObject.GetComponent<Rigidbody2D>().gravityScale = 0f;

            yield return new WaitForSeconds(1f);

            gameObject.GetComponent<Rigidbody2D>().velocity = new Vector2(-30f, 50f);
            gameObject.GetComponent<Rigidbody2D>().gravityScale = 2f;
            _anim.Play("Hop");
            yield return null;
            currentPlat.LocateMyFSM("Control").SendEvent("PLAT RETRACT");

            while (true)
            {
                RaycastHit2D hit = Physics2D.Raycast(gameObject.transform.position, Vector2.left);
                // If it hits something...

                if (hit.collider != null && hit.collider.name == "Chunk 0 2")
                {
                    float distance = Mathf.Abs(hit.point.x - gameObject.transform.position.x);
                    if (distance < 0.8f)
                    {
                        break;
                    }
                }
                yield return null;
            }
            Log("Found da wall");
            gameObject.GetComponent<Rigidbody2D>().velocity = new Vector2(0f, 0f);
            gameObject.GetComponent<Rigidbody2D>().gravityScale = 0f;
            _anim.Play("Wall Cling");
            yield return new WaitWhile(() => _anim.IsPlaying("Wall Cling"));
            StartCoroutine(SetUpSawPhase5());
        }

        private IEnumerator SetUpSawPhase5()
        {
            gameObject.GetComponent<Rigidbody2D>().gravityScale = 1f;

            while (gameObject.transform.GetPositionY() >= 7.5f)
            {
                yield return null;
            }
            gameObject.GetComponent<Rigidbody2D>().gravityScale = 0f;
            gameObject.GetComponent<Rigidbody2D>().velocity = new Vector2(0f, 0f);
            gameObject.GetComponent<BoxCollider2D>().enabled = false;
            setUpEnd = true;
            for (float i = 85f; i < 121f; i += 3.7f)
            {
                StartCoroutine(SpawnPlatform(0, i, 3f));
                currentPlat.AddComponent<MoveFloorUp>();
            }

            yield return new WaitForSeconds(2f);
            Log("Spawn upper spike");
            for (int i = 84; i < 122; i += 2)
            {
                StartCoroutine(SpawnPlatform(1, i, 9f, 1));
            }
            yield return new WaitForSeconds(1f);

            for (int i = 0; i < 5; i++)
            {
                var saw = Instantiate(LurkerFinder.sawOrig);
                saw.transform.SetPosition2D(ThrowingSpike.AllDagPos[i]);
                saw.SetActive(false);
                saw.AddComponent<SawHandler>();
                AllSaws[i] = saw;

                saw = Instantiate(LurkerFinder.sawOrig);
                saw.transform.SetPosition2D(85.5f, AllSaws[i].transform.GetPositionY());
                saw.SetActive(false);
                saw.AddComponent<SawHandler>();
                AllSaws[i + 5] = saw;
                yield return null;
            }
            StartCoroutine(SawPhase6());
        }
        public IEnumerator SawPhase6()
        {
            int rand = 0;
            float speed = -10f;
            int round = 0;
            bool leftOrRight;

            while (round <= 5)
            {
                rand = UnityEngine.Random.Range(1, 4);
                for (int i = 0; i < 5; i++)
                {
                    if (i == rand)
                    {
                        AllSaws[i].SetActive(false);
                        AllSaws[i + 5].SetActive(false);
                        continue;
                    }
                    AllSaws[i].SetActive(true);
                    AllSaws[i + 5].SetActive(true);
                    AllSaws[i].GetComponent<Rigidbody2D>().velocity = new Vector2(speed, 0f);
                }
                yield return new WaitWhile(() => AllSaws[0].transform.GetPositionX() > 80.5f);
                for (int i = 0; i < 5; i++)
                {
                    AllSaws[i].SetActive(false);
                    AllSaws[i].transform.SetPosition2D(ThrowingSpike.AllDagPos[i]);
                    AllSaws[i].GetComponent<Rigidbody2D>().velocity = new Vector2(0f, 0f);
                }
                if (round % 5 == 0)
                {
                    speed -= 2f;
                }
                round++;
                Log("get new saws");
            }

            round = 0;
            speed = -10f;
            while (round <= 5)
            {
                rand = UnityEngine.Random.Range(1, 4);
                for (int i = 0; i < 5; i++)
                {
                    if (i == rand)
                    {
                        AllSaws[i].SetActive(false);
                        AllSaws[i + 5].SetActive(false);
                        continue;
                    }
                    AllSaws[i].SetActive(true);
                    AllSaws[i].GetComponent<Rigidbody2D>().velocity = new Vector2(speed, 0f);
                    AllSaws[i + 5].SetActive(true);
                    AllSaws[i + 5].GetComponent<Rigidbody2D>().velocity = new Vector2(-1f * speed, 0f);
                }
                yield return new WaitWhile(() => AllSaws[0].transform.GetPositionX() > 80.5f);
                for (int i = 0; i < 5; i++)
                {
                    AllSaws[i].SetActive(false);
                    AllSaws[i].transform.SetPosition2D(ThrowingSpike.AllDagPos[i]);
                    AllSaws[i].GetComponent<Rigidbody2D>().velocity = new Vector2(0f, 0f);
                    AllSaws[i + 5].SetActive(false);
                    AllSaws[i + 5].transform.SetPosition2D(85.5f, ThrowingSpike.AllDagPos[i].y);
                    AllSaws[i + 5].GetComponent<Rigidbody2D>().velocity = new Vector2(0f, 0f);
                }
                if (round % 5 == 0)
                {
                    speed -= 2f;
                }
                round++;
                Log("get new saws");
            }


            Log("Move down");

        }

        private static GameObject[] _allSpikes = new GameObject[25];
        bool setUpEnd = false;
        public static int _temp = 0;
        GameObject[] AllSaws = new GameObject[12];
        IEnumerator EnemyTrial(float x, float y)
        {
            StartCoroutine(SpawnEnemies(x - 5f, y + 2f));
            yield return null;
            StartCoroutine(SpawnEnemies(x + 5f, y + 2f));
            yield return null;
            StartCoroutine(SpawnEnemies(x - 7f, y + 5f));
            yield return null;
            StartCoroutine(SpawnEnemies(x + 7f, y + 5f));
            yield return new WaitForSeconds(2.5f);
            //yield return new WaitForSeconds(1f);
            yield return new WaitWhile(() => GameObject.Find("Super Spitter Col(Clone)") != null);
            if (!setUpEnd)
            {
                StartCoroutine(EnemyTrial(x, y));
            }
        }


        //6x13x14
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


    }
}