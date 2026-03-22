using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GASFarmDefense.UIToolkit.TranHuongDao
{
    public class TDCardItemUI
    {
        public VisualElement Root { get; private set; }


        private Vector2 _dragStartPos;
        private Vector2 _clickStartPos;
        private bool _isDragging = false;


        private VisualElement _originalParent;
        private int _originalIndex;
        private Vector2 _originalPosition;
        private IPointerEvent _lastPointerEvent;

        public event Action<TDCardItemUI, Vector2> OnCardPlayed;
        public event Action<TDCardItemUI> OnCardClicked;

        private Abel.TranHuongDao.Core.TowerDragDropManager _dragManager;

        public TDCardItemUI(VisualTreeAsset template, Abel.TranHuongDao.Core.TowerDragDropManager dragManager)
        {
            _dragManager = dragManager;
            Root = template.CloneTree().Q<VisualElement>(null, "card-item");


            Root.RegisterCallback<PointerDownEvent>(OnPointerDown);
            Root.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            Root.RegisterCallback<PointerUpEvent>(OnPointerUp);
            Root.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            Root.RegisterCallback<TransitionEndEvent>(OnTransitionEnd);
        }

        public void SetData(string name, string level, bool isGold = false)
        {
            Root.Q<Label>("card-name").text = name;
            Root.Q<Label>("card-lvl").text = level;

            // Initial states: gold has special border, but base class is standard

            if (isGold) Root.AddToClassList("gold");
            else Root.RemoveFromClassList("gold");
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0) return;

            _isDragging = true;
            _dragStartPos = evt.position;
            _clickStartPos = evt.position;


            _originalParent = Root.parent;
            _originalIndex = _originalParent.IndexOf(Root);
            _originalPosition = new Vector2(Root.layout.x, Root.layout.y);

            Root.CapturePointer(evt.pointerId);


            Root.style.position = Position.Absolute;
            Root.style.left = _originalPosition.x;
            Root.style.top = _originalPosition.y;
            Root.BringToFront();

            Root.style.transitionDuration = StyleKeyword.Null;

            // Visual state: Dragging

            Root.AddToClassList("dragging");

            // Notify Manager to show 3D preview

            _dragManager?.StartDragging();

            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_isDragging || !Root.HasPointerCapture(evt.pointerId)) return;

            Vector2 currentPos = evt.position;
            Vector2 delta = currentPos - _dragStartPos;
            _dragStartPos = currentPos;

            Root.style.left = Root.resolvedStyle.left + delta.x;
            Root.style.top = Root.resolvedStyle.top + delta.y;

            // Check placement validity for visual class
            if (_dragManager != null)
            {
                bool isValid = _dragManager.IsValidPlacement(currentPos, out _, out _);
                if (isValid)
                {
                    Root.AddToClassList("valid");
                    Root.RemoveFromClassList("invalid");
                }
                else
                {
                    Root.AddToClassList("invalid");
                    Root.RemoveFromClassList("valid");
                }
            }


            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!_isDragging || !Root.HasPointerCapture(evt.pointerId)) return;

            Root.ReleasePointer(evt.pointerId);
            EndDrag(evt.position);
            evt.StopPropagation();
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (_isDragging)
            {
                EndDrag(_dragStartPos);
            }
        }

        private void EndDrag(Vector2 endPos)
        {
            _isDragging = false;

            // Clean up visual classes

            Root.RemoveFromClassList("dragging");
            Root.RemoveFromClassList("valid");
            Root.RemoveFromClassList("invalid");

            // Check if we should spawn
            if (_dragManager != null && _dragManager.IsValidPlacement(endPos, out var gridPos, out var snappedPos))
            {
                // Spawn and destroy card
                string cardName = Root.Q<Label>("card-name").text;
                // Normalize ID if needed, e.g., Archer Tower -> ARCHER_TOWER
                string towerID = cardName.Replace(" ", "_").ToUpper();

                _dragManager.TryDropTower(gridPos, snappedPos, towerID);
                _dragManager.StopDragging();

                // Card disappears

                Root.RemoveFromHierarchy();
                OnCardPlayed?.Invoke(this, endPos);
            }
            else
            {
                // Cancel dragging preview
                _dragManager?.StopDragging();

                // Smooth return
                Root.style.transitionProperty = new StyleList<StylePropertyName>(new List<StylePropertyName> { "left", "top", "rotate", "scale", "translate" });
                Root.style.transitionDuration = new StyleList<TimeValue>(new List<TimeValue> { new TimeValue(0.2f) });


                if (Vector2.Distance(_clickStartPos, endPos) < 10f)
                {
                    OnCardClicked?.Invoke(this);
                }
                ResetToOriginal();
            }
        }

        private void ResetToOriginal()
        {
            // Move back to cached original position using transition
            Root.style.left = _originalPosition.x;
            Root.style.top = _originalPosition.y;

            // We wait for TransitionEndEvent to actually switch back to Relative

        }

        private void OnTransitionEnd(TransitionEndEvent evt)
        {
            if (_isDragging) return;

            // Important: only reset layout if we are actually at the original position
            // (Standard TransitionEnd fires for any property, we check if we're "home")
            if (Mathf.Approximately(Root.resolvedStyle.left, _originalPosition.x) &&

                Mathf.Approximately(Root.resolvedStyle.top, _originalPosition.y))
            {
                Root.style.position = Position.Relative;
                Root.style.left = StyleKeyword.Null;
                Root.style.top = StyleKeyword.Null;

                // Ensure it returns to its correct sibling order

                if (_originalParent != null && _originalParent.IndexOf(Root) != _originalIndex)
                {
                    _originalParent.Insert(_originalIndex, Root);
                }

                // Reset transition to USS defaults
                Root.style.transitionDuration = StyleKeyword.Null;
                Root.style.transitionProperty = StyleKeyword.Null;
            }
        }
    }
}
