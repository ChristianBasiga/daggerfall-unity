
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop.Game;

using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;

using DaggerfallRandomEncounterEvents.Enums;

namespace DaggerfallRandomEncounterEvents.Utils
{

    //Object representing the json file.
    /// <summary>
    /// Format would be like this
    /*
        {
            eventId: Robber
            context: World,
            type: Negative
            filters: [
                {
                    context: weather,
                    value: rain,
                },
                {
                    context: climate,
                    value: mountain
                }

            ]

        }


     */
    /// </summary>
    ///


    [System.Serializable]
    public struct EncounterData
    {
        public string eventId;
        public string context;
       
        public List<FilterData> filters;

        //For now making it string, then enum.parsing
        // public EncounterType type;
        public string type;
    }


    [System.Serializable]
    public struct FilterData
    {    
        public string context;
        public string value;
    }


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
                //So it loads it in correctly.
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