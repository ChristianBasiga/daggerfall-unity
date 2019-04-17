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
    [RandomEncounterIdentifier(EncounterId = "NPCEncounter")]
    public class CustomNpc : RandomEncounter
    {
        GameObject person;
        GameObject testNPC;

        public override void begin()
        {
            warning = "You hear the displeased voices of the masses!?";

            
            //GameObjectHelper.AddQuestNPC();

            //Also uses the above method however it creates the quest npc in a building
            //person = GameObjectHelper.AddQuestResourceObjects()
           
            Races npcRace = GameManager.Instance.PlayerGPS.GetRaceOfCurrentRegion();
            person.GetComponent<MobilePersonNPC>().RandomiseNPC(npcRace);
            person.transform.parent = this.transform;
            
           
            base.begin();
        }


        public override void end()
        {
            base.end();

        }
    }
}

