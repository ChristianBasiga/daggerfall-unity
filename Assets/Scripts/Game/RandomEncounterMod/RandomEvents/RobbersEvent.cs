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


 
    [RandomEncounterIdentifier(EncounterId = "Robbers")]
    public class RobbersEvent : RandomEncounter
    {
        GameObject spawner;


      
        GameObject[] robbers;
        //Default one.
        int spawnCount = 1;
    

        public override void begin()
        {

            warning = "You hear footsteps coming towards you.";

            closure = "You don't hear anyone else following you.";

            //Gets list of robbers
            robbers = GameObjectHelper.CreateFoeGameObjects(GameManager.Instance.PlayerObject.transform.position,
               MobileTypes.Thief, spawnCount, MobileReactions.Hostile);


            //Creates spawner for spawning them, then begins spawn of robbers.
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

                        //TODO:Then steal from player's inventory.

                    }
                    else if (!GameManager.Instance.PlayerEnterExit.IsPlayerInside)
                    {
                        if (EncounterUtils.hasActiveSpawn(robbers))
                        {
                            return;
                        }

                        //If didn't return then all dead, end encounter
                        end();
                        return;
                    }
                }
            }
        }

        public override void end()
        {

            base.end();
        }
    }
}