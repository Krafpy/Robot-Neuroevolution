using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {

    public GameObject defaultTarget;
    public GameObject Target;
    public int MaxCamSize;
    public int MinCamSize;
    [HideInInspector] public int CamSize;
    public float smoothing;
    public float zoomLerp;

    void Update () {
        if(Target == null){
            Target = defaultTarget;
            transform.position = Target.transform.position;
        }
        //print(Target);
        transform.position = Vector3.Lerp(transform.position, new Vector3(Target.transform.position.x, Target.transform.position.y, -10), smoothing);
        //transform.position = new Vector3(Target.transform.position.x, Target.transform.position.y, -10);

        if (Input.GetAxis("Mouse ScrollWheel") * 100f > 0 && CamSize > MinCamSize) // forward
        {
            CamSize--;
        }
        if (Input.GetAxis("Mouse ScrollWheel") * 100f < 0 && CamSize < MaxCamSize) // backwards
        {
            CamSize++;
        }
        gameObject.GetComponent<Camera>().orthographicSize = Mathf.Lerp(gameObject.GetComponent<Camera>().orthographicSize, CamSize, zoomLerp);
    }
}