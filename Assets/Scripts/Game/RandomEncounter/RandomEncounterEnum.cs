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
        POSITIVE,
        NEGATIVE,
        NEUTRAL,

    };


    //I could have value be templated, but then the factory won't work.
    //cause using same factory instance for all of it, or am i?
    //Different factory instance for each individual trigger?
    //Then has own set of encounters for that trigger then divided deeper with type and filter.
    public class EncounterFilter : System.IEquatable<EncounterFilter>
    {

        //Dictionary instead, this way it will work for fast travel encounters too.
        Dictionary<string, string> filters;

        //Could also be straight up variables, only some will be unused depending on context.


        public EncounterFilter()
        {
            // For world encounters 0: weather, 1: time:, 2: left dungeon or town, 3: climate(mountains, desert,...)
            //Above only applies if applying multiple filters to guarantee same hash for abc and bca.
            //Will think of better way later.

            
            filters = new Dictionary<string, string>();
        }

        public List<EncounterFilter> splitFilters()
        {


            List<EncounterFilter> splitFilters = new List<EncounterFilter>();



            return splitFilters;
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
    


    //Could be strings instead of enum, for the tables, so more easily extendable.
    //cause then people are stuck to only these filters. Problem with that though is combination
    //hash of newDay, rainy, left dungeon, and rainy, left dungeon, newDay would be different, but should
    //map to same thing, also that's more expensive than newDay | rainy | leftDungeon, essentially the trigger
    //shouldn't change the resulting encounters.
    //this also makes it so more constraints stricter subset, but there would be stuff that appear
    //on just raining then random from there, but then should just raining encapsulate every combination with raining in it?
    //No cause that wouldn't make sense lol, cause if raining doesn't mean meets rest of criteria, which
    //would make sense to be more random but eh.

    //So first layer of indirection is neutral,positive,negative, then next layer are these filters,
    //then from there player reputation, but taking that into account isn't just use of keys,
    //unless as simple as keys of who player has more reputation with, hmm, that's another layer of
    //complexity, try this first, see how works out.
   /* public enum EncounterFilter
    {

        //Weather based filters, sucks that essentiall rewriting these.
        //I could have another layer of keys with just weather, but weather isn't always
        //a criterion

        //Hmm okay cause can have rain and new day.
        //But can't have rain and cloudy.
        //
        RAIN = 1,
        CLOUDY = 2,
        FOG = 4,
        SNOW = 8,
        THUNDER = 16,
        SUNNY = 32,
        OVERCAST = 64,

        //Time based filters
        NEW_DAY = 
        DAWN,
        DUSK,
        MID_DAY,
        MID_NIGHT,


        //Whether left dungeon last or town.
        LEFT_DUNGEON,
        LEFT_TOWN,

    }*/
}