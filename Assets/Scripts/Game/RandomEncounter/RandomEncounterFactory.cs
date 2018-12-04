using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallRandomEncountersMod.Enums;
using DaggerfallRandomEncountersMod.RandomEncounters;
using DaggerfallWorkshop.Game.Utility.ModSupport;

namespace DaggerfallRandomEncountersMod
{

    //Maybe does have to be monobehaviour, atleast to make it buildable with each other.
    public class RandomEncounterFactory
    {
        
        Dictionary<EncounterType, Dictionary<EncounterFilter, List<RandomEncounter>>> possibleEvents = new Dictionary<EncounterType, Dictionary<EncounterFilter, List<RandomEncounter>>>();

        //Adds random event, along with it's key.
        //First layer of keys is Encounter Type, then set of filters for encounter, after that it is random.
        //Will add player rep stuff later on.
        public void addRandomEvent(EncounterType type, RandomEncounter evt, EncounterFilter filters)
        {

            //Lazy loads the entries.
            if (!possibleEvents.ContainsKey(type))
            {
                possibleEvents[type] = new Dictionary<EncounterFilter, List<RandomEncounter>>();
            }
            if (!possibleEvents[type].ContainsKey(filters))
            {
                possibleEvents[type][filters] = new List<RandomEncounter>();
            }

            
            //Pushes random evt into that table.
            possibleEvents[type][filters].Add(evt);
        }

        

        //So it builds up pool of possibilties.
        //All filters applied, then just indiviudal filter applied, then just m of the n filters applied.
        //then random event among that big pool, should work with instance filter instead of creating for each
        // trigger.

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
            

           List<EncounterFilter> split = filter.splitFilters();

            //This could get fucking huge man.
            //Maybe there's a better structure for this.
            //Something where like as it goes down, it adds into pool.
            //Though actually I think would turn out same time complexity.
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
                return null;
            }

            RandomEncounter randomEvent = null;

            //If don't care about above or not possible then actually clone it.
            int index = Random.Range(0, subset.Count - 2);
            randomEvent = GameObject.Instantiate(subset[index].gameObject, Vector3.zero, Quaternion.identity).GetComponent<RandomEncounter>();

            return randomEvent;
        }
    }



}