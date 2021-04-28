using Battlehub.RTCommon;
using Battlehub.RTEditor;
using Simulator.Editor;
using Simulator.Map;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ToggleItem
{
    public Image background;
    public Image checkmark;
    public string lable;
}

public class AnnotationHdMapView : MonoBehaviour
{
    private MapHolder mapHolder;
    private int waypointTotal = 3;
    private int boundryLineType = 3;

    public GameObject togglePrefab;

    public GameObject LeftBoundryParent;
    private ToggleItem[] toggleItems;

    public GameObject buondryLineTypeParent;
    private ToggleItem[] boundryLineTypeItems;


    public void LeftBoundryTypeToggles()
    {
        foreach (var item in toggleItems)
        {
            GameObject go = Instantiate(togglePrefab);
            go.name = item.lable;
            go.GetComponentInChildren<TMP_Text>().text = item.lable;
            go.transform.SetParent(LeftBoundryParent.transform);
            go.transform.localScale = Vector3.one;
            go.transform.localPosition = new Vector3(go.transform.localPosition.x,transform.localPosition.y,0);
            go.GetComponent<Toggle>().group = LeftBoundryParent.GetComponent<ToggleGroup>();            
        }
    }

    public void BoundryLineTypeToggles()
    {
        int row1 = 5;
        int row2 = boundryLineTypeItems.Length - row1;
        Transform row1Trans = buondryLineTypeParent.transform.Find("Row1");
        Transform row2Trans = buondryLineTypeParent.transform.Find("Row2");

        for (int i = 0; i < row1; i++)
        {
            GameObject go = Instantiate(togglePrefab);
            go.name = boundryLineTypeItems[i].lable;
            go.GetComponentInChildren<TMP_Text>().text = boundryLineTypeItems[i].lable;
            go.transform.SetParent(row1Trans.transform);
            go.transform.localScale = Vector3.one;
            go.transform.localPosition = new Vector3(go.transform.localPosition.x, transform.localPosition.y, 0);
            go.GetComponent<Toggle>().group = buondryLineTypeParent.GetComponent<ToggleGroup>();
        }

        for (int i = 0; i < row2; i++)
        {
            GameObject go = Instantiate(togglePrefab);
            go.name = boundryLineTypeItems[i].lable;
            go.GetComponentInChildren<TMP_Text>().text = boundryLineTypeItems[i].lable;
            go.transform.SetParent(row2Trans.transform);
            go.transform.localScale = Vector3.one;
            go.transform.localPosition = new Vector3(go.transform.localPosition.x, transform.localPosition.y, 0);
            go.GetComponent<Toggle>().group = buondryLineTypeParent.GetComponent<ToggleGroup>();
        }
    }


    private void Awake()
    {
        toggleItems = new ToggleItem[]
        {
            new ToggleItem{background=null,checkmark=null,lable="UnknownBoundry" },
            new ToggleItem{background=null,checkmark=null,lable="DYBoundry" },
            new ToggleItem{background=null,checkmark=null,lable="DWBoundry" },
            new ToggleItem{background=null,checkmark=null,lable="SYBoundry" },
            new ToggleItem{background=null,checkmark=null,lable="SWBoundry" },
            new ToggleItem{background=null,checkmark=null,lable="DoubleYellowBoundry" },
            new ToggleItem{background=null,checkmark=null,lable="CurbBoundry" },
        };

        boundryLineTypeItems = new ToggleItem[]
        {
            new ToggleItem{background=null,checkmark=null,lable="UnknownBoundry" },
            new ToggleItem{background=null,checkmark=null,lable="SWBoundry" },
            new ToggleItem{background=null,checkmark=null,lable="SYBoundry" },
            new ToggleItem{background=null,checkmark=null,lable="DWBoundry" },
            new ToggleItem{background=null,checkmark=null,lable="DYBoundry" },
            new ToggleItem{background=null,checkmark=null,lable="DoubleWhiteBoundry" },
            new ToggleItem{background=null,checkmark=null,lable="DoubleYellowBoundry" },
            new ToggleItem{background=null,checkmark=null,lable="CurbBoundry" },
            new ToggleItem{background=null,checkmark=null,lable="VirtualBoundry" },
        };

        LeftBoundryTypeToggles();
        BoundryLineTypeToggles();
    }

