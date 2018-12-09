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

        
        GameObject[] summoners;
        GameObject summon;

        

        //Separate because mage may begin summoning again.
        const float summoningTime = 10.0f;
        float timeLeftToSummon;



        //Setter here for this, or shall this depend on player stats withGin here too?
        //Will make setter as needed.
        MobileTypes summonerType = MobileTypes.Mage;

        //Same as above, maybe ancient Lich later.
        //It kills people, look into other options they can summon.
        MobileTypes summonType = MobileTypes.SkeletalWarrior;


        //Two by default.
        int summonerCount = 2;

        public override void begin()
        {

            warning = "You hear chants of spell nearby";
           

            Debug.LogError("summoners event began");
            Debug.LogError(this);

            //Why are they not staying passive? Works for robber.
            summoners = GameObjectHelper.CreateFoeGameObjects(GameManager.Instance.PlayerObject.transform.position, summonerType,1, MobileReactions.Passive);


            //The position of encounter itself should be somehow within area,
            //though the onDeath or onLeave should be enough to callback to end encounters

            summoners[0].transform.parent = this.transform;

            //It didn't make sense that provide count and mobile type same time.
            //So if want to pass custom paramesters into foe spawner, create own array of Enemies
            //using the prefab, not CreateFoeGameObjects,but then cnt set to passive
            //unless get component.
            spawner = GameObjectHelper.CreateFoeSpawner();

            Debug.LogError(summoners[0].GetComponent<DaggerfallEntityBehaviour>().Entity);


            //Okay, so the problem is setting it in the tick.

            summoners[0].GetComponent<DaggerfallEntityBehaviour>().Entity.OnDeath += (DaggerfallEntity entity) =>
            {
                //Okay works here.
                Debug.LogError("summoner dead");


                //If stil time left to summon, then end it.
                
                if (timeLeftToSummon > 0)
                {
                    end();
                }
            };

            summon = null;

           

            //Same thing happening.
            spawner.GetComponent<FoeSpawner>().SetFoeGameObjects(summoners);



            timeLeftToSummon = summoningTime;
            base.begin();
        }

        // Update is called once per frame
        public override void tick()
        {
            base.tick();

           
            if (spawner == null)
            {

                //This is enough of a check
                //Because turned hostile from beign attacked or from summon succeeding.
                if (summoners[0].GetComponent<EnemyMotor>().IsHostile)
                {
                    stayOnTarget();
                }

                if (timeLeftToSummon > 0)
                {
                    timeLeftToSummon -= Time.deltaTime;
                }
                //Otherwise if time has run out, then summon.
                else if (summon == null)
                {
                    Debugging.AlertPlayer("You hear bones rattling");

                    //They both focus on player at beginning, but still
                    //could end up targeting each other.
                    summoners[0].GetComponent<EnemyMotor>().IsHostile = true;

                    //Forces their target to be player.
                    summoners[0].GetComponent<DaggerfallEntityBehaviour>().Target = GameManager.Instance.PlayerEntityBehaviour;

                    summon = GameObjectHelper.CreateEnemy("summon", summonType, GameManager.Instance.PlayerObject.transform.position);

                    summon.GetComponent<DaggerfallEntityBehaviour>().Entity.OnDeath += (DaggerfallEntity entity) =>
                    {
                        Debug.LogError("Killed summon");

                        //If both summoner and summon dead.
                        if (summoners[0] == null)
                        {
                            end();
                        }
                    };

                    summon.GetComponent<DaggerfallEntityBehaviour>().Target = GameManager.Instance.PlayerEntityBehaviour;
                    //It may be averted as it goes on.
                }
            }
        }

        public override void end()
        {
            //If summon failed and mage dead, increase stealth skil.

            closure = summon == null ? "You stopped the summon" : "You don't hear anymore chanting";
            base.end();

        }

        private void OnDestroy()
        {

            if (summoners != null && summoners[0] != null)
                Destroy(summoners[0].gameObject);

            if (summon != null)
                Destroy(summon.gameObject);

            // end();
        }

        private void stayOnTarget()
        {
            DaggerfallEntityBehaviour player = GameManager.Instance.PlayerEntityBehaviour;

            if (summon != null)
            {
                if (summon.GetComponent<DaggerfallEntityBehaviour>().Target != player)
                {
                    summon.GetComponent<DaggerfallEntityBehaviour>().Target = player;
                }
            }


            if (summoners[0].GetComponent<DaggerfallEntityBehaviour>().Target != player)
            {
                summoners[0].GetComponent<DaggerfallEntityBehaviour>().Target = player;
            }

        }
    }
}