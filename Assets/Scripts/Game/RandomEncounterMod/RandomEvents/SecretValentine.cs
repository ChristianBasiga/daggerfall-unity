using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace DaggerfallRandomEncountersMod.RandomEncounters
{


    //Encounter where you wake with a random item in your inventory.
    [RandomEncounterIdentifier(EncounterId = "SecretValentine")]
    public class SecretValentine : RandomEncounter
    {
        bool wokeUp;
        bool deliveredItem;
        double timeToDeliver = 0.5f;
        double timeTillDeliver = 0;
        int framePassed = 0;
        int framesToWait = 2000;
        bool isResting = false;

        public override void begin()
        {

            //Shouldn't ever happen if this began, but yeah.

           

            deliveredItem = false;
            base.begin();

        }

        public override void tick()
        {
            base.tick();

            if (!(DaggerfallUI.Instance.UserInterfaceManager.TopWindow is DaggerfallRestWindow) && !isResting)
            { 
                if (framePassed == framesToWait)
                {
                    end();
                }
                else
                {
                    framePassed += 1;
                }
            }

            else if ((DaggerfallUI.Instance.UserInterfaceManager.TopWindow is DaggerfallRestWindow))
            {

                isResting = true;
                timeTillDeliver = timeToDeliver;
               
            }
            if (deliveredItem)
            {
                end();
            }

            
            if (timeTillDeliver > 0)
            {
                timeTillDeliver -= Time.deltaTime * 2;
            }

            else
            {
                //otherwise if player still resting choose an item.

                ItemCollection playerItems = GameManager.Instance.PlayerEntity.Items;
                Debug.LogError("Get to here ever");

                //Get flowers
                //Assuming group index is that
                Array enumArray = DaggerfallUnity.Instance.ItemHelper.GetEnumArray(ItemGroups.PlantIngredients1);

                //Ideally group then index with int casted enum value, but from what I've seen they do so can only assume 11 is the red rose? Maybe randomize flowers.
                DaggerfallUnityItem redRoses = new DaggerfallUnityItem(ItemGroups.PlantIngredients1, 11);


                //Pretty instant maybe have time to deliver.

                playerItems.AddItem(redRoses, ItemCollection.AddPosition.Front);

                deliveredItem = true;
            }



        }

        public override void end()
        {
            if (wokeUp)
                closure = "I'll work up the nerve some day...";
            else
                closure = "You look great when you're asleep";

            base.end();
        }
    }
}