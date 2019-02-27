using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallConnect;

//For mapsfile and climate.
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Game.UserInterfaceWindows; //required for pop-up window
using DaggerfallWorkshop.Game.Utility.ModSupport;   //required for modding features
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game.Weather;
using DaggerfallRandomEncountersMod.Utils;
using DaggerfallRandomEncountersMod.RandomEncounters;
using DaggerfallRandomEncountersMod.Enums;
using DaggerfallRandomEncountersMod.Filter;


using Newtonsoft.Json;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallConnect.Utility;
using System.Collections;

namespace DaggerfallRandomEncountersMod
{
    
    //Only this mod uses this, others would use factory directly.
    public class RandomEncounterManager : MonoBehaviour
    {
        

        static Dictionary<string, System.Type> concreteRandomEncounters;

        public static string[] getConcreteTypes(){
            return concreteRandomEncounters.Keys.ToArray();
        }

        PoolManager objectPool;

        #region Contexts


        const string World = "World";
        const string Resting = "Resting";
        const string FastTravel = "Fast Travel";

        #endregion


        #region Encounter Info

        //Also static because it is activeEncounters in game, and there is only one instance of game..
        static LinkedList<RandomEncounters.RandomEncounter> activeEncounters;
        

        //Same for these
        private static RandomEncounterFactory worldEventsFactory;
        private static RandomEncounterFactory restEventsFactory;
        private static RandomEncounterFactory fastTravelEventsFactory;

        #endregion

        #region Filters are state data for different contexts.

        //There will be diff filters for each kind of trigger area.
        //Essentially the observers of state of game.
        EncounterFilter worldFilter;
        EncounterFilter fastTravelFilter;
        EncounterFilter restFilter;

        #endregion



        private IEnumerator randomWildernessTriggerCoroutine;

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


        //Makes sure it is only instance, if it's not returns false.
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


        void Start()
        {

            GameManager.Instance.PlayerEntity.CrimeCommitted = PlayerEntity.Crimes.Trespassing;
            Debug.LogError("Crime committed "  + GameManager.Instance.PlayerEntity.CrimeCommitted.ToString());

            objectPool = PoolManager.Instance;

            objectPool.PoolCapacity = 20;

            activeEncounters = new LinkedList<RandomEncounters.RandomEncounter>();

            Debug.LogError("encounter type count " + concreteRandomEncounters.Count);

            initStates();
            setUpObservers();


            //randomEncounterWildernessTrigger();

            //If player dies, clears encounters, so then garbage collected.
            GameManager.Instance.PlayerEntity.OnDeath += (DaggerfallEntity entity) =>
            {
                Debug.LogError("I happen");
                killActive();
            };

        }

        //Sets filters / observer states to current state of game on load.
        void initStates()
        {

            worldFilter = new EncounterFilter();
            fastTravelFilter = new EncounterFilter();
            restFilter = new EncounterFilter();

            //Initial values of each one would be current state of game
            WeatherType currentWeather = GameManager.Instance.WeatherManager.PlayerWeather.WeatherType;

            worldFilter.setFilter("weather", currentWeather.ToString());



            //lastBuilding should also be from last save, so in my case dungeon, but not sure what would be by default
            //cause not currently serialized I believe? I'll have to do more digging.


            DaggerfallDateTime currentTime = DaggerfallUnity.Instance.WorldTime.Now;

            //Prob better key than time, but this is fine.
            worldFilter.setFilter("time", currentTime.IsDay ? "day" : "night");
            randomWildernessTriggerCoroutine = randomEncounterWildernessTrigger();

            worldFilter.setFilter("crime", PlayerEntity.Crimes.Trespassing.ToString());


            StateManager.OnStartNewGame += (object sender, System.EventArgs e) =>
            {
                StartCoroutine(randomWildernessTriggerCoroutine);
            };
        }

        //Chance to for random encounter to occur in wilderness
        IEnumerator randomEncounterWildernessTrigger()
        {


            while (GameManager.Instance.StateManager.CurrentState == StateManager.StateTypes.Start || GameManager.Instance.StateManager.CurrentState == StateManager.StateTypes.Game)
            {
                //Random Chance 
                int rand = Random.Range(0, 100);
                if (GameManager.Instance.PlayerEnterExit.IsPlayerInside || GameManager.Instance.PlayerGPS.IsPlayerInTown(false, true))
                {
                    //I believe this is the wilderness? Was looking at pixel first... If not will change later

                    //  Debug.LogError("Random chance to spawn " + rand);

                    //Todo: Add check to make sure player in wilderness, look in Update.
                    // if (rand < 50) //50%
                    //{
                    // Create encounter but still has conditions present I believe
                    Debug.LogError("Trigger happening ");
                    trySpawningEncounter(World);
                    //}
                }


                //Can change to seconds, look up coroutines.
                yield return new WaitForSeconds(5.0f);
            }
        }

