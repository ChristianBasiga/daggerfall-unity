using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Utility;
using DaggerfallRandomEncounterEvents.Utils;

namespace DaggerfallRandomEncounterEvents.RandomEvents
{


    //In reality these would be prefabs.
    public class RobbersEvent : RandomEvent
    {
        GameObject spawner;


        //This maybe in base, since always have something to spawn, but then again not always array.
        //But foe encounters always array even if one could create specifically a FoeEvent class
        //that these kinds of events inherit from.
        GameObject[] robbers;
        int spawnCount = 1;
        // Use this for initialization
        void Start()
        {


            
        }

        public override void begin()
        {
            //triggers other begin
            base.begin();

            //lol, in hindsight I could just do the
            //here too huh. So the on stuff is essentially irrelevant.
            //I've already tied this to Daggerfall by using their util functions, so might as well just do the graphic stuff directly.
            MobileTypes type = MobileTypes.Burglar;

            Debugging.AlertPlayer("You hear someone following you");

            //Spawns Burglar or Thief depending on player stat(need to look into differences)
            //Also may change mobile reaction to Custom for if player resting cause not attacking, just taking.
            robbers = GameObjectHelper.CreateFoeGameObjects(GameManager.Instance.PlayerObject.transform.position,
               type, spawnCount, MobileReactions.Hostile);

            spawner = GameObjectHelper.CreateFoeSpawner();
            if (spawner != null)
            {
                Debug.Log("spawning stuff");
                spawner.GetComponent<FoeSpawner>().SetFoeGameObjects(robbers);
            }
        }

        // Update is called once per frame
        public override void Update()
        {
            base.Update();

            //For world encounter nothing special, they just attack the player, then when none left in world
            //encounter is over.
            if (Began)
            {

                //If done spawning.
                if (spawner == null)
                {
                    if (GameManager.Instance.PlayerEntity.IsResting)
                    {

                        //Then steal from player's inventory.

                    }
                    else if (!GameManager.Instance.PlayerEnterExit.IsPlayerInside)
                    {
                        if (EncounterUtils.hasActiveSpawn(robbers))
                        {
                            return;
                        }

                        //If didn't return then all dead, end encounter
                        end();
                    }
                }
            }
        }

        public override void end()
        {

            base.end();
            Debugging.AlertPlayer("You don't hear anybody else following you");
        }
    }
}