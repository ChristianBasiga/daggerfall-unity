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
    [RandomEncounterIdentifier(EncounterId = "PassiveEncounter")]
    public class PassiveEncounter : RandomEncounter
    {

        bool talkedTo;
        bool running;
        StaticNPC merchant;
        GameObject npc;
        DaggerfallMerchantServicePopupWindow merchantWindow;

        //For before tried pickpocketing, quick way to check.
        //this state would actually be stored in filter for how much gold
        //the player holds, then compare that to up to date version.
        //Problem is observing that may make it up to date before hand.
        int goldPlayerHeld;

        //For my own because may be frame behind before that gets processed.

        public override void begin()
        {

            warning = "You hear the rustling of items";

            Debug.LogError("Passive encounter bgun");

            npc = GameObjectHelper.InstantiatePrefab(DaggerfallUnity.Instance.Option_MobileNPCPrefab.gameObject, "Merchant Encounter", this.transform, GameManager.Instance.PlayerObject.transform.position);
//            merchant = npc.AddComponent<StaticNPC>();
            //Works, but he has no sprite.
            Races npcRace = GameManager.Instance.PlayerGPS.GetRaceOfCurrentRegion();
            npc.GetComponent<MobilePersonNPC>().RandomiseNPC(npcRace);

            //So instance is, but not component, for now I'll set it directly until they fix the prefab themselves.
            npc.GetComponent<MobilePersonNPC>().Motor = npc.GetComponent<MobilePersonMotor>();

            // Destroy(npc.GetComponent<MobilePersonNPC>());
            // npc.RandomiseNPC(Races.Breton);
            //  merchantWindow = new DaggerfallMerchantServicePopupWindow(DaggerfallUI.Instance.UserInterfaceManager, merchant, DaggerfallMerchantServicePopupWindow.Services.Sell);

            //Gold pieces not get current gold, cause that will include credits.
            goldPlayerHeld = GameManager.Instance.PlayerEntity.GoldPieces;
            base.begin();
        }

        public override void tick()
        {

            base.tick();



            //Only problem is city guards come, which makes no sense.
            if (npc.GetComponent<MobilePersonNPC>().PickpocketByPlayerAttempted)
            {

                if (goldPlayerHeld != GameManager.Instance.PlayerEntity.GoldPieces)
                {
                    Debugging.AlertPlayer("Successfully stole from person");
                }
                else
                {

                    Debugging.AlertPlayer("You try to steal from me? You won't get away with this!");
                }
                //Need someway to check if pickpocket worked or not.
                //Pickocket guaranteed atleast get 1 gold,
                //So can just check if gold different from before to see if worked.

                npc.GetComponent<DaggerfallEntityBehaviour>().Entity.OnDeath += (DaggerfallEntity entity) =>
                {
                        //If this merchant dies, then cancel.
                        Debug.LogError("I'm ending it?");
                    closure = "He didn't drop anything";
                    end();
                };

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