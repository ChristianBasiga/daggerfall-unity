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

    [RandomEncounterIdentifier(EncounterId = "Assassins")]
    public class AssassinEncounter : RandomEncounter
    {

        private GameObject assassin;
        bool assasinationAttempted = false;
        int rollOvers;
        const int maxRollOvers = 2;

        public override void begin()
        {

            assassin = GameObjectHelper.CreateEnemy("Assassin", MobileTypes.Assassin, GameManager.Instance.PlayerObject.transform.position);
            assassin.SetActive(false);

            if (!GameManager.Instance.PlayerEntity.IsResting)
            {
                warning = "You feel bloodlust";
            }
            else
            {
                Debug.LogError("assassin encounter started");
            }

            DaggerfallEntity entity = assassin.GetComponent<DaggerfallEntityBehaviour>().Entity;

            entity.OnDeath += (DaggerfallEntity e) =>
            {

                closure = "You should find a safer place to rest";
                end();

            };



            base.begin();
        }

        public override void tick()
        {

            base.tick();

            if (DaggerfallUI.Instance.UserInterfaceManager.TopWindow is DaggerfallRestWindow)
            {

                DaggerfallEntity assassinEntity = assassin.GetComponent<DaggerfallEntityBehaviour>().Entity;

                int playerLuckLevel = GameManager.Instance.PlayerEntity.Stats.LiveLuck;

                int assassinStealth = assassinEntity.Skills.GetLiveSkillValue(DaggerfallConnect.DFCareer.Skills.Stealth);


                int roll = UnityEngine.Random.Range(-assassinStealth, assassinStealth / 2) + playerLuckLevel;
                assasinationAttempted = true;

                if (roll > playerLuckLevel)
                {

                    GameManager.Instance.PlayerEntity.DecreaseHealth(GameManager.Instance.PlayerEntity.CurrentHealth / 2);

                    Debugging.AlertPlayer("You feel something poking you.");
                    assassin.SetActive(true);

                }
                else
                {
                    rollOvers += 1;

                    if (rollOvers == maxRollOvers)
                    {
                        DaggerfallUI.Instance.UserInterfaceManager.PopWindow();
                        Debugging.AlertPlayer("You see an assassin.");

                    }
                }

            }
        }


     
    }
}