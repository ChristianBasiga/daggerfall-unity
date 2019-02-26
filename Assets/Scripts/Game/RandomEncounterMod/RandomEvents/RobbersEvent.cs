using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Utility;
using DaggerfallRandomEncountersMod.Utils;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.UserInterfaceWindows;

namespace DaggerfallRandomEncountersMod.RandomEncounters
{


 
    [RandomEncounterIdentifier(EncounterId = "Robbers")]
    public class RobbersEvent : RandomEncounter
    {
        GameObject spawner;



        const float thiefEscapeTime = 10.0f;
        float timeTillThiefGone;

        //Items stolen, must have this intermediate stop cause even if
        //enemies inactive, still triggers rest interrupt.
        ItemCollection stolenItems;
        //Will have timer here for time it takes robber to steal, and time to leave.
      
        GameObject[] robbers;


        //Default one.
        int spawnCount = 1;

        bool stoleItem = false;
        bool wokeUp = false;

        public override void begin()
        {
            if (!GameManager.Instance.PlayerEntity.IsResting)
            {
                return;
                warning = "You hear footsteps coming towards you";

                robbers = GameObjectHelper.CreateFoeGameObjects(GameManager.Instance.PlayerObject.transform.position,
                  MobileTypes.Thief, spawnCount, MobileReactions.Hostile);

                //Prob better to assign an Ondeath for it too, but it's just default in world.
                robbers[0].GetComponent<DaggerfallEntityBehaviour>().Entity.OnDeath += (DaggerfallEntity e) =>
                {
                    closure = "You don't hear anybody else sneaking around";
                    end();
                };

                spawner = GameObjectHelper.CreateFoeSpawner();
                spawner.GetComponent<FoeSpawner>().SetFoeGameObjects(robbers);
            }

        
            
          
            //Gets list of robbers




            timeTillThiefGone = thiefEscapeTime;
            Debug.LogError("Item count before" + GameManager.Instance.PlayerEntity.Items.Count);

            //So fucking true here, yet,
            Debug.LogError("Is resting from robber event" + GameManager.Instance.PlayerEntity.IsResting);
            stolenItems = new ItemCollection();

            Debug.LogError(DaggerfallUI.Instance.UserInterfaceManager.TopWindow);
            base.begin();

        }

        // Update is called once per frame
        public override void tick()
        {
            base.tick();

            //For world encounter nothing special, they just attack the player, then when none left in world
            //encounter is over. So this tick only for resting case.

            //If already stole item thief is running while player still sleeping.
            if (stoleItem && !wokeUp)
            {
                //It should actually roll the chance at every point.
                if (timeTillThiefGone > 0)
                {

                    timeTillThiefGone -= Time.deltaTime;
                    tryWakeUp();

                }
                //If gone, then wait until rest window gone for them to be notified that they lost something.
                //Maybe change to contains, but will take more time, it's something to do later incase it is not the top and resting.
                //even if it's not, though it's somewhre below and those above will pop
                else if (!(DaggerfallUI.Instance.UserInterfaceManager.TopWindow is DaggerfallRestWindow))
                {
                    closure = "You notice you're missing something from your inventory";

                    end();
                }

            }

            //Okay, so checking it in instance vs on new hour for some reason isn't synced up??
            else if (DaggerfallUI.Instance.UserInterfaceManager.TopWindow is DaggerfallRestWindow)
            {

                //Transfer random item from player inventory into robber stolen Inventory.
                //ToDo: Instead of completely random, filter out quest items and items they can't get back.
                //cause that would break the game / quest they're on..
                
                ItemCollection playerItems = GameManager.Instance.PlayerEntity.Items;


                List<int> indicesOfItemsCanSteal= new List<int>();



                //Filtering to only range indicies of items that are valid to steal,
                //ie: not quest items.
                //There might be better way, but this seems fine.
                for (int i = 0; i <  playerItems.Count; ++i)
                {

                    DaggerfallUnityItem item = playerItems.GetItem(i);

                    //Maybe allow to steal from equipped from higher chance
                    //will wake up player.
                    if (!item.IsArtifact && !item.IsQuestItem && !item.IsEnchanted && !item.IsEquipped)
                    {
                        indicesOfItemsCanSteal.Add(i);
                    }
                }
                //Since adding in order the first one is min and last one is max index of item can get.
                int itemIndex = UnityEngine.Random.Range(indicesOfItemsCanSteal[0], indicesOfItemsCanSteal[indicesOfItemsCanSteal.Count]);

                DaggerfallUnityItem itemToSteal = playerItems.GetItem(itemIndex);


                stolenItems.Transfer(itemToSteal, playerItems);

                Debug.LogError("Item count After" + GameManager.Instance.PlayerEntity.Items.Count);
                stoleItem = true;

                robbers = GameObjectHelper.CreateFoeGameObjects(GameManager.Instance.PlayerObject.transform.position,
                MobileTypes.Thief, spawnCount, MobileReactions.Hostile);

                tryWakeUp();

                if (wokeUp)
                {
                  
                    DaggerfallUI.Instance.UserInterfaceManager.PopWindow();
                    Debugging.DebugLog("You hear someone rummaging through your inventory.");
                    //In this case the enemy will force the interrupt, so not good case of that, but still cool.

                    //Transfer items from stolen loot into robbers items so that they're dropped on death.
                    //May clear items so only that, but makes sense that thieves would have other stuff though.
                    //Maybe that'll vary on rep, climate, and such.
                    robbers[0].GetComponent<DaggerfallEntityBehaviour>().Entity.Items.TransferAll(stolenItems);


                    robbers[0].GetComponent<DaggerfallEntityBehaviour>().Entity.OnDeath += (DaggerfallEntity entity) =>
                    {

                        closure = "You see the items that were in your inventory";
                        end();
                    };

                    spawner = GameObjectHelper.CreateFoeSpawner();
                    spawner.GetComponent<FoeSpawner>().SetFoeGameObjects(robbers);
                }
            }
            else if (!wokeUp)
            {
                //If rest window not up anymore, then they woke up either on thier own or some other reason.
                Debugging.AlertPlayer("You notice thief rummaging through your inventory");
                wokeUp = true;
            }
            
            
            

        }


        private void tryWakeUp()
        {
            //Compare players luck with thief's stealth and maybe luck too?
            //ToDo: Make wake up criterion closer to what would look at for game.

            int playerLuckLevel = GameManager.Instance.PlayerEntity.Stats.LiveLuck;

            DaggerfallEntity robberEntity = robbers[0].GetComponent<DaggerfallEntityBehaviour>().Entity;

            int thiefStealth = robberEntity.Skills.GetLiveSkillValue(DaggerfallConnect.DFCareer.Skills.Stealth);

            Debug.LogError("player luck " + playerLuckLevel);
            Debug.LogError("thief stealth level " + thiefStealth);

            //Randomize on stealth with offset of player luck, just random as fuck equation lol.
            int roll = UnityEngine.Random.Range(-thiefStealth, thiefStealth / 2) + playerLuckLevel;
            wokeUp = roll > playerLuckLevel;
        }

        public override void end()
        {
            
            base.end();
        }
    }
}