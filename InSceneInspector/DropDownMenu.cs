using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

namespace InSceneInspector
{
    public class DropDownMenu : MonoBehaviour
    {
        public Button triggerButton;
        public VerticalLayoutGroup menu;
        public RectTransform menuBounds;

        CanvasGroup menuCanvasGroup;
        LayoutElement menuLayoutElement;
        bool menuIgnoreLayout;

        // Use this for initialization
        void Start()
        {
            menuCanvasGroup = menuBounds.GetComponent<CanvasGroup>();
            menuLayoutElement = menuBounds.GetComponent<LayoutElement>();
            menuIgnoreLayout = menuLayoutElement.ignoreLayout;

            triggerButton.onClick.AddListener(delegate ()
            {
                ToggleMenu();
            });

            HideMenu();
        }

        public void AddDropDownList(DropDownList dropDownList)
        {
            dropDownList.transform.SetParent(menu.transform, false);
        }

        public void AddButton(Button button)
        {
            button.transform.SetParent(menu.transform, false);

            button.onClick.AddListener(delegate ()
            {
                HideMenu();
            });
        }

        public void ClearContent()
        {
            while (menu.transform.childCount > 0)
            {
                GameObject child = menu.transform.GetChild(0).gameObject;
                DestroyImmediate(child);
            }
        }

        public void MoveMenuToFront()
        {
            menuBounds.transform.SetAsLastSibling();
        }

        public void HideMenu() { Visable = false; }
        public void ShowMenu() { Visable = true; }
        public void ToggleMenu() { Visable = !Visable; }

        private bool Visable
        {
            set
            {
                if (value)
                {
                    menuCanvasGroup.alpha = 1;
                    menuCanvasGroup.interactable = true;
                    menuCanvasGroup.blocksRaycasts = true;
                    menuLayoutElement.ignoreLayout = menuIgnoreLayout;
                    MoveMenuToFront();
                }
                else
                {
                    menuCanvasGroup.alpha = 0;
                    menuCanvasGroup.interactable = false;
                    menuCanvasGroup.blocksRaycasts = false;
                    menuLayoutElement.ignoreLayout = true;
                }
            }

            get
            {
                return menuCanvasGroup.alpha > 0;
            }
        }
    }
}
