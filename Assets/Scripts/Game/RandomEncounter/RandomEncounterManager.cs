using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DaggerfallWorkshop;

using DaggerfallWorkshop.Game;

using DaggerfallWorkshop.Game.UserInterfaceWindows; //required for pop-up window
using DaggerfallWorkshop.Game.Utility.ModSupport;   //required for modding features
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game.Weather;
using DaggerfallRandomEncounterEvents.Utils;
using DaggerfallRandomEncounterEvents.RandomEvents;
using DaggerfallRandomEncounterEvents.Enums;
using System;

namespace DaggerfallRandomEncounterEvents
{
    //Only this mod uses this, others would use factory directly.
    public class RandomEncounterManager : MonoBehaviour
    {


        



        List<RandomEvent> activeEncounters;


        //Though honestly I could, also just have anoother layer of keys.
        //but it's a set 3, so fine this way.
        //
        RandomEventFactory worldEventsFactory;
        RandomEventFactory restEventsFactory;
        RandomEventFactory fastTravelEventsFactory;


        //There will be diff filters for each kind of trigger area.
        //Essentiall the observers of state of game.
        EncounterFilter worldFilter;
        EncounterFilter fastTravelFilter;
        EncounterFilter restFilter;

        GameObject eventHolder;


        //Just here for testing.
        bool hitDusk = false;

        private static RandomEncounterManager instance;

        public static RandomEncounterManager Instance
        {

            get
            {
                return instance;
            }
        }



        private void Awake()
        {

            if (!SetUpSingleton())
            {
                //If false then it's new instance, so destroy it.
                Destroy(this);
                return;
            }
        }

        private bool SetUpSingleton()
        {

            if (instance == null)
            {

                instance = this;

                //So when move scenes doesn't destroy this instance.
                DontDestroyOnLoad(instance);
            }
            else if (instance != this)
            {

                DaggerfallUnity.LogMessage("Multiple Encounter Manager instances detected in scene!", true);
            }


            return instance == this;
        }

        // Use this for initialization
        void Start()
        {
            worldEventsFactory = gameObject.AddComponent<RandomEventFactory>();
            activeEncounters = new List<RandomEvent>();
            //For now just adding encounters directly.

            //It could be on manager instead too.
            eventHolder = new GameObject("RandomEncounterHolder");

            setUpFilters();
            setUpTriggers();
            setUpFactories();

            GameManager.Instance.PlayerEntity.GodMode = true;
            
        }

        #region Initializing Factories


        void setUpFactories()
        {
            worldEventsFactory = new RandomEventFactory();
            fastTravelEventsFactory = new RandomEventFactory();
            restEventsFactory = new RandomEventFactory();
            loadEncounterData();
        }

        //Initializing the factories from json files.
        void loadEncounterData()
        {
            //Time to test this.

            //Original way was just creating encounter then adding to factory manually
            //but doing via jsons makes it so source code here doesn't have to change
            Debug.Log("loading encounter data");
            List<string> encounterJSONData = EncounterUtils.loadEncounterData();

            foreach (string json in encounterJSONData)
            {
                //Loads json into object.
                EncounterData encounterData = (EncounterData)JsonUtility.FromJson<EncounterData>(json);

                try
                {
                    #region  Testing for valid input.


                    //Will throw exception if invalid EncounterType
                    EncounterType type = (EncounterType)Enum.Parse(typeof(EncounterType), encounterData.type);

                    #endregion
                    //processes object.
                    RandomEvent randomEvent = null;


                    if (encounterData.eventId == "Summoning")
                    {
                        randomEvent = gameObject.AddComponent<SummoningEvent>();
                    }


                    //Then check for each one.

                    //Instantiates filter using filter data within json object.
                    EncounterFilter filter = new EncounterFilter();

                    foreach (FilterData data in encounterData.filters)
                    {
                        filter.setFilter(data.context, data.value);
                    }




                    //Adds to respective factory.
                    switch (encounterData.context)
                    {
                        case "World":
                            worldEventsFactory.addRandomEvent(type, randomEvent, filter);
                            break;

                        case "Rest":
                            restEventsFactory.addRandomEvent(type, randomEvent, filter);
                            break;

                        case "Fast Travel":
                            fastTravelEventsFactory.addRandomEvent(type, randomEvent, filter);
                            break;

                        default:

                            //Throws exception, maybe in future make even the factories reside in dictionary
                            //to further make it extensible.
                            throw new Exception("Invalid context");
                    }

                }
                //Will make more specific catches later.
                catch (Exception exception)
                {
                    //Will make a toString for it later so this is better, but that's all polish.
                    Debug.LogError(exception.Message);
                }
            }
                

        }


#endregion


