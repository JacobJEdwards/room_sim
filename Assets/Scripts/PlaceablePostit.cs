// Scripts/PlaceablePostit.cs

using UnityEngine;
using Interfaces;
using Managers;
using System.Collections.Generic;

// This script goes on the PARENT object, which should have a BoxCollider.
[RequireComponent(typeof(Collider))]
public class DrawablePostIt : MonoBehaviour, IInteractable, IHasName
{
    [Header("Component References")]
    [SerializeField]
    [Tooltip("CRITICAL: Drag the MeshCollider from the CHILD object here.")]
    private Collider drawingCollider;

    [Header("Drawing Settings")] [SerializeField]
    private int textureSize = 512;

    [SerializeField] private Color paperColor = new Color(1f, 1f, 0.8f, 1f);
    [SerializeField] private Color penColor = Color.black;
    [SerializeField] private float penSize = 2f;

    [Header("Movement Settings")] [SerializeField]
    private float moveSmoothTime = 0.05f;

    [SerializeField] private float rotationSpeed = 100f;

    [Header("Placement Settings")] [SerializeField]
    private float wallDetectionDistance = 0.5f;

    [SerializeField] private LayerMask wallLayerMask = -1;

    [Header("Interaction Prompts")] [SerializeField]
    private string defaultPromptDesktop = "Click to move, Press E to draw";

    [SerializeField] private string stopDrawingPromptDesktop = "Drawing... (Press E to stop)";
    [SerializeField] private string movingPromptDesktop = "Moving... (Click to place)";

    public string Name => "Post-It";

    // --- Core Components ---
    private Renderer _renderer;
    private Camera _mainCamera;
    private Collider _physicsCollider;

    // --- Managers ---
    private UIManager _uiManager;

    private GameManager _gameManager;

    // --- ADDED ---
    private InteractionManager _interactionManager;

    // --- State Control ---
    private bool _isDrawing = false;
    private bool _isHeld = false;
    private bool _isMouseDownForDrawing = false;

    // --- Drawing Data ---
    private RenderTexture _drawingTexture;
    private Texture2D _penTexture;
    private Vector2 _lastDrawPosition;
    private readonly List<Vector2> _smoothingBuffer = new List<Vector2>();
    private const int SMOOTHING_SAMPLES = 3;

    // --- Movement Data ---
    private Vector3 _velocity = Vector3.zero;
    private float _heldDistance;

