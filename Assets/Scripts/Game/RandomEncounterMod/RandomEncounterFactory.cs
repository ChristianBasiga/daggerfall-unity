using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallRandomEncountersMod.Enums;
using DaggerfallRandomEncountersMod.RandomEncounters;
using DaggerfallWorkshop.Game.Utility.ModSupport;


using DaggerfallRandomEncountersMod.Filter;
namespace DaggerfallRandomEncountersMod
{
    public class RandomEncounterFactory
    {
        
        Dictionary<EncounterType, Dictionary<EncounterFilter, List<RandomEncounter>>> possibleEvents = new Dictionary<EncounterType, Dictionary<EncounterFilter, List<RandomEncounter>>>();

        //Adds random event, along with it's key.
        //First layer of keys is Encounter Type, then set of filters for encounter, after that it is random.
        //Will add player rep stuff later on.
        public void addRandomEvent(EncounterType type, RandomEncounter evt, EncounterFilter filters)
        {
            if (evt == null)
            {
                Debug.LogError("Adding " + type.ToString() + " but encounter is null");
                throw new System.Exception("RandomEncounter prototype cannot be null.");
            }

            //Lazy loads the entries.
            if (!possibleEvents.ContainsKey(type))
            {
                possibleEvents[type] = new Dictionary<EncounterFilter, List<RandomEncounter>>();
            }
            if (!possibleEvents[type].ContainsKey(filters))
            {
                possibleEvents[type][filters] = new List<RandomEncounter>();
            }

         
            //Pushes random evt into that list.
            possibleEvents[type][filters].Add(evt);
        }

        

        public RandomEncounter getRandomEvent(EncounterFilter filter)
        {

            //Randomizes if neutral, positive, or negative.

            //Choosing unity random over system random cause it's only over three values so don't need consistency
            //for testing. Ranges: 1-5 = neutral, 6-10 = positive, 11-15 = negative.
            int result = Random.Range(1, 16);

            EncounterType type;

            if (result < 6)
            {
                type = EncounterType.defaultTypes["Neutral"];
            }
            else if (result < 11)
            {
                type = EncounterType.defaultTypes["Positive"];
            }
            else
            {
                type = EncounterType.defaultTypes["Negative"];
            }


            if (!possibleEvents.ContainsKey(type))
            {
                Debug.LogError("Invalid type" + type);
                //And it's fine it is, then that means no random encounter spawned this time.
                return null;
            }

            List<RandomEncounter> subset;


            if (possibleEvents[type].ContainsKey(filter))
            {
                subset = possibleEvents[type][filter];
            }
            else
            {
                subset = new List<RandomEncounter>();
            }
            

            //Below is if filter has weather:rain, time: day, but only check for filter time:day in prototype,
            //then split to check for them individually as well.
            List<EncounterFilter> split = filter.splitFilters();
           
            for (int i = 0; i < split.Count; ++i)
            {
                if (possibleEvents[type].ContainsKey(split[i]))
                {
                    foreach (RandomEncounter evt in possibleEvents[type][split[i]])
                    {
                        subset.Add(evt);
                    }
                }
            }
            
            if (subset.Count == 0)
            {
                Debug.LogError("no encounters for the filter " + filter.ToString());

                //Which is also fine, not all game states will have encounter.
                return null;
            }


            RandomEncounter randomEvent = null;

            //Chooses random encounter within subset then clones it.
            int index = subset.Count == 1? 0 : Random.Range(0, subset.Count - 2);
            randomEvent = GameObject.Instantiate(subset[index]).GetComponent<RandomEncounter>();

            return randomEvent;
        }
    }



}