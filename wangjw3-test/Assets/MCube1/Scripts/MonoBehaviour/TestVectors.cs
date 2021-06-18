using System.Collections;
using System.Collections.Generic;

using UnityEngine;

[ExecuteInEditMode]
public class TestVectors : MonoBehaviour
{
    // Start is called before the first frame update
    private void Start()
    {
        Vector4 a = new Vector4(1, 2, 3, 4);
        print((Vector3)a);
    }

    // Update is called once per frame
    private void Update()
    {
    }
}