using UnityEngine;
using UnityEngine.Rendering;

namespace Circus
{
    /// <summary>Runtime gizmos with GL lines (works in builds): Zatan vision cone, noise radii, NavMesh route.</summary>
    public class DebugDraw : MonoBehaviour
    {
        [SerializeField] Material lineMaterial;
        [SerializeField] int circleSegments = 40;

        void OnEnable() => RenderPipelineManager.endCameraRendering += OnEndCamera;
        void OnDisable() => RenderPipelineManager.endCameraRendering -= OnEndCamera;

        void OnEndCamera(ScriptableRenderContext ctx, Camera cam)
        {
            if (!Tunables.I.showGizmos || lineMaterial == null) return;
            if (cam.cameraType != CameraType.Game || !cam.CompareTag("MainCamera")) return;

            GL.PushMatrix();
            lineMaterial.SetPass(0);
            GL.LoadProjectionMatrix(GL.GetGPUProjectionMatrix(cam.projectionMatrix, false));
            GL.modelview = cam.worldToCameraMatrix;
            GL.Begin(GL.LINES);

            foreach (var n in NoiseSystem.Recent)
            {
                float age = Time.time - n.time;
                if (age > NoiseSystem.GizmoLifetime) continue;
                var c = n.byZatan ? new Color(1f, 0.3f, 1f) : new Color(1f, 0.85f, 0.2f);
                c.a = 1f - age / NoiseSystem.GizmoLifetime;
                Circle(n.pos + Vector3.up * 0.1f, n.radius, c);
            }

            var brain = ZatanBrain.I;
            var perc = ZatanPerception.I;
            if (brain && perc)
            {
                var col = perc.CanSeePlayer ? Color.red : new Color(0.3f, 1f, 0.4f);
                var eye = perc.Eye.position;
                float half = Tunables.I.visionAngle * 0.5f;
                float range = perc.CurrentRange;
                Vector3 prev = eye;
                for (int i = 0; i <= 16; i++)
                {
                    float a = Mathf.Lerp(-half, half, i / 16f);
                    var p = eye + Quaternion.Euler(0, a, 0) * brain.transform.forward * range;
                    if (i == 0 || i == 16) Line(eye, p, col);
                    if (i > 0) Line(prev, p, col);
                    prev = p;
                }
                if (brain.Agent && brain.Agent.enabled && brain.Agent.hasPath)
                {
                    var corners = brain.Agent.path.corners;
                    for (int i = 1; i < corners.Length; i++) Line(corners[i - 1] + Vector3.up * 0.2f, corners[i] + Vector3.up * 0.2f, Color.cyan);
                }
            }

            GL.End();
            GL.PopMatrix();
        }

        void Circle(Vector3 c, float r, Color col)
        {
            for (int i = 0; i < circleSegments; i++)
            {
                float a0 = i * Mathf.PI * 2 / circleSegments, a1 = (i + 1) * Mathf.PI * 2 / circleSegments;
                Line(c + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * r, c + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * r, col);
            }
        }

        static void Line(Vector3 a, Vector3 b, Color c) { GL.Color(c); GL.Vertex(a); GL.Vertex(b); }
    }
}
