using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawSphere : MonoBehaviour
{
    public static GameObject targetWaypointGO;
    public static bool createTargetWaypoint;
    private LayerMask layerMask;
    private Camera cam;

    private void OnEnable()
    {
        layerMask = 1 << LayerMask.NameToLayer("Default");
    }

    private void Start()
    {
        cam = transform.GetComponent<Camera>(); //Camera.main;
        //targetWaypointGO =Instantiate(Resources.Load("Sphere")as GameObject);        
    }

    private void CreateTargetWaypoint()
    {        
        if (cam == null) return;
        if (targetWaypointGO == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(ray, out hit, 1000.0f, layerMask.value))
        {
            targetWaypointGO.transform.position = hit.point;
        }
    }

    private void Update()
    {
        if (createTargetWaypoint)
        {
            CreateTargetWaypoint();
        }        
    }
}
