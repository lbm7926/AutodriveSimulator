using Simulator;
using Simulator.Database;
using Simulator.Database.Services;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class BuildNewSimulator : MonoBehaviour
{
    public InputField simulatorName;
    public Dropdown vehicles;
    public InputField ipAddress;
    public Dropdown maps;
    public Dropdown weathers;
    public Slider time;
    public Toggle hasTraffic;
    public Toggle hasPeople;

    List<string> vehicleList;
    List<string> mapList;

    int mapIndex = 1;
    int vehicleIndex = 1;
     
    private void Awake()
    {
        vehicleList = new List<string>();
        mapList = new List<string>();

        vehicles.onValueChanged.AddListener(VehicleValueChanged);
        maps.onValueChanged.AddListener(MapValueChanged);
    }

    public void MapValueChanged(int index)
    {
        mapIndex = index+1;
        Debug.Log("Map"+ mapIndex);
    }

    public void VehicleValueChanged(int index)
    {
        vehicleIndex = index+1;
        Debug.Log("Vehicle" + vehicleIndex);
    }

    public void BuilNew()
    {
        AddData();
        UpdateDropDownItem(vehicles,vehicleList);
        UpdateDropDownItem(maps,mapList);
    }

    private void UpdateDropDownItem(Dropdown dropdown,List<string> showNames)
    {
        dropdown.options.Clear();
        Dropdown.OptionData tempData;
        for (int i = 0; i < showNames.Count; i++)
        {
            tempData = new Dropdown.OptionData();
            tempData.text = showNames[i];
            dropdown.options.Add(tempData);
        }
        dropdown.captionText.text = showNames[0];
    }

    private void AddData()
    {
        VehicleService vehicleService = new VehicleService();
        List<VehicleModel> vehicleModels = vehicleService.List("", 0, 100, "A").ToList();
        for (int i = 0; i < vehicleModels.Count; i++)
        {
            vehicleList.Add(vehicleModels[i].Name);
        }

        MapService mapService = new MapService();
        List<MapModel> mapModels = mapService.List("", 0, 100, "A").ToList();
        for (int i = 0; i < mapModels.Count; i++)
        {
            mapList.Add(mapModels[i].Name);
        }
    }

    public void StartSimulator()
    {
        Loader.StartAsync(new SimulationModel()
        {
            ApiOnly = false,
            Cloudiness = 0,
            Cluster = 0,
            Error = "",
            Fog = 0,
            Headless = false,
            Id = 0,
            Interactive = true,
            Map = mapIndex,
            Name = "1",
            Owner = "",
            Rain = 0,
            Seed = 0,
            Status = "Valid",
            Vehicles = new ConnectionModel[] { new ConnectionModel { Id = 3, Simulation = 0, Vehicle = vehicleIndex, Connection = "localhost:9090" } }
        });
    }
}
