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
        bool paused = false;
        protected bool effectReputation;

        public bool Began
        {
            get
            {
                return began;
            }
        }

        public bool Paused
        {
            get
            {
                return paused;
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


            //If load new game, then end encounter.
            //Or rather, cancel cause not really ended.
            StateManager.OnStartNewGame += (object sender, System.EventArgs e) =>
            {
                cancel();
            };

            //Pause encounter.
            StateManager.OnStateChange += (StateManager.StateTypes newState) =>
            {

                if (newState == StateManager.StateTypes.Paused || newState == StateManager.StateTypes.UI)
                {
                    paused = true;
                }

                Debug.LogError("new state " + newState.ToString());
            };

            began = true;
            Debugging.AlertPlayer(warning);


            if (OnBegin != null)
            {
                OnBegin(this);
            }

           
        }




        //I could still use tick instead of update so still optimal
        //so updates aren't always happening
        public virtual void Update() {

            //Debug.LogError(GameManager.Instance.StateManager.CurrentState.ToString());
            //Only if event considered begun, then do tick for event.
            if (began && !paused)
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


                //The encounter manager should realistically manage this pausing.
                //If not Game as well as not paused.
                else if ((!paused && GameManager.Instance.StateManager.CurrentState != StateManager.StateTypes.Game) || GameManager.Instance.PlayerEnterExit.IsPlayerInside || GameManager.Instance.PlayerGPS.IsPlayerInTown())
                {
                    Debug.LogError("I happen");
                    end();
                }
                else
                {
                    //Otherwise unpause and continue encounter.
                    paused = false;
                }


            }
        }

        //Also todo: make these uppercase cause that's practice in C#
        protected virtual void cancel()
        {
            //Gotta figure out why this is called 3 times, but for now quick fix.
            //If already cancelled don't trigger callback again.
            if (!began) return;

            began = false;
            Debug.LogError("Cancelling");
            if (OnEnd != null)
            {
                OnEnd(this, true);
            }
        }

        public virtual void end() {


            if (!began) return;

            began = false;

            //Prob their own closures for when player is dead.
            Debug.LogError("Ended encounter");
            Debugging.AlertPlayer(closure);

            if (OnEnd != null)
            {
                OnEnd(this, false);
            }


          //  Destroy(this);
          //  Debug.Log("random event ended");


        }
    }
}


        
