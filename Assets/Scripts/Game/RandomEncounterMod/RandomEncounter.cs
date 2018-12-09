using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallRandomEncountersMod.Enums;
using DaggerfallWorkshop.Game;

using DaggerfallRandomEncountersMod.Utils;
using DaggerfallWorkshop.Game.Utility.ModSupport;

namespace DaggerfallRandomEncountersMod.RandomEncounters
{

    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple= false, Inherited = false)]
    public class RandomEncounterIdentifierAttribute :  System.Attribute{

        public string EncounterId { get; set; }
    }



    //Any kind of event will extend from here.
    //The base ones part of this will have the Positive Neutral Negative, can move to other files later.
    //Really only need base, since no difference in representation from positive, neutral, and negative.
    //those enums will instead of be used in the factory.
    public abstract class RandomEncounter : MonoBehaviour
    {
        //Will pass itself into event so onBegins know specifically what event began.
        public delegate void OnBeginEventHandler(RandomEncounter evt);
        public event OnBeginEventHandler OnBegin;

        //This should just have extra param for cancelled
        //cause no other case where attribute for cancelled would be check except when they end?
        //If can argue the point then we'll add as property too.
        //Perhaps make this second argument be another struct for OnEnd information.
        public delegate void OnEndEventHandler(RandomEncounter evt, bool cancelled);
        public event OnEndEventHandler OnEnd;

        //May add more params her later.
        public delegate void OnTickEventHandler(RandomEncounter evt);
        public event OnTickEventHandler OnTick;


        protected string warning;
        protected string closure;

        //If encounter caused player to die.
        protected bool playerDied;
        bool began = false;
        protected bool effectReputation;

        public bool Began
        {
            get
            {
                return began;
            }
        }

        public bool EffectReputation
        {
            get
            {
                return effectReputation;
            }
        }


        public virtual void begin() {
           
            began = true;
            Debugging.AlertPlayer(warning);


            if (OnBegin != null)
            {
                OnBegin(this);
            }

           
        }




        //I could still use tick instead of update so still optimal
        //so updates aren't always happening
        public virtual void tick()
        {
            //If Began check still needs to happen, to make sure.
            if (!began)
            {
                throw new System.Exception("Error:Encounters must have began to tick them");
            }
            if (OnTick != null)
            {
                OnTick(this);
            }
        }

        public virtual void end() {


            //if (!began) return;

            began = false;

            //Prob their own closures for when player is dead.
            Debug.LogError("Ended encounter");
            Debugging.AlertPlayer(closure);

            if (OnEnd != null)
            {
                if (this != null)
                {
                    OnEnd(this, false);
                }
            }

        }
    }
}


        
