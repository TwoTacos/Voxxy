#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Voxxy {
    public class VoxImportSettings : ScriptableObject {

        public DefaultAsset VoxAsset = null;

        public float ScaleFactor = 0.125f;
        public float LastScaleFactor = 0f;

        public Vector3 Center = new Vector3(0.5f, 0.5f, 0.5f);
        public Vector3 LastCenter = new Vector3(0.5f, 0.5f, 0.5f);

        public int MaxPercent = 40;
        public int LastMaxPercent = 0;

        public bool Success;
        public string Message;

        public DateTime FileDate;

        public bool HaveChanged(FileInfo file) {
            return FileDate != file.LastWriteTimeUtc ||
                    Center != LastCenter ||
                    ScaleFactor != LastScaleFactor ||
                    MaxPercent != LastMaxPercent;
        }

    }

}

#endif
