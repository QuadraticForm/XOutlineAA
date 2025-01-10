using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XUtil
{
    public class AttachmentMigrate : MonoBehaviour
    {
        public Transform source;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public static void Migrate(Transform source, Transform dest)
		{
            // find all child that: directly under source, and can't find child of the same name under dest

            List<Transform> childToMigrate = new List<Transform>();

            for (int ci = 0; ci < source.childCount; ++ci)
			{
                var child = source.GetChild(ci);
                if (dest.Find(child.name) == null)
				{
                    childToMigrate.Add(child);
                }
			}

            // migrate child found

            foreach (var child in childToMigrate)
			{
                child.SetParent(dest, false);
            }

            // traverse deeper down 

            for (int ci = 0; ci < source.childCount; ++ci)
            {
                var childSource = source.GetChild(ci);

                var childDest = dest.Find(childSource.name);
                if (childDest != null)
                    Migrate(childSource, childDest);
            }
        }
    }
}
