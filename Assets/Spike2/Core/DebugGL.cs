using UnityEngine;
using UnityEngine.Rendering;

namespace Spike2.Core
{
    /// <summary>Line helpers handed to gizmo callbacks (DebugRegistry.AddGizmo). Works in builds (GL lines).</summary>
    public class DebugGL
    {
        internal DebugGL() { }

        public void Line(Vector3 a, Vector3 b, Color c)
        {
            GL.Color(c);
            GL.Vertex(a);
            GL.Vertex(b);
        }

        public void Circle(Vector3 center, float radius, Color c, int segments = 40)
        {
            Vector3 prev = center + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                var p = center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
                Line(prev, p, c);
                prev = p;
            }
        }

        /// <summary>Flat vision cone: apex, forward direction, full angle in degrees, range.</summary>
        public void Cone(Vector3 apex, Vector3 forward, float angleDeg, float range, Color c, int segments = 16)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 1e-4f) return;
            forward.Normalize();
            float half = angleDeg / 2f;
            Vector3 prev = apex + Quaternion.Euler(0f, -half, 0f) * forward * range;
            Line(apex, prev, c);
            for (int i = 1; i <= segments; i++)
            {
                var p = apex + Quaternion.Euler(0f, -half + angleDeg * i / segments, 0f) * forward * range;
                Line(prev, p, c);
                prev = p;
            }
            Line(apex, prev, c);
        }

        public void Cross(Vector3 p, float size, Color c)
        {
            Line(p - Vector3.right * size, p + Vector3.right * size, c);
            Line(p - Vector3.forward * size, p + Vector3.forward * size, c);
            Line(p, p + Vector3.up * size * 2f, c);
        }
    }

    /// <summary>Renders the visible DebugRegistry gizmos on the main camera. Created by SystemsBootstrap.</summary>
    public class DebugGizmoRenderer : MonoBehaviour
    {
        public Material lineMaterial;
        readonly DebugGL gl = new DebugGL();

        void OnEnable() => RenderPipelineManager.endCameraRendering += OnEndCamera;
        void OnDisable() => RenderPipelineManager.endCameraRendering -= OnEndCamera;

        void EnsureMaterial()
        {
            if (lineMaterial != null) return;
            var sh = Shader.Find("Hidden/Internal-Colored");
            if (sh == null) return;
            lineMaterial = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };
            lineMaterial.SetInt("_ZTest", (int)CompareFunction.Always);
        }

        void OnEndCamera(ScriptableRenderContext ctx, Camera cam)
        {
            if (cam.cameraType != CameraType.Game || !cam.CompareTag("MainCamera")) return;
            bool any = false;
            foreach (var _ in DebugRegistry.Gizmos()) { any = true; break; }
            if (!any) return;
            EnsureMaterial();
            if (lineMaterial == null) return;

            GL.PushMatrix();
            lineMaterial.SetPass(0);
            GL.LoadProjectionMatrix(GL.GetGPUProjectionMatrix(cam.projectionMatrix, false));
            GL.modelview = cam.worldToCameraMatrix;
            GL.Begin(GL.LINES);
            foreach (var e in DebugRegistry.Gizmos())
            {
                try { e.drawGizmo(gl); }
                catch (System.Exception ex) { Debug.LogException(ex); e.visible = false; }
            }
            GL.End();
            GL.PopMatrix();
        }
    }
}
