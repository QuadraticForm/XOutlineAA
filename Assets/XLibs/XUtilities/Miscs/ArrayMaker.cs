using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrayMaker : MonoBehaviour
{
    public GameObject objectToCopy = null;

    public Vector3Int num = new Vector3Int(1,1,1);

    public Vector3 gap = new Vector3(2, 2, 2);

    public void Clear()
    {
        while (gameObject.transform.childCount > 0)
        {
            var child = gameObject.transform.GetChild(0).gameObject;
            child.transform.parent = null;
            Destroy(child);
        }
    }

    public void CreateArray()
    {
        if (gameObject == null)
            return;

        var arrayHead = new GameObject("ArrayHead");
        arrayHead.transform.parent = transform;

        for (int x = 0; x < num.x; ++x)
            for (int y = 0; y < num.y; ++y)
                for (int z = 0; z < num.z; ++z)
                {
                    var element = Instantiate(objectToCopy);
                    element.transform.parent = arrayHead.transform;

                    var pos = new Vector3(x, y, z);
                    pos.Scale(gap);

                    element.transform.localPosition = pos;
                }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        num.x = Math.Max(1, num.x);
        num.y = Math.Max(1, num.y);
        num.z = Math.Max(1, num.z);
    }
}
