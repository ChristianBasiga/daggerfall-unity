using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop.Game.Weather;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Utility.ModSupport;

using DaggerfallRandomEncountersMod.Utils;
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

        public override string ToString()
        {
            return id;
        }

        public override int GetHashCode()
        {
            return id.GetHashCode();
        }


    }



  
}