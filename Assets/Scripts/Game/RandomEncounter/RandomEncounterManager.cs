using System.Collections.Generic;
using UnityEngine;
using System.Collections;

using DaggerfallWorkshop;

using DaggerfallWorkshop.Game;

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
using System.Linq;
using Newtonsoft.Json;
using DaggerfallConnect;

namespace DaggerfallRandomEncountersMod
{
    
    //Only this mod uses this, others would use factory directly.
    public class RandomEncounterManager : MonoBehaviour
    {

        static Dictionary<string, System.Type> randomEncounterCache;



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

        //Just here for testing.
     //   bool hitDusk = false;


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

            initStates();
            setUpObservers();
            setUpFactories();

            GameManager.Instance.PlayerEntity.GodMode = true;
            
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
            worldFilter.setFilter("time", currentTime.IsDay ? "Day" : "Night");

            updateReputationObserver();
        }

        
        //ToDo: Remove these.
        void updateReputationObserver()
        {
            string socialGroup = getSocialGroup();

            //For updating the filter for factory
            //May sometimes need to just check faction directly
            worldFilter.setFilter("socialGroup", socialGroup);

            //This will include crime changes, maybe last crime committed could also be something in filter.
        }


        //Gets max reputation in socal group array, then assigns corresponding social group in state filter.
        public string getSocialGroup()
        {
            short[] reputation = GameManager.Instance.PlayerEntity.SGroupReputations;

            //Player can't belong to specific social group, so no reason to find max.

            int maxIndex = 0;
            int max = reputation[0];

            for (int i = 1; i < reputation.Length; ++i)
            {
                if (reputation[i] > max)
                {
                    maxIndex = i;
                    max = reputation[i];
                }
            }

            string socialGroup = "Commoners";

            switch (maxIndex)
            {

                case 1:
                    socialGroup = "Merchants";
                    break;

                case 2:
                    socialGroup = "Nobility";
                    break;

                case 3:
                    socialGroup = "Scholars";
                    break;
                case 4:
                    socialGroup = "Underworld";
                    break;

            }


            //Can also make the player join a guild.
            //So maybe after an encounter, can just force him to join guild
            ///Though that will violate game state.
            //Unless TEMPORARY. Like pretend to be part of guild,
            //then at end of encounter, remove from guild.
            //Guild also has eligibility stuff.
            //So for actually joining can check that bool first, then act accordingly.
            //Can also check crimes committed when leave town as encounter
            return socialGroup;
        }

        //Basically set up observers to listen to triggers.
        void setUpObservers()
        {

            //Perhaps probability of encounter spawning will be skewed by what called it?
            //If we're putting all into one pool.

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

            WorldTime.OnNewDay += () =>
            {
                worldFilter.setFilter("time", "day");
                trySpawningEncounter(World);
            };

            WorldTime.OnDawn += () =>
            {
                worldFilter.setFilter("time", "dawn");
                trySpawningEncounter(World);
            };

            
            WorldTime.OnMidnight += () =>
            {


                worldFilter.setFilter("time", "midnight");
                trySpawningEncounter(World);
            };


            WorldTime.OnDusk += () =>
            {
             

              
                worldFilter.setFilter("time", "night");
                trySpawningEncounter(World);
                
            };


#endregion

        }

#region Spawning Encounters
        void trySpawningEncounter(string context)
        {


            //
            if (GameManager.Instance.PlayerEnterExit.IsPlayerInside)
            {
                return;
            }
            //Make this so not such high chance
            //the layer of filters also has chance to make it so not possible.

            //If even, then don't spawn.
            bool dontSpawn = (Random.Range(2, 6) & 1) == 0;

            if (dontSpawn)
            {
                return;
            }

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

                //Could push and pop queue, but random encounters don't end in same order always.
                //Also right now active encounters isn't used for anything.
                evt.OnEnd += (RandomEncounters.RandomEncounter a) =>
                {
                    //Once encounter over, remove from active encounters.

                    if (a != null)
                    {
                        activeEncounters.Remove(a);
                    }

                    //So encounters themselves can effect game state directly,
                    //so the onEnd will see what was updated, then update the filters accordingly.

                    //Because if encounter didn't effect reputation don't want to go through time to update it.
                    //Temporary way of doing, prob will make struct called EncounterEffects, or whatever for what
                    //in game / player state is mutated within encounter, then that's what's pased in here
                    if (a.EffectReputation)
                    {
                        updateReputationObserver();
                    }
                };

                //Begin the encounter
                evt.begin();
            }
        }


