using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;

namespace DaggerfallRandomEncounterEvents.Utils
{
    public class Debugging : MonoBehaviour
    {

        static DaggerfallMessageBox debugMessage;


        public static void DebugLog(string message)
        {
            if (debugMessage == null)
            {
                debugMessage = new DaggerfallMessageBox(DaggerfallWorkshop.Game.DaggerfallUI.UIManager);
                debugMessage.AllowCancel = true;
                debugMessage.ClickAnywhereToClose = true;
                debugMessage.ParentPanel.BackgroundColor = Color.clear;
            }

            debugMessage.SetText(message);
            DaggerfallUI.UIManager.PushWindow(debugMessage);
        }
    }
}