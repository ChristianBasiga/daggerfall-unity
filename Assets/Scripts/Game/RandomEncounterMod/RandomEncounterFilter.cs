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
        List<FilterData> split;
        
        public EncounterFilter()
        {

            split = new List<FilterData>();
            filters = new Dictionary<string, string>();
        }

        public List<FilterData> splitFilters
        {
            get
            {
                return split;
            }
        }

      





        public void setFilter(string context, string value)
        {

            filters[context] = value;

            //Filter data makes more sense and more concrete, but problem is now I have to do this.
            FilterData prevEntry = split.Find((FilterData data) => { return string.Equals(context, data.context); });

            if (prevEntry != null)
            {
                prevEntry.value = value;
            }
            else
            {
                FilterData filterData = new FilterData();
                filterData.context = context;
                filterData.value = value;
                split.Add(filterData);
                split.Sort((FilterData a, FilterData b) => { return string.Compare(a.context, b.context); });
            }
        }

        public void setFilter(FilterData filterData)
        {

            filters[filterData.context] = filterData.value;
            
            FilterData found = split.Find((FilterData data) => string.Equals(filterData.context, data.context));
            if ( found != null)
            {
                found.value = filterData.value;
            }
            else
            {
                split.Add(filterData);

                split.Sort((FilterData a, FilterData b) => { return string.Compare(a.context, b.context); });
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
            int hash = 0;
            //foreach (FilterData entry in oldFilters)

            //This makes it so don't gotta sort, but feel like bound to fail, need to come up with better hash.
            foreach (KeyValuePair<string,string> entry in filters)
            {

                hash += entry.Value.GetHashCode();
            }


            return hash;

        }
    }
}