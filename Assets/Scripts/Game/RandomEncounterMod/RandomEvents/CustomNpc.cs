using UnityEngine;

using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Utility;

using DaggerfallWorkshop.Game.Entity;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Game.Questing;

namespace DaggerfallRandomEncountersMod.RandomEncounters
{
    [RandomEncounterIdentifier(EncounterId = "NPCEncounter")]
    public class CustomNpc : RandomEncounter
    {
        GameObject person;
        GameObject testNPC;
        void createQuestNPC(SiteTypes siteType, Quest quest, QuestMarker marker, Person person, Transform parent)
        {
            // Get billboard texture data
            FactionFile.FlatData flatData;
            if (person.IsIndividualNPC)
            {
                // Individuals are always flat1 no matter gender
                flatData = FactionFile.GetFlatData(person.FactionData.flat1);
            }
            if (person.Gender == Genders.Male)
            {
                // Male has flat1
                flatData = FactionFile.GetFlatData(person.FactionData.flat1);
            }
            else
            {
                // Female has flat2
                flatData = FactionFile.GetFlatData(person.FactionData.flat2);
            }

            // Create target GameObject
            GameObject go;
            go = GameObjectHelper.CreateDaggerfallBillboardGameObject(flatData.archive, flatData.record, parent);
            go.name = string.Format("Quest NPC [{0}]", person.DisplayName);

            // Set position and adjust up by half height if not inside a dungeon
            Vector3 dungeonBlockPosition = new Vector3(marker.dungeonX * RDBLayout.RDBSide, 0, marker.dungeonZ * RDBLayout.RDBSide);
            go.transform.localPosition = dungeonBlockPosition + marker.flatPosition;
            DaggerfallBillboard dfBillboard = go.GetComponent<DaggerfallBillboard>();
            if (siteType != SiteTypes.Dungeon)
                go.transform.localPosition += new Vector3(0, dfBillboard.Summary.Size.y / 2, 0);

            // Add people data to billboard
            dfBillboard.SetRMBPeopleData(person.FactionIndex, person.FactionData.flags);

            // Add QuestResourceBehaviour to GameObject
            QuestResourceBehaviour questResourceBehaviour = go.AddComponent<QuestResourceBehaviour>();
            questResourceBehaviour.AssignResource(person);

            // Set QuestResourceBehaviour in Person object
            person.QuestResourceBehaviour = questResourceBehaviour;

            // Add StaticNPC behaviour
            StaticNPC npc = go.AddComponent<StaticNPC>();
            npc.SetLayoutData((int)marker.flatPosition.x, (int)marker.flatPosition.y, (int)marker.flatPosition.z, person);

            // Set tag
            go.tag = QuestMachine.questPersonTag;
        }
        public override void begin()
        {
           
            warning = "You hear the displeased voices of the masses!?";
            createQuestNPC(SiteTypes.None,);
            //GameObjectHelper.AddQuestNPC();

            
            
           
            base.begin();
        }


        public override void end()
        {
            base.end();



        }
    }
}

