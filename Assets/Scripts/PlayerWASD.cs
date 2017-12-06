using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWASD : MonoBehaviour {

    void Update()
    {
        var x = Input.GetAxis("Horizontal") * Time.deltaTime * 5.0f;
        var z = Input.GetAxis("Vertical") * Time.deltaTime * 5.0f;
        //var y = Input.GetAxis("")
        //transform.Rotate(x, 0, 0);
        transform.Translate(x, 0, z);
    }
}
