using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallRandomEncounterEvents.Enums;
using DaggerfallWorkshop.Game;

namespace DaggerfallRandomEncounterEvents.RandomEvents
{

    //Any kind of event will extend from here.
    //The base ones part of this will have the Positive Neutral Negative, can move to other files later.
    //Really only need base, since no difference in representation from positive, neutral, and negative.
    //those enums will instead of be used in the factory.
    public abstract class RandomEvent : MonoBehaviour
    {
        //Will pass itself into event so onBegins know specifically what event began.
        public delegate void OnBeginEventHandler(RandomEvent evt);
        public event OnBeginEventHandler OnBegin;

        public delegate void OnEndEventHandler(RandomEvent evt);
        public event OnEndEventHandler OnEnd;

        //May add more params her later.
        public delegate void OnTickEventHandler(RandomEvent evt);
        public event OnTickEventHandler OnTick;

        bool began = false;

        public bool Began
        {
            get
            {
                return began;
            }
        }

        public virtual void begin() {

            began = true;



            if (OnBegin != null)
            {
                OnBegin(this);
            }

           
        }
        public virtual void Update() {


            //Only if event considered begun, then do tick for event.
            if (began)
            {

                //Need to check distance to player, then despawn accordingly.
                //Could be lazy and set timer instead.

                if (OnTick != null)
                {

                    OnTick(this);
                }
            }
        }
        public virtual void end() {

            if (OnEnd != null)
            {
                OnEnd(this);
            }

            began = false;
            Debug.Log("random event ended");
            Destroy(this);


        }
    }
}


        
