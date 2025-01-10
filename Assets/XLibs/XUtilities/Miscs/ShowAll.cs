using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class ShowAll : MonoBehaviour
{
    public bool showAll = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (showAll)
		{
            showAll = false;

            var allGameObjects = GameObject.FindObjectsOfType<GameObject>();

            foreach (GameObject gameObj in allGameObjects)
            {
                gameObj.SetActive(true);
                gameObj.SetActiveRecursively(true);
                gameObj.active = true;
            }
        }
    }
}