    private void Awake()
    {
        _physicsCollider = GetComponent<Collider>();
        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer == null) Debug.LogError("DrawablePostIt requires a Renderer on a child object.");
        if (drawingCollider == null)
            Debug.LogError("Please assign the child's MeshCollider to the 'Drawing Collider' field in the Inspector.");
        _mainCamera = Camera.main;
        InitializeTextures();
    }

    private void Start()
    {
        _uiManager = UIManager.Instance;
        _gameManager = GameManager.Instance;
        // --- ADDED ---
        _interactionManager = FindObjectOfType<InteractionManager>();
    }


    private void OnMouseDown()
    {
        if (GameManager.IsMobilePlatform) return;

        if (_isDrawing) return;

        _isHeld = !_isHeld;

        if (_isHeld)
        {
            if (_interactionManager) _interactionManager.LockInteractable(this);

            float distance = Vector3.Distance(_mainCamera.transform.position, transform.position);
            _heldDistance = (distance < 0.5f || distance > 10f) ? 2f : distance;
            _velocity = Vector3.zero;

            if (_uiManager) _uiManager.SetHint(movingPromptDesktop);
        }
        else
        {
            PlacePostIt();

            if (_interactionManager) _interactionManager.UnlockInteractable();

            if (_uiManager) _uiManager.ClearHint();
        }
    }

    public void ToggleMovement()
    {
        // Ignore if we are in drawing mode
        if (_isDrawing) return;

        // Toggle the _isHeld state
        _isHeld = !_isHeld;

        if (_isHeld)
        {
            // --- Just picked up ---
            // Lock the interaction manager to this object
            if (_interactionManager) _interactionManager.LockInteractable(this);

            // Use a reasonable default distance if the object is far away or just spawned
            float distance = Vector3.Distance(_mainCamera.transform.position, transform.position);
            _heldDistance = (distance < 0.5f || distance > 10f) ? 2f : distance;
            _velocity = Vector3.zero;

            if (_uiManager)
            {
                _uiManager.SetHint("Moving... (Tap Pickup button to place)");
                // *** Directly tell the UI to show the 'Drop' button ***
                _uiManager.SetHoldingUI(true);
            }
        }
        else
        {
            // --- Just placed down ---
            PlacePostIt();

            // Unlock the interaction manager
            if (_interactionManager) _interactionManager.UnlockInteractable();

            if (_uiManager)
            {
                _uiManager.ClearHint();
                // *** Directly tell the UI to hide the 'Drop' button ***
                _uiManager.SetHoldingUI(false);
            }
        }
    }

    private void Update()
    {
        if (_isHeld)
        {
            HandleHeldMovement();
            HandleRotation();
        }
        else if (_isDrawing)
        {
            HandleDrawingInput();
        }
    }

    public void OnInteract(GameObject interactor)
    {
        if (_isHeld) return;
        _isDrawing = !_isDrawing;
        if (_isDrawing) StartDrawing();
        else StopDrawing();
    }

    // --- MOVEMENT & PLACEMENT LOGIC ---
    private void PlacePostIt()
    {
        // This is called when we click to drop the object
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, wallDetectionDistance,
                wallLayerMask))
        {
            transform.position = hit.point + (hit.normal * 0.001f);
            transform.rotation = Quaternion.LookRotation(-hit.normal);
        }
    }

    private void HandleHeldMovement()
    {
        // Use viewport center for mobile, mouse position for desktop
        Ray ray;
        if (Managers.GameManager.IsMobilePlatform)
        {
            ray = _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        }
        else
        {
            ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        }

        Vector3 targetPosition = ray.GetPoint(_heldDistance);
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _velocity, moveSmoothTime);

        if (Physics.Raycast(transform.position, _mainCamera.transform.forward, out RaycastHit hit,
                wallDetectionDistance, wallLayerMask))
        {
            var targetRotation = Quaternion.LookRotation(-hit.normal);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
        else
        {
            transform.rotation = _mainCamera.transform.rotation;
        }
    }

    private void HandleRotation()
    {
        // Only handle rotation on desktop
        if (Managers.GameManager.IsMobilePlatform) return;

        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            transform.Rotate(Vector3.forward, scrollInput * rotationSpeed * 10f, Space.Self);
        }
    }

    // --- DRAWING LOGIC (UNCHANGED) ---
    private void HandleDrawingInput()
    {
        // Only allow right-click clear on desktop
        if (!Managers.GameManager.IsMobilePlatform && Input.GetMouseButtonDown(1)) ClearDrawing();

        // Get the appropriate input position
        Ray ray;
        if (Managers.GameManager.IsMobilePlatform && Input.touchCount > 0)
        {
            // Use actual touch position for drawing
            Touch touch = Input.GetTouch(0);
            ray = _mainCamera.ScreenPointToRay(touch.position);
        }
        else
        {
            // Use mouse position for desktop
            ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        }

        _physicsCollider.enabled = false;
        bool hit = Physics.Raycast(ray, out RaycastHit hitInfo);
        _physicsCollider.enabled = true;

        if (hit && hitInfo.collider == drawingCollider)
        {
            var pixelPos = new Vector2(hitInfo.textureCoord.x * textureSize,
                (1f - hitInfo.textureCoord.y) * textureSize);

            _smoothingBuffer.Add(pixelPos);
            if (_smoothingBuffer.Count > SMOOTHING_SAMPLES) _smoothingBuffer.RemoveAt(0);

            Vector2 smoothedPos = Vector2.zero;
            foreach (var pos in _smoothingBuffer) smoothedPos += pos;
            smoothedPos /= _smoothingBuffer.Count;

            // Handle drawing input based on platform
            bool isDrawing = false;
            if (Managers.GameManager.IsMobilePlatform && Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                // Draw on any touch phase except Ended and Canceled
                isDrawing = (touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled);
            }
            else
            {
                isDrawing = Input.GetMouseButton(0);
            }

            if (isDrawing)
            {
                if (_isMouseDownForDrawing) DrawLine(_lastDrawPosition, smoothedPos);
                else DrawDot(smoothedPos);
                _lastDrawPosition = smoothedPos;
                _isMouseDownForDrawing = true;
            }
            else
            {
                _isMouseDownForDrawing = false;
                _smoothingBuffer.Clear();
            }
        }
        else
        {
            _isMouseDownForDrawing = false;
            _smoothingBuffer.Clear();
        }
    }

    #region Unchanged Code

    private void StartDrawing()
    {
        _isMouseDownForDrawing = false;
        _smoothingBuffer.Clear();
        if (_gameManager) _gameManager.SetMode(GameManager.ControlMode.Menu);
        // --- ADDED ---
        if (_interactionManager) _interactionManager.LockInteractable(this);

        // Only modify cursor on desktop
        if (!Managers.GameManager.IsMobilePlatform)
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }

        if (_uiManager)
        {
            if (Managers.GameManager.IsMobilePlatform)
                _uiManager.SetHint("Touch to draw (Tap Interact to stop)");
            else
                _uiManager.SetHint(stopDrawingPromptDesktop);
        }
    }

    private void StopDrawing()
    {
        if (_gameManager) _gameManager.SetMode(GameManager.ControlMode.Camera);
        // --- ADDED ---
        if (_interactionManager) _interactionManager.UnlockInteractable();

        // Only modify cursor on desktop
        if (!Managers.GameManager.IsMobilePlatform)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (_uiManager) _uiManager.ClearHint();
    }

    public bool CanInteract(GameObject interactor)
    {
        return !_isHeld;
    }

    public string GetInteractionPromptDesktop(GameObject interactor)
    {
        if (_isHeld) return movingPromptDesktop;
        return _isDrawing ? stopDrawingPromptDesktop : defaultPromptDesktop;
    }

    public string GetInteractionPromptMobile(GameObject interactor)
    {
        if (_isHeld) return "Moving... (Tap Pickup button to place)";
        return _isDrawing ? "Drawing... (Tap Interact to stop)" : "Tap Interact to draw";
    }

    public void ResetObject()
    {
        if (_isDrawing) StopDrawing();
        if (_isHeld) _isHeld = false;
        ClearDrawing();
    }

    private void InitializeTextures()
    {
        _drawingTexture = new RenderTexture(textureSize, textureSize, 0) { filterMode = FilterMode.Bilinear };
        ClearDrawing();
        _penTexture = CreatePenTexture((int)penSize * 4);
        if (_renderer && _renderer.material) _renderer.material.mainTexture = _drawingTexture;
    }

    private Texture2D CreatePenTexture(int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.SmoothStep(1, 0, distance / radius);
                texture.SetPixel(x, y, new Color(penColor.r, penColor.g, penColor.b, alpha));
            }
        }

        texture.Apply();
        return texture;
    }

    private void DrawLine(Vector2 from, Vector2 to)
    {
        int steps = Mathf.Max((int)(Vector2.Distance(from, to) / (penSize * 0.5f)), 1);
        for (int i = 0; i <= steps; i++)
        {
            DrawDot(Vector2.Lerp(from, to, i / (float)steps));
        }
    }

    private void DrawDot(Vector2 position)
    {
        RenderTexture.active = _drawingTexture;
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, textureSize, textureSize, 0);
        Graphics.DrawTexture(
            new Rect(position.x - _penTexture.width * 0.5f, position.y - _penTexture.height * 0.5f, _penTexture.width,
                _penTexture.height), _penTexture);
        GL.PopMatrix();
        RenderTexture.active = null;
    }

    private void ClearDrawing()
    {
        RenderTexture.active = _drawingTexture;
        GL.Clear(true, true, paperColor);
        RenderTexture.active = null;
    }

    private void OnDestroy()
    {
        if (_drawingTexture)
        {
            _drawingTexture.Release();
            Destroy(_drawingTexture);
        }

        if (_penTexture) Destroy(_penTexture);
    }

    #endregion
}