using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    private Vector3 vec = new Vector3(1,0,0);
    private Vector3 sp = new Vector3(0,0,0);
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Quaternion xBias = Quaternion.AngleAxis(10*Time.deltaTime, Vector3.left);
        Quaternion yBias = Quaternion.AngleAxis(10 * Time.deltaTime, Vector3.forward);
        vec = xBias * vec;
        vec = yBias * vec;
        Debug.DrawRay(sp, vec, Color.red);
    }
}
