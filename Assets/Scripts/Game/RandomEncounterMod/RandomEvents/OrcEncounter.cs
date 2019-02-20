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


            //Spawn number of hunters based on crime.
            //
            switch (GameManager.Instance.PlayerEntity.CrimeCommitted)
            {

                //Knight chased you from city.
                case PlayerEntity.Crimes.Trespassing:

                    orcCount = 2;
                    orcType = MobileTypes.Orc;
                    break;

                case PlayerEntity.Crimes.Theft:

                    orcCount = 3;

                    orcType = MobileTypes.OrcSergeant;
                    break;
                case PlayerEntity.Crimes.Assault:
                    orcCount = 4;
                    orcType = MobileTypes.OrcShaman;
                    break;
                case PlayerEntity.Crimes.Murder:

                    orcCount = 6;
                    orcType = MobileTypes.OrcWarlord;
                    break;
            }

            orcs = GameObjectHelper.CreateFoeGameObjects(GameManager.Instance.PlayerObject.transform.position, orcType, 1, MobileReactions.Hostile);
            foeSpawner = GameObjectHelper.CreateFoeSpawner(false, orcType, 1, 4, 10).GetComponent<FoeSpawner>();

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
    }
}

