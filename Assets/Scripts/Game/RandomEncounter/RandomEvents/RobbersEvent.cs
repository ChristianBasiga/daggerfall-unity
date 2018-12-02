using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Utility;
using DaggerfallRandomEncountersMod.Utils;

namespace DaggerfallRandomEncountersMod.RandomEncounters
{


    //In reality these would be prefabs.
    //In general this just an attacking event until implement stealing, but we'll see if get to that.
    [RandomEncounterIdentifier(EncounterId = "Robbers")]
    public class RobbersEvent : RandomEncounter
    {
        GameObject spawner;


        //This maybe in base, since always have something to spawn, but then again not always array.
        //But foe encounters always array even if one could create specifically a FoeEvent class
        //that these kinds of events inherit from.
        GameObject[] robbers;
        int spawnCount = 1;


        //Amount of robbers to spawn.
        //All of this stuff will be set by manager
        //so then the encounters don't need to look inside player directly.
        //But if they do, it makes it more dynamic and so that the encounters can behave on their own.

        int SpawnCount
        {
            set
            {
                spawnCount = value;
            }
            get
            {
                return spawnCount;
            }
        }

        // Use this for initialization
        void OnEnable()
        {
            warning = "You hear footsteps coming towards you.";

            closure = "You don't hear anyone else following you.";
        }

        public override void begin()
        {
            //triggers other begin
            //lol, in hindsight I could just do the
            //here too huh. So the on stuff is essentially irrelevant.
            //I've already tied this to Daggerfall by using their util functions, so might as well just do the graphic stuff directly.
            MobileTypes type = MobileTypes.Burglar;

            //Debugging.AlertPlayer(warning);

            //Spawns Burglar or Thief depending on player stat(need to look into differences)
            //Also may change mobile reaction to Custom for if player resting cause not attacking, just taking.
            robbers = GameObjectHelper.CreateFoeGameObjects(GameManager.Instance.PlayerObject.transform.position,
               type, spawnCount, MobileReactions.Hostile);

            Debug.Log("Began robbers");

            spawner = GameObjectHelper.CreateFoeSpawner();
            if (spawner != null)
            {
                spawner.GetComponent<FoeSpawner>().SetFoeGameObjects(robbers);
            }

            base.begin();

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
            Debug.Log("Ended robbers");

            base.end();
        }
    }
}