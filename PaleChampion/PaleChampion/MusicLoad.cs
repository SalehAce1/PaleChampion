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
using System.Reflection;
using System.IO;

namespace PaleChampion
{
    internal class MusicLoad: MonoBehaviour
    {
        public static class LoadAssets
        {
            public static List<AudioClip> music = new List<AudioClip>();

            public static void LoadWavFile()
            {
                Log("Starting");
                foreach (string res in Assembly.GetExecutingAssembly().GetManifestResourceNames())
                {
                    if (res.EndsWith(".wav"))
                    {
                        Modding.Logger.Log("Found sound effect " + res + "! Saving it.");
                        Stream audioStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(res);
                        if (audioStream != null)
                        {
                            byte[] buffer = new byte[audioStream.Length];
                            audioStream.Read(buffer, 0, buffer.Length);
                            audioStream.Dispose();
                            WAV mus = new WAV(buffer);
                            AudioClip audioClip = AudioClip.Create(res, mus.SampleCount, 1, mus.Frequency, false);
                            audioClip.SetData(mus.LeftChannel, 0);
                            //audioClip.SetData(mus.RightChannel, 0);
                            music.Add(audioClip);
                        }
                    }
                }
            }


        }

        private static void Log(object obj)
        {
            Logger.Log("[Audio] " + obj);
        }
    }
}
