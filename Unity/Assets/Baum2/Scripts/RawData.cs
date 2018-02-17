using System.Collections.Generic;
using UnityEngine;

namespace Baum2
{
    public class RawData : MonoBehaviour
    {
        public Dictionary<string, object> Info { get; private set; }

        public RawData()
        {
            Info = new Dictionary<string, object>();
        }
    }
}
