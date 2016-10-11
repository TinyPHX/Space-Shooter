using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace InSceneInspector
{
    public class ScrollRect2 : ScrollRect
    {
        private float previousScrollHeight;
        private float previousScrollPosition;
        private bool positionLocked;

        private float overrideScrollPostion;

        protected override void Start()
        {
            base.Start();

            previousScrollHeight = ScrollHeight;
            previousScrollPosition = ScrollPosition;
            positionLocked = false;

            //Action<string> loginSucceededEvent;

            onValueChanged.AddListener(delegate (Vector2 vector)
            {
                UpdatePosition();
            });
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();

            UpdatePosition();
        }

        private void UpdatePosition()
        {

            if (positionLocked && ScrollPosition != previousScrollPosition)
            {
                ScrollPosition = previousScrollPosition;

                positionLocked = false;
            }

            if (ScrollHeight != previousScrollHeight)
            {
                ScrollPosition = 1 - (previousScrollHeight * (1 - previousScrollPosition) / ScrollHeight);
            }

            previousScrollPosition = ScrollPosition;
            previousScrollHeight = ScrollHeight;
        }

        public override void OnInitializePotentialDrag(PointerEventData eventData)
        {
            positionLocked = true;
            base.OnInitializePotentialDrag(eventData);
        }

        public override void OnDrag(PointerEventData eventData)
        {
            positionLocked = false;
            base.OnDrag(eventData);
        }

        private float ScrollHeight
        {
            get { return content.rect.height - viewport.rect.height; }
        }

        private float ScrollPosition
        {
            set { verticalNormalizedPosition = value; }
            get { return verticalNormalizedPosition; }
        }
    }
}