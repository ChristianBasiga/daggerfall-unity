using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop.Game.Weather;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Utility.ModSupport;

namespace DaggerfallRandomEncountersMod.Enums
{
    /*
   public enum EncounterType
    { 
        Positive,
        Negative,
        Neutral,

    };
    */  
    //Enums don't work with mvc compiler, so have to simulate.


  


    public class EncounterType :  System.IEquatable<EncounterType>
    {
        private string id;


        //Dictionary to make taking json data into encounter type easier.
        public static readonly Dictionary<string, EncounterType> defaultTypes = new Dictionary<string, EncounterType>()
        {
            {"Positive" , new EncounterType("Positive") },
            {"Negative" , new EncounterType("Negative") },
            {"Neutral" , new EncounterType("Neutral") },
        };

        public EncounterType(string id)
        {
            this.id = id;
        }

        public bool Equals(EncounterType type)
        {

            return type.id == id;
        }

        public override bool Equals(object obj)
        {
            return Equals((EncounterType)obj);
        }

        public override int GetHashCode()
        {
            return id.GetHashCode();
        }


    }



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
            List<EncounterFilter> split = new List<EncounterFilter>();

            //Also only adds single filter, todo: make it so gets sub combinations too.
            foreach (KeyValuePair<string,string> entry in filters)
            {
                EncounterFilter filter = new EncounterFilter();
                filter.setFilter(entry.Key, entry.Value);
                split.Add(filter);
            }

            return split;
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
            
            foreach (KeyValuePair<string,string> entry in other.filters)
            {
                //If don't share a key, then not equal.
                if (!filters.ContainsKey(entry.Key))
                {
                    return false; 
                }
                //If witin that shared key, their values not same, then not equal.
                else if (filters[entry.Key] != entry.Value)
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
            
            foreach (KeyValuePair<string,string> entry in filters)
            {

                totalFilter += entry.Value;
            }
            

            return totalFilter.GetHashCode();
            
        }
    }

    //Then enums that effect Encounters, so essentially all of the weather stuff
    //climate stuff, etc. Sucks that have to rewrite them, but from what I've seen that's what others have done.
    //I could create a class that has them instead, so for sure using same ones.
    


  
}