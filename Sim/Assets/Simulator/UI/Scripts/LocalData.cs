using Simulator.Database;
using Simulator.Database.Services;
using Simulator.Web.Modules;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class LocalData : MonoBehaviour
{
    private string mapName;
    private string mapModelPath;

    private string vehicleName;
    private string vehicleModelPath;
    private string bridgeType;
    private string sensors;

    public InputField mapNameInput;
    public InputField mapModelInput;

    public InputField vehicleNameInput;
    public InputField vehicleModelPathInput;
    public InputField bridgeTypeInput;
    public InputField sensorsInput;

    public Button loadData;

    public GameObject NewScene;

    public void OpenNewScenePanel()
    {
        NewScene.SetActive(true);
    }

    private void Awake()
    {
        loadData.onClick.AddListener(LoadData);
    }

    public void LoadData()
    {
        vehicleName = "Vehicle";//vehicleNameInput.text;
        vehicleModelPath = @"D:\LG\simulatorOrigin\simulator\AssetBundles\Vehicles\vehicle_Jaguar2015XE";//vehicleModelPathInput.text;
        bridgeType = "";//"No bridge";//bridgeTypeInput.text;
        sensors = sensorsInput.text;

        mapName = "MyScene";//mapNameInput.text;
        mapModelPath = @"D:\LG\simulatorOrigin\simulator\AssetBundles\Environments\environment_MyScene";//mapModelInput.text;

        var vehicle = new VehicleModel()
        {
            Name = vehicleName,
            Status = "Valid",
            Owner = "A",
            Url = vehicleModelPath,
            PreviewUrl = "",
            LocalPath = vehicleModelPath,
            BridgeType = bridgeType,
            Sensors = sensors,
            Error = ""
        };
        VehicleService vehicleService = new VehicleService();
        vehicle.Id = vehicleService.Add(vehicle);

        var map = new MapModel()
        {
            Name = mapName,
            Status = "Valid",
            Owner = "A",
            Url = mapModelPath,
            PreviewUrl = "",
            LocalPath = mapModelPath,
            Error = ""
        };
        MapService service = new MapService();
        map.Id = service.Add(map);

        var simulator = new SimulationModel()
        {
            ApiOnly = false,
            Cloudiness = 0,
            Cluster = 0,
            Error = "",
            Fog = 0,
            Headless = false,
            Interactive = true,
            Map = 2,
            Name = "1",
            Owner = "A",
            Rain = 0,
            Seed = 0,
            Status = "Valid",
            Vehicles = new ConnectionModel[] { new ConnectionModel() { Id = 2, Simulation = 3, Vehicle = 2 } }
        };
        SimulationService simulationService = new SimulationService();
        simulator.Id = simulationService.Add(simulator);

        Debug.Log("写入成功！" + "MapID:" + map.Id + "VehicleID:" + vehicle.Id + "SimulatorID:" + simulator.Id);
    }

    public void LoadDataList()
    {
        VehicleService vehicleService = new VehicleService();
        VehicleModel[] vehiclesModules= vehicleService.List("", 0, 100, "A").ToArray();

        for (int i = 0; i < vehiclesModules.Length; i++)
        {
            Debug.Log(vehiclesModules[i].Name);
        }
    }
}