        void setUpFilters()
        {

            worldFilter = new EncounterFilter();
            fastTravelFilter = new EncounterFilter();
            restFilter = new EncounterFilter();
         
            //setting world filters.
            //Initial values of each one would be current state of game
            WeatherType currentWeather = GameManager.Instance.WeatherManager.PlayerWeather.WeatherType;
   
            worldFilter.setFilter("weather", currentWeather.ToString());
        


            //lastBuilding should also be from last save, so in my case dungeon, but not sure what would be by default
            //cause not currently serialized I believe? I'll have to do more digging.


            DaggerfallDateTime currentTime = DaggerfallUnity.Instance.WorldTime.Now;
            //Prob better key than time, but this is fine.
            worldFilter.setFilter("time", currentTime.IsDay ? "Day" : "Night");


            //This would depend on the reputation array, whatever the max is.
            
            worldFilter.setFilter("faction", "Merhcants");

            //TODO: set up filter for fast travel and resting.


            //Then filters like player rep will be either at trigger or within each encounter instead.
            //Could see how this would do with considering rep, maybe like faction tight with?
        }

        

        //For adding onto active.
        private void addEncounter(RandomEvent evt)
        {

            //This part will also be repeated.
            if (evt != null)
            {
                evt.OnBegin += (RandomEvent a) =>
                {
                    activeEncounters.Add(evt);
                };


                evt.OnEnd += (RandomEvent a) =>
                {
                    //Once encounter over, remove from active encounters.
                    activeEncounters.Remove(a);
                };

                //Begin the encounter
                evt.begin();
            }
        }

        //Basically set up observers
        void setUpTriggers()
        {

            WorldTime.OnMidnight += () =>
            {

                //This filter is essentially state of game and adds what just happened to it.

                //So keeps what was there before and sets specific spot.


            };

            //When hits dusk, look for where I can force this change.
            //So the these are essentially the observers that updates filters for encounter.



            WorldTime.OnDusk += () =>
            {
                //If more than 2 encounters currently active, don't make more?
                if (activeEncounters.Count > 2)
                {
                    return;
                }

                //So this is null, thank god not factory.
                //Is closure not happening? 
                Debug.Log(worldFilter);
                worldFilter.setFilter("time", "night");


                RandomEvent evt = worldEventsFactory.getRandomEvent(worldFilter);

                addEncounter(evt);
                
            };


        }

        // Update is called once per frame
        void Update()
        {
            if (!hitDusk)
            DaggerfallUnity.Instance.WorldTime.Now.RaiseTime(60 * 60);

        }


        //this method will be called automatically by the modmanager after the main game scene is loaded.
        //The following requirements must be met to be invoked automatically by the ModManager during setup for this to happen:
        //1. Marked with the [Invoke] custom attribute
        //2. Be public & static class method
        //3. Take in an InitParams struct as the only parameter
        [Invoke(StateManager.StateTypes.Game, 0)]
        public static void Init(InitParams initParams)
        {
            GameObject randomEncounter = new GameObject("RandomEncounterManager");
            randomEncounter.AddComponent<RandomEncounterManager>();


            ModManager.Instance.GetMod(initParams.ModTitle).IsReady = true;

        }

    }
}