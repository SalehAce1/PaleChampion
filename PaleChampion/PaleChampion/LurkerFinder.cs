using System.Collections;
using UnityEngine;
using HutongGames.PlayMaker.Actions;
using Logger = Modding.Logger;
using UnityEngine.SceneManagement;
using UObject = UnityEngine.Object;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace PaleChampion
{
    internal class LurkerFinder : MonoBehaviour
    {
        private GameObject Lurker;
        private string _lastScene;
        private void Start()
        { 
            Logger.Log("[Pale Champion] Added PaleLurker MonoBehaviour");
            DontDestroyOnLoad(this);
            PlayerData.instance.killedPaleLurker = false;

            //StartCoroutine(loadingMantis());
        }
        private void Update()
        {
            //if (!PlayerData.instance.killedPaleLurker) return;
            
            if (Lurker != null) return;
            if (HeroController.instance.transform.GetPositionX() >= 109.3f && HeroController.instance.transform.GetPositionY() >= 79.4f)
            {
                Lurker = GameObject.Find("Pale Lurker");
            }
            if (Lurker == null) return;
            Logger.Log("Tell me when it starts");
            Lurker.AddComponent<PaleLurker>();
            
        }
        IEnumerator timer()
        {
            yield return new WaitForSeconds(5f);
        }
        /*IEnumerator loadingMantis()
        {
            Logger.Log("Wait2");
            GameManager.instance.LoadScene("GG_Mantis_Lords");
            yield return null;
            Resources.LoadAll<GameObject>("");
            foreach (var i in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (i.name == "Shot Mantis Lord")
                {
                    _mantis = Instantiate(i);
                }
            }
            if (_mantis == null)
            {
                Modding.Logger.Log("Not found.");
            }
            else
            {
                DontDestroyOnLoad(_mantis);
                Destroy(_mantis.LocateMyFSM("Control"));
                _mantis.SetActive(false);
                Modding.Logger.Log("Wow I actually found it?");
            }
        }*/
    }
}