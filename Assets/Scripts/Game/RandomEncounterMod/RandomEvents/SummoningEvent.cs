using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Utility;
using DaggerfallRandomEncountersMod.Utils;
using DaggerfallWorkshop.Game.Entity;

namespace DaggerfallRandomEncountersMod.RandomEncounters
{
    //Currently not working as intended, only summons mages
    [RandomEncounterIdentifier(EncounterId = "Summoners")]
    public class SummoningEvent : RandomEncounter
    {

        GameObject spawner;

        //Either ust mage or sorcerer, mage for now until I know more about lore of game.
        //Array of mage gameobjects.
        GameObject[] summoners;
        GameObject[] summon;

        

        //Separate because mage may begin summoning again.
        const float summoningTime = 10.0f   ;
        float timeLeftToSummon;



        //Setter here for this, or shall this depend on player stats withGin here too?
        //Will make setter as needed.
        MobileTypes summonerType = MobileTypes.Mage;

        //Same as above, maybe ancient Lich later.
        //It kills people, look into other options they can summon.
        MobileTypes summonType = MobileTypes.SkeletalWarrior;


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

      
      
        public override void begin()
        {

            warning = "You hear chants of spell nearby";
           

            Debug.LogError("summoners event began");


            //Why are they not staying passive? Works for robber.
            summoners = GameObjectHelper.CreateFoeGameObjects(GameManager.Instance.PlayerObject.transform.position, summonerType,1, MobileReactions.Passive);

            summoners[0].transform.parent = this.transform;

            //It didn't make sense that provide count and mobile type same time.
            //So if want to pass custom paramesters into foe spawner, create own array of Enemies
            //using the prefab, not CreateFoeGameObjects,but then cnt set to passive
            //unless get component.
            spawner = GameObjectHelper.CreateFoeSpawner();


            //Same thing happening.
            spawner.GetComponent<FoeSpawner>().SetFoeGameObjects(summoners);

            timeLeftToSummon = summoningTime;
            base.begin();
        }

        // Update is called once per frame
        public override void Update()
        {
            base.Update();


            if (Began)
            {
                if (spawner == null)
                {

                    //If mage is dead.
                    if (!summoners[0].activeInHierarchy)
                    {
                        //If summon never happened
                        if (summon == null)
                        {
                            end();
                        }
                    }

                    //Otherwise if mage is still alive and time left to spawn, then tick
                    //summon time.
                    else if (timeLeftToSummon > 0)
                    {
                        //Debug.LogError("ticking summon");
                        timeLeftToSummon -= Time.deltaTime;
                    }
                    //Otherwise if time has run out, then summon.
                    else if (summon == null)
                    {
                        Debug.LogError("Begin summon");

                        //Ideally 
                        Debugging.AlertPlayer("You hear bones rattling");

                        summon = new GameObject[1];
                        summon[0] = GameObjectHelper.InstantiatePrefab(DaggerfallUnity.Instance.Option_EnemyPrefab.gameObject, "summon", null,
                            summoners[0].transform.position);
                        //summon[0].SetActive(false);
                        summon = GameObjectHelper.CreateFoeGameObjects(summoners[0].transform.position, summonType,
                            1);
                            
                        //Maybe assign an ondeath to summon, but not really needed cause of this update happening anyway.


                       spawner = GameObjectHelper.CreateFoeSpawner(false, summonType, 1, 4, 5, this.transform);
                        DaggerfallEntityBehaviour daggerfallEntityBehaviour = summon[0].GetComponent<DaggerfallEntityBehaviour>();

                        daggerfallEntityBehaviour.Entity.OnDeath += (DaggerfallEntity entity) =>
                        {

                            //If both summoner and summon dead.
                            if (summoners[0] == null)
                            {
                                end();
                            }
                        };

                        spawner.GetComponent<FoeSpawner>().SetFoeGameObjects(summon);

                    }
                    
                }


            }


        }

        public override void end()
        {
            //If summon failed and mage dead, increase stealth skil.

            closure = summon == null ? "You stopped the summon" : "You don't hear anymore chanting";
            base.end();

        }
    }
}