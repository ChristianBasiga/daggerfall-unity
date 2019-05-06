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
        double timeToDeliver = 5.0f;
        double timeTillDeliver = 0;


        public override void begin()
        {

            //Shouldn't ever happen if this began, but yeah.

            if (!(DaggerfallUI.Instance.UserInterfaceManager.TopWindow is DaggerfallRestWindow))
            {
                return;

            }

            warning = "You hear giggling";
            deliveredItem = false;
            base.begin();

            timeTillDeliver = timeToDeliver;
        }

        public override void tick()
        {
            base.tick();

            if (!(DaggerfallUI.Instance.UserInterfaceManager.TopWindow is DaggerfallRestWindow))
            {

                wokeUp = true;
                deliveredItem = false;
                end();

            }
            else if (timeTillDeliver > 0)
            {
                timeTillDeliver -= Time.deltaTime;
            }
            else
            {
                //otherwise if player still resting choose an item.

                ItemCollection playerItems = GameManager.Instance.PlayerEntity.Items;


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