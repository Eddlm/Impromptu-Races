using GTA;
using GTA.Math;
using GTA.Native;
using System;
using System.Collections.Generic;




public class ImpromptuRaces : Script
{
    List<Ped> group_challengers = new List<Ped>();
    List<Vehicle> group_vehicles = new List<Vehicle>();

    private Ped PedChallenged;
    private Vector3 FinishLine;
    private Blip FinishLineBlip;
    private bool OnRace = false;
    private bool show_path_to_finish;
    private bool fast_start;
    private string parking_side;
    private int parking_side_distance;

    Random rnd = new Random();
    protected int GetRandomInt(int min, int max)
    {
        return rnd.Next(min, max);
    }


    public ImpromptuRaces()
    {
        ScriptSettings config = ScriptSettings.Load(@"scripts\impromptu_races.ini");
        show_path_to_finish = config.GetValue<bool>("SETTINGS", "show_path_to_finish", false);
        fast_start = config.GetValue<bool>("SETTINGS", "show_path_to_finish", false);
        parking_side = config.GetValue<string>("SETTINGS", "parking_side", "right");
        parking_side_distance = config.GetValue<int>("SETTINGS", "parking_side_distance", 3);
        Tick += OnTick;
        Interval = 500;
    }

    void OnTick(object sender, EventArgs e)
    {
        if (OnRace == true && Game.Player.Character.IsOnFoot)
        {
            PedChallenged.IsInvincible = false;
            UI.ShowSubtitle("~r~You have canceled the race.");
            for (int vehicle = 0; vehicle < group_vehicles.Count; vehicle++) { group_vehicles[vehicle].IsPersistent = false; }
            for (int ped = 0; ped < group_challengers.Count; ped++) { group_challengers[ped].IsPersistent = false; group_challengers[ped].Task.WanderAround();  group_challengers[ped].MarkAsNoLongerNeeded(); group_challengers.RemoveAt(ped); }
            FinishLineBlip.Remove();
            OnRace = false;

        }

        if (group_challengers.Count > 0)
        {

            for (int i = 0; i < group_challengers.Count; i++)
              {
                if (group_challengers[i].Position.DistanceTo(FinishLine) < 20 || Game.Player.Character.Position.DistanceTo(FinishLine) < 20)
                {                 
                    if (group_challengers[i].Position.DistanceTo(FinishLine) < Game.Player.Character.Position.DistanceTo(FinishLine))
                    {
                        PedChallenged.IsInvincible = false;
                        OnRace = false;
                        UI.Notify("The ~b~" + PedChallenged.CurrentVehicle.FriendlyName + "~w~ won!");
                        group_challengers[i].AlwaysKeepTask = false;
                        group_challengers[i].IsPersistent = false;
                        group_challengers[i].MarkAsNoLongerNeeded();
                        group_challengers[i].CurrentBlip.Remove();
                        group_challengers.RemoveAt(i);
                        FinishLineBlip.Remove();
                        for (int i2 = 0; i2 < group_vehicles.Count; i2++) { group_vehicles[i2].IsPersistent = false; }
                    }
                    else
                    {
                        PedChallenged.IsInvincible = false;
                        OnRace = false;
                        UI.Notify("You won!");
                        group_challengers[i].AlwaysKeepTask = false;
                        group_challengers[i].IsPersistent = false;
                        group_challengers[i].MarkAsNoLongerNeeded();
                        group_challengers[i].CurrentBlip.Remove();
                        group_challengers.RemoveAt(i);
                        FinishLineBlip.Remove();
                        for (int i2 = 0; i2 < group_vehicles.Count; i2++) { group_vehicles[i2].IsPersistent = false; }

                    }
                }
            }
        }

        if (Game.Player.Character.IsInVehicle() && (Game.Player.Character.CurrentVehicle.IsInBurnout() || GTA.Native.Function.Call<bool>(GTA.Native.Hash.IS_PLAYER_PRESSING_HORN, Game.Player)) && OnRace == false)
        {
            if (1 == 1)
            {
                var CarNearby = World.GetNearbyVehicles(Game.Player.Character.Position, 5);
                for (int i = 0; i < CarNearby.Length; i++)
                {
                    if (CarNearby[i].GetPedOnSeat(VehicleSeat.Driver) != Game.Player.Character  && GetRandomInt(1, 3) == 1)
                    {

                        if (CarNearby[i].IsSeatFree(VehicleSeat.Driver))
                        {
                            UI.Notify("The owner heard you.");
                            PedChallenged = GTA.World.CreateRandomPed(World.GetNextPositionOnStreet(Game.Player.Character.Position.Around(50)));
                        }
                        else
                        {
                            UI.Notify("Impromptu race started");
                            PedChallenged = CarNearby[i].GetPedOnSeat(VehicleSeat.Driver);
                            PedChallenged.AlwaysKeepTask = true;
                            GTA.Native.Function.Call(GTA.Native.Hash.SET_VEHICLE_BURNOUT, CarNearby[i], true);

                        }
                        GTA.Native.Function.Call(GTA.Native.Hash.SET_VEHICLE_BURNOUT, CarNearby[i], false);
                        CarNearby[i].RollDownWindow(VehicleWindow.FrontLeftWindow);
                        OnRace = true;
                        CarNearby[i].IsPersistent = true;
                        CarNearby[i].NeedsToBeHotwired = false;

                        DrivingStyle style = DrivingStyle.AvoidTrafficExtremely;
                        UI.Notify(Game.Player.Character.CurrentVehicle.FriendlyName + "~w~ Vs ~b~ " + CarNearby[i].FriendlyName);
                        group_vehicles.Add(CarNearby[i]);
                        group_challengers.Add(PedChallenged);
                        PedChallenged.AddBlip();
                        PedChallenged.CurrentBlip.Color = BlipColor.Blue;
                        PedChallenged.IsInvincible = true;
                        Function.Call(GTA.Native.Hash.SET_PED_STEERS_AROUND_PEDS, PedChallenged, true);
                        Function.Call(GTA.Native.Hash.SET_DRIVER_ABILITY, PedChallenged, 1.0);
                        Function.Call(GTA.Native.Hash.SET_DRIVER_AGGRESSIVENESS, PedChallenged, 0.0f);
                        Script.Wait(2000);
                        UI.ShowSubtitle("Wait for the owner to get in the ~b~" + CarNearby[i].FriendlyName + ".", 5000);
                        var ParkingPos = Game.Player.Character.Position;

                        while (PedChallenged.IsInVehicle() == false)
                        {
                            UI.ShowSubtitle("Wait for the owner to get in the ~b~" + CarNearby[i].FriendlyName + ".", 5000);
                            ParkingPos = Game.Player.Character.Position + ((Game.Player.Character.RightVector * parking_side_distance) + (Game.Player.Character.ForwardVector));
                            if(fast_start == true)
                            {
                                PedChallenged.SetIntoVehicle(CarNearby[i], VehicleSeat.Driver);
                            }
                            else
                            {
                                PedChallenged.Task.DriveTo(CarNearby[i], ParkingPos, 5, 20, Convert.ToInt32(style));
                            }
                            Script.Wait(5000);
                        }

                        while (!PedChallenged.IsInRangeOf(ParkingPos, 1.5f) || Game.Player.Character.CurrentVehicle.IsStopped == false || PedChallenged.CurrentVehicle.IsStopped == false)
                        {
                            if (parking_side == "right")
                            {
                                ParkingPos = Game.Player.Character.Position + ((Game.Player.Character.RightVector * parking_side_distance) + (Game.Player.Character.ForwardVector));
                            }
                            else
                            {
                                ParkingPos = Game.Player.Character.Position + ((Game.Player.Character.RightVector * -parking_side_distance) + (Game.Player.Character.ForwardVector));
                            }
                            if (PedChallenged.Position.DistanceTo(Game.Player.Character.Position) > 20)
                            {
                                if(PedChallenged.Position.DistanceTo(Game.Player.Character.Position) > 70)
                                {
                                    PedChallenged.Task.DriveTo(CarNearby[i], ParkingPos, 5, 40, Convert.ToInt32(style));
                                    if (PedChallenged.CurrentVehicle.Speed < 5)
                                    {
                                        Script.Wait(3000);
                                    }
                                    else
                                    {
                                        Script.Wait(1000);
                                    }
                                }
                                else
                                {
                                    PedChallenged.Task.DriveTo(CarNearby[i], ParkingPos, 5, 10, Convert.ToInt32(style));
                                    if (PedChallenged.CurrentVehicle.Speed < 5)
                                    {
                                        Script.Wait(3000);
                                    }
                                    else
                                    {
                                        Script.Wait(1000);
                                    }
                                }
                                UI.ShowSubtitle("Choose a starting position and wait for the ~b~" + PedChallenged.CurrentVehicle.FriendlyName + ".", 5000);

                            }
                            else
                            {
                                UI.ShowSubtitle("The ~b~" + PedChallenged.CurrentVehicle.FriendlyName + "~w~ is getting to your " + parking_side +  ".", 5000);
                                PedChallenged.Task.ParkVehicle(CarNearby[i], ParkingPos, Game.Player.Character.Heading);
                                Script.Wait(100);
                            }
                        }
                        if (Function.Call<bool>(Hash.IS_WAYPOINT_ACTIVE))
                        {
                            Blip waypoint = new Blip(Function.Call<int>(Hash.GET_FIRST_BLIP_INFO_ID, 8));
                            FinishLine = World.GetNextPositionOnStreet(Function.Call<Vector3>(Hash.GET_BLIP_COORDS, waypoint));
                            FinishLineBlip = World.CreateBlip(FinishLine);
                            FinishLineBlip.Sprite = BlipSprite.Race;
                            FinishLineBlip.IsFlashing = true;
                        }
                        else
                        {
                            FinishLine = World.GetNextPositionOnStreet(Game.Player.Character.Position + Game.Player.Character.ForwardVector * 500);
                            FinishLineBlip = World.CreateBlip(FinishLine);
                            FinishLineBlip.Sprite = BlipSprite.Race;
                            FinishLineBlip.IsFlashing = true;
                        }
                        if (show_path_to_finish == true)
                        {
                            FinishLineBlip.ShowRoute = true;
                        }
                        UI.ShowSubtitle("The ~b~" + PedChallenged.CurrentVehicle.FriendlyName + "~w~ is in position.", 2000);
                        PedChallenged.Task.ClearAll();
                        GTA.Native.Function.Call(GTA.Native.Hash.SET_VEHICLE_BURNOUT, CarNearby[i], true);
                        GTA.Native.Function.Call(GTA.Native.Hash.SET_VEHICLE_BURNOUT, Game.Player.Character.CurrentVehicle, true);
                        Script.Wait(2000);
                        UI.ShowSubtitle("Race  the ~b~" + PedChallenged.CurrentVehicle.FriendlyName + "~w~ to the ~y~finish line.", 5000);
                        PedChallenged.CurrentVehicle.SoundHorn(500);
                        Script.Wait(1000);
                        PedChallenged.CurrentVehicle.SoundHorn(500);
                        Script.Wait(1000);
                        PedChallenged.CurrentVehicle.SoundHorn(500);
                        Script.Wait(500);
                        PedChallenged.Task.DriveTo(CarNearby[i], FinishLine, 5, 100, Convert.ToInt32(style));
                        Script.Wait(500);
                        PedChallenged.CurrentVehicle.SoundHorn(1000);
                        GTA.Native.Function.Call(GTA.Native.Hash.SET_VEHICLE_BURNOUT, CarNearby[i], false);
                        GTA.Native.Function.Call(GTA.Native.Hash.SET_VEHICLE_BURNOUT, Game.Player.Character.CurrentVehicle, false);
                    }
                }
            }
        }
    }
}