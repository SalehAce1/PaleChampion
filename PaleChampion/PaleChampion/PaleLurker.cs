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
using On.HutongGames.PlayMaker.Actions;

namespace PaleChampion
{
    internal class PaleLurker : MonoBehaviour
    {
        private readonly Dictionary<string, float> _fpsDict = new Dictionary<string, float> //Use to change animation speed
        {
            ["DigIn 1"] = 60,
            ["DigIn 2"] = 60,
            ["DigOut"] = 60
        };
        public static HealthManager _hm;
        private tk2dSpriteAnimator _anim;
        private PlayMakerFSM _control;
        private static bool _lurkerChanged;
        private Texture _oldTex;
        private bool isDigFixed;
        public static int hits = 0;
        public static GameObject[] sawBlades = new GameObject[4];
        private PlayMakerFSM dreamNail;
        private string[] HeroAttacks = { "Slash", "DownSlash", "UpSlash", "Fireball2 Spiral(Clone)", "Sharp Shadow",
                                         "Hit R", "Hit L", "Hit U", "Q Fall Damage", "Great Slash", "Dash Slash" };

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

        public void FixDig()
        {
            if (!isDigFixed)
            {
                _control.ChangeTransition("Dig 2", "FINISHED", "Teleport");
                _control.RemoveAction("Teleport", 0);
                _control.RemoveAction("Teleport", 0);
                _control.RemoveAction("Teleport", 0);
                _control.InsertMethod("Teleport", 0, teleportLurker);
                isDigFixed = true;
            }
        }

        public void teleportLurker()
        {
            var xH = HeroController.instance.transform.GetPositionX();
            var yH = HeroController.instance.transform.GetPositionY();
            var xL = gameObject.transform.GetPositionX();
            var yL = gameObject.transform.GetPositionY();

            if (xH - xL > 0)
            {
                var newX = xH + 5f;
                if (newX > 156.1f && newX < 162.8f && xH < 156.1f) newX = xH - 5f;
                if (newX > 202.7f) newX = xH - 5f;
                gameObject.transform.SetPosition2D(newX, yL);
            }
            else
            {
                var newX = xH - 5f;
                if (newX > 156.1f && newX < 162.8f && xH < 156.1f) newX = xH + 5f;
                if (newX < 117.1f) newX = xH + 5f;
                gameObject.transform.SetPosition2D(newX, yL);
            }
        }

        public void Evade()
        {
            var xH = HeroController.instance.transform.GetPositionX();
            var yH = HeroController.instance.transform.GetPositionY();
            var xL = gameObject.transform.GetPositionX();
            var yL = gameObject.transform.GetPositionY();

            if (hits > 0 || Math.Abs(xL-xH) > 10f)
            {
                _control.SetState("Dig 1");
                hits = 0;
            }
        }

        private void Awake()
        {
            Log("Added PaleLurker Mono");
            _hm = gameObject.GetComponent<HealthManager>();
            _control = gameObject.LocateMyFSM("Lurker Control");
            _anim = gameObject.GetComponent<tk2dSpriteAnimator>();
            dreamNail = HeroController.instance.gameObject.LocateMyFSM("Dream Nail");
        }

        private void Start()
        {
            Log("Setting health");
            _hm.hp = 1800;

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
                _control.InsertMethod(" Hop", 6,velocitySetter);

                Log("Make Lurker stop throwing for now.");
                _control.ChangeTransition("Hopthrow Antic", "FINISHED", "Hop Air");
                _control.ChangeTransition("Hopland", "FINISHED", "After Land");

                Log("Stop slash attack for now");
                _control.ChangeTransition("After Land", "SLASH", "Hop Antic");
                _control.RemoveAction("Alert", 0);

                Log("Lurker now teleports near you when digging.");
                _control.InsertMethod("Dig Out", 0, FixDig);

                Log("Also, stopping her from digging for now");
                _control.RemoveAction("Alert", 3);
                _control.RemoveTransition("Idle", "HERO CAST SPELL");
                _control.RemoveTransition("Alert", "HERO CAST SPELL");
                _control.RemoveTransition("Hop Antic", "HERO CAST SPELL");

                Log("Dig if hit once or if player is too far");
                _control.InsertMethod("Hop Antic", 0, Evade);

                Log("Make pillar spikes appear when digging");
                _control.InsertCoroutine("Dig 1", 0, SpawnDungPillar);

                Log("Adding saw blades");
                for (int i = 0; i < sawBlades.Length; i++)
                {
                    sawBlades[i] = Instantiate(LurkerFinder.sawOrig);
                    sawBlades[i].SetActive(true);
                    sawBlades[i].GetComponent<DamageHero>().hazardType = 0;
                    sawBlades[i].AddComponent<Rigidbody2D>();
                    sawBlades[i].GetComponent<Rigidbody2D>().gravityScale = 0f;
                    sawBlades[i].GetComponent<Rigidbody2D>().isKinematic = true;
                    sawBlades[i].AddComponent<SawBladeMove>();
                }
                sawBlades[0].transform.SetPosition2D(109.3f, 79.4f-1.5f);
                sawBlades[1].transform.SetPosition2D(162.8f, 79.4f - 1.5f);
                sawBlades[2].transform.SetPosition2D(117.1f, 108.4f - 1.5f);
                sawBlades[3].transform.SetPosition2D(162.8f, 108.4f - 1.5f);
                var a = Instantiate(HeroController.instance.gameObject);
                a.transform.SetPosition2D(115.3f, 109f);
                a.SetActive(true);

                Log("Checking if Dream Nailed, needed later.");
                dreamNail.InsertMethod("End", 0, LogThis);

                foreach (var i in Resources.FindObjectsOfTypeAll<GameObject>())
                {
                    Log("Names: " + i.name);
                }

            }
            catch (Exception e)
            {
                Log(e);
            }
        }

        public void LogThis()
        {
            Log("wow this is working?");
        }

        void OnTriggerEnter2D(Collider2D col)
        {
            //Needs fluke, elegy, and more
            if (HeroAttacks.Contains(col.name))
            {
                hits++;
            }
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
                DungPill1.transform.SetPosition2D(xL + (i * 2.2f) + 1.5f, yL+4.5f);
                DungPill2.transform.SetPosition2D(xL - (i * 2.2f) - 1.5f, yL+4.5f);
                DungPill2.transform.localScale = new Vector3(-1f * xSpikeScale, ySpikeScale, zSpikeScale);
                Log(yL);
                yield return new WaitForSeconds(0.125f);
            }
        }

        private void Update()
        {
            var xH = HeroController.instance.transform.GetPositionX();
            var yH = HeroController.instance.transform.GetPositionY();
            var xL = gameObject.transform.GetPositionX();
            var yL = gameObject.transform.GetPositionY();
        }

        private void OnDestroy()
        {
            ModHooks.Instance.TakeHealthHook -= TakeDamage;
            
            if (!_lurkerChanged) return;

            tk2dSpriteDefinition def = gameObject.GetComponent<tk2dSprite>().GetCurrentSpriteDef();

            def.material.mainTexture = _oldTex;

            _lurkerChanged = false;
        }

        private static void Log(object obj)
        {
            Logger.Log("[Pale Champion] " + obj);
        }
    }
}