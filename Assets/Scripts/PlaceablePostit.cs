using UnityEngine;
using Interfaces;
using Managers;
using System.Collections.Generic;

// This script goes on the PARENT object, which should have a BoxCollider.
[RequireComponent(typeof(Collider))]
public class DrawablePostIt : MonoBehaviour, IInteractable
{
    [Header("Component References")]
    [SerializeField]
    [Tooltip("CRITICAL: Drag the MeshCollider from the CHILD object here.")]
    private Collider drawingCollider; // Assign the child's MeshCollider here

    [Header("Drawing Settings")]
    [SerializeField] private int textureSize = 512;
    [SerializeField] private Color paperColor = new Color(1f, 1f, 0.8f, 1f);
    [SerializeField] private Color penColor = Color.black;
    [SerializeField] private float penSize = 2f;

    [Header("Movement Settings")]
    [SerializeField] private float moveSmoothTime = 0.05f;
    [SerializeField] private float rotationSpeed = 100f;

    [Header("Interaction Prompts")]
    [SerializeField] private string defaultPromptDesktop = "Click to move, Press E to draw";
    [SerializeField] private string stopDrawingPromptDesktop = "Drawing... (Press E to stop)";

    // --- Core Components ---
    private Renderer _renderer;
    private Camera _mainCamera;
    private Collider _physicsCollider; // The BoxCollider on this parent object

    // --- Managers ---
    private UIManager _uiManager;
    private GameManager _gameManager;

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
        if (drawingCollider == null) Debug.LogError("Please assign the child's MeshCollider to the 'Drawing Collider' field in the Inspector.");
        _mainCamera = Camera.main;
        InitializeTextures();
    }

    private void Start()
    {
        _uiManager = UIManager.Instance;
        _gameManager = GameManager.Instance;
    }

    private void OnMouseDown()
    {
        if (!_isDrawing)
        {
            _isHeld = true;
            _heldDistance = Vector3.Distance(_mainCamera.transform.position, transform.position);
            _velocity = Vector3.zero;
        }
    }

    private void OnMouseUp()
    {
        if (_isHeld)
        {
            _isHeld = false;
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

    private void HandleDrawingInput()
    {
        if (Input.GetMouseButtonDown(1)) ClearDrawing();

        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

        _physicsCollider.enabled = false;
        bool hit = Physics.Raycast(ray, out RaycastHit hitInfo);
        _physicsCollider.enabled = true;

        if (hit && hitInfo.collider == drawingCollider)
        {
            // --- THE FIX IS HERE ---
            // The Y-coordinate is now correctly inverted, just like the original script.
            var pixelPos = new Vector2(hitInfo.textureCoord.x * textureSize, (1f - hitInfo.textureCoord.y) * textureSize);
            // --- END OF FIX ---

            _smoothingBuffer.Add(pixelPos);
            if (_smoothingBuffer.Count > SMOOTHING_SAMPLES) _smoothingBuffer.RemoveAt(0);

            Vector2 smoothedPos = Vector2.zero;
            foreach (var pos in _smoothingBuffer) smoothedPos += pos;
            smoothedPos /= _smoothingBuffer.Count;

            if (Input.GetMouseButton(0))
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

    // --- The rest of the script is unchanged ---
    #region Unchanged Code
    private void HandleHeldMovement()
    {
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        Vector3 targetPosition = ray.GetPoint(_heldDistance);
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _velocity, moveSmoothTime);
    }

    private void HandleRotation()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            transform.Rotate(Vector3.forward, scrollInput * rotationSpeed * 10f, Space.Self);
        }
    }

    private void StartDrawing()
    {
        _isMouseDownForDrawing = false;
        _smoothingBuffer.Clear();
        if (_gameManager) _gameManager.SetMode(GameManager.ControlMode.Menu);
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
        if (_uiManager) _uiManager.SetHint(stopDrawingPromptDesktop);
    }

    private void StopDrawing()
    {
        if (_gameManager) _gameManager.SetMode(GameManager.ControlMode.Camera);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (_uiManager) _uiManager.ClearHint();
    }

    public bool CanInteract(GameObject interactor)
    {
        return !_isHeld;
    }

    public string GetInteractionPromptDesktop(GameObject interactor)
    {
        if (_isHeld) return "";
        return _isDrawing ? stopDrawingPromptDesktop : defaultPromptDesktop;
    }

    public string GetInteractionPromptMobile(GameObject interactor)
    {
        return GetInteractionPromptDesktop(interactor);
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
        Graphics.DrawTexture(new Rect(position.x - _penTexture.width * 0.5f, position.y - _penTexture.height * 0.5f, _penTexture.width, _penTexture.height), _penTexture);
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