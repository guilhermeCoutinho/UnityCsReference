// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.EditorTools;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor
{
    [EditorTool("Edit Box Collider", typeof(BoxCollider))]
    class BoxPrimitiveColliderTool : PrimitiveColliderTool<BoxCollider>
    {
        readonly BoxBoundsHandle m_BoundsHandle = new BoxBoundsHandle();
        protected override PrimitiveBoundsHandle boundsHandle { get { return m_BoundsHandle; } }

        protected override void CopyColliderPropertiesToHandle(BoxCollider collider)
        {
            m_BoundsHandle.center = TransformColliderCenterToHandleSpace(collider.transform, collider.center);
            m_BoundsHandle.size = Vector3.Scale(collider.size, collider.transform.lossyScale);
        }

        protected override void CopyHandlePropertiesToCollider(BoxCollider collider)
        {
            collider.center = TransformHandleCenterToColliderSpace(collider.transform, m_BoundsHandle.center);
            Vector3 size = Vector3.Scale(m_BoundsHandle.size, InvertScaleVector(collider.transform.lossyScale));
            size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
            collider.size = size;
        }
    }

    [CustomEditor(typeof(BoxCollider))]
    [CanEditMultipleObjects]
    class BoxColliderEditor : Collider3DEditorBase
    {
        SerializedProperty m_Center;
        SerializedProperty m_Size;

        private static class Styles
        {
            public static readonly GUIContent sizeContent = EditorGUIUtility.TrTextContent("Size", "The size of the Collider in the X, Y, Z directions.");
        }

        public override void OnEnable()
        {
            base.OnEnable();

            m_Center = serializedObject.FindProperty("m_Center");
            m_Size = serializedObject.FindProperty("m_Size");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.EditorToolbarForTarget(EditorGUIUtility.TrTempContent("Edit Collider"), this);
            GUILayout.Space(5);
            EditorGUILayout.PropertyField(m_IsTrigger, BaseStyles.triggerContent);
            EditorGUILayout.PropertyField(m_Material, BaseStyles.materialContent);
            EditorGUILayout.PropertyField(m_Center, BaseStyles.centerContent);
            EditorGUILayout.PropertyField(m_Size, Styles.sizeContent);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
