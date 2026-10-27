using System;
using GTA;
using GTA.Native;

namespace RPCore
{
    public static class VehicleSnapshot
    {
        static int[] Colors(Vehicle v, Hash native, int count)
        {
            var a = new OutputArgument(); var b = new OutputArgument(); var c = new OutputArgument();
            if (count == 1) Function.Call(native, v, a);
            else if (count == 2) Function.Call(native, v, a, b);
            else Function.Call(native, v, a, b, c);
            return count == 1 ? new[] { a.GetResult<int>() } : count == 2 ? new[] { a.GetResult<int>(), b.GetResult<int>() } : new[] { a.GetResult<int>(), b.GetResult<int>(), c.GetResult<int>() };
        }
        public static SavedVehicle Capture(Vehicle v)
        {
            var color = Colors(v, Hash.GET_VEHICLE_COLOURS, 2); var extra = Colors(v, Hash.GET_VEHICLE_EXTRA_COLOURS, 2);
            var record = new SavedVehicle {
                Model = v.Model.Hash, Plate = Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT, v).Trim(), Name = v.LocalizedName,
                TiresCanBurst = Function.Call<bool>(Hash.GET_VEHICLE_TYRES_CAN_BURST, v),
                PlateStyle = Function.Call<int>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT_INDEX, v),
                PrimaryColor = color[0], SecondaryColor = color[1], PearlColor = extra[0], WheelColor = extra[1],
                DashboardColor = Colors(v, Hash.GET_VEHICLE_EXTRA_COLOUR_5, 1)[0], InteriorColor = Colors(v, Hash.GET_VEHICLE_EXTRA_COLOUR_6, 1)[0],
                WheelType = Function.Call<int>(Hash.GET_VEHICLE_WHEEL_TYPE, v), WindowTint = Function.Call<int>(Hash.GET_VEHICLE_WINDOW_TINT, v),
                Livery = Function.Call<int>(Hash.GET_VEHICLE_LIVERY, v), EngineHealth = v.EngineHealth, BodyHealth = v.BodyHealth, TankHealth = v.PetrolTankHealth,
                Dirt = v.DirtLevel, Fuel = v.FuelLevel, X = v.Position.X, Y = v.Position.Y, Z = v.Position.Z, Heading = v.Heading,
                NeonColor = Colors(v, Hash.GET_VEHICLE_NEON_COLOUR, 3), SmokeColor = Colors(v, Hash.GET_VEHICLE_TYRE_SMOKE_COLOR, 3)
            };
            if (Function.Call<bool>(Hash.GET_IS_VEHICLE_PRIMARY_COLOUR_CUSTOM, v)) record.CustomPrimary = Colors(v, Hash.GET_VEHICLE_CUSTOM_PRIMARY_COLOUR, 3);
            if (Function.Call<bool>(Hash.GET_IS_VEHICLE_SECONDARY_COLOUR_CUSTOM, v)) record.CustomSecondary = Colors(v, Hash.GET_VEHICLE_CUSTOM_SECONDARY_COLOUR, 3);
            for (int i = 0; i < 50; i++)
            {
                if (Function.Call<int>(Hash.GET_NUM_VEHICLE_MODS, v, i) <= 0) continue;
                record.Mods[i] = Function.Call<int>(Hash.GET_VEHICLE_MOD, v, i); record.Variations[i] = Function.Call<bool>(Hash.GET_VEHICLE_MOD_VARIATION, v, i);
            }
            foreach (int toggle in new[] { 17, 18, 19, 20, 21, 22 }) record.Toggles[toggle] = Function.Call<bool>(Hash.IS_TOGGLE_MOD_ON, v, toggle);
            for (int i = 0; i <= 14; i++) if (Function.Call<bool>(Hash.DOES_EXTRA_EXIST, v, i)) record.Extras[i] = Function.Call<bool>(Hash.IS_VEHICLE_EXTRA_TURNED_ON, v, i);
            for (int i = 0; i < 4; i++) record.Neons[i] = Function.Call<bool>(Hash.GET_VEHICLE_NEON_ENABLED, v, i);
            foreach (int i in new[] { 0, 1, 2, 3, 4, 5, 6, 7, 45, 47 }) if (Function.Call<bool>(Hash.IS_VEHICLE_TYRE_BURST, v, i, false)) record.BurstTires.Add(i);
            for (int i = 0; i < 8; i++) if (!Function.Call<bool>(Hash.IS_VEHICLE_WINDOW_INTACT, v, i)) record.BrokenWindows.Add(i);
            for (int i = 0; i < 6; i++) if (Function.Call<bool>(Hash.IS_VEHICLE_DOOR_DAMAGED, v, i)) record.BrokenDoors.Add(i);
            return record;
        }

        public static void Apply(Vehicle v, SavedVehicle r)
        {
            Function.Call(Hash.SET_VEHICLE_MOD_KIT, v, 0);
            if (r.TiresCanBurst.HasValue) Function.Call(Hash.SET_VEHICLE_TYRES_CAN_BURST, v, r.TiresCanBurst.Value);
            Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT, v, r.Plate);
            Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT_INDEX, v, r.PlateStyle);
            Function.Call(Hash.SET_VEHICLE_WHEEL_TYPE, v, r.WheelType);
            if (r.Mods != null) foreach (var mod in r.Mods)
            {
                if (mod.Key < 0 || mod.Key >= 50 || mod.Value < -1 || mod.Value >= Function.Call<int>(Hash.GET_NUM_VEHICLE_MODS, v, mod.Key)) continue;
                bool variation; bool useVariation = r.Variations != null && r.Variations.TryGetValue(mod.Key, out variation) && variation;
                Function.Call(Hash.SET_VEHICLE_MOD, v, mod.Key, mod.Value, useVariation);
            }
            if (r.Toggles != null) foreach (var mod in r.Toggles) if (mod.Key >= 17 && mod.Key <= 22) Function.Call(Hash.TOGGLE_VEHICLE_MOD, v, mod.Key, mod.Value);
            Function.Call(Hash.SET_VEHICLE_COLOURS, v, r.PrimaryColor, r.SecondaryColor);
            Function.Call(Hash.SET_VEHICLE_EXTRA_COLOURS, v, r.PearlColor, r.WheelColor);
            Function.Call(Hash.SET_VEHICLE_EXTRA_COLOUR_5, v, r.DashboardColor); Function.Call(Hash.SET_VEHICLE_EXTRA_COLOUR_6, v, r.InteriorColor);
            ApplyRgb(v, Hash.SET_VEHICLE_CUSTOM_PRIMARY_COLOUR, r.CustomPrimary); ApplyRgb(v, Hash.SET_VEHICLE_CUSTOM_SECONDARY_COLOUR, r.CustomSecondary);
            ApplyRgb(v, Hash.SET_VEHICLE_NEON_COLOUR, r.NeonColor); ApplyRgb(v, Hash.SET_VEHICLE_TYRE_SMOKE_COLOR, r.SmokeColor);
            Function.Call(Hash.SET_VEHICLE_WINDOW_TINT, v, r.WindowTint);
            if (r.Livery >= 0) Function.Call(Hash.SET_VEHICLE_LIVERY, v, r.Livery);
            if (r.Extras != null) foreach (var extra in r.Extras) if (extra.Key >= 0 && extra.Key <= 14 && Function.Call<bool>(Hash.DOES_EXTRA_EXIST, v, extra.Key)) Function.Call(Hash.SET_VEHICLE_EXTRA, v, extra.Key, !extra.Value);
            if (r.Neons != null) foreach (var neon in r.Neons) if (neon.Key >= 0 && neon.Key < 4) Function.Call(Hash.SET_VEHICLE_NEON_ENABLED, v, neon.Key, neon.Value);
            v.EngineHealth = r.EngineHealth; v.BodyHealth = r.BodyHealth; v.PetrolTankHealth = r.TankHealth; v.DirtLevel = r.Dirt; v.FuelLevel = r.Fuel;
            if (r.BurstTires != null) foreach (var i in r.BurstTires) if ((i >= 0 && i <= 7) || i == 45 || i == 47) Function.Call(Hash.SET_VEHICLE_TYRE_BURST, v, i, true, 1000f);
            if (r.BrokenWindows != null) foreach (var i in r.BrokenWindows) if (i >= 0 && i < 8) Function.Call(Hash.SMASH_VEHICLE_WINDOW, v, i);
            if (r.BrokenDoors != null) foreach (var i in r.BrokenDoors) if (i >= 0 && i < 6) Function.Call(Hash.SET_VEHICLE_DOOR_BROKEN, v, i, true);
            Function.Call(Hash.SET_VEHICLE_ENGINE_ON, v, false, true, false);
        }
        static void ApplyRgb(Vehicle v, Hash native, int[] rgb)
        {
            if (rgb != null && rgb.Length == 3) Function.Call(native, v, Math.Max(0, Math.Min(255, rgb[0])), Math.Max(0, Math.Min(255, rgb[1])), Math.Max(0, Math.Min(255, rgb[2])));
        }
    }
}