        //Basically set up observers to listen to triggers.
        void setUpObservers()
        {

            //Perhaps probability of encounter spawning will be skewed by what called it?
            //If we're putting all into one pool.

            PlayerGPS.OnRegionIndexChanged += (int regionIndex) =>
            {

                string newRegion = DaggerfallUnity.Instance.ContentReader.MapFileReader.GetRegionName(regionIndex);
                Debug.LogError(newRegion);
                worldFilter.setFilter("region", newRegion);

            };

            StreamingWorld.OnTeleportToCoordinates += (DaggerfallConnect.Utility.DFPosition pos) =>
            {
                Debug.LogError("teleporting to coordinates " + pos.ToString());

            };


            #region Location Transition Listeners

            //I believe this is any new grid location?
            //It's honestly hard to say.
            PlayerGPS.OnEnterLocationRect += (DFLocation location) =>
            {
                //Let's see how often this is called... idk
                //happened once on load, then never again as I walked.
                Debug.LogError("New location is " + location.ToString());

                //Maybe near dungeon? Can do alot with location.
                //worldFilter.setFilter("")
            };

            PlayerGPS.OnClimateIndexChanged += (int climateIndex) =>
            {
                MapsFile.Climates currentClimate = (MapsFile.Climates)GameManager.Instance.PlayerGPS.CurrentClimateIndex;

                //G means the word reprsentation.
                worldFilter.setFilter("climate", currentClimate.ToString());

              //  trySpawningEncounter(World);

            };

            PlayerGPS.OnRegionIndexChanged += (int regionIndex) =>
            {

                string newRegion = DaggerfallUnity.Instance.ContentReader.MapFileReader.GetRegionName(regionIndex);
                Debug.LogError(newRegion);
                worldFilter.setFilter("region", newRegion);
                //trySpawningEncounter(World);
            };

            PlayerEnterExit.OnTransitionDungeonExterior += (PlayerEnterExit.TransitionEventArgs args) =>
            {

                worldFilter.setFilter("lastInside", "dungeon");
                //trySpawningEncounter(World);
            };

            //If go inside, prob will make a method for OnLeaveWorld then clear encounters.
            PlayerEnterExit.OnTransitionDungeonInterior += OnLeaveWorld;
            PlayerEnterExit.OnTransitionInterior += OnLeaveWorld;

            #endregion


            //Weather listener.
            WeatherManager.OnWeatherChange += (WeatherType newWeather) =>
            {
                worldFilter.setFilter("weather", newWeather.ToString());
                //trySpawningEncounter(World);
            };


            #region Time Listenters

            WorldTime.OnNewHour += () =>
            {
                if (GameManager.Instance.PlayerEntity.IsResting)
                {
                    Debug.LogError(GameManager.Instance.StateManager.CurrentState.ToString());
                  //  trySpawningEncounter(World);
                }
            };

            WorldTime.OnMidday += () =>
            {

                updateTimeState("day");
            };


            WorldTime.OnNewDay += () =>
            {
                updateTimeState("day");
            };

            WorldTime.OnDawn += () =>
            {
                updateTimeState("dawn");

            };

            
            WorldTime.OnMidnight += () =>
            {

                updateTimeState("midnight");
            };


            WorldTime.OnDusk += () =>
            {



                updateTimeState("night");
                
            };


#endregion

        }

        

        void updateTimeState(string state)
        {
            worldFilter.setFilter("time", state);
            //Try spawning.
         //   trySpawningEncounter(World);

        }

        #region Spawning Encounters
        //Maybe only try spawning encounter for location rects,
        //not neccesarrily the other parts, so only observe those
        //to update filter, but only try when enter new rect.
        void trySpawningEncounter(string context)
        {

            //Okay, cause much of these trigger when load cause technically all that stuff changes.
            //I mean honestly not really an error, adds immersion.
            //The world continues without us so we can load game and run into encounter

            //Debug.LogError("I am called");

            //Becaues only spawn in world.
            if (GameManager.Instance.PlayerEnterExit.IsPlayerInside || GameManager.Instance.PlayerGPS.IsPlayerInTown(false,true) ||

                //Only one encounter active during rest, this was mainly for testing but makes sense to have too.
               (GameManager.Instance.PlayerEntity.IsResting && activeEncounters.Count > 0))

            {
                return;
            }



            PlayerEntity.Crimes crime = GameManager.Instance.PlayerEntity.CrimeCommitted;



            RandomEncounter encounter = null;
            switch (context)
            {
                case World:

                    Debug.LogError("world");
                    encounter = worldEventsFactory.getRandomEvent(worldFilter);
                    break;

                case Resting:
                    encounter = restEventsFactory.getRandomEvent(restFilter);
                    break;

                case FastTravel:
                    encounter = fastTravelEventsFactory.getRandomEvent(fastTravelFilter);
                    break;

            }

            addEncounter(encounter);
        }


