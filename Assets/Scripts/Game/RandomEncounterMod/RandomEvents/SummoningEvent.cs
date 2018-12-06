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
    //Currently not working as intended, only summons mages
    [RandomEncounterIdentifier(EncounterId = "Summoners")]
    public class SummoningEvent : RandomEncounter
    {

        FoeSpawner foeSpawner;

        //Either ust mage or sorcerer, mage for now until I know more about lore of game.
        //Array of mage gameobjects.
        GameObject[] summoners;
        GameObject summon;

        

        float summoningTime = 200.0f;
        float timeLeftToSummon;



        //Setter here for this, or shall this depend on player stats withGin here too?
        //Will make setter as needed.
        MobileTypes summonerType = MobileTypes.Mage;

        //Same as above, maybe ancient Lich later.
        //It kills people, look into other options they can summon.
        MobileTypes summonType = MobileTypes.Lich;


        //Two by default.
        int summonerCount = 2;

        public int SummonerCount
        {
            get
            {
                return summonerCount;
            }
            set
            {
                summonerCount = value;
            }
        }

        public float SummoningTime
        {
            set
            {
                summoningTime = value;
            }
            get
            {

                return summoningTime;
            }
        }
      
        public override void begin()
        {

            warning = "You hear chants of spell nearby";
           

            Debug.LogError("summoners event began");

            timeLeftToSummon = summoningTime;

            //Why are they not staying passive? Works for robber.
            summoners = GameObjectHelper.CreateFoeGameObjects(GameManager.Instance.PlayerObject.transform.position, summonerType,1, MobileReactions.Passive);


            foeSpawner = GameObjectHelper.CreateFoeSpawner(false, summonerType, 1, 10, 30, this.transform).GetComponent<FoeSpawner>();


            //Same thing happening.
            foeSpawner.SetFoeGameObjects(summoners);


            base.begin();
        }
        /*
        IEnumerator summonMagesInGroup()
        {
            
            doneSummoningMages = false;
            summoners = new GameObject[SummonerCount];

            Vector3 spawnPosition = GameManager.Instance.PlayerObject.transform.position;

            FoeSpawner spawner = GameObjectHelper.CreateFoeSpawner(false, summonerType, 1, 50, 60).GetComponent<FoeSpawner>();


            for (int i = 1; i <= SummonerCount; ++i)
            {
                GameObject[] mage = GameObjectHelper.CreateFoeGameObjects(spawnPosition, summonerType, 1, MobileReactions.Passive);
                Debug.LogError("Creating new mage");
                spawner.SetFoeGameObjects(mage);
                Debug.LogError("AFter setting mage to spawn, mage is ");
                Debug.LogError(mage[0]);
                //Loads into list of mages.
                 
                mage[0].name = "SummonerEncounter" + (i - 1);
                Debug.LogError("After inserting spawned mage into summoenrs array, mage is ");
                Debug.LogError(mage[0]);

                Debug.LogError("Spawned first mage, waiting for spawner to be null to reuse again");
                //Wait until done spawning mage before moving on.
                spawnPosition = mage[0].transform.position;
                yield return new WaitUntil(() => { return spawner == null; });
                Debug.LogError("Done waiting, resetting spawner and mage");
                summoners[i - 1] = GameObject.Find("SummonerEncounter" + (i - 1));
                //So some point ater the wait until it becomes null
                //so when spawner done spawnnning the references to mages become null, lol.
                //A work around for this which shouldn't have to happen, is tagging them then adding back in
                if (summoners[i-1] == null)
                {
                    Debug.LogError("previous mage set to null??");
                }
                //Will play around with values for grouping
                spawner = GameObjectHelper.CreateFoeSpawner(false, summonerType, 1, 5, 10).GetComponent<FoeSpawner>();
                Debug.LogError("creating new spawner");

              
            }


            doneSummoningMages = true;
            
        }
        */
        // Update is called once per frame
        public override void Update()
        {

            if (Began)
            {
                if (foeSpawner == null)
                {

                    if (!GameManager.Instance.PlayerEnterExit.IsPlayerInside)
                    {
                        if (EncounterUtils.hasActiveSpawn(summoners))
                        {
                            return;
                        }

                        //If didn't return then all dead, end encounter
                        end();
                        return;
                    }
                }
            }
            base.Update();


        }

        public override void end()
        {

            //If summon was successful, then drop loot at that point.


            /*if (summon.Length > 0 && summon[0] == null)
            {

                //Cause when lich is killed, has own corpose and that should be lootable.
                //Okay that's already created via EnemyDeath script, but I could use that method for that Ghost Event.

                //Then in EnemyDeath script, it generates the items via enemy entity id, so it's set already what will
                //be dropped. What I could do instead is drop something if summon not work.
               
            }*/


          //  closure = summon == null ? "You don't hear anymore chanting" : "You have slain the beast, it seems there are no more mages around.";
            //base.end();

        }
    }
}