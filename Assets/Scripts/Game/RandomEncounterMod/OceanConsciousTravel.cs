using DaggerfallConnect.Arena2;
using DaggerfallConnect.Utility;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OceanConsciousTravel : MonoBehaviour {


    //Component strictly for modifying how they calculate fast travel and doing path for that shit.
    PathBuilder pathBuilder;
    PathTimeCalculator pathTimeCalculator;
    DecoratedTravelWindow decoratedTravelWindow;
    FastTravelInterrupt fastTravelInterrupt;
    static OceanConsciousTravel instance;
    Dictionary<int, Dictionary<int, List<DFPosition>>> bTreeOceanPixels;
    List<DFPosition> oceanPixels;

    public class FastTravelInterrupt : PathBuilder.PathBuiltAction
    {

        public int daysTillInterrupt;
        public DFPosition interruptedPosition;


        public void Execute(LinkedList<DFPosition> fullPath, bool travelShip)
        {

            Debug.LogError("I'm totally happening yo");

            if (travelShip) return;


            //Otherwise should random point along full path.

            int randomStop = Random.Range(0, fullPath.Count);


            int i = 0;

            foreach (DFPosition pos in fullPath)
            {
                if (i == randomStop)
                {
                    interruptedPosition = pos;
                    break;
                }

                i += 1;

            }

            int travelTimeMinutes = OceanConsciousTravel.Instance.PathTimeCalculator.CalculateTime(fullPath, interruptedPosition);

            // Players can have fast travel benefit from guild memberships
            travelTimeMinutes = GameManager.Instance.GuildManager.FastTravel(travelTimeMinutes);

            int travelTimeDaysTotal = (travelTimeMinutes / 1440);

            // Classic always adds 1. For DF Unity, only add 1 if there is a remainder to round up.
            if ((travelTimeMinutes % 1440) > 0)
                travelTimeDaysTotal += 1;

            this.daysTillInterrupt = travelTimeDaysTotal;
            //Then get travel time between these points.

        }
    }


    void initBTree()
    {
        bTreeOceanPixels = new Dictionary<int, Dictionary<int, List<DFPosition>>>();

        //Initialize layers.
        //Loop through all pixels to gather ocean pixles, in future just do this once at start.
        oceanPixels = new List<DFPosition>();
        MapsFile mapsFile = DaggerfallUnity.Instance.ContentReader.MapFileReader;


        for (int i = 0; i < MapsFile.MaxMapPixelY; ++i)
        {

            int yKey = getYKey(i);

            for (int j = 0; j < MapsFile.MaxMapPixelX; ++j)
            {


                //Only do this if ocean ideally, but to compute same one across multiple xes

                //If ocean, store in respective list on b tree.
                if (mapsFile.GetClimateIndex(j, i) == (int)MapsFile.Climates.Ocean)
                {
                    // Debug.LogError("here");

                    //Could do this just once, but need to do this only if ocean.
                    //hmm


                    int xKey = getXKey(j);


                    //Lazy load it as amy not need some lists in be tree.

                    if (!bTreeOceanPixels.ContainsKey(yKey))
                    {
                        bTreeOceanPixels[yKey] = new Dictionary<int, List<DFPosition>>();
                    }

                    if (!bTreeOceanPixels[yKey].ContainsKey(xKey))
                    {

                        bTreeOceanPixels[yKey][xKey] = new List<DFPosition>();
                    }

                    oceanPixels.Add(new DFPosition(j, i));
                    bTreeOceanPixels[yKey][xKey].Add(new DFPosition(j, i));

                }
            }
        }

    }

    public int getYKey(int pixel)
    {

        int yKey = 400;
        //Store within ranges.
        if (pixel <= 50)
        {
            yKey = 0;
        }
        else if (pixel <= 100)
        {
            yKey = 50;
        }
        else if (pixel <= 150)
        {
            yKey = 100;
        }
        else if (pixel <= 200)
        {
            yKey = 150;
        }
        else if (pixel <= 250)
        {
            yKey = 200;
        }
        else if (pixel <= 300)
        {
            yKey = 250;
        }
        else if (pixel <= 350)
        {
            yKey = 300;
        }
        else if (pixel <= 400)
        {
            yKey = 350;
        }

        return yKey;
    }

    public int getXKey(int pixel)
    {

        int xKey = 950;
        //Then x in ranges fo 200
        //ranges of 100 instead, there is prob smarter way than else ifs.
        if (pixel <= 50)
        {
            xKey = 0;
        }
        else if (pixel <= 100)
        {
            xKey = 50;
        }
        else if (pixel <= 150)
        {
            xKey = 100;
        }
        else if (pixel <= 200)
        {
            xKey = 150;
        }
        else if (pixel <= 250)
        {
            xKey = 200;
        }
        else if (pixel <= 300)
        {
            xKey = 250;
        }
        else if (pixel <= 350)
        {
            xKey = 300;
        }
        else if (pixel <= 400)
        {
            xKey = 350;
        }
        else if (pixel <= 450)
        {
            xKey = 400;
        }
        else if (pixel <= 500)
        {
            xKey = 450;
        }
        else if (pixel <= 550)
        {
            xKey = 500;
        }
        else if (pixel <= 600)
        {
            xKey = 550;
        }
        else if (pixel <= 650)
        {
            xKey = 600;
        }
        else if (pixel <= 700)
        {
            xKey = 650;
        }
        else if (pixel <= 750)
        {
            xKey = 700;
        }
        else if (pixel <= 800)
        {
            xKey = 750;
        }
        else if (pixel <= 850)
        {
            xKey = 800;
        }
        else if (pixel <= 900)
        {
            xKey = 850;
        }
        else if (pixel <= 950)
        {
            xKey = 900;
        }
        else if (pixel <= 1000)
        {
            xKey = 950;
        }


        return xKey;
    }



    //Then travel interrupt will be something manager still hsa and will add later.


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


    // Use this for initialization
    void Start () {

        pathBuilder = new PathBuilder();
        pathTimeCalculator = new PathTimeCalculator();

        pathBuilder.addPathBuiltAction(pathTimeCalculator);


        decoratedTravelWindow = new DecoratedTravelWindow(DaggerfallUI.UIManager);
        decoratedTravelWindow.setCalculator(pathTimeCalculator);
        DaggerfallUI.Instance.DfTravelMapWindow = decoratedTravelWindow;

        pathBuilder.addPathBuiltAction(decoratedTravelWindow);

        fastTravelInterrupt = new FastTravelInterrupt();

        pathBuilder.addPathBuiltAction(fastTravelInterrupt);


     

    }


    public static OceanConsciousTravel Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject holder = new GameObject("OceanConsciousManager");
                instance = holder.AddComponent<OceanConsciousTravel>();
                DontDestroyOnLoad(instance);

            }

            return instance;
        }
    }




    public PathTimeCalculator PathTimeCalculator
    {
        get { return pathTimeCalculator; }
    }

    public PathBuilder PathBuilder
    {
        get { return pathBuilder; }
    }

    public DecoratedTravelWindow DecoratedTravelWindow
    {
        get
        {
            if (decoratedTravelWindow == null)
            {

                decoratedTravelWindow = new DecoratedTravelWindow(DaggerfallUI.UIManager);
            }

            return decoratedTravelWindow;
        }
    }



    // Update is called once per frame
    void Update () {

      

        if (fastTravelInterrupt.interruptedPosition != null && DaggerfallUI.UIManager.TopWindow is DaggerfallTravelPopUp)
        {
            DaggerfallTravelPopUp popup = DaggerfallUI.UIManager.TopWindow as DaggerfallTravelPopUp;

            //If days it takes to get to interrupt has passed, interrupt it.
            if (popup.CountDownDays <= popup.TotalTravelDays - fastTravelInterrupt.daysTillInterrupt && popup.CountDownDays > 0)
            {
                DaggerfallUI.Instance.UserInterfaceManager.PopWindow();
                decoratedTravelWindow.CloseTravelWindows(true);
                DaggerfallUI.Instance.FadeBehaviour.FadeHUDFromBlack();
                DaggerfallUI.AddHUDText("Travel has been interrupted", 1.5f);
                GameManager.Instance.StreamingWorld.TeleportToCoordinates((int)fastTravelInterrupt.interruptedPosition.X, (int)fastTravelInterrupt.interruptedPosition.Y, StreamingWorld.RepositionMethods.DirectionFromStartMarker);

                fastTravelInterrupt.interruptedPosition = null;
            }

        }

    }
}
