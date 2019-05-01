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
        public void placeQNPCNearPlayer(GameObject[] gameObjects, float minDistance = 5f, float maxDistance = 20f)
        {
            const float overlapSphereRadius = 0.65f;
            const float separationDistance = 1.25f;
            const float maxFloorDistance = 4f;

            // Must have received a valid array
            if (gameObjects == null || gameObjects.Length == 0)
                return;
            // Get roation of spawn ray
            Quaternion rotation;
            if (LineOfSightCheck)
            {
                // Try to spawn outside of player's field of view
                float directionAngle = GameManager.Instance.MainCamera.fieldOfView;
                directionAngle += UnityEngine.Random.Range(0f, 4f);
                if (UnityEngine.Random.Range(0f, 1f) > 0.5f)
                    rotation = Quaternion.Euler(0, -directionAngle, 0);
                else
                    rotation = Quaternion.Euler(0, directionAngle, 0);
            else
            {
                // Don't care about player's field of view (e.g. at rest)
                rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 361), 0);
            }

            // Get direction vector and create a new ray
            Vector3 angle = (rotation * Vector3.forward).normalized;
            Vector3 spawnDirection = GameManager.Instance.PlayerObject.transform.TransformDirection(angle).normalized;
            Ray ray = new Ray(GameManager.Instance.PlayerObject.transform.position, spawnDirection);

            // Check for a hit
            Vector3 currentPoint;
            RaycastHit initialHit;
            if (Physics.Raycast(ray, out initialHit, maxDistance))
            {
                // Separate out from hit point
                float extraDistance = UnityEngine.Random.Range(0f, 2f);
                currentPoint = initialHit.point + initialHit.normal.normalized * (separationDistance + extraDistance);

                // Must be greater than minDistance
                if (initialHit.distance < minDistance)
                    return;
            }
            else
            {
                // Player might be in an open area (e.g. outdoors) pick a random point along spawn direction
                currentPoint = GameManager.Instance.PlayerObject.transform.position + spawnDirection * UnityEngine.Random.Range(minDistance, maxDistance);
            }

            // Must be able to find a surface below
            RaycastHit floorHit;
            ray = new Ray(currentPoint, Vector3.down);
            if (!Physics.Raycast(ray, out floorHit, maxFloorDistance))
                return;

            // Ensure this is open space
            Vector3 testPoint = floorHit.point + Vector3.up * separationDistance;
            Collider[] colliders = Physics.OverlapSphere(testPoint, overlapSphereRadius);
            if (colliders.Length > 0)
                return;
        }
        public override void begin()
        {
           
            warning = "You hear the displeased voices of the masses!?";
            //createQuestNPC(SiteTypes.None,);
            //GameObjectHelper.AddQuestNPC();

            
            
           
            base.begin();
        }


        public override void end()
        {
            base.end();



        }
    }
}

