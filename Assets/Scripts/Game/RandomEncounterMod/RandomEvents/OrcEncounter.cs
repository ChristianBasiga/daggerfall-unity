using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Utility;
using DaggerfallRandomEncountersMod.Utils;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.UserInterfaceWindows;


namespace DaggerfallRandomEncountersMod.RandomEncounters
{
    [RandomEncounterIdentifier(EncounterId = "OrcEncounter")]
    public class OrcEncounter : RandomEncounter
    {

        GameObject[] orcs;

        //Updates accordingly
        int orcCount;


        FoeSpawner foeSpawner;

        public override void begin()
        {
            warning = "You sense a dangerous air surrounding you!";

            closure = "The orcs have been slain!";

            MobileTypes orcType = MobileTypes.Orc;
            Debug.LogError("1. Crime committed " + GameManager.Instance.PlayerEntity.CrimeCommitted.ToString());

            switch (GameManager.Instance.PlayerEntity.CrimeCommitted)
            {

                //Knight chased you from city.
                case PlayerEntity.Crimes.Trespassing:

                    orcCount = 1;
                    orcType = MobileTypes.Orc;
                    break;

                case PlayerEntity.Crimes.Theft:

                    orcCount = 2;

                    orcType = MobileTypes.OrcSergeant;
                    break;
                case PlayerEntity.Crimes.Assault:
                    orcCount = 3;
                    orcType = MobileTypes.OrcShaman;
                    break;
                case PlayerEntity.Crimes.Murder:

                    orcCount = 4;
                    orcType = MobileTypes.OrcWarlord;
                    break;
                default:
                    break;
 
            }

            orcs = GameObjectHelper.CreateFoeGameObjects(GameManager.Instance.PlayerObject.transform.position, orcType, orcCount, MobileReactions.Hostile);
            foeSpawner = GameObjectHelper.CreateFoeSpawner(false, orcType, orcCount, 4, 10).GetComponent<FoeSpawner>();

            foeSpawner.SetFoeGameObjects(orcs);
            base.begin();
        }

        public override void tick()
        {

            base.tick();
            if (foeSpawner == null)
            {
                if (!EncounterUtils.hasActiveSpawn(orcs))
                {
                    end();
                    return;
                }

            }

        }
        public override void end()
        {

            effectReputation = true;



            base.end();

            //Effect their reputation, for how many they killed, if killed all.
        }
    }
}

