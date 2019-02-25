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
    [RandomEncounterIdentifier(EncounterId = "WerewolfEncounter")]
    public class WerewolfEncounter : RandomEncounter
    {

        GameObject[] werebeasts;

        FoeSpawner foeSpawner;

        public override void begin()
        {
            warning = "You hear a howl in the distance and the sound of crashing branches approaching!";

            closure = "You have survived this encounter...for now...";

            MobileTypes wereType = MobileTypes.Werewolf;

            //if player has lycanthropy, don't spawn werebeast and end encounter
            if (GameManager.Instance.PlayerEntity.IsInBeastForm) //temp fix. Ideally we have if player has lycanthropy.
            {
                //end encounter?
            }
            //else soawn werebeast
            else
            {
                werebeasts = GameObjectHelper.CreateFoeGameObjects(GameManager.Instance.PlayerObject.transform.position, wereType, 1, MobileReactions.Hostile);
                foeSpawner = GameObjectHelper.CreateFoeSpawner(false, wereType, 1, 4, 10).GetComponent<FoeSpawner>();

                foeSpawner.SetFoeGameObjects(werebeasts);

                base.begin();
            }

           

        }

        public override void tick()
        {

            base.tick();
            if (foeSpawner == null)
            {
                if (!EncounterUtils.hasActiveSpawn(werebeasts))
                {
                    end();
                    return;
                }

            }

        }
    }
}