    private void Start()
    {
        m_createMapHolderBtn.onClick.AddListener(CreateMapHolder);
        m_noneTog.onValueChanged.AddListener(None);
        m_laneOrLineTog.onValueChanged.AddListener(LaneOrLine);

        m_waypointBtn.onClick.AddListener(CreateTempWaypoint);
        m_strainghtConnectBtn.onClick.AddListener(CreateStraight);

        m_LaneTog.onValueChanged.AddListener(Lane);
        m_LineStopTog.onValueChanged.AddListener(LineStop);
        m_LineBoundryTog.onValueChanged.AddListener(LineBoundry);

        m_ExportHDMapBtn.onClick.AddListener(ExportHDMap);
    }

    #region CreateMapHolder
    public Button m_createMapHolderBtn;
    public void CreateMapHolder()
    {
        var tempGO = new GameObject("Map" + SceneManager.GetActiveScene().name);
        tempGO.transform.position = Vector3.zero;
        tempGO.transform.rotation = Quaternion.identity;
        tempGO.AddComponent<ExposeToEditor>();
        mapHolder = tempGO.AddComponent<MapHolder>();
        var trafficLanes = new GameObject("TrafficLanes").transform;
        trafficLanes.gameObject.AddComponent<ExposeToEditor>();
        trafficLanes.SetParent(tempGO.transform);

        mapHolder.trafficLanesHolder = trafficLanes;
        var intersections = new GameObject("Intersections").transform;
        intersections.gameObject.AddComponent<ExposeToEditor>();
        intersections.SetParent(tempGO.transform);

        mapHolder.intersectionsHolder = intersections;
    }
    #endregion

    #region Create Modes
    public ToggleGroup m_createModesTogGroup;
    public Toggle m_noneTog;
    public Toggle m_laneOrLineTog;
    public Toggle m_signalTog;

    public void None(bool isOn)
    {
        DrawSphere.createTargetWaypoint = false;
        if (DrawSphere.targetWaypointGO != null)
        {
            DrawSphere.targetWaypointGO.SetActive(false);
        }
    }

    public void LaneOrLine(bool isOn)
    {
        CreateTargetWaypoint();
    }

    public void Signal()
    {

    }
    #endregion

    public void CreateTargetWaypoint()
    {
        if (DrawSphere.targetWaypointGO == null)
        {
            DrawSphere.targetWaypointGO = Instantiate(Resources.Load("Sphere") as GameObject);
            DrawSphere.targetWaypointGO.name = "TARGET_WAYPOINT";
        }
        else
        {
            DrawSphere.targetWaypointGO.SetActive(true);
        }
        DrawSphere.createTargetWaypoint = true;
    }

    #region Create Lane/Line
    public Button m_waypointBtn;
    public Button m_strainghtConnectBtn;
    public Button m_curveConnectBtn;
    public Toggle m_LaneTog;
    public Toggle m_LineStopTog;
    public Toggle m_LineBoundryTog;
    public GameObject MapObjectTypeChange;

    private int createType;
    private List<GameObject> tempWaypoints = new List<GameObject>();
    public void CreateTempWaypoint()
    {
        GameObject tempWaypointGO = Instantiate(Resources.Load("TEMP_WAYPOINT") as GameObject);
        tempWaypointGO.transform.position = DrawSphere.targetWaypointGO.transform.position;
        tempWaypoints.Add(tempWaypointGO);
        Debug.Log(DrawSphere.targetWaypointGO.transform.position);
    }

    
    public void Lane(bool isOn)
    {
        createType = 0;
        int i = 0;
        for ( i = 0; i < MapObjectTypeChange.transform.childCount; i++)
        {
            if (i==0)
            {
                MapObjectTypeChange.transform.GetChild(i).gameObject.SetActive(true);
            }
            else
            {
                MapObjectTypeChange.transform.GetChild(i).gameObject.SetActive(false);
            }            
        }        

    }

