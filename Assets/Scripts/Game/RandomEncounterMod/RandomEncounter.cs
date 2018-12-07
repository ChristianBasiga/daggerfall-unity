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

        public delegate void OnEndEventHandler(RandomEncounter evt);
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
        public virtual void Update() {


            //Only if event considered begun, then do tick for event.
            if (began)
            {
                if (OnTick != null)
                {

                    OnTick(this);
                }
                //If player dead, end the encounter, cause not active anymore, then clean up will happen.
                //GameObject will be parent of motor of encounter, that or move this else where
                if (GameManager.Instance.PlayerDeath.DeathInProgress)
                {
                    playerDied = true;
                    end();
                }

                if (GameManager.Instance.PlayerEnterExit.IsPlayerInside || GameManager.Instance.PlayerGPS.IsPlayerInTown())
                {
                    end();
                }


            }
        }
        public virtual void end() {

            //Prob their own closures for when player is dead.
           
            Debugging.AlertPlayer(closure);

            if (OnEnd != null)
            {
                OnEnd(this);
            }


          //  Destroy(this);
          //  Debug.Log("random event ended");


        }
    }
}


        
