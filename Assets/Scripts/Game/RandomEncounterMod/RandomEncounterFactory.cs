using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallRandomEncountersMod.Enums;
using DaggerfallRandomEncountersMod.RandomEncounters;
using DaggerfallWorkshop.Game.Utility.ModSupport;


using DaggerfallRandomEncountersMod.Filter;
using DaggerfallRandomEncountersMod.Utils;
namespace DaggerfallRandomEncountersMod
{
    public class RandomEncounterFactory
    {



        Dictionary<EncounterType, Dictionary<EncounterFilter, List<RandomEncounter>>> possibleEvents = new Dictionary<EncounterType, Dictionary<EncounterFilter, List<RandomEncounter>>>();

        //I HAVE TO go through all subsets
        //because the goal is to get all subsets and check them
        //There is optimization when the goal is something else, and easy way is to first produce subsets

        //But I also don't want to repeat, so now comes dynamic programming, in this case I can just do memoization.
        

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



        //Generate all possible events given a filter.
        private List<RandomEncounter> getAllPossibleEvents(Dictionary<EncounterFilter, List<RandomEncounter>> allEvents, EncounterFilter filter)
        {
            List<FilterData> split = filter.splitFilters;
            List<RandomEncounter> possibleEvents = new List<RandomEncounter>();

            //To make sure don't pull from same subset of events twice.
            Dictionary<int, bool> memoization = new Dictionary<int, bool>();



            List<EncounterFilter> subetFilter = new List<EncounterFilter>();
            List<FilterData> subset = new List<FilterData>();
    
            getAllPossibleEvents(subset, split, 0, subetFilter);


            foreach (EncounterFilter f in subetFilter)
            {

                if (allEvents.ContainsKey(f))
                {

                    foreach (RandomEncounter encounter in allEvents[f])
                    {

                        possibleEvents.Add(encounter);
                    }
                }
            }

            
            return possibleEvents;
        }


        //Generates all possible subsets of states.
        private void getAllPossibleEvents( List<FilterData> subset, List<FilterData> set, int index, List<EncounterFilter> subsetFilter)
        {

            for (int i = index; i < set.Count; ++i)
            {
                subset.Add(set[i]);

                getAllPossibleEvents(subset, set, i + 1, subsetFilter);

                EncounterFilter filter = new EncounterFilter();
                foreach (FilterData data in subset)
                {
                    filter.setFilter(data);
                    
                }

                subsetFilter.Add(filter);

                Debug.LogError("filter subset " + filter.ToString());
                subset.RemoveAt(subset.Count - 1);
            }


           
        }


        public RandomEncounter getRandomEvent(EncounterFilter filter)
        {


            Debug.LogError("filter " + filter.ToString());

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

            type = EncounterType.defaultTypes["Negative"];

            foreach (EncounterFilter f in possibleEvents[type].Keys) {

                Debug.LogError("filter in possible events has " + f.ToString());
            }

            if (!possibleEvents.ContainsKey(type))
            {
                //And it's fine it is, then that means no random encounter spawned for this type right now.
                return null;
            }

            List<RandomEncounter> subset = getAllPossibleEvents(possibleEvents[type], filter);
            
            if (subset.Count == 0)
            {
                Debug.LogError("No encounters associated with current state.");
                return null;
            }
            foreach(RandomEncounter e in subset)
            {

                Debug.LogError("encounter in subset " + e.ToString());
            }

            Reusable holder = PoolManager.Instance.acquireObject();


            

            //Chooses random encounter within subset then clones it.
            int index = subset.Count == 1? 0 : Random.Range(0, subset.Count);


            RandomEncounter randomEvent = (RandomEncounter)holder.gameObject.AddComponent((subset[index].GetComponent<RandomEncounter>().GetType()));
            Debug.LogError("chosen event "  + randomEvent.ToString());
            return randomEvent;
        }
    }



}