using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using DaggerfallRandomEncountersMod.Utils;
using Newtonsoft.Json;
using System.IO;

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

        string encounterId;
        int chosenEncounterType;
        List<FilterData> filtersAdded;

        static string[] encounterTypes = new string[] { "Positive", "Negative", "Neutral" };


        [MenuItem("Window/RandomEncountersLoader")]
        static EncounterLoadWindow Init()
        {

            

           
            // Get existing open window or if none, make a new one:
           return EditorWindow.GetWindow(typeof(EncounterLoadWindow)) as EncounterLoadWindow;
          // encounterLoadWindow.Show();
        }


        //Called when window open
        private void Awake()
        {
            encounterId = "";
            filtersAdded = new List<FilterData>();

            //The can choose to add more.
            filtersAdded.Add(new FilterData());
        }

        private void OnGUI()
        {

            GUILayout.Label("Encounter Info", EditorStyles.boldLabel);
           // encounterId = EditorGUILayout.TextField("Encounter Id", encounterId);
            encounterId = EditorGUILayout.TextField("Enter encounter Id", encounterId);

            GUILayout.Label("Encounter Type", EditorStyles.boldLabel);

            chosenEncounterType = EditorGUILayout.Popup(0, encounterTypes);

            //This works, what doesn't is the refresh of window.
            for (int i = 0; i < filtersAdded.Count; ++i) {

              
                filtersAdded[i].context = EditorGUILayout.TextField("Context: ", filtersAdded[i].context);
                filtersAdded[i].value = EditorGUILayout.TextField("Value: ", filtersAdded[i].value);
            }
            //If this button is pressed do this.

            if (GUILayout.Button("Add Filter"))
            {
                Debug.LogError("Adding filter");
                //Adds new filter so new field will populate next OnGui call..
                filtersAdded.Add(new FilterData());
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
            encounterData.encounterId = encounterId;
            encounterData.context = "World";
            encounterData.filter = filtersAdded;

            encounterData.type = encounterTypes[chosenEncounterType];

            //Then serialize it, no checks needed here as options allowed to be set aren't invalid.
            string json = JsonConvert.SerializeObject(encounterData);
            Debug.LogError(json);
            TextAsset textAssetJson = new TextAsset(json);
            textAssetJson.name = "test";
            Debug.LogError(textAssetJson.text);
            //Writes json into folder.
            //The name doesn't matter, and as long as in json format, doesn't need to be .json either.


            //Will add as file instead.
            //Loading as text assets is fine though, but for consistency, may chage that too.
            File.WriteAllText("Assets/Resources/RandomEncounters/test.json", json);
            //Okay, adding as text asset adds alot of shit.
           // AssetDatabase.CreateAsset(textAssetJson, "Assets/Resources/RandomEncounters/test.json");
        }

        


    }
}