    public void LineStop(bool isOn)
    {
        createType = 1;
        int i = 1;
        for (i = 0; i < MapObjectTypeChange.transform.childCount; i++)
        {
            if (i == 1)
            {
                MapObjectTypeChange.transform.GetChild(i).gameObject.SetActive(true);
            }
            else
            {
                MapObjectTypeChange.transform.GetChild(i).gameObject.SetActive(false);
            }
        }
    }

    public void LineBoundry(bool isOn)
    {
        createType = 2;
        int i = 2;
        for (i = 0; i < MapObjectTypeChange.transform.childCount; i++)
        {
            if (i == 2)
            {
                MapObjectTypeChange.transform.GetChild(i).gameObject.SetActive(true);
            }
            else
            {
                MapObjectTypeChange.transform.GetChild(i).gameObject.SetActive(false);
            }
        }
    }

    public void CreateStraight()
    {
        tempWaypoints.RemoveAll(p => p == null);
        if (tempWaypoints.Count < 2)
        {
            return;
        }

        var newGo = new GameObject();
        switch (createType)
        {
            case 0:
                newGo.name = "MapLane";
                newGo.AddComponent<MapLane>();
                break;
            case 1:
                break;
            case 2:
                newGo.name = "MapLineBoundry";
                newGo.AddComponent<MapLine>();
                break;
            default:
                break;
        }

        Vector3 avePos = Vector3.Lerp(tempWaypoints[0].transform.position, tempWaypoints[tempWaypoints.Count - 1].transform.position, 0.5f);
        newGo.transform.position = avePos;
        var dir = (tempWaypoints[tempWaypoints.Count - 1].transform.position - tempWaypoints[0].transform.position).normalized;
        if (createType == 1)
        {

        }
        else
        {
            newGo.transform.rotation = Quaternion.LookRotation(dir);
        }

        List<Vector3> tempLocalPos = new List<Vector3>();
        if (tempWaypoints.Count == 2)
        {
            Debug.Log("Connect with Waypoint Count");
            float t = 0f;
            Vector3 position = Vector3.zero;
            Vector3 p0 = tempWaypoints[0].transform.position;
            Vector3 p1 = tempWaypoints[1].transform.position;
            for (int i = 0; i < waypointTotal; i++)
            {
                t = i / (waypointTotal - 1.0f);
                position = (1.0f - t) * p0 + t * p1;
                tempLocalPos.Add(position);
            }
        }
        else
        {
            for (int i = 0; i < tempWaypoints.Count; i++)
            {
                tempLocalPos.Add(tempWaypoints[i].transform.position);
            }
        }

        switch (createType)
        {
            case 0:
                var lane = newGo.GetComponent<MapLane>();
                foreach (var pos in tempLocalPos)
                {
                    lane.mapLocalPositions.Add(pos);
                }
                lane.DrawEvent();
                break;
            case 1:
                break;
            case 2:
                var bLine = newGo.GetComponent<MapLine>();
                foreach (var pos in tempLocalPos)
                {
                    bLine.mapLocalPositions.Add(pos);
                }
                newGo.GetComponent<MapLine>().lineType = (MapData.LineType)(boundryLineType - 1);
                break;
        }



        tempWaypoints.ForEach(p => DestroyImmediate(p.gameObject));
        tempWaypoints.Clear();

        newGo.AddComponent<ExposeToEditor>();
    }
    #endregion

    #region Export
    public Button m_ExportHDMapBtn;

    public void ExportHDMap()
    {
        //ApolloMapTool apolloMapTool = new ApolloMapTool(ApolloMapTool.ApolloVersion.Apollo_5_0);
        //apolloMapTool.Export("C:/Users/libomeng-v/Desktop/map.bin");


        OpenDriveMapExporter openDriveMapExporter = new OpenDriveMapExporter();
        openDriveMapExporter.Export("C:/Users/libomeng-v/Desktop/My_OpenDRIVE.xodr");
    }

    #endregion
}
