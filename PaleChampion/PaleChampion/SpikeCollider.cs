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
    internal class SpikeCollider : MonoBehaviour
    {
        void OnTriggerEnter2D(Collider2D col)
        {
            if (col.name == "HeroBox")
            {
                HeroController.instance.TakeDamage(HeroController.instance.gameObject, GlobalEnums.CollisionSide.other, 1, 0);
            }
        }
        private static void Log(object obj)
        {
            Logger.Log("[Spike Collider] " + obj);
        }
    }
}