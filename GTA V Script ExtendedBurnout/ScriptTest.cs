using GTA;
using GTA.Math;
using GTA.Native;
using NativeUI;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;

public class ImpromptuRaces : Script
{
    List<Vehicle> CarsNearby = new List<Vehicle>();
    List<Entity> EntitiesToClear = new List<Entity>();

    private string DriverFollowState;
    private DateTime FollowToStartLineTimeout;
    private DateTime GetIntoVehicleTimeout;
    private Ped PedChallenged;
    private Vehicle CarChallenged;
    private Vector3 FinishLine;
    private Blip FinishLineBlip;
    private bool show_path_to_finish;
    private bool fast_start;
    private string parking_side;
    private int parking_side_distance;
    private Vector3 ParkingPos;
    private string race_phase = "Not Racing";
    private int random_finish_distance;
    private bool reckless_racing;
    private int money_bet;
    private int racetrigger;
    private int CopChase;
    private bool cops_enabled;

    Random rnd = new Random();
    protected int GetRandomInt(int min, int max)
    {
        return rnd.Next(min, max);
    }
    
    bool CanWeUse(Entity entity)
    {
        return entity != null && entity.Exists();
    }




    void ClearCarAndPed()
    {
        race_phase = "Not Racing";
        if (FinishLineBlip != null)
        {
            FinishLineBlip.Remove();
        }
        CopChase = 0;
        PedChallenged.AlwaysKeepTask = false;
        PedChallenged.IsPersistent = false;
        PedChallenged.MarkAsNoLongerNeeded();
        CarChallenged.MarkAsNoLongerNeeded();
        PedChallenged.CurrentBlip.Remove();
            foreach(Entity d in EntitiesToClear)
        {
            d.MarkAsNoLongerNeeded();
        }
    }


    public ImpromptuRaces()
    {
        ScriptSettings config = ScriptSettings.Load(@"scripts\impromptu_races.ini");
        show_path_to_finish = config.GetValue<bool>("SETTINGS", "show_path_to_finish", false);
        fast_start = config.GetValue<bool>("SETTINGS", "fast_start", false);
        parking_side = config.GetValue<string>("SETTINGS", "parking_side", "right");
        parking_side_distance = config.GetValue<int>("SETTINGS", "parking_side_distance", 3);
        random_finish_distance = config.GetValue<int>("SETTINGS", "random_finish_distance", 1000);
        reckless_racing = config.GetValue<bool>("SETTINGS", "reckless_racing", true);
        cops_enabled = config.GetValue<bool>("SETTINGS", "cops_enabled", true);

        Tick += OnTick;
        KeyDown += OnKeyDown;
        KeyUp += OnKeyUp;
    }

    void OnKeyDown(object sender, KeyEventArgs e)
    {

    }

    void OnKeyUp(object sender, KeyEventArgs e)
    {
        if (race_phase == "Race Setup")

        {

            if (Game.Player.Money >= money_bet+50)
            { 
            // UI.Notify("Racer Max Velocity:" + Function.Call<float>(Hash._GET_VEHICLE_MAX_SPEED, CarChallenged.Model));
            //UI.Notify("Your Max Velocity:" + Function.Call<float>(Hash._GET_VEHICLE_MAX_SPEED, Game.Player.Character.CurrentVehicle.Model));

            if (e.KeyCode == Keys.Add)
            {
                if (Function.Call<float>(Hash._GET_VEHICLE_MAX_SPEED, CarChallenged.Model) < 40)
                {
                    UI.Notify("The Challenger doesn't want to bet.");
                }
                else
                if (Function.Call<float>(Hash._GET_VEHICLE_MAX_SPEED, CarChallenged.Model) < 45 && (money_bet + 50 > 50))
                {
                    UI.Notify("The Challenger doesn't want to bet more than " + money_bet + ".");
                }
                else
                 if ((money_bet + 50 > PedChallenged.Money))
                {
                    UI.Notify("The Challenger doesn't want to bet more than " + money_bet + ".");
                }
                else
                {
                    money_bet = money_bet + 50;
                    //UI.Notify("Money bet: " + money_bet);
                }
            }

                if (e.KeyCode == Keys.Subtract)
                {
                    if (money_bet > 0)
                    {
                        money_bet = money_bet - 50;
                    }
                }
            }
            else
            {
                UI.Notify("You don't have " + (money_bet+50) + "$.");
            }
        }
    }


