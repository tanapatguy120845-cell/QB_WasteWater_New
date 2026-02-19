using System;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

namespace PGroup
{
    public class DeviceDataController : MonoBehaviour
    {
        public DeviceData deviceData;

        private void Start()
        {
            deviceData = new DeviceData();
        }
    }
    [Serializable]
    public class DeviceData
    {
        public string id;
        public string category;
        public string type;
        public string name;
        public string preview_image;
        public bool is_active;
    }
}
