using System.Collections;
using System.Collections.Generic;

using DaggerfallRandomEncountersMod.Utils;

namespace DaggerfallRandomEncountersMod.Filter
{ 

    public class EncounterFilter : System.IEquatable<EncounterFilter>
    {

        //Dictionary instead, this way it will work for fast travel encounters too.

        //Keeping Dictionary probably would've been better.
        //
        Dictionary<string, string> filters;
        List<FilterData> oldFilters;
        public EncounterFilter()
        {

            oldFilters = new List<FilterData>();
            filters = new Dictionary<string, string>();
        }

        //Returning FilterObjects of each individual split/
        //Consider doing this within factory, so can split and use filters to get rest of random encounters
        //at the same time to reduce the overall time complexity, not huge change but something.

        public List<EncounterFilter> splitFilters()
        {
            List<EncounterFilter> split = new List<EncounterFilter>();

            //Also only adds single filter, todo: make it so gets sub combinations too.
            foreach (FilterData filterData in oldFilters)
            {
                EncounterFilter encounterFilter = new EncounterFilter();
                encounterFilter.setFilter(filterData);
                split.Add(encounterFilter);
            }

            return split;
        }






        public void setFilter(string context, string value)
        {

            filters[context] = value;
            return;

            //Filter data makes more sense and more concrete, but problem is now I have to do this.
            FilterData prevEntry = oldFilters.Find((FilterData data) => { return string.Equals(context, data.context); });

            if (prevEntry != null)
            {
                prevEntry.value = value;
            }
            else
            {
                FilterData filterData = new FilterData();
                filterData.context = context;
                filterData.value = value;
                oldFilters.Add(filterData);
                oldFilters.Sort((FilterData a, FilterData b) => { return string.Compare(a.context, b.context); });
            }
        }

        public void setFilter(FilterData filterData)
        {
            filters[filterData.context] = filterData.value;
            return;

            FilterData found = oldFilters.Find((FilterData data) => string.Equals(filterData.context, data.context));
            if ( found != null)
            {
                found.value = filterData.value;
            }
            else
            {
                oldFilters.Add(filterData);
                oldFilters.Sort((FilterData a, FilterData b) => { return string.Compare(a.context, b.context); });
            }
        }


        public bool Equals(EncounterFilter other)
        {

            foreach( KeyValuePair<string,string> entry in other.filters)
            //foreach (FilterData filter in other.oldFilters)
            {
                //If doesn't have same key, then not right.
                if (!filters.ContainsKey(entry.Key))
                {
                    return false;
                }
                //otherwise compare values.
                else if (!filters[entry.Key].Equals(entry.Value))
                {
                    return false;
                }
            }

            return true;

        }

        public override string ToString()
        {
            string rep = "";

            foreach (KeyValuePair<string,string> entry in filters)

            {
                rep += "context: " + entry.Key + " Value: " + entry.Value + "\n";
            }
            return rep;
        }

        public override bool Equals(System.Object other)
        {
            return Equals((EncounterFilter)(other));
        }

        public override int GetHashCode()
        {
            string totalFilter = "";

            //foreach (FilterData entry in oldFilters)
            foreach (KeyValuePair<string,string> entry in filters)
            {

                totalFilter += entry.Key + entry.Value;
            }


            return totalFilter.GetHashCode();

        }
    }
}