using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class Mirror : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera reflectionCamera;
    [SerializeField] private RenderTexture renderTexture;
    [SerializeField] private GameObject target;

    private Camera _mainCamera;
    private Renderer _renderer;

    private void Start()
    {
        if (!reflectionCamera || !renderTexture)
        {
            Debug.LogError("Mirror: Assign reflectionCamera and renderTexture.");
            return;
        }

        _mainCamera = Camera.main;
        _renderer = target.GetComponent<Renderer>();

        reflectionCamera.targetTexture = renderTexture;
        _renderer.sharedMaterial.mainTexture = renderTexture;
    }

    private void LateUpdate()
    {
        if (!_mainCamera || !reflectionCamera)
            return;

        var mirror = target.transform;

        var mirrorNormal = mirror.forward;

        var camForward = -_mainCamera.transform.forward;
        camForward.y = 0;
        var reflectedForward = ReflectDirection(camForward, mirrorNormal);

        var camUp = _mainCamera.transform.up;
        var reflectedUp = ReflectDirection(camUp, mirrorNormal);

        reflectionCamera.transform.position = target.transform.position;
        reflectionCamera.transform.rotation = Quaternion.LookRotation(reflectedForward, reflectedUp);


        reflectionCamera.fieldOfView = _mainCamera.fieldOfView;
        reflectionCamera.aspect = _mainCamera.aspect;

        reflectionCamera.projectionMatrix = _mainCamera.projectionMatrix;
        GL.invertCulling = true;
        reflectionCamera.Render();
        GL.invertCulling = false;
    }

    private static Vector3 ReflectPoint(Vector3 point, Vector3 planePos, Vector3 planeNormal)
    {
        Vector3 toPoint = point - planePos;
        return point - 2 * Vector3.Dot(toPoint, planeNormal) * planeNormal;
    }

    private static Vector3 ReflectDirection(Vector3 dir, Vector3 planeNormal)
    {
        return Vector3.Reflect(dir, planeNormal);
    }
}
