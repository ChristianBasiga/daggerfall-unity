using DaggerfallWorkshop.Game;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OceanConsciousTravel : MonoBehaviour {


    //Component strictly for modifying how they calculate fast travel and doing path for that shit.
    PathBuilder pathBuilder;
    PathTimeCalculator pathTimeCalculator;
    DecoratedTravelWindow decoratedTravelWindow;
    static OceanConsciousTravel instance;
    //Then travel interrupt will be something manager still hsa and will add later.

    
	// Use this for initialization
	void Start () {

        pathBuilder = new PathBuilder();
        pathTimeCalculator = new PathTimeCalculator();

        pathBuilder.addPathBuiltAction(pathTimeCalculator);

        DaggerfallUI.Instance.DfTravelMapWindow = decoratedTravelWindow;
		
	}


    public static OceanConsciousTravel Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject holder = new GameObject("OceanConsciousManager");
                instance = holder.AddComponent<OceanConsciousTravel>();
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

        //Then here set it, just incase.
        if (!(DaggerfallUI.Instance.DfTravelMapWindow is DecoratedTravelWindow))
        {
            decoratedTravelWindow = new DecoratedTravelWindow(DaggerfallUI.UIManager);
            decoratedTravelWindow.setCalculator(pathTimeCalculator);
            pathBuilder.addPathBuiltAction(decoratedTravelWindow);
            DaggerfallUI.Instance.DfTravelMapWindow = decoratedTravelWindow;

        }

    }
}
