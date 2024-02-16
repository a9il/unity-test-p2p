using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Debugger
{
    public class DebugConsole : MonoBehaviour
    {

        [SerializeField]
        private DraggableBtn debuggerBtn;
        [SerializeField]
        private Transform container;
        [SerializeField]
        private DebugButtonItem btnPrefab;
        [SerializeField]
        private GameObject logScrollView;
        [SerializeField]
        private TMP_InputField logText;
        [SerializeField]
        private ContentSizeFitter contentSizeFitter;

        private static DebugConsole _instance = null;
#if BYTEWARS_DEBUG
        private static bool _isEnabled = true;
#else
        private static bool isEnabled = false;
#endif

        internal void Start()
        {
            debuggerBtn.SetClickCallback(ClickDebugger);
            //AddButton("close", Close);
            //AddButton("destroy debugger", Destroy);
            //AddButton("toggle log", SwitchLogVisibility);
            //AddButton("clear log", ClearLog);
            if(_instance==null)
            {
                _instance = GetComponent<DebugConsole>();
            }
            
        }

        private void Destroy()
        {
            Destroy(gameObject);
        }

        private void Close()
        {
            container.gameObject.SetActive(false);
        }

        private void ClickDebugger()
        {
            container.gameObject.SetActive(true);
        }

        private void SwitchLogVisibility()
        {
            logScrollView.SetActive(!logScrollView.activeSelf);
            if(logScrollView.activeSelf)
            {
                _instance.StartCoroutine(waitOneFrame(() => { _instance.contentSizeFitter.enabled = true; }));
            }
        }

        private static void OnReceivedMsg(string logString, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Warning)
            {
                _instance.logText.text += logString + '\n';
                _instance.contentSizeFitter.enabled = false;
                _instance.StartCoroutine(waitOneFrame(() => { _instance.contentSizeFitter.enabled = true; }));
            }
        }

        private void ClearLog()
        {
            logText.text = "";
            contentSizeFitter.enabled = false;
            StartCoroutine(waitOneFrame(() => { _instance.contentSizeFitter.enabled = true; }));
        }

        public static void AddButton(string btnLabel, UnityAction callback)
        {
            if(!_isEnabled)return;
            DebugButtonItem localButton = Instantiate(_instance.btnPrefab, _instance.container, false);
            localButton.SetBtn(btnLabel, callback);
            localButton.name = btnLabel;
        }

        public static void AddButton(string btnLabel, UnityAction<string[]> callback, string[] parameters)
        {
            //show popup with list of parameter keys with ok button
            //
        }

        private static bool isInitialized;
        public static void Log(string text)
        {
            if (!isInitialized)
            {
                Application.logMessageReceived += OnReceivedMsg;
                isInitialized = true;
            }
            Debug.LogWarning(text);
            
        }

        static IEnumerator waitOneFrame(Action callback)
        {
            yield return new WaitForEndOfFrame();
            if(callback!=null)
            {
                callback();
            }
        }
    }
}
