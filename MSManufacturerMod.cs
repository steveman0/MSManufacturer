using System;
using UnityEngine;


public class MSManufacturerMod : FortressCraftMod
{
    public ushort ManufacturerType = ModManager.mModMappings.CubesByKey["steveman0.MSManufacturer"].CubeType;

    public override ModRegistrationData Register()
    {
        ModRegistrationData modRegistrationData = new ModRegistrationData();
        modRegistrationData.RegisterEntityHandler("steveman0.MSManufacturer");
        modRegistrationData.RegisterEntityUI("steveman0.MSManufacturer", new MSManufacturerWindow());
        //modRegistrationData.RegisterEntityHandler("steveman0.MSManufacturerMK1");
        //modRegistrationData.RegisterEntityHandler("steveman0.MSManufacturerMK2");
        //modRegistrationData.RegisterEntityHandler("steveman0.MSManufacturerMK3");

        Debug.Log("Mass Storage Manufacturer Mod V10 registered");

        UIManager.NetworkCommandFunctions.Add("steveman0.MSManufacturerWindow", new UIManager.HandleNetworkCommand(MSManufacturerWindow.HandleNetworkCommand));


        return modRegistrationData;
    }

    public override ModCreateSegmentEntityResults CreateSegmentEntity(ModCreateSegmentEntityParameters parameters)
    {
        ModCreateSegmentEntityResults result = new ModCreateSegmentEntityResults();

        if (parameters.Cube == ManufacturerType)
        {
            parameters.ObjectType = SpawnableObjectEnum.MassStorageOutputPort;
            result.Entity = new MSManufacturer(parameters);
        }

        //foreach (ModCubeMap cubeMap in ModManager.mModMappings.CubeTypes)
        //{
        //    if (cubeMap.CubeType == parameters.Cube)
        //    {
        //        if (cubeMap.Key.Equals("steveman0.MSManufacturer") || cubeMap.Key.Equals("steveman0.MSManufacturerMK1") || cubeMap.Key.Equals("steveman0.MSManufacturerMK2") || cubeMap.Key.Equals("steveman0.MSManufacturerMK3"))
        //            result.Entity = new MSManufacturer(parameters.Segment, parameters.X, parameters.Y, parameters.Z, parameters.Cube, parameters.Flags, parameters.Value, parameters.LoadFromDisk);
        //    }
        //}
        return result;
    }
}
