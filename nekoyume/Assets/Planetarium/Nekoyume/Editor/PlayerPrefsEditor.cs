using Nekoyume.BlockChain;
using Nekoyume.UI;
using UnityEditor;
using UnityEngine;

namespace Planetarium.Nekoyume.Editor
{
    public static class PlayerPrefsEditor
    {
        [MenuItem("Tools/Delete All Of PlayerPrefs")]
        public static void DeleteAllOfPlayerPrefs()
        {
            PlayerPrefs.DeleteAll();
        }
        
        [MenuItem("Tools/Delete Dialog PlayerPrefs Of Current AvatarState(Play Mode)", true)]
        public static bool ValidateDeleteDialogPlayerPrefsOfCurrentAvatarState()
        {
            return Application.isPlaying
                && States.Instance.CurrentAvatarState.Value != null;
        }
        
        [MenuItem("Tools/Delete Dialog PlayerPrefs Of Current AvatarState(Play Mode)", false)]
        public static void DeleteDialogPlayerPrefsOfCurrentAvatarState()
        {
            var index = 1;
            while (true)
            {
                var key = Dialog.GetPlayerPrefsKeyOfCurrentAvatarState(index);
                if (!PlayerPrefs.HasKey(key))
                {
                    break;
                }

                PlayerPrefs.DeleteKey(key);
                index++;
            }
        }
    }
}
