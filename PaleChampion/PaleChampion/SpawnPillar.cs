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

namespace PaleChampion
{ 
    internal class SpawnPillar : MonoBehaviour
    {
        Rigidbody2D rgb2;
        public void Start()
        {
            rgb2 = gameObject.GetComponent<Rigidbody2D>();
        }
        bool _once;
        public void Update()
        {
            if (!_once)
            {
                //gameObject.transform.Find("normal").Rotate(Vector3.forward * -15);
                gameObject.transform.Rotate(Vector3.forward * -1500f * Time.deltaTime);
                rgb2.velocity += new Vector2(0.01f, 0f);
            }
        }

        public IEnumerator DestroyBomb()
        {
            yield return null;
            rgb2.velocity = new Vector2(0f, 0f);
            rgb2.gravityScale = 0f;
            _once = true;
            yield return new WaitForSeconds(1.5f);
            var a = Instantiate(PaleChampion.preloadedGO["pillar"]);
            a.transform.SetPosition2D(gameObject.transform.position.x, gameObject.transform.Find("normal").gameObject.transform.position.y + 6.5f); //orig: x,9f
            a.SetActive(true);
            StartCoroutine(PillarMus(a));
            //if (gameObject.transform.GetPositionX() < 85.5f) a.transform.SetRotation2D(-90f);
            //if (gameObject.transform.GetPositionX() > 119.5f) a.transform.SetRotation2D(90f);
            yield return new WaitForSeconds(0.5f); 

            Destroy(gameObject);
        }

        IEnumerator PillarMus(GameObject go)
        {
            var aud = go.AddComponent<AudioSource>();
            aud.clip = GOLoader.pillAud;
            aud.Play();
            aud.volume = GameManager.instance.gameSettings.soundVolume==0f ? 0f : 0.25f;
            yield return new WaitForSeconds(0.45f);
            aud.Stop();
            aud.clip = GOLoader.pillAud2;
            aud.pitch = 1;
            aud.volume = GameManager.instance.gameSettings.soundVolume == 0f ? 0f : 0.25f;
            aud.Play();
        }
    }
}
