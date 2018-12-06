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


namespace DaggerfallRandomEncountersMod
{
    
    //Only this mod uses this, others would use factory directly.
    public class RandomEncounterManager : MonoBehaviour
    {
        

        static Dictionary<string, System.Type> concreteRandomEncounters;



        #region Contexts


        const string World = "World";
        const string Resting = "Resting";
        const string FastTravel = "Fast Travel";

        #endregion


        #region Encounter Info

        //Also static because it is activeEncounters in game, and there is only one instance of game..
        static List<RandomEncounters.RandomEncounter> activeEncounters;
        

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
            activeEncounters = new List<RandomEncounters.RandomEncounter>();

         //   RandomEncounter encounter = new PassiveEncounter();

            Debug.LogError("encounter type count " + concreteRandomEncounters.Count);
            initStates();
            setUpObservers();

            GameManager.Instance.PlayerEntity.PreventEnemySpawns = true;

            GameManager.Instance.PlayerEntity.GodMode = true;

            //Okay, so it's not issue of doing in console.
           // GameManager.Instance.WeatherManager.SetWeather(WeatherType.Rain_Normal);
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

        }

      
        

        //Basically set up observers to listen to triggers.
        void setUpObservers()
        {

            //Perhaps probability of encounter spawning will be skewed by what called it?
            //If we're putting all into one pool.

            StreamingWorld.OnTeleportToCoordinates += (DaggerfallConnect.Utility.DFPosition pos) =>
            {
                Debug.LogError("teleporting to coordinates " + pos.ToString());

            };


            #region Location Transition Listeners

            //I believe this is any new grid location?
            //It's honestly hard to say.
            PlayerGPS.OnEnterLocationRect += (DFLocation location) =>
            {

                //Maybe near dungeon? Can do alot with location.
                //worldFilter.setFilter("")
            };

            PlayerGPS.OnClimateIndexChanged += (int climateIndex) =>
            {
                MapsFile.Climates currentClimate = (MapsFile.Climates)GameManager.Instance.PlayerGPS.CurrentClimateIndex;

                //G means the word reprsentation.
                worldFilter.setFilter("climate", currentClimate.ToString());

                trySpawningEncounter(World);

            };

            PlayerGPS.OnRegionIndexChanged += (int regionIndex) =>
            {

                string newRegion = DaggerfallUnity.Instance.ContentReader.MapFileReader.GetRegionName(regionIndex);
                Debug.LogError(newRegion);
                worldFilter.setFilter("region", newRegion);
                trySpawningEncounter(World);
            };

            PlayerEnterExit.OnTransitionDungeonExterior += (PlayerEnterExit.TransitionEventArgs args) =>
            {

                worldFilter.setFilter("lastInside", "dungeon");
                trySpawningEncounter(World);
            };

            #endregion


            //Weather listener.
            WeatherManager.OnWeatherChange += (WeatherType newWeather) =>
            {
                worldFilter.setFilter("weather", newWeather.ToString());
                trySpawningEncounter(World);
            };


            #region Time Listenters

            WorldTime.OnNewHour += () =>
            {
                //This maybe good time to check if player committed new crime or something like that.
                //As well as resting.
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
            trySpawningEncounter(World);

        }

        #region Spawning Encounters
        void trySpawningEncounter(string context)
        {



            //Becaues only spawn in world.
            if (GameManager.Instance.PlayerEnterExit.IsPlayerInside || GameManager.Instance.PlayerGPS.IsPlayerInTown(false,true))
            {
                return;
            }
            //Make this so not such high chance
            //the layer of filters also has chance to make it so not possible.

            //If even, then don't spawn, just for quick testing.
            bool dontSpawn = (Random.Range(2, 6) & 1) == 0;

            RandomEncounter encounter = null;
            switch (context)
            {
                case World:

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
                    activeEncounters.Add(evt);
                };

                
                evt.OnEnd += (RandomEncounters.RandomEncounter a) =>
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
                    activeEncounters.Remove(a);
                    //Remove the encounter from the scene.
                    Destroy(a.gameObject);

                };

                //Begin the encounter
                evt.begin();
            }
        }


#endregion
        // Update is called once per frame
        void Update()
        {
            //Before switched to monobehaviours would be calling update on each encounter,
            //there was error with mod compiler for doing that, cause may have been something else though.

        }


        //this method will be called automatically by the modmanager after the main game scene is loaded.
        //The following requirements must be met to be invoked automatically by the ModManager during setup for this to happen:
        //1. Marked with the [Invoke] custom attribute
        //2. Be public & static class method
        //3. Take in an InitParams struct as the only parameter
        [Invoke(StateManager.StateTypes.Game, 0)]
        public static void Init(InitParams initParams)
        {

            //Adds object of manager into scene.
            GameObject randomEncounter = new GameObject("RandomEncounterManager");

            randomEncounter.AddComponent<RandomEncounterManager>();



            ModManager.Instance.GetMod(initParams.ModTitle).IsReady = true;

        }

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void InitConcreteTypes(InitParams initParams)
        {
            concreteRandomEncounters = new Dictionary<string, System.Type>();

            //Okay so all of the classes need to be taken via asset
            initParams.Mod.LoadAllAssetsFromBundle();
            //Initializes cache with all RandomEncounters available in bundle.
            initRandomEncounterCache();

            //Initializes the factories with their prototypes using the json data.
            setUpFactories();


         

          
        }




        private static void initRandomEncounterCache()
        {


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
            List<string> encounterJSONData = EncounterUtils.loadEncounterJson();
            foreach (string jsonFile in encounterJSONData)
            {

                //Loads json into object.
                EncounterData encounterData = JsonConvert.DeserializeObject<EncounterData>(jsonFile);
                //EncounterData encounterData = JsonUtility.FromJson<EncounterData>(jsonFile);

                try
                {
                    if (!EncounterType.defaultTypes.ContainsKey(encounterData.type))
                    {
                        throw new System.Exception("This is not a valid EncounterType: " + encounterData.type);
                    }

                    EncounterType type = EncounterType.defaultTypes[encounterData.type];

                    if (!concreteRandomEncounters.ContainsKey(encounterData.encounterId))
                    {
                        throw new System.Exception("There is no RandomEncounter with the id: " + encounterData.encounterId);
                    }

                    Debug.LogError("Encounter: " + encounterData.encounterId + " type is " + type);

                    var randomEncounterToLoad = concreteRandomEncounters[encounterData.encounterId];
                  //  RandomEncounter randomEncounter = (RandomEncounter)System.Activator.CreateInstance(randomEncounterToLoad);
                    GameObject holder = new GameObject(encounterData.encounterId + "Encounter");
                    RandomEncounter randomEncounter = holder.AddComponent(randomEncounterToLoad) as RandomEncounter;


                    //Instantiates filter using filter data within json object.
                    EncounterFilter filter = new EncounterFilter();
                    foreach (FilterData data in encounterData.filter)
                    {
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
                catch (System.ArgumentException argExcept)
                {

                    //Then will actually log it for ourselves later on, this is all polish.

                    //In this case can only mean that the value they put in json was invalid.
                    Debug.LogError("Failed to load encounter from " + jsonFile);

                }
                //Will make more specific catches later.
                catch (System.Exception exception)
                {
                    Debug.LogError(exception.Message);
                }
            }

        }
        #endregion
    }
}
 