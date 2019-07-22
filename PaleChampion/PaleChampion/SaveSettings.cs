using System;
using Modding;
using UnityEngine;

namespace PaleChampion
{
    //Copied from 56's pale prince :)
    //https://github.com/5FiftySix6/HollowKnight.Pale-Prince/blob/master/Pale%20Prince/SaveSettings.cs

    [Serializable]
    public class SaveSettings : ModSettings, ISerializationCallbackReceiver
    {
        public BossStatue.Completion Completion = new BossStatue.Completion
        {
            isUnlocked = true
        };

        public void OnBeforeSerialize()
        {
            StringValues["Completion"] = JsonUtility.ToJson(Completion);
        }

        public void OnAfterDeserialize()
        {
            StringValues.TryGetValue("Completion", out string @out);

            if (string.IsNullOrEmpty(@out)) return;

            Completion = JsonUtility.FromJson<BossStatue.Completion>(@out);
        }
    }
}