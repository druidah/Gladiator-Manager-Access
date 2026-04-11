using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using MelonLoader;
using HarmonyLib;
using System.Text.RegularExpressions;
using DuloGames.UI;

namespace GladiatorManagerAccess
{
    public class PopupHandler : IAccessibleHandler
    {
        private static bool _isOpen = false;
        private static string _currentTitle = "";
        private static string _currentBody = "";
        private static Popup _currentPopup = null;
        private static UIModalBox _currentModal = null;

        public string GetHelpText()
        {
            if (_currentModal != null) return "Decision: Enter to Confirm, Escape to Cancel.";
            return "Popup: Press Enter or Space to dismiss.";
        }

        public void Update()
        {
            if (!_isOpen) return;

            // Handle Modal Box
            if (_currentModal != null)
            {
                if (!_currentModal.gameObject.activeInHierarchy || !_currentModal.isActive)
                {
                    OnClose();
                    return;
                }

                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    ConfirmModal();
                }
                else if (Input.GetKeyDown(KeyCode.Escape))
                {
                    CancelModal();
                }
                return;
            }

            // Handle simple Popup
            if (_currentPopup == null || !_currentPopup.gameObject.activeInHierarchy || !_currentPopup.Visible)
            {
                OnClose();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                Dismiss();
            }
        }

        private void ConfirmModal()
        {
            if (_currentModal != null)
            {
                // UIModalBox doesn't have a direct public Confirm() that is easy to call without side effects sometimes,
                // but we can invoke the onClick of the confirm button.
                var btn = Traverse.Create(_currentModal).Field("m_ConfirmButton").GetValue<Button>();
                if (btn != null) btn.onClick.Invoke();
                else _currentModal.onConfirm.Invoke();
            }
            OnClose();
        }

        private void CancelModal()
        {
            if (_currentModal != null)
            {
                var btn = Traverse.Create(_currentModal).Field("m_CancelButton").GetValue<Button>();
                if (btn != null) btn.onClick.Invoke();
                else _currentModal.onCancel.Invoke();
            }
            OnClose();
        }

        private void Dismiss()
        {
            if (_currentPopup != null)
            {
                var ui = ZUIManager.Instance;
                if (ui != null)
                {
                    ui.ClosePopup(_currentPopup);
                }
                else
                {
                    _currentPopup.ChangeVisibility(false);
                }
            }
            OnClose();
        }

        private void OnClose()
        {
            _isOpen = false;
            _currentPopup = null;
            _currentModal = null;
            AccessStateManager.Exit(AccessStateManager.State.Popup);
            ScreenReader.Say("Popup dismissed.");
        }

        public static void OnPopupOpen(Popup popup, string info, string title)
        {
            _currentPopup = popup;
            _currentModal = null;
            _currentTitle = StripFormatting(title);
            _currentBody = StripFormatting(info);
            _isOpen = true;

            AccessStateManager.TryEnter(AccessStateManager.State.Popup);
            
            string announcement = "";
            if (!string.IsNullOrEmpty(_currentTitle)) announcement += _currentTitle + ". ";
            if (!string.IsNullOrEmpty(_currentBody)) announcement += _currentBody;
            
            ScreenReader.Say(announcement);
        }

        public static void OnModalOpen(UIModalBox modal)
        {
            _currentModal = modal;
            _currentPopup = null;
            _isOpen = true;

            AccessStateManager.TryEnter(AccessStateManager.State.Popup);

            string text1 = StripFormatting(Traverse.Create(modal).Field("m_Text1").GetValue<string>());
            string text2 = StripFormatting(Traverse.Create(modal).Field("m_Text2").GetValue<string>());
            
            string announcement = "Decision required. ";
            if (!string.IsNullOrEmpty(text1)) announcement += text1 + ". ";
            if (!string.IsNullOrEmpty(text2)) announcement += text2;
            
            ScreenReader.Say(announcement);
        }

        private static string StripFormatting(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            string clean = Regex.Replace(input, "<.*?>", string.Empty);
            return clean.Replace("\n", " ").Trim();
        }

        [HarmonyPatch(typeof(ZUIManager), "OpenPopup", new System.Type[] { typeof(Popup), typeof(string), typeof(string) })]
        public static class OpenPopupPatch
        {
            public static void Postfix(Popup popup, string info, string title)
            {
                OnPopupOpen(popup, info, title);
            }
        }

        [HarmonyPatch(typeof(UIModalBox), "Show")]
        public static class ModalShowPatch
        {
            public static void Postfix(UIModalBox __instance)
            {
                OnModalOpen(__instance);
            }
        }
    }
}
