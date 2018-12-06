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
        public override void begin()
        {

            warning = "You hear the rustling of items";

            Debug.LogError("Passive encounter bgun");

            npc = GameObjectHelper.InstantiatePrefab(DaggerfallUnity.Instance.Option_MobileNPCPrefab.gameObject, "Merchant Encounter", this.transform, GameManager.Instance.PlayerObject.transform.position);

//            merchant = npc.AddComponent<StaticNPC>();
            
           // npc.RandomiseNPC(Races.Breton);
           //merchantWindow = new DaggerfallMerchantServicePopupWindow(DaggerfallUI.Instance.UserInterfaceManager, merchant, DaggerfallMerchantServicePopupWindow.Services.Sell);
            
            //Works, but he has no sprite.
            Races npcRace = GameManager.Instance.PlayerGPS.GetRaceOfCurrentRegion();
            npc.GetComponent<MobilePersonNPC>().RandomiseNPC(npcRace);

            base.begin();
        }

        public override void Update()
        {

            base.Update();
            if (Began)
            {

                if (running)
                {
                    if (GameManager.Instance.PlayerMotor.DistanceToPlayer(npc.transform.position) > 30.0f)
                    {
                        closure = "He got away. Someone may be coming after you if he reports you.";

                        end();
                    }

                }

                //Only problem is city guards come, which makes no sense.
                else if (npc.GetComponent<MobilePersonNPC>().PickpocketByPlayerAttempted && !running)
                {


                    Debugging.AlertPlayer("You steal from me? You won't get away with this!");

                    //Set timer for asssassin to come and kill you.
                    npc.GetComponent<DaggerfallEntity>().OnDeath += (DaggerfallEntity entity) =>
                    {
                        //If this merchant dies, then cancel.
                        closure = "He didn't drop anything";
                        end();
                    };

                    //So doesn't repeat it.
                    npc.GetComponent<MobilePersonNPC>().PickpocketByPlayerAttempted = false;

                    //NPC will start running away, need check if pickpocket successful.
                    running = true;
                }

            }
        }




        public override void end()
        {
            base.end();
            
        }
    }
}