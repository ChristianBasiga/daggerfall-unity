using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DaggerfallRandomEncounterEvents.RandomEvents;


//Prob make new namespace for stuff like this and json data.
namespace DaggerfallRandomEncounterEvents.Utils
{
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false)]
    public class RandomEncounterIdentifierAttribute : Attribute
    {
        //Metadata about random encounter, such as the script name.
        //All RandomEncounters must create this attribute, inheritance doesn't enforce it.
        //So that is extra check needed to be made, when adding to cache is if a class
        //that has this attribute is infact a RandomEvent.

        //Name set here must match with name set in json data for encounter loading.
        public string Name { get; set; }

    }

    public class NotRandomEncounterException : Exception
    {
        private string typeFullName;

        private const string solution = "You must derive from " + RandomEncounterCache.randomEventFullName;

        public NotRandomEncounterException(string typeFullName) : base(typeFullName + " is not a RandomEncounter")
        {
            this->typeFullName = typeFullName;
        }

        public string TypeFullName
        {
            get
            {
                return typeFullName;
            }
        }

        public string Solution
        {
            get
            {
                return solution;
            }
        }


    }


    //Collection of all the Random Encounter scripts in current assembly.
    public class RandomEncounterCache
    {
        //Still needs to be private so people can't reset it, or put in Types other than RandomEncounter types.
        static Dictionary<string, Type> allRandomEvents;
        public static const string randomEventFullName = "DaggerfallRandomEncounterEvents.RandomEvents.RandomEvent";
        public static const string baseType = "System.Object";
        public static void addToCache(string name, Type type)
        {

            if (isValidType(type))
            {
                //Adds into cache.
                allRandomEvents.Add(name, type);
            }
            else
            {
                //Log that this isn't valid script because not a RandomEvent.
                throw new NotRandomEncounterException(name);
            }
        }

        //Goes up the heirarchy to make sure that it is a RandomEncounter.
        private static bool isValidType(Type type)
        {


            //Start off with base type, because current type cannot be RandomEncounter itself as it is abstract.
            type = type.BaseType;

            while (type.FullName != randomEventFullName && type.FullName != baseType)
            {
                type = type.BaseType;
            }

            //If got to base type without hitting randomEventFullName, then is not randomEvent.
            return type.FullName == randomEventFullName;
            
        }
    }
}