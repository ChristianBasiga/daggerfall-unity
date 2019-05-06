using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using DaggerfallRandomEncountersMod.Utils;
using Newtonsoft.Json;
using System.IO;
using System.Linq;


namespace DaggerfallRandomEncountersMod.GUI
{

    /// <summary>
    /// This is window for generating jsons using values in fields.
    /// Will be custom editor for class called EncounterLoader.
    /// </summary>
    ///

    public class EncounterLoadWindow : EditorWindow
    {

        //In future this will have instance of EncounterLoader, that will load json files into it
        //also RandomEncounterManager will also load it in.

        //Strictly for this
        private class FilterEntry
        {
            public int contextIndex;
            public int valueIndex;
        }

        //For now here, reoganize where filte ris.
        static readonly string weather = "weather";
        static readonly string crime = "crime";

        int encounterIdIndex;
        string[] possibleEncounterIds;
        int chosenEncounterTypeIndex;
        List<FilterEntry> filtersAdded;


        /// <summary>
        /// All of these will be in their own respective files / class later.
        /// </summary>
        static string[] encounterTypes = new string[] { "Positive", "Negative", "Neutral" };
        static string[] possibleTimes = new string[] { "day", "night", "dawn", "dusk", "midday", "midnight" };

        //Will have weather types, crimes etc, so essentially prototype filters they can choose from.

        //Keys are context, then values are possible options for it.
        //I don't want to hard type filterdata, but may in the future.

        static string[] possibleFilters;
        static List<string[]> filterDomains;


        //Dictionary<string, string[]> possibleFilters;

        [MenuItem("RandomEncountersMod/RandomEncountersLoader")]
        static EncounterLoadWindow Init()
        {


            //These shouldn't change, problem with doing it in init though  is if does change.
            //need way to check that to avoid it.
            //not updated
            initFilterValues();



            // Get existing open window or if none, make a new one:
            EncounterLoadWindow wow =  EditorWindow.GetWindow(typeof(EncounterLoadWindow)) as EncounterLoadWindow;
            

            return wow;
          // encounterLoadWindow.Show();
        }


        //Called when window open
        private void Awake()
        {


            
            

            filtersAdded = new List<FilterEntry>();
            //For now doing this everytime ope, make it so only if different somehow later.
            RandomEncounterManager.initRandomEncounterCache();
            possibleEncounterIds = RandomEncounterManager.getConcreteTypes();



        }

        private static void initFilterValues()
        {

            //This key should also be from a constant somewhere
            List<string> temp = new List<string>();
            filterDomains = new List<string[]>();

            temp.Add(weather);
            filterDomains.Add(System.Enum.GetNames(typeof(DaggerfallWorkshop.Game.Weather.WeatherType)));

            temp.Add(crime);
            filterDomains.Add(System.Enum.GetNames(typeof(DaggerfallWorkshop.Game.Entity.PlayerEntity.Crimes)));


            temp.Add("time");
            filterDomains.Add(possibleTimes);



            possibleFilters = temp.ToArray();


        }

        private void OnGUI()
        {

            
            GUILayout.Label("Encounter Id", EditorStyles.boldLabel);
            // encounterId = EditorGUILayout.TextField("Encounter Id", encounterId);
            encounterIdIndex = EditorGUILayout.Popup(encounterIdIndex, possibleEncounterIds) ;

            GUILayout.Label("Encounter Type", EditorStyles.boldLabel);

            chosenEncounterTypeIndex = EditorGUILayout.Popup(chosenEncounterTypeIndex, encounterTypes);




            for (int i = 0; i < filtersAdded.Count; ++i) {

                FilterData filter = new FilterData();

                //Generates correct set of values depending on context
                GUILayout.Label("Context", EditorStyles.boldLabel);


                //But then requires O(n) operation at end where n is amount of filters added
                //but don't want to repeat those instrunctions every gui frame either.

                //Unless make this indices instead, cause as is right now would need two more lists
                //for context indices and value indices, or list of pairs, essentially acting like these though.
                filtersAdded[i].contextIndex = EditorGUILayout.Popup(filtersAdded[i].contextIndex, possibleFilters);

                GUILayout.Label("Value", EditorStyles.boldLabel);

                filtersAdded[i].valueIndex = EditorGUILayout.Popup(filtersAdded[i].valueIndex, filterDomains[filtersAdded[i].contextIndex]);

            }
            //If this button is pressed do this.

            if (GUILayout.Button("Add Filter"))
            {
                Debug.LogError("Adding filter");
                //Adds new filter so new field will populate next OnGui call..
                filtersAdded.Add(new FilterEntry());
            }

            if (GUILayout.Button("Generate Json file"))
            {
                Debug.LogError("Generating json file");

                //If this isn't valid encounter type, disallow. Granted parsing already handles that, but
                //this gui is to make it easier for them, not allow them to make errors in json.
                //though in future will be limiting this to list generated.
                //but that functionality isn't done.
                generateJson();
            }

            //I suppose the json name doesn't actually matter, this should just be uid.
            //GUILayout.Label("Encounter ID" , EditorStyles.boldLabel);
            //this.Repaint();
        }


        //For now just method.

        private void generateJson()
        {

            //based on fields create EncounterData object.
            EncounterData encounterData = new EncounterData();
            encounterData.encounterId = possibleEncounterIds[encounterIdIndex];
            encounterData.context = "Resting";

            List<FilterData> filters = new List<FilterData>();

            //Doing here instead of repeating these two instrunctions every gui frame.
            //seems like this is more time complexity but repeating these 2 more than need is over all
            //more time consuming than loop at end like this.
            foreach (FilterEntry entry in filtersAdded)
            {
                FilterData filterData = new FilterData();
                filterData.context = possibleFilters[entry.contextIndex];
                filterData.value = filterDomains[entry.contextIndex][entry.valueIndex];
                filters.Add(filterData);
            }

            encounterData.filter = filters;

            encounterData.type = encounterTypes[chosenEncounterTypeIndex];

            //Then serialize it, no checks needed here as options allowed to be set aren't invalid.
            string json = JsonConvert.SerializeObject(encounterData);


            //Writes json into folder.


            //Will add as file instead.
            //Loading as text assets is fine though, but for consistency, may chage that too.
            //random uids here instead.
            //There to be unique name, which would be filter
            File.WriteAllText("Assets/Resources/RandomEncounters/"+ encounterData.encounterId + "_" + encounterData.type+ "_" + encounterData.filter[0].ToString()+".json" , json);
            //Okay, adding as text asset adds alot of shit.
           // AssetDatabase.CreateAsset(textAssetJson, "Assets/Resources/RandomEncounters/test.json");
        }

        


    }
}