#endregion
        // Update is called once per frame
        void Update()
        {
         //   if (!hitDusk)
           // DaggerfallUnity.Instance.WorldTime.Now.RaiseTime(60 * 60);

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
        public static void InitCache(InitParams initParams)
        {
            randomEncounterCache = new Dictionary<string, System.Type>();
            
            //Initializes cache with all RandomEncounters available in bundle.
            initRandomEncounterCache();

            //Initializes the factories with their prototypes using the json data.
            setUpFactories();


            //Okay so all of the classes need to be taken via asset
            ModManager.Instance.GetComponent<MonoBehaviour>().StartCoroutine(initParams.Mod.LoadAllAssetsFromBundleAsync(true));

          
        }




        private static void initRandomEncounterCache()
        {


            //Goes through assembly of all files within the mod asset bundle.

            foreach (System.Reflection.Assembly assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {

                //Goes through all the types in currenty assembly that inherit from RandomEncounter directly or indirectly.
                foreach (System.Type currentType in assembly.GetTypes().Where(_ => typeof(RandomEncounter).IsAssignableFrom(_)))
                {

                  
                    //Gets all attributes of this type.
                    var attributes = currentType.GetCustomAttributes(typeof(RandomEncounterIdentifierAttribute), true);

                    if (attributes.Length > 0)
                    {

                        //Only first, cause theres should only be one kind of this attribute on the class.
                        var targetAttribute = attributes[0] as RandomEncounterIdentifierAttribute;


                        if (randomEncounterCache.ContainsKey(targetAttribute.EncounterId))
                        {
                            string err = ("There is already a RandomEncounter with the id " + targetAttribute.EncounterId + " in the cache\n " +
                                 "Please make sure all of your custom RandomEncounters have unique EncounterIds");


                            Debug.LogError(err);
                            //Throw exception cause shouldn't continue, or should it? It won't behave like they would expect if don't crash it.
                            // throw new Exception(err);
                        }
                        else
                        {


                            //Adds encounter into cache.
                            randomEncounterCache.Add(targetAttribute.EncounterId, currentType);
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

                    if (!randomEncounterCache.ContainsKey(encounterData.encounterId))
                    {
                        throw new System.Exception("There is no RandomEncounter with the id: " + encounterData.encounterId);
                    }



                    var randomEncounterToLoad = randomEncounterCache[encounterData.encounterId];
                    GameObject holder = new GameObject("Random Encounter:" + encounterData.encounterId);
                    RandomEncounter randomEvent = holder.AddComponent(randomEncounterToLoad) as RandomEncounter;

                    //Instantiates filter using filter data within json object.
                    EncounterFilter filter = new EncounterFilter();
                    foreach (FilterData data in encounterData.filter)
                    {
                        filter.setFilter(data.context, data.value);
                    }




                    //Adds to respective factory.
                    switch (encounterData.context)
                    {
                        case World:
                            worldEventsFactory.addRandomEvent(type, randomEvent, filter);
                            break;

                        case Resting:

                            restEventsFactory.addRandomEvent(type, randomEvent, filter);
                            break;

                        case FastTravel:

                            fastTravelEventsFactory.addRandomEvent(type, randomEvent, filter);
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
                    //Will make a toString for it later so this is better, but that's all polish.
                    Debug.LogError("I happen?" + exception.Message);
                }
            }


        }


        #endregion

    }
}
 