﻿using System;
using System.Collections.Generic;
using System.Threading;
using TeslaLib;

namespace TeslaConsole
{
    public class Program
    {
        private const String TESLA_CLIENT_ID = "e4a9949fcfa04068f59abb5a658f2bac0a3428e4652315490b659d5ab3f35a9e";
        private const String TESLA_CLIENT_SECRET = "c75f14bbadc8bee3a7594412c31416f8300256d7668ea7e6e7f06727bfb9d220";

        public static void Main(string[] args)
        {
            string clientId = TESLA_CLIENT_ID;
            string clientSecret = TESLA_CLIENT_SECRET;

            string email = "";
            string password = "";

            TeslaClient.OAuthTokenStore = new FileBasedOAuthTokenStore();

            TeslaClient client = new TeslaClient(email, clientId, clientSecret);

            // If we have logged in previously with the same email address, then we can use this method and refresh tokens,
            // assuming the refresh token hasn't expired.
            //client.LoginUsingTokenStoreWithoutPasswordAsync().Wait();
            //client.LoginUsingTokenStoreAsync(password).Wait();
            //client.LoginAsync(password).Wait();
            
            
            Console.Write("Enter Tesla multi-factor authentication code --> ");
            String mfaCode = Console.ReadLine().Trim();
            client.LoginWithMultiFactorAuthenticationCodeAsync(password, mfaCode).Wait();
            

            //client.GetAllProductsAsync(CancellationToken.None).Wait();
            List<EnergySite> energySites = client.GetEnergySitesAsync(CancellationToken.None).Result;
            if (energySites != null && energySites.Count != 0)
            {
                Console.WriteLine("Found {0} energy sites", energySites.Count);
                foreach(EnergySite energySite in energySites)
                {
                    Console.WriteLine($"Energy site name: {energySite.SiteName}  SoC: {energySite.StateOfCharge.ToString("0.0")}%  Power: {energySite.BatteryPower}  Energy left: {energySite.EnergyLeft.ToString("0")} / {energySite.TotalPackEnergy}");
                }
            }

            var vehicles = client.LoadVehicles();

            foreach (TeslaVehicle car in vehicles)
            {
                Console.WriteLine(car.DisplayName + "   VIN: " + car.Vin + "  Model refresh number: "+car.Options.ModelRefreshNumber);
                Console.WriteLine("Car state: {0}", car.State);

                TimeSpan maxWakeupTime = TimeSpan.FromSeconds(30);
                DateTimeOffset startWaking = DateTimeOffset.Now;
                var newState = car.State;
                while (newState == TeslaLib.Models.VehicleState.Asleep || newState == TeslaLib.Models.VehicleState.Offline)
                {
                    Console.Write("Waking up...  ");
                    newState = car.WakeUp();
                    Console.WriteLine("WakeUp returned.  New vehicle state: {0}", newState);
                    Thread.Sleep(2000);
                    if (DateTimeOffset.Now - startWaking > maxWakeupTime)
                    {
                        Console.WriteLine("Giving up on waking up.");
                        break;
                    }
                }

                Console.WriteLine("Is mobile access enabled?  {0}", car.LoadMobileEnabledStatus());

                var vehicleState = car.LoadVehicleStateStatus();
                if (vehicleState == null)
                    Console.WriteLine("Vehicle state was null!  Is the car not awake?");
                else
                {
                    Console.WriteLine(" Roof state: {0}", vehicleState.PanoramicRoofState);
                    Console.WriteLine(" Odometer: {0}", vehicleState.Odometer);
                    Console.WriteLine(" Sentry Mode available: {0}  Sentry mode on: {1}", 
                        vehicleState.SentryModeAvailable, vehicleState.SentryMode);
                    Console.WriteLine("API version: {0}  Car version: {1}", vehicleState.ApiVersion.GetValueOrDefault(), vehicleState.CarVersion);
                }

                var vehicleConfig = car.LoadVehicleConfig();
                Console.WriteLine("From VehicleConfig, Car type: {0}  special type: {1}  trim badging: {2}", vehicleConfig.CarType, 
                    vehicleConfig.CarSpecialType, vehicleConfig.TrimBadging);
                Console.WriteLine("Use range badging? {0}  Spoiler type: {1}", vehicleConfig.UseRangeBadging, vehicleConfig.SpoilerType);
                Console.WriteLine("Color: {0}  Roof color: {1}  Has sunroof? {2}", vehicleConfig.ExteriorColor, vehicleConfig.RoofColor, 
                    vehicleConfig.SunRoofInstalled.HasValue ? vehicleConfig.SunRoofInstalled.Value.ToString() : "false");
                Console.WriteLine("Wheels: {0}", vehicleConfig.WheelType);

                var chargeState = car.LoadChargeStateStatus();
                Console.WriteLine($" State of charge: {chargeState.BatteryLevel}%  Desired State of charge: {chargeState.ChargeLimitSoc}%");
                Console.WriteLine($" Charging state: {(chargeState.ChargingState.HasValue ? chargeState.ChargingState.Value.ToString() : "unknown")}");
                Console.WriteLine($"  Time until full charge: {chargeState.TimeUntilFullCharge} hours ({60*chargeState.TimeUntilFullCharge} minutes)  Usable battery level: {chargeState.UsableBatteryLevel}%");
                Console.WriteLine($" Scheduled charging time: {chargeState.ScheduledChargingStartTime}");
                Console.WriteLine($" Scheduled departure time: {chargeState.ScheduledDepartureTime}");
                Console.WriteLine($" Scheduled charging pending? {chargeState.ScheduledChargingPending}");
                Console.WriteLine($" Managed charging active? {chargeState.ManagedChargingActive}  Managed charging start time? {chargeState.ManagedChargingStartTime}");
                Console.WriteLine($" Managed charging user canceled? {chargeState.ManagedChargingUserCanceled}");

                var driveState = car.LoadDriveStateStatus();
                Console.WriteLine("  Shift state: {0}", driveState.ShiftState);
                var guiSettings = car.LoadGuiStateStatus();
                Console.WriteLine("  Units for distance: {0}   For temperature: {1}", guiSettings.DistanceUnits, guiSettings.TemperatureUnits);

                var options = car.Options;
                Console.WriteLine($"  Battery size: {options.BatterySize}  Has firmware limit? {(options.BatteryFirmwareLimit.HasValue ? options.BatteryFirmwareLimit.ToString() : false.ToString())}");
                // Note there is a BatteryRange and an EstimatedBatteryRange.  The BatteryRange seems to be about 4% higher on Brian's Model 3.  The Tesla app prints out BatteryRange.
                Console.WriteLine($"  Battery range: {chargeState.BatteryRange}  Estimated battery range: {chargeState.EstimatedBatteryRange}  Usable battery level: {chargeState.UsableBatteryLevel}");
                Console.WriteLine($"  Charger limit: {options.ChargerLimit}");
                Console.WriteLine($"Option Codes: {car.Options.RawOptionCodes}");

                var climate = car.LoadClimateStateStatus();
                Console.WriteLine("Climate:");
                Console.WriteLine($"  Driver temperature: {climate.DriverTemperatureSetting}  Passenger: {climate.PassengerTemperatureSetting}");
                Console.WriteLine($"  ClimateKeeperMode: {climate.ClimateKeeperMode}");
            }

            client.RefreshLoginTokenAndUpdateTokenStoreAsync().Wait();

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}
