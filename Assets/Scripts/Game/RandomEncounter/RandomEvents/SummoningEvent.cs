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

    //This is the event where Wizards will be spawned passive.
    //Then over time, a demon will be spawned if they weren't interrupted.
    //Player actions:
    //  Attack Wizards before summon.
    //  Attack Wizards and Demon after summon.
    //  Ignore.
    public class SummoningEvent : RandomEvent
    {


        //Only need one pointer because after spawn wizards, will reuse variable to spawn demon.
        FoeSpawner spawner;


        //Either ust mage or sorcerer, mage for now until I know more about lore of game.
        //Array of mage gameobjects.
        GameObject[] summoners;

        //This will vary depending on amount of mages which will vary depending on player stats
        //and reputation, but the properties will just be set by manager, or should it be
        //checked here? If check here that couples it to always reference player in same way.
        //For finishing quikc leave as property to leave decoupled.
        GameObject[] summon;

        float summoningTime = 5.0f;
        float timeLeftToSummon;



        //Setter here for this, or shall this depend on player stats within here too?
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

        // Use this for initialization
        void Start()
        {

            timeLeftToSummon = summoningTime;
         //   summonedDemon = false;
            
        }

        public override void begin()
        {
            base.begin();

            //Ideally reaction would actually be custom, but until I learna bout world coords and how to move them
            //I'll keep passive so not bother player until player bothers them cause focused on summoning.
            //But maybe after summoning they go agro? But then will be auto negative encounter not neutral.


            //Efficiency wise, the summoner should be allocated in start and all clones will use those same
            //set of summoners, adding more as needed, note to optimize with pooling later.

            //Allocates the summoner.
            summoners = GameObjectHelper.CreateFoeGameObjects(GameManager.Instance.PlayerObject.transform.position,
                summonerType, SummonerCount, MobileReactions.Passive);



            //Will create billboard parent for spawner alter.
            spawner = GameObjectHelper.CreateFoeSpawner(false, summonerType, SummonerCount, 10, 30).GetComponent<FoeSpawner>();

            //Starts the spawn for summoners
            spawner.SetFoeGameObjects(summoners);
        }

        // Update is called once per frame
        public override void Update()
        {

            if (Began)
            {
                //Cause if spawner null, then done spawning wizards.
                //Don't want to start clock until then.
                if (spawner == null)
                {

                    //Also ends, if summoners are dead regardles of time though.
                    //Could be a util method.

                    //Checking this every frame is potentially expensive.
                    if (!EncounterUtils.hasActiveSpawn(summoners))
                    {
                        //If no more active summoners and summon either dead or was never summoned
                        //then end encounter.
                        if ((summon.Length > 0 && summon[0] == null) || summon.Length == 0)
                        {
                            end();
                            return;

                        }

                        //Otherwise if summon still active, keep going.
                    }
                    

                    //Tick down summoning time.
                    if (timeLeftToSummon > 0)
                    {
                        timeLeftToSummon -= Time.deltaTime;
                    }
                    else
                    {

                        //This means summon has been summoned.
                        if (summon.Length > 0)
                        {
                            //If inactive, then dead. End event.
                            if (!summon[0].activeInHierarchy)
                            {
                                end();
                            }
                        }
                        else
                        {

                            //Then spawns the lich? Or if any are dead, then no summon?
                            //That's logistics and extra shit, for testing will leave as this.
                            //Also should be spawned at where they're circled at
                            //or at last summoners position, if laddter, I need to make own loop.

                            //First active one it sees.
                            GameObject activeSpawn = EncounterUtils.getActiveSpawn(summoners);

                            if (activeSpawn != null)
                            {
                                //Allocates the summon.
                                summon = GameObjectHelper.CreateFoeGameObjects(activeSpawn.transform.position,
                                    summonType, 1);

                                //Recreates spawner and spawns the summon, should spawn realy near to summoner
                                spawner = GameObjectHelper.CreateFoeSpawner(false, summonType, 1, 4, 6).GetComponent<FoeSpawner>();
                                spawner.SetFoeGameObjects(summon);
                            }
                            else
                            {
                                //If timer out and no more summoners left, just end the encounter.
                                end();
                            }
                        }

                    }
                }
            }

        }

        public override void end()
        {
            base.end();

            //If summon was successful, then drop loot at that point.


            /*if (summon.Length > 0 && summon[0] == null)
            {

                //Cause when lich is killed, has own corpose and that should be lootable.
                //Okay that's already created via EnemyDeath script, but I could use that method for that Ghost Event.

                //Then in EnemyDeath script, it generates the items via enemy entity id, so it's set already what will
                //be dropped. What I could do instead is drop something if summon not work.
               
            }*/

            //But only if they killed them will it drop.
            if (summon.Length == 0)
            {

            }
        }
    }
}