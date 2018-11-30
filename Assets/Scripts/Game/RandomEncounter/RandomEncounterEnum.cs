using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop.Game.Weather;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop;

namespace DaggerfallRandomEncounterEvents.Enums
{
   public enum EncounterType
    { 
        Positive,
        Negative,
        Neutral,

    };



    public class EncounterFilter : System.IEquatable<EncounterFilter>
    {

        //Dictionary instead, this way it will work for fast travel encounters too.
        Dictionary<string, string> filters;



        public EncounterFilter()
        {
            
            filters = new Dictionary<string, string>();
        }

        //Returning FilterObjects of each individual split/
        //Consider doing this within factory, so can split and use filters to get rest of random encounters
        //at the same time to reduce the overall time complexity, not huge change but something.
        public List<EncounterFilter> splitFilters()
        {
            List<EncounterFilter> splitFilters = new List<EncounterFilter>();

            //Also only adds single filter, todo: make it so gets sub combinations too.
            foreach (string filterKey in filters.Keys)
            {
                EncounterFilter split = new EncounterFilter();
                splitFilters.Add(split);
            }

            return splitFilters;
        }

        public Dictionary<string,string> Filters
        {
            get
            {
                return filters;
            }
        }


        //Note: Change this to property later, but not prio.
        public string getFilter(string index)
        {
            return filters[index];
        }

        public void setFilter(string index, string filter)
        {
            filters[index] = filter;
        }

        public bool Equals(EncounterFilter other)
        {
            foreach (string key in other.filters.Keys)
            {
                //If don't share a key, then not equal.
                if (!filters.ContainsKey(key))
                {
                    return false; 
                }
                //If witin that shared key, their values not same, then not equal.
                else if (filters[key] != other.filters[key])
                {
                    return false;
                }
            }

            return true;

        }
        public override bool Equals(System.Object other)
        {
            return Equals((EncounterFilter)(other));
        }

        public override int GetHashCode()
        {
            string totalFilter = "";
            foreach (string key in filters.Keys)
            {

                totalFilter += filters[key];
            }


            return totalFilter.GetHashCode();
            
        }
    }

    //Then enums that effect Encounters, so essentially all of the weather stuff
    //climate stuff, etc. Sucks that have to rewrite them, but from what I've seen that's what others have done.
    //I could create a class that has them instead, so for sure using same ones.
    


  
}