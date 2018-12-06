
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop.Game;

using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;

using DaggerfallRandomEncountersMod.Enums;
using DaggerfallWorkshop.Game.Utility.ModSupport;


namespace DaggerfallRandomEncountersMod.Utils
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


    #region Objects for json loading.

    [System.Serializable]
    public class EncounterData
    {
        public string encounterId;
        public string context;
        public string type;

        public List<FilterData> filter;
    }


    [System.Serializable]
    public class FilterData
    {    
        public string context;
        public string value;

        public override bool Equals(object obj)
        {
            FilterData other = (FilterData)obj;
            return string.Equals(context, other.context) && string.Equals(value, other.value);
        }

        public static bool operator<(FilterData lhs, FilterData rhs)
        {
            return string.Compare(lhs.context, rhs.context) < 0;
        }

        public static bool operator>(FilterData lhs, FilterData rhs)
        {
            return string.Compare(lhs.context, rhs.context) > 0;
        }

        public override int GetHashCode()
        {
            return (context + value).GetHashCode();
        }
    }

    #endregion

    public class EncounterUtils
    {
        //Loading in encouner jsons from resources folder to create prototypes for factory.
        public static List<string> loadEncounterJson()
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


        //Later on make this hard type to be EncounterResource
        public static bool hasActiveSpawn(GameObject[] spawns)
        {
            //Cause if null then there wasn't any active objects.
            return (getActiveSpawn(spawns) != null);
        }

        public static GameObject getActiveSpawn(GameObject[] spawns)
        {

            for (int i = 0; i < spawns.Length; ++i)
            {
                if (spawns[i] != null && spawns[i].activeInHierarchy)
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
            if (message == null)
            {
                Debug.Log("message is null");
                return;
            }
            DaggerfallUI.AddHUDText(message, 1.5f);
        }
    }
}