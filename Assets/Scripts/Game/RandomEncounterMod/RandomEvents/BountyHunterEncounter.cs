using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallRandomEncountersMod.Utils;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Utility;

namespace DaggerfallRandomEncountersMod.RandomEncounters
{
    
    [RandomEncounterIdentifier(EncounterId = "BountyHunter")]
    public class BountyHunterEncounter : RandomEncounter
    {

       
        GameObject[] hunters;

        //Updates accordingly
        int hunterCount;


        FoeSpawner foeSpawner;


        public BountyHunterEncounter()
        {

            
         

        }


    

        public override void begin()
        {

            
            warning = "You hear someone say, I'm going to get a jackpot off of you";

            closure = "There doesn't seem to be anymore hunters, you better keep crime activity low for a while";




            MobileTypes hunterType = MobileTypes.Archer;


            //Spawn number of hunters based on crime.
            //
            switch (GameManager.Instance.PlayerEntity.CrimeCommitted)
            {

                //Knight chased you from city.
                case PlayerEntity.Crimes.Trespassing:

                    hunterCount = 1;
                    hunterType = MobileTypes.Knight;
                    break;

                case PlayerEntity.Crimes.Theft:

                    hunterCount = 1;

                    hunterType = MobileTypes.Ranger;
                    break;
                case PlayerEntity.Crimes.Assault:
                    hunterCount = 2;
                    hunterType = MobileTypes.Warrior;
                    break;
                case PlayerEntity.Crimes.Murder:

                    hunterCount = 3;
                    hunterType = MobileTypes.Assassin;
                    break;
            }

            hunters = GameObjectHelper.CreateFoeGameObjects(GameManager.Instance.PlayerObject.transform.position, hunterType, 1, MobileReactions.Hostile);
            foeSpawner = GameObjectHelper.CreateFoeSpawner(false, hunterType, 1, 4, 10).GetComponent<FoeSpawner>();

            foeSpawner.SetFoeGameObjects(hunters);

            base.begin();

        }

        public override void Update()
        {

            base.Update();
            
            if (Began)
            {
                if (foeSpawner == null)
                {
                    if (!EncounterUtils.hasActiveSpawn(hunters))
                    {
                        end();
                        return;
                    }

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