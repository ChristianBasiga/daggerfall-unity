using System.Collections;
using System.Collections.Generic;

using DaggerfallRandomEncountersMod.Utils;

namespace DaggerfallRandomEncountersMod.Filter
{ 

    public class EncounterFilter : System.IEquatable<EncounterFilter>
    {

        //Dictionary instead, this way it will work for fast travel encounters too.

        List<FilterData> filters;
        public EncounterFilter()
        {

            filters = new List<FilterData>();
        }

        //Returning FilterObjects of each individual split/
        //Consider doing this within factory, so can split and use filters to get rest of random encounters
        //at the same time to reduce the overall time complexity, not huge change but something.

        public List<EncounterFilter> splitFilters()
        {
            List<EncounterFilter> split = new List<EncounterFilter>();

            //Also only adds single filter, todo: make it so gets sub combinations too.
            foreach (FilterData filterData in filters)
            {
                EncounterFilter encounterFilter = new EncounterFilter();
                encounterFilter.setFilter(filterData);
                split.Add(encounterFilter);
            }

            return split;
        }




        public void setFilter(string context, string value)
        {
            FilterData filterData = new FilterData();
            filterData.context = context;
            filterData.value = value;
            filters.Add(filterData);
            filters.Sort((FilterData a, FilterData b) => { return string.Compare(a.context, b.context); });
        }

        public void setFilter(FilterData filterData)
        {
            filters.Add(filterData);
            filters.Sort((FilterData a, FilterData b) => { return string.Compare(a.context, b.context); });

        }


        public bool Equals(EncounterFilter other)
        {

            foreach (FilterData filter in other.filters)
            {
                //If at any point it doesn't contain filter then not equal
                if (!filters.Contains(filter))
                {
                    return false;
                }
            }

            return true;

        }

        public override string ToString()
        {
            string rep = "";
            foreach (FilterData entry in filters)
            {
                rep += "context: " + entry.context + " Value: " + entry.value + "\n";
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

            foreach (FilterData entry in filters)
            {

                totalFilter += entry.context + entry.value;
            }


            return totalFilter.GetHashCode();

        }
    }
}