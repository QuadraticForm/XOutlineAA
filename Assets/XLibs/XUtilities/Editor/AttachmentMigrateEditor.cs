# if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace XUtil
{
	[CustomEditor(typeof(AttachmentMigrate))]
	public class AttachmentMigrateEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			var attachmentMigrate = target as AttachmentMigrate;

			if (attachmentMigrate.source == null)
				return;

			if (GUILayout.Button("Migrate"))
			{
				AttachmentMigrate.Migrate(attachmentMigrate.source, attachmentMigrate.transform);
			}
		}
	}
}
#endif
