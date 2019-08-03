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
    internal class GOLoader : MonoBehaviour
    {
        Texture _oldTexAsp;
        Texture _oldTexSpit;
        Texture _oldTexDung;
        Texture _oldTexOb;
        public static AudioClip spitAud;
        public static AudioClip pillAud;
        public static AudioClip pillAud2;

        void Start()
        {
            music = new List<AudioClip>();
            StartCoroutine(LoadGO());
        }
        IEnumerator LoadGO()
        {
            yield return null;
            DontDestroyOnLoad(PaleChampion.preloadedGO["platform"]);
            PaleChampion.preloadedGO["platform"].SetActive(false);
            Logger.Log("Found platform");

            PaleChampion.preloadedGO["spike"].AddComponent<TinkSound>();
            PaleChampion.preloadedGO["spike"].AddComponent<SpikeFix>();
            DontDestroyOnLoad(PaleChampion.preloadedGO["spike"]);
            PaleChampion.preloadedGO["spike"].SetActive(false);
            Logger.Log("Found spike");

            _oldTexAsp = PaleChampion.preloadedGO["aspid"].GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture;
            PaleChampion.preloadedGO["aspid"].GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture = PaleChampion.SPRITES[8].texture;
            PaleChampion.preloadedGO["aspid"].AddComponent<AspidControl>();
            spitAud = PaleChampion.preloadedGO["aspid"].LocateMyFSM("spitter").GetAction<AudioPlay>("Fire", 0).oneShotClip.Value as AudioClip;
            DontDestroyOnLoad(PaleChampion.preloadedGO["aspid"]);
            PaleChampion.preloadedGO["aspid"].SetActive(false);
            Logger.Log("Found aspid");

            _oldTexSpit = PaleChampion.preloadedGO["spit"].GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture;
            PaleChampion.preloadedGO["spit"].GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture = PaleChampion.SPRITES[9].texture;
            DontDestroyOnLoad(PaleChampion.preloadedGO["spit"]);
            PaleChampion.preloadedGO["spit"].SetActive(false);
            Logger.Log("Found spit");

            DontDestroyOnLoad(PaleChampion.preloadedGO["cage"]);
            PaleChampion.preloadedGO["cage"].SetActive(false);
            Logger.Log("Found cage");

            DontDestroyOnLoad(PaleChampion.preloadedGO["cage obb"]);
            PaleChampion.preloadedGO["cage obb"].SetActive(false);
            Logger.Log("Found cage obb");

            Destroy(PaleChampion.preloadedGO["dung"].LocateMyFSM("Control"));
            DontDestroyOnLoad(PaleChampion.preloadedGO["dung"]);
            _oldTexDung = PaleChampion.preloadedGO["dung"].GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture;
            PaleChampion.preloadedGO["dung"].GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture = PaleChampion.SPRITES[0].texture;
            PaleChampion.preloadedGO["dung"].SetActive(false);
            Logger.Log("Fixed dung spike");

            PaleChampion.preloadedGO["saw"].GetComponent<SpriteRenderer>().sprite.texture.LoadImage(PaleChampion.SPRITEBYTE[2]);
            //Destroy(PaleChampion.preloadedGO["saw"].GetComponent<TinkEffect>());
            PaleChampion.preloadedGO["saw"].SetActive(false);
            DontDestroyOnLoad(PaleChampion.preloadedGO["saw"]);
            Logger.Log("Fixed saw");

            PaleChampion.preloadedGO["wp spike"] = new GameObject();
            PaleChampion.preloadedGO["wp spike"].AddComponent<BoxCollider2D>();
            var bc = PaleChampion.preloadedGO["wp spike"].GetComponent<BoxCollider2D>();
            bc.size = new Vector2(1.5f, 0.3f);
            bc.isTrigger = true;
            PaleChampion.preloadedGO["wp spike"].AddComponent<SpriteRenderer>().sprite = PaleChampion.SPRITES[4];//i.sprite;
            PaleChampion.preloadedGO["wp spike"].AddComponent<Rigidbody2D>();
            PaleChampion.preloadedGO["wp spike"].GetComponent<Rigidbody2D>().isKinematic = true;
            PaleChampion.preloadedGO["wp spike"].layer = 12;
            PaleChampion.preloadedGO["wp spike"].AddComponent<DamageHero>().enabled = true;
            PaleChampion.preloadedGO["wp spike"].GetComponent<DamageHero>().damageDealt = 1;
            DontDestroyOnLoad(PaleChampion.preloadedGO["wp spike"]);
            PaleChampion.preloadedGO["wp spike"].SetActive(false);
            Logger.Log("Fixed wp spike");

            DontDestroyOnLoad(PaleChampion.preloadedGO["lurker"]);
            PaleChampion.preloadedGO["lurker"].SetActive(false);
            Logger.Log("Fixed pale lurker");

            try
            {
                PaleChampion.preloadedGO["lurker2"] = new GameObject("lurker2");
                Logger.Log("1");
                PaleChampion.preloadedGO["lurker2"].AddComponent<SpriteRenderer>().enabled = true;
                PaleChampion.preloadedGO["lurker2"].AddComponent<CustomAnimator>();
                DontDestroyOnLoad(PaleChampion.preloadedGO["lurker2"]);
                PaleChampion.preloadedGO["lurker2"].SetActive(false);
                Logger.Log("Fixed pale lurker2");
            }
            catch(System.Exception e)
            {
                Logger.Log(e);
            }

            DontDestroyOnLoad(PaleChampion.preloadedGO["barb"]);
            PaleChampion.preloadedGO["barb"].SetActive(false);
            Logger.Log("Fixed barb");

            DontDestroyOnLoad(PaleChampion.preloadedGO["ob"]);
            _oldTexOb = PaleChampion.preloadedGO["ob"].GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture;
            PaleChampion.preloadedGO["ob"].GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture = PaleChampion.SPRITES[10].texture;
            PaleChampion.preloadedGO["ob"].SetActive(false);
            Logger.Log("Fixed oblobble");

            int temp = 0;
            foreach (ParticleSystem i in PaleChampion.preloadedGO["pillar"].GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule settings = i.main;
                if (temp % 2 == 0)
                    settings.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 69f / 255f, 0f));
                else
                    settings.startColor = new ParticleSystem.MinMaxGradient(Color.red);
                temp++;
            }
            foreach (SpriteRenderer i in PaleChampion.preloadedGO["pillar"].GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (i.gameObject.name.Contains("haze"))
                {
                    Destroy(i);
                }
            }
            pillAud = PaleChampion.preloadedGO["flame"].LocateMyFSM("Control").GetAction<AudioPlaySimple>("Pillar", 1).oneShotClip.Value as AudioClip;
            pillAud2 = PaleChampion.preloadedGO["flame"].LocateMyFSM("Control").GetAction<AudioPlayerOneShotSingle>("Shake", 2).audioClip.Value as AudioClip;
            DontDestroyOnLoad(PaleChampion.preloadedGO["pillar"]);
            PaleChampion.preloadedGO["pillar"].SetActive(false);
            Logger.Log("Fixed pillar");

            Logger.Log("Getting bombs");
            PaleChampion.preloadedGO["bomb"] = new GameObject("bomb")
            {
                layer = 14
            };
            PaleChampion.preloadedGO["bomb"].AddComponent<Rigidbody2D>().isKinematic = true;

            var trail = PaleChampion.preloadedGO["bomb"].AddComponent<ParticleSystem>();
            var rend = trail.GetComponent<ParticleSystemRenderer>();
            ParticleSystem.MainModule main = trail.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.prewarm = true;
            main.duration = 2f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 2f); //.5,2
            main.startSpeed = 0f;
            main.startSize = 0.3f; //0.01
            main.gravityModifier = 0.5f;
            main.maxParticles *= 2;
            main.startColor = new ParticleSystem.MinMaxGradient(Color.red, new Color(174f, 80f, 53f, 248f));

            ParticleSystem.EmissionModule emit = trail.emission;
            emit.enabled = true;
            emit.rateOverTime = new ParticleSystem.MinMaxCurve(80f, 100f);

            ParticleSystem.ShapeModule shape = trail.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 0.01f;

            ParticleSystem.VelocityOverLifetimeModule velLife = trail.velocityOverLifetime;
            velLife.enabled = true;
            velLife.space = ParticleSystemSimulationSpace.World;
            velLife.x = new ParticleSystem.MinMaxCurve(-1f, 1); //1,10
            velLife.y = new ParticleSystem.MinMaxCurve(3f, 6f); //10,15
            velLife.z = new ParticleSystem.MinMaxCurve(-1f, 1f);

            ParticleSystem.LimitVelocityOverLifetimeModule limVel = trail.limitVelocityOverLifetime;
            limVel.enabled = true;
            limVel.limit = 6.44f;
            limVel.dampen = 1f;

            ParticleSystem.TrailModule trl = trail.trails;
            trl.enabled = true;
            trl.mode = ParticleSystemTrailMode.PerParticle;
            trl.lifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
            trl.minVertexDistance = 0.1f;
            trl.colorOverLifetime = new ParticleSystem.MinMaxGradient(Color.red, new Color(174f, 80f, 53f, 248f));
            trl.dieWithParticles = true;

            rend.material = new Material(Shader.Find("Particles/Additive"))
            {
                mainTexture = Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(x => x.name == "Default-Particle"),
                color = Color.black
            };

            rend.trailMaterial = new Material(Shader.Find("Particles/Additive"))
            {
                mainTexture = Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(x => x.name == "Default-Particle"),
                color = Color.black
            };
            rend.renderMode = ParticleSystemRenderMode.Mesh;
            rend.mesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");


            GameObject normBomb = new GameObject("normal")
            {
                layer = 14
            };
            normBomb.AddComponent<SpriteRenderer>().sprite = PaleChampion.SPRITES[5];
            normBomb.AddComponent<BoxCollider2D>().isTrigger = true;
            normBomb.GetComponent<BoxCollider2D>().size /= 1.5f;
            normBomb.transform.localScale /= 6;

            normBomb.layer = 12;
            //normBomb.AddComponent<DamageHero>().enabled = true;
            //normBomb.GetComponent<DamageHero>().damageDealt = 1;

            normBomb.transform.SetParent(PaleChampion.preloadedGO["bomb"].transform);
            normBomb.SetActive(true);
            GameObject endBomb = new GameObject("explode")
            {
                layer = 14
            };
            endBomb.AddComponent<SpriteRenderer>().sprite = PaleChampion.SPRITES[6];
            endBomb.transform.localScale /= 6f;
            endBomb.transform.SetParent(PaleChampion.preloadedGO["bomb"].transform);
            endBomb.SetActive(false);
            PaleChampion.preloadedGO["bomb"].SetActive(false);
            DontDestroyOnLoad(PaleChampion.preloadedGO["bomb"]);
            Logger.Log("Fixed bomb");

            Logger.Log(SystemInfo.operatingSystem);
            string assetName = "";
            string assetName2 = "";
            if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows)
            {
                assetName2 = "soundWin";
                assetName = "smoke";
            }
            else if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX)
            {
                assetName2 = "soundOSX";
                assetName = "smokeMC";
            }
            else if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Linux)
            {
                assetName2 = "soundLin";
                assetName = "smokeULin";
            }
            else Logger.Log("ERROR OS NOT SUPPORTED.");
            AssetBundle ab = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, assetName));
            GameObject[] go = ab.LoadAllAssets<GameObject>();
            PaleChampion.preloadedGO["smoke"] = Instantiate(go[0]);
            ab.Unload(false);
            PaleChampion.preloadedGO["smoke"].SetActive(false);
            var wow = PaleChampion.preloadedGO["smoke"].GetComponent<ParticleSystem>().main;
            wow.startSize = new ParticleSystem.MinMaxCurve(3f, 4f);
            wow.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.3f);
            PaleChampion.preloadedGO["smoke"].layer = 18;
            DontDestroyOnLoad(PaleChampion.preloadedGO["smoke"]);
            Logger.Log("Fixed smoke");

            AssetBundle ab2 = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, assetName2));
            AudioClip[] go2 = ab2.LoadAllAssets<AudioClip>();
            foreach(var i in go2)
            {
                Logger.Log("aud " + i.name);
                DontDestroyOnLoad(i);
                music.Add(i);
            }
            ab2.Unload(false);
            Logger.Log("Fixed music");

            PaleChampion.preloadedGO["music box"] = new GameObject("music box");
            GameObject bg = new GameObject("bg music");
            bg.AddComponent<AudioSource>();
            PaleChampion.preloadedGO["music box"].AddComponent<AudioSource>();
            bg.transform.parent = PaleChampion.preloadedGO["music box"].transform;
            DontDestroyOnLoad(PaleChampion.preloadedGO["music box"]);
            DontDestroyOnLoad(bg);
            PaleChampion.preloadedGO["music box"].SetActive(false);
            Logger.Log("Fixed music box");
        }
        public static List<AudioClip> music = new List<AudioClip>();
        private void OnDestroy()
        {
            Logger.Log("Dead Tex");
            PaleChampion.preloadedGO["dung"].GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture = _oldTexDung;
            PaleChampion.preloadedGO["ob"].GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture = _oldTexOb;
            PaleChampion.preloadedGO["aspid"].GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture = _oldTexAsp;
            PaleChampion.preloadedGO["spit"].GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture = _oldTexSpit;
        }
    }
}
