using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop.Game;

using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;

namespace DaggerfallRandomEncounterEvents.Utils
{



    public class EncounterUtils
    {
        //Loading in encouner jsons from resources folder to create prototypes for factory.
        public static List<string> loadEncounterData()
        {

            List<string> encounterData = new List<string>();

            Object[] jsonData = Resources.LoadAll("RandomEncounters", typeof(TextAsset));


            foreach (Object json in jsonData)
            {
                TextAsset textAsset = (TextAsset)json;

                encounterData.Add(textAsset.text);
            }

            return encounterData;

        }

        public static bool hasActiveSpawn(GameObject[] spawns)
        {
            //Cause if null then there wasn't any active objects.
            return (getActiveSpawn(spawns) != null);
        }

        public static GameObject getActiveSpawn(GameObject[] spawns)
        {

            for (int i = 0; i < spawns.Length; ++i)
            {
                if (spawns[i].activeInHierarchy)
                {
                    return spawns[i];
                }
            }

            return null;
        }

    }



    public class Debugging : MonoBehaviour
    {

        static DaggerfallMessageBox debugMessage;


        public static void DebugLog(string message)
        {
            if (debugMessage == null)
            {
                debugMessage = new DaggerfallMessageBox(DaggerfallWorkshop.Game.DaggerfallUI.UIManager);
                debugMessage.AllowCancel = true;
                debugMessage.ClickAnywhereToClose = true;
                debugMessage.ParentPanel.BackgroundColor = Color.clear;
            }

            debugMessage.SetText(message);
            DaggerfallUI.UIManager.PushWindow(debugMessage);
        }

        public static void AlertPlayer(string message)
        {
            DaggerfallUI.AddHUDText(message, 1.5f);
        }
    }
}