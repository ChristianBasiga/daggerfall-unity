using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace DaggerfallRandomEncountersMod.Utils
{

    //A script to add to GameObjects that should be reusable so essentailly be pooled.
    public class Reusable : MonoBehaviour
    {
        public delegate void OnDoneUsingEventHandler(Reusable reusable);

        public event OnDoneUsingEventHandler OnDoneUsing;

        public void OnDone()
        {
            if (OnDoneUsing != null)
            {
                OnDoneUsing(this);
            }
        }

    }
}