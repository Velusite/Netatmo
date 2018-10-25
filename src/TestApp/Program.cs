﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Util;
using Netatmo;
using Netatmo.Models.Client.Energy;
using Netatmo.Models.Client.Energy.RoomMeasure;
using Newtonsoft.Json;
using NodaTime;
using NodaTime.Extensions;

namespace TestApp
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            JsonConvert.DefaultSettings = Configuration.JsonSerializer;

            IClient client = new Client(
                SystemClock.Instance, " https://api.netatmo.com/",
                Environment.GetEnvironmentVariable("NETATMO_CLIENT_ID"),
                Environment.GetEnvironmentVariable("NETATMO_CLIENT_SECRET"));

            await client.GenerateToken(
                Environment.GetEnvironmentVariable("NETATMO_USERNAME"),
                Environment.GetEnvironmentVariable("NETATMO_PASSWORD"),
                new[]
                {
                    Scope.CameraAccess, Scope.CameraRead, Scope.CameraWrite, Scope.HomecoachRead, Scope.PresenceAccess, Scope.PresenceRead,
                    Scope.StationRead, Scope.StationWrite, Scope.ThermostatRead
                });

            var token = client.CredentialManager.CredentialToken;

            Console.WriteLine($"Token : {token.AccessToken}");

            Console.WriteLine("Stations data :");
            var stationsData = await client.Weather.GetStationsData();
            Console.WriteLine(JsonConvert.SerializeObject(stationsData, Formatting.Indented));

            Console.WriteLine("Energy Homes data :");
            var homesData = await client.Energy.GetHomesData();
            Console.WriteLine(JsonConvert.SerializeObject(homesData, Formatting.Indented));

            Console.WriteLine("Energy Homes data :");
            foreach (var home in homesData.Body.Homes)
            {
                Console.WriteLine(home.Name);
                var homeStatus = await client.Energy.GetHomeStatus(home.Id);
                Console.WriteLine(JsonConvert.SerializeObject(homeStatus, Formatting.Indented));

                Console.WriteLine("Energy room measure :");
                foreach (var room in home.Rooms)
                {
                    Console.WriteLine(room.Name);
                    var parameters = new GetRoomMeasureParameters
                    {
                        HomeId = home.Id,
                        RoomId = room.Id,
                        Scale = Scale.Max,
                        Type = ThermostatMeasurementType.Temperature,
                        BeginAt = SystemClock.Instance.InUtc().GetCurrentLocalDateTime().PlusDays(-1),
                        EndAt = SystemClock.Instance.InUtc().GetCurrentLocalDateTime()
                    };
                    var roomMeasure = await client.Energy.GetRoomMeasure<TemperatureStep>(parameters);
                    Console.WriteLine(JsonConvert.SerializeObject(roomMeasure, Formatting.Indented));
                }
            }

            Console.WriteLine("RefreshToken :");
            Thread.Sleep(9000);
            await client.RefreshToken();
            var newToken = client.CredentialManager.CredentialToken;
            Console.WriteLine($"Old token expires at : {token.ExpiresAt.ToInvariantString()}");
            Console.WriteLine($"New token expires at : {newToken.ExpiresAt.ToInvariantString()}");
        }
    }
}