        //For adding onto active.
        private void addEncounter(RandomEncounters.RandomEncounter evt)
        {

            //This should never happen, but it is a safeguard.
            if (evt != null)
            {


                evt.OnBegin += (RandomEncounters.RandomEncounter a) =>
                {
                    activeEncounters.AddLast(evt);
                };

                
                evt.OnEnd += (RandomEncounters.RandomEncounter a, bool cancelled) =>
                {

                    //Because if encounter didn't effect reputation don't want to go through time to update it.
                    //Temporary way of doing, prob will make struct called EncounterEffects, or whatever for what
                    //in game / player state is mutated within encounter, then that's what's pased in here
                    if (a.EffectReputation)
                    {
                        //There isn't really any filter right now for it.
                        //Need to think about how use reputation in filter.
                    }

                    //Once encounter over, remove from active encounters.
                    // activeEncounters.Remove(a);
                    //Remove the encounter from the scene.

                    //Not only put back in pool, but remove the encounter script on it.
                    //So still instantiating the scripts, but not game objects.
                    //Could pool the randomencounters themselves somehow too
                    //then I would need to convert it to dictionary.
                    //and add an interface to add another pool into it.
                    //but not really worth because there isn't going to be alot
                    //of same encounter happening, it may still be worth to, then just simple key string or type
                    //whatever.
                    Destroy(a.GetComponent<RandomEncounter>());
                    a.GetComponent<Reusable>().OnDone();

                    //Destroy(a.gameObject);
                };

                //Begin the encounter

                evt.begin();
            }
        }


#endregion
        // Update is called once per frame
        void Update()
        {
            Debug.LogError(DaggerfallUI.Instance.UserInterfaceManager.TopWindow.ToString());
            
            if ((GameManager.Instance.StateManager.GameInProgress && GameManager.Instance.StateManager.CurrentState != StateManager.StateTypes.UI) ||
                DaggerfallUI.Instance.UserInterfaceManager.TopWindow is DaggerfallRestWindow)
            {


                //Problem with this is it may be mutated when I do the tick.
                List<RandomEncounter> toRemove = new List<RandomEncounter>();
                foreach (RandomEncounter encounter in activeEncounters)
                {
                    Debug.LogError("encounter " +  encounter.ToString());

                    if (encounter.Began)
                    {
                        encounter.tick();
                    }

                    else if (!encounter.Began)
                    {
                        toRemove.Add(encounter);
                    }

                }


                foreach( var encounter in toRemove)
                {
                    activeEncounters.Remove(encounter);
                }
            }
        }




        #region Mod Initialization
       
        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void InitEngineData(InitParams initParams)
        {

            //Okay so all of the classes need to be taken via asset
            initParams.Mod.LoadAllAssetsFromBundle();
            //Initializes cache with all RandomEncounters available in bundle.
            //This is happening everytime window to add encounters is open, so only do this if not already set.
            //ie: they didn't open the window.
            if (concreteRandomEncounters == null)
            {
                
                initRandomEncounterCache();
            }

            //Initializes the factories with their prototypes using the json data.
            setUpFactories();


            //Adds object of manager into scene.
            GameObject randomEncounter = new GameObject("RandomEncounterManager");

            randomEncounter.AddComponent<RandomEncounterManager>();


            //Cancel all encounters, also since is static in itself prob move this to invoke method in mod loading.
            StateManager.OnStartNewGame += (object sender, System.EventArgs e) =>
            {
                //So this not hannen
                //OnDestroy should be auto triggered for them when lose reference.
                killActive();
            };

            StateManager.OnStateChange += (StateManager.StateTypes newState) =>
            {
                Debug.LogError("Prev state is " + GameManager.Instance.StateManager.LastState);
                Debug.LogError("new State is " + newState.ToString());
                if (newState == StateManager.StateTypes.Start)
                {
                    killActive();
                }
            };


            ModManager.Instance.GetMod(initParams.ModTitle).IsReady = true;


        }



