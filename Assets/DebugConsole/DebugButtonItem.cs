
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System;

namespace Debugger
{
    public class DebugButtonItem : MonoBehaviour
    {
        [SerializeField]
        private Button btn;
        [SerializeField]
        private Text text;
        
        public void SetBtn(string label, UnityAction callback)
        {
            text.text = label;
            btn.onClick.AddListener(callback);
        }
    }
}
