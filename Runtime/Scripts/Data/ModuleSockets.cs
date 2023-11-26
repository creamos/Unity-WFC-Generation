using UnityEngine;

[SelectionBase]
public class ModuleSockets : MonoBehaviour
{
    public const float MODULE_SIZE = 3;

    [Min(.1f)]
    public float weight = 1;

#if NAUGHTY_ATTR
    [NaughtyAttributes.EnumFlags]
#endif
    public ModuleFlag flag = (ModuleFlag)(-1);

    [Socket(SocketAttribute.ESocketMode.Side)]
    public string xPrev, xNext;

    [Socket(SocketAttribute.ESocketMode.Vertical)]
    public string yPrev, yNext;

    [Socket(SocketAttribute.ESocketMode.Side)]
    public string zPrev, zNext;

#if UNITY_EDITOR
    private static bool _drawSockets = true;

#if NAUGHTY_ATTR
    [NaughtyAttributes.Button]
#endif
    private void ToggleSocketVisibility()
    {
        _drawSockets = !_drawSockets;
    }
    
    private void OnDrawGizmos ()
    {
        void ShowLabel (string socket, Vector3 offset, GUIStyle style)
        {
            SocketUtility.ParseSocketIndex(socket, out int id);
            Random.InitState(id);

            Color color = (SocketUtility.IsInvalid(socket) || SocketUtility.IsWildCard(socket)) ?
                    Color.red
                    : Random.ColorHSV(0,1, .5f,1, 1,1);
            style.normal.textColor = color;

            style.active = style.normal;
            style.hover = style.normal;
            style.focused = style.normal;

            if (SocketUtility.IsInvalid(socket) || SocketUtility.IsWildCard(socket))
                DrawInvalid(offset);

            else if (SocketUtility.IsSymmetric(socket))
                DrawSymmetry(color, offset);

            else if (SocketUtility.IsDirectional(socket) && SocketUtility.ParseSocketLastDigit(socket, out int angle))
                DrawQuarter(color, offset, angle);

            else if (SocketUtility.IsFlippable(socket))
                DrawFlip(color, offset, SocketUtility.IsFlipped(socket));

            var oldMatrix = UnityEditor.Handles.matrix;
            UnityEditor.Handles.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            
            UnityEditor.Handles.Label(MODULE_SIZE * .55f * offset,
                SocketUtility.IsWildCard(socket) ? "?" : socket, style);
            
            UnityEditor.Handles.matrix = oldMatrix;
        }

        void DrawInvalid (Vector3 offset)
        {
            Gizmos.color = Color.red;

            Vector3 normal = offset.normalized;
            float upwardDot = Vector3.Dot(Vector3.up, normal);
            float forwardDot = Vector3.Dot(Vector3.forward, normal);

            Quaternion rotation = Mathf.Abs(upwardDot) < .75f 
                    // If SIDE socket
                    ? Mathf.Abs(forwardDot) < .75f
                        ? Quaternion.Euler(0, 90, 0)
                        : Quaternion.Euler(0, 0, 0)
                    // If VERTICAL socket
                    : Quaternion.Euler(-90, 0, 0);

            var objectPosition = transform.position;
            var objectRotation = transform.rotation;
            
            var oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(objectPosition + objectRotation * (MODULE_SIZE / 2f * offset),
                objectRotation * rotation, new Vector3(900, 900, 1.0f));
            
            Gizmos.DrawFrustum(Vector3.zero, 90, .001f, 0, 1.0f);
            Gizmos.matrix = oldMatrix;
        }

        void DrawQuarter (Color color, Vector3 offset, int angle)
        {
            angle *= 90;
            Vector3 normal = offset.normalized;
            Vector3 right = Vector3.right;
            float dot = Vector3.Dot(Vector3.up, normal);
            if (Mathf.Abs(dot) < .75f)
                right = Vector3.up;
            else
                normal = Vector3.up;

            Vector3 from = Quaternion.AngleAxis(angle, normal) * right;
            Vector3 to = Quaternion.AngleAxis(90, normal) * from;

            Gizmos.color = color;
            
            var objectPosition = transform.position;
            var objectRotation = transform.rotation;
            var objectScale = transform.lossyScale;
            
            // Store default debug matrices
            var oldGizmosMatrix = Gizmos.matrix;
            var oldHandlesMatrix = UnityEditor.Handles.matrix;
                
            // Update debug matrices
            Gizmos.matrix = Matrix4x4.TRS(objectPosition,
                objectRotation, objectScale);
            UnityEditor.Handles.matrix = Gizmos.matrix;
            
            Gizmos.DrawLine(
                MODULE_SIZE / 2f * offset,
                MODULE_SIZE / 2f * offset + from / 2);
            Gizmos.DrawLine(
                MODULE_SIZE / 2f * offset,
                MODULE_SIZE / 2f * offset + to / 2);
            UnityEditor.Handles.DrawWireArc(
                MODULE_SIZE / 2f * offset, 
                normal, from, 90, MODULE_SIZE / 4f);
            
            // Reset debug matrices
            Gizmos.matrix = oldGizmosMatrix;
            UnityEditor.Handles.matrix = oldHandlesMatrix;
        }

        void DrawFlip (Color color, Vector3 offset, bool flipped)
        {
            var objectPosition = transform.position;
            var objectRotation = transform.rotation;
            var objectScale = transform.lossyScale;
            
            // Store default debug matrices
            var oldGizmosMatrix = Gizmos.matrix;
            var oldHandlesMatrix = UnityEditor.Handles.matrix;
                
            // Update debug matrices
            Gizmos.matrix = Matrix4x4.TRS(objectPosition,
                objectRotation, objectScale);
            UnityEditor.Handles.matrix = Gizmos.matrix;
            
            Vector3 normal = offset.normalized;
            float dot = Vector3.Dot(Vector3.up, normal);
            
            // If SIDE socket
            if (Mathf.Abs(dot) < .75f) {
                Vector3 from = Vector3.up;
                Vector3 to = -from;
            
                Gizmos.color = color;
                Gizmos.DrawLine(MODULE_SIZE / 2f * offset + from / 2, MODULE_SIZE / 2f * offset + to / 2);
                UnityEditor.Handles.DrawWireArc(MODULE_SIZE / 2f * offset, normal, flipped ? from : to, 180, MODULE_SIZE / 4f);
            }

            // If VERTICAL socket
            else
            {
                UnityEditor.Handles.color = color;
                UnityEditor.Handles.DrawWireCube(
                    MODULE_SIZE / 2f * offset,
                    new Vector3(flipped ? 1f : .5f, 0, !flipped ? 1f : .5f) * MODULE_SIZE * .8f);
            }
            
            // Reset debug matrices
            Gizmos.matrix = oldGizmosMatrix;
            UnityEditor.Handles.matrix = oldHandlesMatrix;
        }

        void DrawSymmetry (Color color, Vector3 offset)
        {
            Vector3 normal = offset.normalized;
            
            var objectPosition = transform.position;
            var objectRotation = transform.rotation;
            var objectScale = transform.lossyScale;
            
            // Store default debug matrices
            var oldGizmosMatrix = Gizmos.matrix;
            var oldHandlesMatrix = UnityEditor.Handles.matrix;
                
            // Update debug matrices
            UnityEditor.Handles.matrix = Matrix4x4.TRS(objectPosition,
                objectRotation, objectScale);
            
            UnityEditor.Handles.DrawWireDisc(
                MODULE_SIZE / 2f * offset,
                offset.normalized,
                MODULE_SIZE / 4f);
            
            // Reset debug matrices
            Gizmos.matrix = oldGizmosMatrix;
            UnityEditor.Handles.matrix = oldHandlesMatrix;
        }


        Gizmos.color = Color.yellow;
        var oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(MODULE_SIZE, MODULE_SIZE, MODULE_SIZE));
        Gizmos.matrix = oldMatrix;

        if (!_drawSockets) return;

        GUIStyle style = new GUIStyle("label") { alignment = TextAnchor.MiddleCenter, fontSize = 20 };

        UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.Less;

        ShowLabel(xPrev, -Vector3.right, style);
        ShowLabel(xNext, Vector3.right, style);
        ShowLabel(yPrev, -Vector3.up, style);
        ShowLabel(yNext, Vector3.up, style);
        ShowLabel(zPrev, -Vector3.forward, style);
        ShowLabel(zNext, Vector3.forward, style);

    }
#endif
}