        //Temporarily public to generate valid list of encounter types.
        //Will reorgnaize everything later.
        public static void initRandomEncounterCache()
        {

            concreteRandomEncounters = new Dictionary<string, System.Type>();

            //Goes through assembly of all files within the mod asset bundle.
            
            foreach (Assembly assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {


                //Gets only the types that inherit from RandomEncounter
                foreach (System.Type currentType in assembly.GetTypes().Where(_ => { return typeof(RandomEncounter).IsAssignableFrom(_); }))
                {


                    //Then only adds into concrete types if they have this attribute set.
                    var attributes = currentType.GetCustomAttributes(typeof(RandomEncounterIdentifierAttribute), false);


                    if (attributes.Length > 0)
                    {
                        var targetAttribute = attributes[0] as RandomEncounterIdentifierAttribute;
                        if (concreteRandomEncounters.ContainsKey(targetAttribute.EncounterId))
                        {
                            string err = ("There is already a RandomEncounter with the id " + targetAttribute.EncounterId + " in the cache\n " +
                                   "Please make sure all of your custom RandomEncounters have unique EncounterIds");

                            Debug.LogError(err);
                        }
                        else
                        {

                            concreteRandomEncounters[targetAttribute.EncounterId] = currentType;
                        }
                    }

                }
                            
            }

        }


        #region Initializing Factories
        private static void setUpFactories()
        {
            worldEventsFactory = new RandomEncounterFactory();
            fastTravelEventsFactory = new RandomEncounterFactory();
            restEventsFactory = new RandomEncounterFactory();

            //Retrieves all json files.
            List<TextAsset> encounterJSONData = EncounterUtils.loadEncounterJson();
            foreach (TextAsset jsonFile in encounterJSONData)
            {

                try
                {
                    Debug.LogError(jsonFile.name);

                    //Loads json into object.
                    EncounterData encounterData = JsonConvert.DeserializeObject<EncounterData>(jsonFile.text);
                    //EncounterData encounterData = JsonUtility.FromJson<EncounterData>(jsonFile);

               
                    if (!EncounterType.defaultTypes.ContainsKey(encounterData.type))
                    {
                        throw new System.Exception("This is not a valid EncounterType: " + encounterData.type);
                    }

                    EncounterType type = EncounterType.defaultTypes[encounterData.type];

                    if (!concreteRandomEncounters.ContainsKey(encounterData.encounterId))
                    {
                        throw new System.Exception("There is no RandomEncounter with the id: " + encounterData.encounterId);
                    }


                    var randomEncounterToLoad = concreteRandomEncounters[encounterData.encounterId];

                    //  RandomEncounter randomEncounter = (RandomEncounter)System.Activator.CreateInstance(randomEncounterToLoad);
                    GameObject holder = new GameObject(encounterData.encounterId + "Encounter");
                    RandomEncounter randomEncounter = holder.AddComponent(randomEncounterToLoad) as RandomEncounter;
                    
                    //Instantiates filter using filter data within json object.
                    EncounterFilter filter = new EncounterFilter();
                    foreach (FilterData data in encounterData.filter)
                    {

                        //So problem for orc is here.
                        filter.setFilter(data);
                    }




                    //Adds to respective factory.
                    switch (encounterData.context)
                    {
                        case World:
                            worldEventsFactory.addRandomEvent(type, randomEncounter, filter);
                            break;

                        case Resting:

                            restEventsFactory.addRandomEvent(type, randomEncounter, filter);
                            break;

                        case FastTravel:

                            fastTravelEventsFactory.addRandomEvent(type, randomEncounter, filter);
                            break;

                        default:

                            throw new System.Exception("Invalid context: " + encounterData.context);
                    }

                }
               
                //Will make more specific catches later.
                catch (System.Exception exception)
                {

                    Debug.LogError("Failed to load encounter data from " + jsonFile.name + ". Error: " + exception.Message);
                    
                }
            }

        }
        #endregion


        //Kills all active encounters and invokes method to place them back into pool
        private static void killActive()
        {
            foreach (RandomEncounter randomEncounter in activeEncounters)
            {
                Debug.LogError("Me too?" + randomEncounter.gameObject.name);
                GameObject holder = randomEncounter.gameObject;
                Destroy(randomEncounter);
                holder.GetComponent<Reusable>().OnDone();
            }

            activeEncounters.Clear();
        }

#endregion

        #region Event Handlers

        //Invoked anytime enter building, town, dungeon, etc.
        private void OnLeaveWorld(PlayerEnterExit.TransitionEventArgs args)
        {

            killActive();
        }

#endregion
    }



    
}
 