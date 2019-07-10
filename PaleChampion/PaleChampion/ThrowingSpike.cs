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
    internal class ThrowingSpike : MonoBehaviour
    {
        private Vector2 initPoint;
        private int myInd = 0;
        void Start()
        {
            //gameObject.AddComponent<DebugColliders>();
            initPoint = gameObject.transform.position;
            PaleLurker.allDagStartPos.Add(initPoint);
            myInd = PaleLurker.allDagStartPos.IndexOf(initPoint);
            float angle = Mathf.Deg2Rad * gameObject.transform.GetRotation2D();
            gameObject.GetComponent<Rigidbody2D>().velocity = new Vector2(20f * Mathf.Cos(angle), 20f * Mathf.Sin(angle));
            gameObject.GetComponent<Rigidbody2D>().isKinematic = true;
            var trail = gameObject.AddComponent<TrailRenderer>();
            trail.material = new Material(Shader.Find("Particles/Additive"))
            {
                mainTexture = Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(x => x.name == "Default-Particle"),
                color = Color.black
            };
            trail.startWidth *= 0.1f;
            trail.time = 999999f;
            trail.receiveShadows = false;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.allowOcclusionWhenDynamic = false;
        }
        private bool _once;
        private float _timeLive = 0f;
        void FixedUpdate()
        {
            if (gameObject.transform.GetPositionY() < 5.7f && !_once)
            {
                _once = true;
                gameObject.GetComponent<Rigidbody2D>().velocity = new Vector2(0f, 0f);
            }
            if (_timeLive > 5f && gameObject.GetComponent<Rigidbody2D>().velocity != Vector2.zero)
            {
                Log("Kill my boi");
                PaleLurker.allDagStartPos.RemoveAt(myInd);
                Destroy(gameObject);
            }
            _timeLive += Time.fixedDeltaTime;
        }
        void OnCollisionEnter2D(Collision2D other)
        {
            
        }
        void OnTriggerEnter2D(Collider2D col)
        {
            if (col.name == "Chunk 0 3" || col.name == "Floor Saver") //col.gameObject.layer == 8 && _timeLive > 0.5f)//
            {
                Log(col.gameObject.name);
                PaleLurker.allDagEndPos[PaleLurker._temp] = gameObject.transform.position;
                PaleLurker._temp++;
                gameObject.GetComponent<Rigidbody2D>().velocity = new Vector2(0f, 0f);
            }
        }
        private static void Log(object obj)
        {
            Logger.Log("[Throwing Knife] " + obj);
        }
    }
}