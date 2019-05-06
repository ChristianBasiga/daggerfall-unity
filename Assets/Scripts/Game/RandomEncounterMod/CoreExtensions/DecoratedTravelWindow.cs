using System.Collections.Generic;
using UnityEngine;
using DaggerfallConnect.Utility;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using System;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallConnect.Arena2;

//Extending base to overwrite the one set in singleton of UI instance
//to instantiate appropriate pop up that uses the calculator.

public class DecoratedTravelWindow : DaggerfallTravelMapWindow, PathBuilder.PathBuiltAction
{

    LinkedList<DFPosition> pathToDraw;
    bool drawingPath = false;
    Color32 pathColor;

    bool prevSelected = true;

    public DecoratedTravelWindow(IUserInterfaceManager uiManager)
          : base(uiManager)
    {
        
    }


   

    protected override void Setup()
    {

        base.Setup();
        exitButton.OnMouseClick += ExtraExitHandler;
        pathColor = Color.red;
        popUp = new DaggerfallTravelPopUp(this.uiManager, DaggerfallUI.UIManager.TopWindow, DaggerfallRandomEncountersMod.RandomEncounterManager.Instance.DecoratedTravelWindow);

        popUp.TravelTimeCalculator = DaggerfallRandomEncountersMod.RandomEncounterManager.Instance.PathTimeCalculator;
    }

    public void setCalculator(PathTimeCalculator calc)
    {
       // popUp.TravelTimeCalculator = calc;
    }

    protected override void CreatePopUpWindow()
    {
        
        base.CreatePopUpWindow();

        if (popUp.TravelTimeCalculator == null)
        {
            popUp.TravelTimeCalculator = DaggerfallRandomEncountersMod.RandomEncounterManager.Instance.PathTimeCalculator;

            //So make sure these match.
            DaggerfallRandomEncountersMod.RandomEncounterManager.Instance.PathBuilder.addPathBuiltAction(this);

            
        }
    }

    // Update is called once per frame
    public override void Update () {

        base.Update();

       
        //Half hour barking up wrong tree, this wasn't problem, idk what the fuck is problem.
        if (drawingPath && SelectedRegion != -1)
        {
            prevSelected = true;
            SetLocationPixels();
            
            DrawPathOfTravel();
            

            Draw(regionTextureOverlayPanel, regionMapOverlayPanel);
        }
        else
        {
            prevSelected = false;
        }
        

    }

    //So to avoid creating custom pop up itself,
    //we just pop it off the stack, when called by the encounter manager accordingly, infact manager itself will handle that.






    #region Drawing Path of Travel

    public void Execute(LinkedList<DFPosition> path, bool travelShip)
    {   
        drawingPath = true;
        draw = true;
        pathToDraw = path;
    }

    //Ideally I make drawing own class but it's fine.
    //And it shouldn't redraw
    void DrawPathOfTravel()
    {


        if (!drawingPath) return;

        drawingPath = true;

        Vector2 origin = OffsetLookUp[SelectedRegionMapNames[MapIndex]];


        foreach (DFPosition pos in pathToDraw)
        {

            //   Debug.LogError(string.Format("Position: {0}, {1}", pos.X, pos.Y));
            int region = DaggerfallUnity.Instance.ContentReader.MapFileReader.GetPoliticIndex(pos.X, pos.Y) - 128;
            if (region != SelectedRegion) continue;
          
            int offSetX = (int)(pos.X - origin.x);
            int offSetY = (int)(pos.Y - origin.y);

            //Width and height would be different as well, so make sure accurate.
            //Actually since width and height difference per zoomed in. Hmm maybe only draw stuff that are in that region? That
            //makes more sense.


            int pixelIndex = (Height - offSetY - 1) * Width + offSetX;


            //Hmm I actually don't have that ocean check anymore.
            //Prob not inside pixel buffer?
            if (pixelIndex < Height * Width)
                PixelBuffer[pixelIndex] = identifyFlashColor;
        }
    }




    #endregion

    #region Callbacks
    //Ayyo why is it not popping off, I CHANGED LITERALLY NOTHING.
    public override void OnPop()
    {
       base.OnPop();


        //This will be overriden to include the drawing stuff for our purposes.
       drawingPath = false;
       pathToDraw = null;



    }

    void ExtraExitHandler(BaseScreenComponent sender, Vector2 position)
    {


        if (!prevSelected)
        {
            DaggerfallUI.UIManager.PopWindow();
        }
        else
        {
            prevSelected = false;
        }
    }


    #endregion
}
