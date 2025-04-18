using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AuLacThanThu.Core;

namespace AuLacThanThu.Core
{
    /// <summary>
    /// Quản lý input từ người chơi (touch, swipe, tap)
    /// </summary>
    public class InputController : MonoBehaviour
    {
        #region Singleton
        private static InputController _instance;
        public static InputController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<InputController>();
                    
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("InputController");
                        _instance = obj.AddComponent<InputController>();
                    }
                }
                
                return _instance;
            }
        }
        #endregion
        
        #region Properties
        [Header("Touch Settings")]
        [SerializeField] private float tapThreshold = 0.2f; // Time in seconds to register as tap
        [SerializeField] private float swipeThreshold = 50f; // Min distance for swipe
        [SerializeField] private float holdThreshold = 0.5f; // Time to register as hold
        [SerializeField] private LayerMask touchableLayers = -1; // All layers by default
        
        // Touch state
        private Vector2 touchStartPosition;
        private Vector2 touchEndPosition;
        private float touchStartTime;
        private bool isTouching = false;
        private bool isHolding = false;
        private GameObject lastTouchedObject = null;
        private GameObject currentHoverObject = null;
        
        // Cached components
        private Camera mainCamera;
        #endregion
        
        #region Events
        // Touch events
        public delegate void TouchEventHandler(Vector2 position, GameObject hitObject);
        public event TouchEventHandler OnTap;
        public event TouchEventHandler OnDoubleTap;
        public event TouchEventHandler OnHoldStart;
        public event TouchEventHandler OnHoldEnd;
        public event TouchEventHandler OnDrag;
        
        // Swipe events
        public delegate void SwipeEventHandler(Vector2 direction, float magnitude);
        public event SwipeEventHandler OnSwipe;
        
        // Aim events
        public delegate void AimEventHandler(Vector2 startPosition, Vector2 currentPosition);
        public event AimEventHandler OnAimStart;
        public event AimEventHandler OnAiming;
        public event AimEventHandler OnAimEnd;
        #endregion
        
        #region Unity Lifecycle
        private void Awake()
        {
            // Ensure singleton
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Get main camera
            mainCamera = Camera.main;
        }
        
        private void Update()
        {
            // Only process input if game is in Playing state
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
                return;
            
            // Get current input based on platform
            ProcessInput();
        }
        
        private void OnEnable()
        {
            // Subscribe to events
        }
        
        private void OnDisable()
        {
            // Unsubscribe from events
        }
        #endregion
        
        #region Input Processing
        private void ProcessInput()
        {
            // Handle mouse input on desktop and touch input on mobile
            #if UNITY_EDITOR || UNITY_STANDALONE
            ProcessMouseInput();
            #else
            ProcessTouchInput();
            #endif
        }
        
        private void ProcessMouseInput()
        {
            Vector2 mousePosition = Input.mousePosition;
            
            // Mouse button down
            if (Input.GetMouseButtonDown(0))
            {
                touchStartPosition = mousePosition;
                touchStartTime = Time.time;
                isTouching = true;
                isHolding = false;
                
                // Raycast to find object under mouse
                GameObject hitObject = GetObjectUnderPosition(mousePosition);
                lastTouchedObject = hitObject;
                
                // Trigger aim start event
                OnAimStart?.Invoke(touchStartPosition, touchStartPosition);
            }
            
            // Mouse drag
            else if (Input.GetMouseButton(0) && isTouching)
            {
                float holdTime = Time.time - touchStartTime;
                
                // Check for hold
                if (!isHolding && holdTime >= holdThreshold)
                {
                    isHolding = true;
                    OnHoldStart?.Invoke(touchStartPosition, lastTouchedObject);
                }
                
                // Get object under current position
                GameObject currentObject = GetObjectUnderPosition(mousePosition);
                
                // Check for hover changes
                if (currentObject != currentHoverObject)
                {
                    // TODO: Handle hover state changes
                    currentHoverObject = currentObject;
                }
                
                // Trigger drag event
                OnDrag?.Invoke(mousePosition, currentObject);
                
                // Trigger aiming event
                OnAiming?.Invoke(touchStartPosition, mousePosition);
            }
            
            // Mouse button up
            else if (Input.GetMouseButtonUp(0) && isTouching)
            {
                touchEndPosition = mousePosition;
                float touchDuration = Time.time - touchStartTime;
                float touchDistance = Vector2.Distance(touchStartPosition, touchEndPosition);
                
                // Get object under touch end position
                GameObject hitObject = GetObjectUnderPosition(touchEndPosition);
                
                // Check for tap
                if (touchDuration < tapThreshold && touchDistance < swipeThreshold)
                {
                    OnTap?.Invoke(touchEndPosition, hitObject);
                }
                // Check for swipe
                else if (touchDistance >= swipeThreshold)
                {
                    Vector2 direction = (touchEndPosition - touchStartPosition).normalized;
                    OnSwipe?.Invoke(direction, touchDistance);
                }
                
                // End hold if needed
                if (isHolding)
                {
                    OnHoldEnd?.Invoke(touchEndPosition, hitObject);
                }
                
                // End aim
                OnAimEnd?.Invoke(touchStartPosition, touchEndPosition);
                
                // Reset touch state
                isTouching = false;
                isHolding = false;
                lastTouchedObject = null;
                currentHoverObject = null;
            }
        }
        
        private void ProcessTouchInput()
        {
            // No touch
            if (Input.touchCount == 0)
                return;
                
            Touch touch = Input.GetTouch(0);
            Vector2 touchPosition = touch.position;
            
            // Touch began
            if (touch.phase == TouchPhase.Began)
            {
                touchStartPosition = touchPosition;
                touchStartTime = Time.time;
                isTouching = true;
                isHolding = false;
                
                // Raycast to find object under touch
                GameObject hitObject = GetObjectUnderPosition(touchPosition);
                lastTouchedObject = hitObject;
                
                // Trigger aim start event
                OnAimStart?.Invoke(touchStartPosition, touchStartPosition);
            }
            
            // Touch moved or stationary
            else if ((touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary) && isTouching)
            {
                float holdTime = Time.time - touchStartTime;
                
                // Check for hold
                if (!isHolding && holdTime >= holdThreshold && touch.phase == TouchPhase.Stationary)
                {
                    isHolding = true;
                    OnHoldStart?.Invoke(touchStartPosition, lastTouchedObject);
                }
                
                // Get object under current position
                GameObject currentObject = GetObjectUnderPosition(touchPosition);
                
                // Check for hover changes
                if (currentObject != currentHoverObject)
                {
                    // TODO: Handle hover state changes
                    currentHoverObject = currentObject;
                }
                
                // Trigger drag event if moving
                if (touch.phase == TouchPhase.Moved)
                {
                    OnDrag?.Invoke(touchPosition, currentObject);
                }
                
                // Trigger aiming event
                OnAiming?.Invoke(touchStartPosition, touchPosition);
            }
            
            // Touch ended or canceled
            else if ((touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) && isTouching)
            {
                touchEndPosition = touchPosition;
                float touchDuration = Time.time - touchStartTime;
                float touchDistance = Vector2.Distance(touchStartPosition, touchEndPosition);
                
                // Get object under touch end position
                GameObject hitObject = GetObjectUnderPosition(touchEndPosition);
                
                // Check for tap
                if (touchDuration < tapThreshold && touchDistance < swipeThreshold)
                {
                    OnTap?.Invoke(touchEndPosition, hitObject);
                }
                // Check for swipe
                else if (touchDistance >= swipeThreshold)
                {
                    Vector2 direction = (touchEndPosition - touchStartPosition).normalized;
                    OnSwipe?.Invoke(direction, touchDistance);
                }
                
                // End hold if needed
                if (isHolding)
                {
                    OnHoldEnd?.Invoke(touchEndPosition, hitObject);
                }
                
                // End aim
                OnAimEnd?.Invoke(touchStartPosition, touchEndPosition);
                
                // Reset touch state
                isTouching = false;
                isHolding = false;
                lastTouchedObject = null;
                currentHoverObject = null;
            }
        }
        
        private GameObject GetObjectUnderPosition(Vector2 screenPosition)
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                    return null;
            }
            
            // Convert screen position to ray
            Ray ray = mainCamera.ScreenPointToRay(screenPosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, Mathf.Infinity, touchableLayers);
            
            if (hit.collider != null)
            {
                return hit.collider.gameObject;
            }
            
            return null;
        }
        #endregion
        
        #region Public Methods
        public bool IsTouching()
        {
            return isTouching;
        }
        
        public bool IsHolding()
        {
            return isHolding;
        }
        
        public Vector2 GetTouchPosition()
        {
            #if UNITY_EDITOR || UNITY_STANDALONE
            return Input.mousePosition;
            #else
            if (Input.touchCount > 0)
                return Input.GetTouch(0).position;
            return Vector2.zero;
            #endif
        }
        
        public Vector2 GetTouchDelta()
        {
            #if UNITY_EDITOR || UNITY_STANDALONE
            return GetTouchPosition() - touchStartPosition;
            #else
            if (Input.touchCount > 0)
                return Input.GetTouch(0).deltaPosition;
            return Vector2.zero;
            #endif
        }
        
        public Vector2 GetWorldTouchPosition()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                    return Vector2.zero;
            }
            
            Vector2 screenPosition = GetTouchPosition();
            return mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 10f));
        }
        #endregion
    }
}