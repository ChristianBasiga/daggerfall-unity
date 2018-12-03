using System.Collections.Generic;
using UnityEngine;
using System.Collections;

using DaggerfallWorkshop;

using DaggerfallWorkshop.Game;

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


        //Also static because it is activeEncounters in game, that doesn't change.
        static List<RandomEncounters.RandomEncounter> activeEncounters;
        

        //Though honestly I could, also just have anoother layer of keys.
        //but it's a set 3, so fine this way.
        //
        private static RandomEncounterFactory worldEventsFactory;
        private static RandomEncounterFactory restEventsFactory;
        private static RandomEncounterFactory fastTravelEventsFactory;


        //There will be diff filters for each kind of trigger area.
        //Essentiall the observers of state of game.
        EncounterFilter worldFilter;
        EncounterFilter fastTravelFilter;
        EncounterFilter restFilter;



        //Just here for testing.
        bool hitDusk = false;


        //Something with instance causing error, not sure what.
        //But the error desn't stop the game, and everything else works fine.
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
            activeEncounters = new List<RandomEncounters.RandomEncounter>();
            //Filters happens at start of game loaded.
            initStates();
            setUpObservers();
            setUpFactories();

            GameManager.Instance.PlayerEntity.GodMode = true;
            
        }





        void initStates()
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


            updateReputationObserver();

        }

        

        void updateReputationObserver()
        {


            short[] reputation = GameManager.Instance.PlayerEntity.SGroupReputations;

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

            string faction = "Commoners";

            switch (maxIndex)
            {

                case 1:
                    faction = "Merchants";
                    break;

                case 2:
                    faction = "Nobility";
                    break;

                case 3:
                    faction = "Scholars";
                    break;
                case 4:
                    faction = "Underworld";
                    break;

            }

            worldFilter.setFilter("faction", faction);
        }

        //Basically set up observers
        void setUpObservers()
        {

            //Anytime reputation changed should also know, this will added to onEnd() of encounter
            //check and update reputation.

            //When leave areas.
            PlayerEnterExit.OnTransitionDungeonExterior += (PlayerEnterExit.TransitionEventArgs args) =>
            {


                worldFilter.setFilter("lastInside", "dungeon");
                trySpawnEncounter(World);
            };


            PlayerEnterExit.OnTransitionDungeonExterior += (PlayerEnterExit.TransitionEventArgs args) =>
            {


                worldFilter.setFilter("lastInside", "building");
                trySpawnEncounter(World);
            };


            WeatherManager.OnWeatherChange += (WeatherType newWeather) =>
            {
                worldFilter.setFilter("weather", newWeather.ToString());
                trySpawnEncounter(World);
            };


            WorldTime.OnNewDay += () =>
            {
                worldFilter.setFilter("time", "day");
                trySpawnEncounter(World);
            };

            WorldTime.OnDawn += () =>
            {
                worldFilter.setFilter("time", "dawn");
                trySpawnEncounter(World);
            };

            
            WorldTime.OnMidnight += () =>
            {

                //This filter is essentially state of game and adds what just happened to it.

                //So keeps what was there before and sets specific spot.
                worldFilter.setFilter("time", "midnight");
                trySpawnEncounter(World);
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

              
                worldFilter.setFilter("time", "night");
                trySpawnEncounter(World);
                
            };

        }

#region Spawning Encounters
        void trySpawnEncounter(string s)
        {

            bool dontSpawn = (Random.Range(1, 100) & 1) == 0;

            if (dontSpawn)
            {
                return;
            }

            RandomEncounter encounter = null;
            switch (s)
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

            //This part will also be repeated.
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

                    //Because if encounter didn't effect reputation don't want to go through time to update it.
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

            //Adds object of manager into scene.
            GameObject randomEncounter = new GameObject("RandomEncounterManager");

            randomEncounter.AddComponent<RandomEncounterManager>();



            ModManager.Instance.GetMod(initParams.ModTitle).IsReady = true;

        }
        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void InitCache(InitParams initParams)
        {
            randomEncounterCache = new Dictionary<string, System.Type>();

            //Initializes cache with all RandomEncounters available.

            Mod mod = initParams.Mod;
            
            initRandomEncounterCache();
            setUpFactories();


            //Okay so all of the classes need to be taken via asset
            ModManager.Instance.GetComponent<MonoBehaviour>().StartCoroutine(mod.LoadAllAssetsFromBundleAsync(true));

            //Will load up factory prefabs and assign accordingly.
         //   mod.GetAsset<RandomEncounterFactory>()
        }




        private static void initRandomEncounterCache()
        {

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


            loadEncounterData();
        }

        //Initializing the factories from json files.
        private static void loadEncounterData()
        {

            List<string> encounterJSONData = EncounterUtils.loadEncounterData();
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



                    //All of the prototypes are components attached to the manager itself.
                    //Wait this is bad, that means will instantiate the GameObject.
                    var randomEncounterToLoad = randomEncounterCache[encounterData.encounterId];
                    GameObject holder = new GameObject("Random Encounter:" + encounterData.encounterId);
                    RandomEncounter randomEvent = holder.AddComponent(randomEncounterToLoad) as RandomEncounter;
                    //  randomEvent = holder.GetComponent<RandomEncounter>();

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
                            Debug.LogError("inserting into world");
                            worldEventsFactory.addRandomEvent(type, randomEvent, filter);
                            break;

                        case Resting:
                            Debug.LogError("inserting into rest");

                            restEventsFactory.addRandomEvent(type, randomEvent, filter);
                            break;

                        case FastTravel:
                            Debug.LogError("inserting into fast travel");

                            fastTravelEventsFactory.addRandomEvent(type, randomEvent, filter);
                            break;

                        default:

                            //Throws exception, maybe in future make even the factories reside in dictionary
                            //to further make it extensible.
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
 