   void CopsMoveIn()
    {
        Vehicle copcar = World.CreateVehicle(VehicleHash.Police, World.GetNextPositionOnSidewalk(Game.Player.Character.Position+Game.Player.Character.ForwardVector*200).Around(30));
        Ped BackUpDriver = Function.Call<Ped>(Hash.CREATE_RANDOM_PED_AS_DRIVER, copcar, true);
        EntitiesToClear.Add(BackUpDriver);
        EntitiesToClear.Add(copcar);
        copcar.SirenActive = true;
        BackUpDriver.AlwaysKeepTask = true;
        BackUpDriver.BlockPermanentEvents = true;
        BackUpDriver.Task.FightAgainst(PedChallenged, -1);
    }


    void OnTick(object sender, EventArgs e)
    {
        var res = UIMenu.GetScreenResolutionMantainRatio();
        var safe = UIMenu.GetSafezoneBounds();
        //const int interval = 45;
        //new UIResText(race_phase.ToString(), new Point((Convert.ToInt32(res.Width) - safe.X)/2, (Convert.ToInt32(res.Height) - safe.Y)/6), 0.5f, Color.White, GTA.Font.ChaletLondon, UIResText.Alignment.Centered).Draw();

        if (race_phase == "Race In Progress" || race_phase == "Race CountDown")
        {
            Function.Call(GTA.Native.Hash.SET_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 0.2f);

            World.DrawMarker(MarkerType.CheckeredFlagRect, FinishLine + new Vector3(0, 0, 15), new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), new Vector3(5f, 5f, 5f), Color.White, true, false, 0, true, "", "", false);
            World.DrawMarker(MarkerType.UpsideDownCone, FinishLine + new Vector3(0, 0, 10), new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), new Vector3(5f, 5f, 5f), Color.Yellow, false, false, 0, false, "", "", false);
        }

        //World.DrawMarker(MarkerType.UpsideDownCone, Game.Player.Character.Position + new Vector3(0, 0, 2), new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), new Vector3(5f, 5f, 5f), Color.Yellow, false, false, 0, false, "", "", false);

        if (race_phase == "Race Setup" || race_phase == "Race In Progress")
        {

            if (money_bet != 0)
            {
               
                //new UIResText("LAP", new Point(Convert.ToInt32(res.Width) - safe.X - 180, Convert.ToInt32(res.Height) - safe.Y - (90 + (3 * interval))), 0.3f, Color.White).Draw();
                new UIResText("Money Bet: ~g~" + money_bet + "$", new Point(Convert.ToInt32(res.Width) - safe.X - 10, Convert.ToInt32(res.Height) - safe.Y - 165), 0.3f, Color.White, GTA.Font.ChaletLondon, UIResText.Alignment.Right).Draw();
                new Sprite("timerbars", "all_black_bg", new Point(Convert.ToInt32(res.Width) - safe.X - 248, Convert.ToInt32(res.Height) - safe.Y - 165), new Size(250, 27), 0f, Color.FromArgb(200, 255, 255, 255)).Draw();
            }

            if (CanWeUse(Game.Player.Character.CurrentVehicle) && CanWeUse(CarChallenged))
            {
                new UIResText(Game.Player.Character.CurrentVehicle.FriendlyName + " Vs ~b~" + CarChallenged.FriendlyName + "", new Point(Convert.ToInt32(res.Width) - safe.X - 10, Convert.ToInt32(res.Height) - safe.Y - 135), 0.4f, Color.White, GTA.Font.ChaletLondon, UIResText.Alignment.Right).Draw();
                new Sprite("timerbars", "all_black_bg", new Point(Convert.ToInt32(res.Width) - safe.X - 248, Convert.ToInt32(res.Height) - safe.Y - 135), new Size(250, 37), 0f, Color.FromArgb(200, 255, 255, 255)).Draw();
            }

        }


        if (race_phase != "Not Racing" && (!Game.Player.Character.IsInVehicle() || PedChallenged.IsDead))
        {
            race_phase = "Not Racing";
            if (PedChallenged.IsDead)
            {
                UI.ShowSubtitle("~r~Race canceled: Challenged driver is dead.");
            }
            if (!Game.Player.Character.IsInVehicle())
            {
                UI.ShowSubtitle("~r~Race canceled.");
            }
            if (Game.Player.Money >= money_bet)
            {
                Game.Player.Money = Game.Player.Money - money_bet;
            }
            else
            {
                Game.Player.Money = 0;
            }
            PedChallenged.Money = PedChallenged.Money + money_bet;
            if (PedChallenged.Money > 500)
            {
                PedChallenged.Task.CruiseWithVehicle(CarChallenged, 40f, (int)DrivingStyle.AvoidTrafficExtremely);
            }
            else
            {
                PedChallenged.Task.CruiseWithVehicle(CarChallenged, 20f, (int)DrivingStyle.Normal);
            }
            ClearCarAndPed();
        }

        if (race_phase == "Not Racing")
        {
            if (Game.Player.Character.IsInVehicle() && (Game.Player.Character.CurrentVehicle.IsInBurnout() || GTA.Native.Function.Call<bool>(GTA.Native.Hash.IS_PLAYER_PRESSING_HORN, Game.Player)) && !Game.Player.Character.CurrentVehicle.SirenActive )
            {
                racetrigger++;
            }
            else
            {
                racetrigger = 0;
            }

            if (racetrigger > 25)
            {
                PedChallenged = null;
                CarChallenged = null;
                var IsAllOk = false;
                var nearbyCars = World.GetNearbyVehicles(Game.Player.Character.Position, 8);
                for (int i = 0; i < nearbyCars.Length; i++)
                {
                    if (nearbyCars[i].GetPedOnSeat(VehicleSeat.Driver) != Game.Player.Character && i < 2)
                    {
                        CarChallenged = nearbyCars[i];
                    }
                }

                if (CanWeUse(CarChallenged))
                {
                    if (!CarChallenged.IsSeatFree(VehicleSeat.Driver))
                    {
                        if (CarChallenged.GetPedOnSeat(VehicleSeat.Driver) != Game.Player.Character)
                        {
                            UI.Notify("Impromptu race started");
                            PedChallenged = CarChallenged.GetPedOnSeat(VehicleSeat.Driver);
                            GTA.Native.Function.Call(GTA.Native.Hash.SET_VEHICLE_BURNOUT, CarChallenged, false);
                            GTA.Native.Function.Call(GTA.Native.Hash.SET_VEHICLE_HANDBRAKE, CarChallenged, false);

                            IsAllOk = true;
                        }
                    }
                    else
                    {
                        UI.Notify("The owner heard you.");
                        PedChallenged = GTA.World.CreatePed("a_m_y_motox_02", World.GetNextPositionOnStreet(Game.Player.Character.Position.Around(50)));

                        IsAllOk = true;
                    }

                    if (IsAllOk == true)
                    {
                        //UI.Notify(PedChallenged.Money.ToString());
                        if (PedChallenged.Money < 50)
                        {
                            PedChallenged.Money = PedChallenged.Money + GetRandomInt(100, 200);
                        }

                        PedChallenged.AlwaysKeepTask = true;
                        money_bet = 0;
                        GTA.Native.Function.Call(GTA.Native.Hash.SET_VEHICLE_HANDBRAKE, CarChallenged, false);

                        CarChallenged.RollDownWindow(VehicleWindow.FrontLeftWindow);
                        CarChallenged.IsPersistent = true;
                        CarChallenged.NeedsToBeHotwired = false;
                        PedChallenged.AddBlip();
                        PedChallenged.CurrentBlip.Color = BlipColor.Blue;
                        PedChallenged.CanBeKnockedOffBike = false;

                        Function.Call(GTA.Native.Hash.SET_PED_STEERS_AROUND_OBJECTS, PedChallenged, true);
                        Function.Call(GTA.Native.Hash.SET_PED_STEERS_AROUND_VEHICLES, PedChallenged, true);
                        Function.Call(GTA.Native.Hash.SET_PED_STEERS_AROUND_PEDS, PedChallenged, true);
                        Function.Call(GTA.Native.Hash.SET_DRIVER_ABILITY, PedChallenged, 1.0);
                        Function.Call(GTA.Native.Hash.SET_DRIVER_AGGRESSIVENESS, PedChallenged, 1.0);
                        Function.Call(GTA.Native.Hash.SET_PED_FLEE_ATTRIBUTES, PedChallenged, 0, 0);
                        Function.Call(GTA.Native.Hash.TASK_SET_BLOCKING_OF_NON_TEMPORARY_EVENTS, PedChallenged, true);
                        var ParkingPos = Game.Player.Character.Position;
                        race_phase = "Race Setup";
                        UI.Notify("Place your bets with + and -.");
                    }
                }
            }
        }

        if (race_phase == "Race Setup")
        {

            //UI.Notify(race_phase);
            if (parking_side == "right")
            {

                ParkingPos = Game.Player.Character.Position + ((Game.Player.Character.RightVector * parking_side_distance) + (Game.Player.Character.ForwardVector));
            }
            else
            {
                ParkingPos = Game.Player.Character.Position + ((Game.Player.Character.RightVector * -parking_side_distance) + (Game.Player.Character.ForwardVector));
            }

            if (!PedChallenged.IsInVehicle())
            {
                UI.ShowSubtitle("Wait for the owner to get in the ~b~" + CarChallenged.FriendlyName + ".", 5000);

                if (fast_start == true)
                {
                    PedChallenged.SetIntoVehicle(CarChallenged, VehicleSeat.Driver);
                }
                else
                if (PedChallenged.IsStopped && DateTime.Now.Subtract(GetIntoVehicleTimeout).TotalSeconds > 1f)
                {
                    PedChallenged.Task.DriveTo(CarChallenged, ParkingPos, 5, 20, (int)DrivingStyle.Normal);
                    GetIntoVehicleTimeout = DateTime.Now;
                }

            }
            if (PedChallenged.IsInVehicle() && DateTime.Now.Subtract(FollowToStartLineTimeout).TotalSeconds > 0.2f)
            {
                if (CarChallenged.IsInRangeOf(ParkingPos, 1.5f))
                {
                    //CarChallenged.FreezePosition = true;
                    CarChallenged.FreezePosition = true;
                    UI.ShowSubtitle("The ~b~" + PedChallenged.CurrentVehicle.FriendlyName + "~w~ is in position. Press your horn when you're ready.", 1000);
                    //PedChallenged.Task.ClearAll();
                    if (Game.Player.Character.CurrentVehicle.IsInBurnout() || GTA.Native.Function.Call<bool>(GTA.Native.Hash.IS_PLAYER_PRESSING_HORN, Game.Player))
                    {
                        race_phase = "Race CountDown";
                    }
                }
                if (!CarChallenged.IsInRangeOf(ParkingPos, 1.5f))
                {
                    UI.ShowSubtitle("Wait for he ~b~" + PedChallenged.CurrentVehicle.FriendlyName + "~w~ in a starting position of your choice", 1000);
                    CarChallenged.FreezePosition = false;

                    if (Game.Player.Character.CurrentVehicle.IsStopped)
                    {
                        if (CarChallenged.IsStopped)
                        {
                            PedChallenged.Task.ParkVehicle(CarChallenged, ParkingPos, Game.Player.Character.Heading);
                        }
                    }
                    else
                    {
                        GTA.Native.Function.Call(GTA.Native.Hash.TASK_VEHICLE_ESCORT, PedChallenged, CarChallenged, Game.Player.Character.CurrentVehicle, -1, 130.0, (int)DrivingStyle.Rushed, 2f, 2f, 10f);
                    }
                }
                    FollowToStartLineTimeout = DateTime.Now;
            }

            if (race_phase == "Race CountDown")
            {
                if (Function.Call<bool>(Hash.IS_WAYPOINT_ACTIVE))
                {
                    FinishLine = World.GetNextPositionOnStreet(Function.Call<Vector3>(Hash.GET_BLIP_COORDS, Function.Call<Blip>(Hash.GET_FIRST_BLIP_INFO_ID, 8)));
                    if (FinishLine == new Vector3(0, 0, 0) || FinishLine.Z == 1)
                    {
                        UI.Notify("~r~ERROR\n~w~Waypoint vector invalid\nRandom finish selected");
                        FinishLine = World.GetNextPositionOnStreet(Game.Player.Character.Position + Game.Player.Character.ForwardVector * random_finish_distance);
                    }
                    FinishLineBlip = World.CreateBlip(FinishLine);
                    FinishLineBlip.Sprite = BlipSprite.Race;
                    FinishLineBlip.IsFlashing = true;
                }
                else
                {
                    FinishLine = World.GetNextPositionOnStreet(Game.Player.Character.Position + Game.Player.Character.ForwardVector * random_finish_distance);
                    FinishLineBlip = World.CreateBlip(FinishLine);
                    FinishLineBlip.Sprite = BlipSprite.Race;
                    FinishLineBlip.IsFlashing = true;
                }
                if (show_path_to_finish == true)
                {
                    FinishLineBlip.ShowRoute = true;
                }
                CarChallenged.FreezePosition = true;
                Game.Player.Character.CurrentVehicle.FreezePosition = true;
                Script.Wait(1000);
                UI.ShowSubtitle("Race the ~b~" + PedChallenged.CurrentVehicle.FriendlyName + "~w~ to the ~y~finish line.", 5000);
                PedChallenged.CurrentVehicle.SoundHorn(500);
                Script.Wait(1000);
                PedChallenged.CurrentVehicle.SoundHorn(500);
                Script.Wait(1000);
                PedChallenged.CurrentVehicle.SoundHorn(500);
                Script.Wait(500);
                //PedChallenged.Task.DriveTo(CarChallenged, FinishLine, 5, 300, 416|32);
                if (reckless_racing == true)
                {
                    // mode 1 drives near coord, 2 no arranca, 3 no existe, 4 no idea,5 no idea,6 no idea,7 no arranca, 8 no arranca, 9 no existe,10 no arranca,11 no arranca, 12 no arranca,13 no arranca,14 se desliza mas?,15 no idea,16 va mas cuidadoso,17 no idea,18 brakes on police?,19 crashes the game,20 weird "freeze" driver thing
                    //Function.Call(Hash.TASK_VEHICLE_MISSION_COORS_TARGET, PedChallenged.Handle, CarChallenged.Handle, FinishLine.X, FinishLine.Y, FinishLine.Z, 20, 200f, (int)DrivingStyle.AvoidTraffic, 1.0,-1.0, 1);
                    //Function.Call(Hash.TASK_VEHICLE_DRIVE_TO_COORD, PedChallenged.Handle, CarChallenged.Handle, FinishLine.X, FinishLine.Y, FinishLine.Z, 500.0, 0, CarChallenged.Model, 6, 0.5, 15.0);
                    Function.Call(Hash.TASK_VEHICLE_MISSION_COORS_TARGET, PedChallenged.Handle, CarChallenged.Handle, FinishLine.X, FinishLine.Y, FinishLine.Z, 4, 120f, 4 | 16 | 32, 5f, 10f, 0);
                    //Function.Call(Hash.TASK_VEHICLE_DRIVE_TO_COORD_LONGRANGE, PedChallenged.Handle, CarChallenged.Handle, FinishLine.X, FinishLine.Y, FinishLine.Z, 70.0f, 786469, 10.0);
                }
                else
                {
                    PedChallenged.Task.DriveTo(CarChallenged, FinishLine, 5, 100f, (int)DrivingStyle.AvoidTrafficExtremely);
                }
                Script.Wait(500);
                CarChallenged.FreezePosition = false;
                Game.Player.Character.CurrentVehicle.FreezePosition = false;
                PedChallenged.CurrentVehicle.SoundHorn(1000);

                race_phase = "Race In Progress";
                if (GetRandomInt(0, 2)==1 && cops_enabled==true)
                {
                    CopChase = 1;
                    CopsMoveIn();
                    CopsMoveIn();
                }

            }
        }

        if (race_phase == "Race In Progress")
        {

            if (Game.Player.Character.Velocity.Y > 40f && Game.Player.WantedLevel<1 && CopChase==1)
            {
                Game.Player.WantedLevel = 1;
            }



            if (PedChallenged.Position.DistanceTo(FinishLine) < 20 || Game.Player.Character.Position.DistanceTo(FinishLine) < 20)
            {
                if (PedChallenged.Position.DistanceTo(FinishLine) < Game.Player.Character.Position.DistanceTo(FinishLine))
                {
                    UI.Notify("The ~b~" + PedChallenged.CurrentVehicle.FriendlyName + "~w~ won!");
                    if (Game.Player.Money >= money_bet)
                    {
                        Game.Player.Money = Game.Player.Money - money_bet;
                    }
                    else
                    {
                        Game.Player.Money = 0;
                    }
                    PedChallenged.Money = PedChallenged.Money + money_bet;

                    if (PedChallenged.Money > 500)
                    {
                        PedChallenged.Task.CruiseWithVehicle(CarChallenged, 40f, (int)DrivingStyle.AvoidTrafficExtremely);
                    }
                    else
                    {
                        PedChallenged.Task.CruiseWithVehicle(CarChallenged, 20f, (int)DrivingStyle.Normal);
                    }

                }
                else
                {
                    if (PedChallenged.Money >= money_bet)
                    {
                        PedChallenged.Money = PedChallenged.Money - money_bet;
                    }
                    else
                    {
                        PedChallenged.Money = 0;
                    }
                    Game.Player.Money = Game.Player.Money + money_bet;
                    UI.Notify("You won!");
                    PedChallenged.Task.CruiseWithVehicle(CarChallenged, 20f, (int)DrivingStyle.Normal);
                }
                ClearCarAndPed();
            }
        }

    }
}