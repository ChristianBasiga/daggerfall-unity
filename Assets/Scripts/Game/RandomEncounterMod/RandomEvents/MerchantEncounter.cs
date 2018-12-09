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
using DaggerfallWorkshop.Game.Items;

namespace DaggerfallRandomEncountersMod.RandomEncounters
{
    [RandomEncounterIdentifier(EncounterId = "MerchantEncounter")]
    public class MerchantEncounter : RandomEncounter
    {

        GameObject npc;

        //For before tried pickpocketing, quick way to check.
        //this state would actually be stored in filter for how much gold
        //the player holds, then compare that to up to date version.
        //Problem is observing that may make it up to date before hand.
        int goldPlayerHeld;

        //For my own because may be frame behind before that gets processed.

        public override void begin()
        {

            warning = "You hear the rustling of items";

            npc = GameObjectHelper.InstantiatePrefab(DaggerfallUnity.Instance.Option_MobileNPCPrefab.gameObject, "Merchant Encounter", this.transform, GameManager.Instance.PlayerObject.transform.position);

            //merchant = npc.AddComponent<StaticNPC>();
            //Works, but he has no sprite.

            Races npcRace = GameManager.Instance.PlayerGPS.GetRaceOfCurrentRegion();

            //Randomizes texture and gender.
            npc.GetComponent<MobilePersonNPC>().RandomiseNPC(npcRace);


            npc.transform.parent = this.transform;

            //So instance is, but not component, for now I'll set it directly until they fix the prefab themselves.
            npc.GetComponent<MobilePersonNPC>().Motor = npc.GetComponent<MobilePersonMotor>();

            goldPlayerHeld = GameManager.Instance.PlayerEntity.GoldPieces;


            npc.GetComponent<DaggerfallEntityBehaviour>().Entity.OnDeath += (DaggerfallEntity entity) =>
            {
                //Random chance that dropped gold, offset by pickpocket attempt.

                Debug.LogError("I happen");
                closure = "He dropped some loot";
                // Randomise container texture

                //entity.EntityBehaviour.GetComponent<MobilePersonMotor>(). I want tdropped there, but o 
                int iconIndex = UnityEngine.Random.Range(0, DaggerfallLootDataTables.randomTreasureIconIndices.Length);
                int iconRecord = DaggerfallLootDataTables.randomTreasureIconIndices[iconIndex];

                //ToDo: Make it so dropped on floor of position not floating lol.
                DaggerfallLoot loot = GameObjectHelper.CreateLootContainer(LootContainerTypes.CorpseMarker, InventoryContainerImages.Merchant,
                    entity.EntityBehaviour.transform.position, null, DaggerfallLootDataTables.randomTreasureArchive,
                    iconRecord);

                //
                Debug.LogError("item count of npc " + entity.Items.Count);

                //So makes sense to npc, not civilian having the best treasure in the fucking worldu
                //Would make more sense, but they have no items.
                //I think I just found the key to doing thieving.
                loot.Items.TransferAll(entity.Items);

                if (loot.Items.Count == 0)
                {
                    //Getting loot table to populate container
                    LootTables.GenerateLoot(loot, (int)GameManager.Instance.PlayerGPS.CurrentLocationType);
                }
                end();
            };

            base.begin();
        }

        public override void tick()
        {

            base.tick();



            //Only problem is city guards come, which makes no sense.
            if (npc.GetComponent<MobilePersonNPC>().PickpocketByPlayerAttempted)
            {


                //Need someway to check if pickpocket worked or not.
                //Pickocket guaranteed atleast get 1 gold,
                //So can just check if gold different from before to see if worked.
                //Update: Works!
                if (goldPlayerHeld != GameManager.Instance.PlayerEntity.GoldPieces)
                {
                    //Debugging.AlertPlayer("Successfully stole from person");

                    //Or just let end naturally.

                    closure = "Best not to test your luck and try again.";
                    end();
                }
                else
                {
                    Debugging.AlertPlayer("You try to steal from me? You won't get away with this!");
                    //This will trigger summon of assasin if noble or something.
                }



             

                //So doesn't repeat it.
                npc.GetComponent<MobilePersonNPC>().PickpocketByPlayerAttempted = false;
                //NPC will start running away, need check if pickpocket successful.
            }


        }




        public override void end()
        {
            base.end();
            
        }
    }
}