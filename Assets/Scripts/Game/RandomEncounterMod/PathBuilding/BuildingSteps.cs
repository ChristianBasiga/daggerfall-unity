using DaggerfallConnect.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingSteps {

    //Could just be delegates in builder but for sake of matching pattern exactly.
    //For these actions we'd like to know slope and current position, and travel options
	public interface TravelAlongSlopeAction
    {
        //There's state of each step.
        //Stuff liek travel options each respective implementation
        //For example calculator will get these values, then it has the travel options to act accordingly.
        //for generalization prob should give travel options but for our cases not needed. Needed for interrupt
        void Execute(DFPosition mapPixel, bool travelShip);
       
    }


    //So actions to do during reroute process, ocean check loop, do something with slope.
    //For calculator can use to get prev, and calculate time at each step, so basically storing in two places.
    public interface PathRerouteAction
    {
        void Execute(int run, double rise);
    }


    //In the while !isAtDestination loop.
    public interface NewLegAction
    {
        //Not sure what other params this needs, for calculation really just adds taken taken this leg to full. If keep the taken this leg variable.
        void Execute(DFPosition newStartingPoint, int legSize);

    }
}
