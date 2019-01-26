/// Credit BinaryX
/// Sourced from - http://forum.unity3d.com/threads/scripts-useful-4-6-scripts-collection.264161/page-2#post-1945602
/// Updated by ddreaper - removed dependency on a custom ScrollRect script. Now implements drag interfaces and standard Scroll Rect.

using System;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace UnityEngine.UI.Extensions
{
    [RequireComponent(typeof(ScrollRect))]
    [AddComponentMenu("Layout/Extensions/Horizontal Scroll Snap")]
    public class HorizontalScrollSnap : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        private Transform _screensContainer;

        private int _screens = 1;
        private int _startingScreen = 1;

        private bool _fastSwipeTimer = false;
        private int _fastSwipeCounter = 0;
        private int _fastSwipeTarget = 30;


        private List<Vector3> _positions = new List<Vector3>();
		public List<Vector3> Positions { get { return _positions; } }
        private ScrollRect _scroll_rect;
        private Vector3 _lerp_target;
        private bool _lerp;

        private int _containerSize;

        [Tooltip("The gameobject that contains toggles which suggest pagination. (optional)")]
        public GameObject Pagination;

        [Tooltip("Button to go to the next page. (optional)")]
        public GameObject NextButton;
        [Tooltip("Button to go to the previous page. (optional)")]
        public GameObject PrevButton;

		public string NextPageAudioEvent;
		public string PrevPageAudioEvent;

        public Boolean UseFastSwipe = true;
        public int FastSwipeThreshold = 100;

        private bool _startDrag = true;
        private Vector3 _startPosition = new Vector3();
        private int _currentScreen = -1;

		[Tooltip("Let this component automatically layout the pages. Leave off if using a Layout component")]
		public bool UseAutoLayout = false;

		[Serializable]
		public class IntEvent : Events.UnityEvent<int> { }
		public IntEvent OnPageChanged;

        // Use this for initialization
        void Start()
        {
			if (_screensContainer == null) Init();
        }

		private void Init()
		{
			_scroll_rect = gameObject.GetComponent<ScrollRect>();
			_screensContainer = _scroll_rect.content;
			DistributePages();

			if (NextButton)
				NextButton.GetComponent<Button>().onClick.AddListener(() => { NextScreen(); });

			if (PrevButton)
				PrevButton.GetComponent<Button>().onClick.AddListener(() => { PreviousScreen(); });
		}

		void Update()
        {
            if (_lerp)
            {
                _screensContainer.localPosition = Vector3.Lerp(_screensContainer.localPosition, _lerp_target, 15f * Time.deltaTime);
                if (Vector3.Distance(_screensContainer.localPosition, _lerp_target) < 0.005f)
                {
                    _lerp = false;
                }

                //change the info bullets at the bottom of the screen. Just for visual effect
                if (Vector3.Distance(_screensContainer.localPosition, _lerp_target) < 10f)
                {
                    ChangeBulletsInfo(CurrentScreen());
                }
            }

            if (_fastSwipeTimer)
            {
                _fastSwipeCounter++;
            }

        }

        private bool fastSwipe = false; //to determine if a fast swipe was performed


        //Function for switching screens with buttons
        public void NextScreen()
        {
			if (CurrentScreen() < _screens - 1)
			{
				_lerp = true;
				_lerp_target = _positions[CurrentScreen() + 1];
				//if (!string.IsNullOrEmpty(NextPageAudioEvent)) GameManager.instance.PlaySoundEvent(NextPageAudioEvent);
				ChangeBulletsInfo(CurrentScreen() + 1);
			}
		}

        //Function for switching screens with buttons
        public void PreviousScreen()
        {
            if (CurrentScreen() > 0)
            {
                _lerp = true;
                _lerp_target = _positions[CurrentScreen() - 1];
				//if(!string.IsNullOrEmpty(PrevPageAudioEvent)) GameManager.instance.PlaySoundEvent(PrevPageAudioEvent);
				ChangeBulletsInfo(CurrentScreen() - 1);
            }
        }

		public void GoToScreen(int screenIndex, bool instant)
		{
			if (screenIndex < 0 || screenIndex >= _positions.Count) return;
			int currentIndex = CurrentScreen();
			if (currentIndex == screenIndex) return;

			Vector3 target = _positions[screenIndex];

			if (instant)
			{
				_lerp = false;
				_screensContainer.localPosition = target;
			}
			else
			{
				//if (screenIndex < currentIndex && !string.IsNullOrEmpty(PrevPageAudioEvent)) GameManager.instance.PlaySoundEvent(PrevPageAudioEvent);
				//if (screenIndex > currentIndex && !string.IsNullOrEmpty(NextPageAudioEvent)) GameManager.instance.PlaySoundEvent(NextPageAudioEvent);
				_lerp = true;
				_lerp_target = target;
			}
			ChangeBulletsInfo(screenIndex);
		}

        //Because the CurrentScreen function is not so reliable, these are the functions used for swipes
        private void NextScreenCommand()
        {
            if (_currentScreen < _screens - 1)
            {
                _lerp = true;
                _lerp_target = _positions[_currentScreen + 1];
				//if (!string.IsNullOrEmpty(NextPageAudioEvent)) GameManager.instance.PlaySoundEvent(NextPageAudioEvent);
				ChangeBulletsInfo(_currentScreen + 1);
            }
        }

        //Because the CurrentScreen function is not so reliable, these are the functions used for swipes
        private void PrevScreenCommand()
        {
            if (_currentScreen > 0)
            {
                _lerp = true;
                _lerp_target = _positions[_currentScreen - 1];
				//if (!string.IsNullOrEmpty(PrevPageAudioEvent)) GameManager.instance.PlaySoundEvent(PrevPageAudioEvent);
				ChangeBulletsInfo(_currentScreen - 1);
            }
        }


        //find the closest registered point to the releasing point
        private Vector3 FindClosestFrom(Vector3 start, System.Collections.Generic.List<Vector3> positions)
        {
            Vector3 closest = Vector3.zero;
            float distance = Mathf.Infinity;

            foreach (Vector3 position in _positions)
            {
                if (Vector3.Distance(start, position) < distance)
                {
                    distance = Vector3.Distance(start, position);
                    closest = position;
                }
            }

            return closest;
        }


        //returns the current screen that the is seeing
        public int CurrentScreen()
        {
            float absPoz = Math.Abs(_screensContainer.gameObject.GetComponent<RectTransform>().offsetMin.x);

            absPoz = Mathf.Clamp(absPoz, 1, _containerSize - 1);

            float calc = (absPoz / _containerSize) * _screens;

            return (int)calc;
        }

        //changes the bullets on the bottom of the page - pagination
        private void ChangeBulletsInfo(int currentScreen)
        {
			if (currentScreen == _currentScreen) return;
			if (Pagination)
			{
				for (int i = 0; i < Pagination.transform.childCount; i++)
				{
					Pagination.transform.GetChild(i).GetComponent<Toggle>().isOn = (currentScreen == i)
						? true
						: false;
				}
			}
			_currentScreen = currentScreen;
			OnPageChanged.Invoke(currentScreen);
        }

		//used for changing between screen resolutions
		public void DistributePages()
        {
			if (_screensContainer == null)
			{
				Init();	//Init also calls DistributePages, so this will still be run
				return;
			}

			if (UseAutoLayout)
			{
				int _offset = 0;
				int _step = Screen.width;
				int _dimension = 0;

				int currentXPosition = 0;

				for (int i = 0; i < _screensContainer.transform.childCount; i++)
				{
					RectTransform child = _screensContainer.transform.GetChild(i).gameObject.GetComponent<RectTransform>();
					currentXPosition = _offset + i * _step;
					child.anchoredPosition = new Vector2(currentXPosition, child.anchoredPosition.y);
					child.sizeDelta = new Vector2(gameObject.GetComponent<RectTransform>().rect.width, gameObject.GetComponent<RectTransform>().rect.height);
				}

				_dimension = currentXPosition + _offset * -1;

				_screensContainer.GetComponent<RectTransform>().offsetMax = new Vector2(_dimension, 0f);
			}

			_screens = _screensContainer.childCount;

			_lerp = false;

			_positions.Clear();

			if (_screens > 1)
			{
				for (int i = 0; i < _screens; ++i)
				{
					_scroll_rect.horizontalNormalizedPosition = (float)i / (float)(_screens - 1);
					_positions.Add(_screensContainer.localPosition);
				}
				_scroll_rect.horizontalNormalizedPosition = (float)(_startingScreen - 1) / (float)(_screens - 1);
			}


			_containerSize = (int)_screensContainer.gameObject.GetComponent<RectTransform>().offsetMax.x;

			ChangeBulletsInfo(CurrentScreen());
		}

        #region Interfaces
        public void OnBeginDrag(PointerEventData eventData)
        {
            _startPosition = _screensContainer.localPosition;
            _fastSwipeCounter = 0;
            _fastSwipeTimer = true;
            _currentScreen = CurrentScreen();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _startDrag = true;
            if (_scroll_rect.horizontal)
            {
                if (UseFastSwipe)
                {
                    fastSwipe = false;
                    _fastSwipeTimer = false;
                    if (_fastSwipeCounter <= _fastSwipeTarget)
                    {
                        if (Math.Abs(_startPosition.x - _screensContainer.localPosition.x) > FastSwipeThreshold)
                        {
                            fastSwipe = true;
                        }
                    }
                    if (fastSwipe)
                    {
                        if (_startPosition.x - _screensContainer.localPosition.x > 0)
                        {
                            NextScreenCommand();
                        }
                        else
                        {
                            PrevScreenCommand();
                        }
                    }
                    else
                    {
                        _lerp = true;
                        _lerp_target = FindClosestFrom(_screensContainer.localPosition, _positions);
                    }
                }
                else
                {
                    _lerp = true;
                    _lerp_target = FindClosestFrom(_screensContainer.localPosition, _positions);
                }
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            _lerp = false;
            if (_startDrag)
            {
                OnBeginDrag(eventData);
                _startDrag = false;
            }
        }
        #endregion
    }
}