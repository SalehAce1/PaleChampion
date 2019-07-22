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
using System;
using Logger = Modding.Logger;

namespace PaleChampion
{
    internal class TinkSound : MonoBehaviour
    {
        private static readonly System.Random Rnd = new System.Random();

        private float _nextTime;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            try
            {
                if (!collision.CompareTag("Nail Attack") || Time.time < _nextTime)
                {
                    return;
                }
                Logger.Log("Nail");
                _nextTime = Time.time + 0.25f;

                float degrees = 0f;
                PlayMakerFSM damagesEnemy = PlayMakerFSM.FindFsmOnGameObject(collision.gameObject, "damages_enemy");
                if (damagesEnemy != null)
                {
                    degrees = damagesEnemy.FsmVariables.FindFsmFloat("direction").Value;
                }
                Logger.Log("deg");
                Vector3 position = HeroController.instance.transform.position;
                Vector3 euler = Vector3.zero;
                switch (DirectionUtils.GetCardinalDirection(degrees))
                {
                    case 0:
                        HeroController.instance.RecoilLeft();
                        position = new Vector3(position.x + 2, position.y, 0.002f);
                        break;
                    case 1:
                        HeroController.instance.RecoilDown();
                        position = new Vector3(position.x, position.y + 2, 0.002f);
                        euler = new Vector3(0, 0, 90);
                        break;
                    case 2:
                        HeroController.instance.RecoilRight();
                        position = new Vector3(position.x - 2, position.y, 0.002f);
                        euler = new Vector3(0, 0, 180);
                        break;
                    default:
                        position = new Vector3(position.x, position.y - 2, 0.002f);
                        euler = new Vector3(0, 0, 270);
                        break;
                }
                Logger.Log("fsm");
                GameObject effect = Instantiate(PaleChampion.preloadedGO["saw"].GetComponent<TinkEffect>().blockEffect);
                Logger.Log("go aud");
                effect.transform.localPosition = position;
                effect.transform.localRotation = Quaternion.Euler(euler);
                effect.GetComponent<AudioSource>().pitch = (85 + Rnd.Next(30)) / 100f;
                effect.SetActive(true);
                Logger.Log("done sound");
            }
            catch(Exception e)
            {
                Logger.Log(e);
            }
        }
    }
}
