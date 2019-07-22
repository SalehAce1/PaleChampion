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
    internal class SpikeFix : MonoBehaviour
    {
        void OnTriggerStay2D(Collider2D col)
        {
            if (col.gameObject.name.Contains("Hero"))
            {
                HeroController.instance.TakeDamage(gameObject, GlobalEnums.CollisionSide.other, 1, 1);
            }
        }
        private static void Log(object obj)
        {
            Logger.Log("[Spike Fix] " + obj);
        }
    }
}
