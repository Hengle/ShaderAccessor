using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace JiongXiaGu.ShaderTools
{

    public interface ISerializedPropertyDrawer
    {
        SerializedProperty SerializedProperty { get; }
        void OnGUI(int mask);
        int Extract();
    }

    public class SerializedPropertyDrawer
    {
        public IShaderFieldGroup Group { get; }
        public SerializedProperty SerializedProperty { get; }
        public List<ISerializedPropertyDrawer> Drawers { get; }

        public SerializedPropertyDrawer(IShaderFieldGroup group, SerializedProperty property)
        {
            if (group == null)
                throw new ArgumentNullException(nameof(group));
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            Group = group;
            SerializedProperty = property;
            Drawers = new List<ISerializedPropertyDrawer>(group.Children.Count);
            FindDrawers(Drawers, group, property);
        }

        public void FindDrawers(List<ISerializedPropertyDrawer> drawers, IShaderFieldGroup group, SerializedProperty property)
        {
            foreach (var child in group.Children)
            {
                if (child is ShaderField)
                {
                    var field = (ShaderField)child;
                    var target = property.FindPropertyRelative(field.ReflectiveField.Name);
                    if (target != null)
                    {
                        var drawer = new ShaderFieldDrawer(target, field);
                        drawers.Add(drawer);
                    }
                }
                else if (child is ShaderKeyword)
                {
                    var field = (ShaderKeyword)child;
                    var target = property.FindPropertyRelative(field.ReflectiveField.Name);
                    if (target != null)
                    {
                        var drawer = new KeywordDrawer(target, field);
                        drawers.Add(drawer);
                    }
                }
                else if (child is ShaderEnumKeyword)
                {
                    var field = (ShaderEnumKeyword)child;
                    var target = property.FindPropertyRelative(field.ReflectiveField.Name);
                    if (target != null)
                    {
                        var drawer = new EnumKeywordDrawer(target, field);
                        drawers.Add(drawer);
                    }
                }
                else if (child is ShaderFieldMark)
                {
                    var field = (ShaderFieldMark)child;
                    var target = property.FindPropertyRelative(field.ReflectiveField.Name);
                    if (target != null)
                    {
                        var drawer = new Drawer(target);
                        drawers.Add(drawer);
                    }
                }
                else if (child is ShaderCustomField)
                {
                    var field = (ShaderCustomField)child;
                    var target = property.FindPropertyRelative(field.ReflectiveField.Name);
                    if (target != null)
                    {
                        var drawer = new Drawer(target);
                        drawers.Add(drawer);
                    }
                }
                else if (child is ShaderFieldGroup)
                {
                    var field = (ShaderFieldGroup)child;
                    var target = property.FindPropertyRelative(field.ReflectiveField.Name);
                    if (target != null)
                    {
                        var drawer = new GroupDrawer(target, field);
                        drawers.Add(drawer);
                        FindDrawers(drawer.Drawers, field, target);
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        public void OnGUI(int mask)
        {
            SerializedProperty.isExpanded = EditorGUILayout.Foldout(SerializedProperty.isExpanded, SerializedProperty.displayName);

            if (SerializedProperty.isExpanded)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    foreach (var drawer in Drawers)
                    {
                        drawer.OnGUI(mask);
                    }
                }
            }
        }

        public int Collect()
        {
            var mask = 0;
            foreach (var drawer in Drawers)
            {
                mask |= drawer.Extract();
            }
            return mask;
        }

        public ISerializedPropertyDrawer Find(params string[] path)
        {
            var list = Drawers;
            ISerializedPropertyDrawer drawer = null;

            foreach (var name in path)
            {
                var index = list.FindIndex(item => item.SerializedProperty.name == name);
                if (index >= 0)
                {
                    var current = list[index];
                    if (current is GroupDrawer)
                    {
                        list = ((GroupDrawer)current).Drawers;
                        drawer = current;
                        continue;
                    }
                }
            }

            return drawer;
        }

        public bool ChangeDrawer(Func<ISerializedPropertyDrawer> getDrawer, params string[] path)
        {
            if (getDrawer == null)
                throw new ArgumentNullException(nameof(getDrawer));

            var list = Drawers;

            for (int i = 0; i < path.Length; i++)
            {
                var name = path[i];
                var index = list.FindIndex(item => item.SerializedProperty.name == name);
                if (index >= 0)
                {
                    if (i + 1 >= path.Length)
                    {
                        list[index] = getDrawer();
                        return true;
                    }

                    var current = list[index];
                    if (current is GroupDrawer)
                    {
                        list = ((GroupDrawer)current).Drawers;
                        continue;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            return false;
        }

        public class Drawer : ISerializedPropertyDrawer
        {
            public SerializedProperty SerializedProperty { get; }

            public Drawer(SerializedProperty serializedProperty)
            {
                SerializedProperty = serializedProperty;
            }

            public virtual void OnGUI(int mask)
            {
                EditorGUILayout.PropertyField(SerializedProperty, true);
            }

            public virtual int Extract()
            {
                return 0;
            }
        }

        public class GroupDrawer : Drawer
        {
            public ShaderFieldGroup Group { get; private set; }
            public List<ISerializedPropertyDrawer> Drawers { get; private set; }

            public GroupDrawer(SerializedProperty serializedProperty, ShaderFieldGroup group) : base(serializedProperty)
            {
                Group = group;
                Drawers = new List<ISerializedPropertyDrawer>(group.Children.Count);
            }

            public override void OnGUI(int mask)
            {
                if ((mask & Group.Mask) != 0)
                {
                    SerializedProperty.isExpanded = EditorGUILayout.Foldout(SerializedProperty.isExpanded, SerializedProperty.displayName);

                    if (SerializedProperty.isExpanded)
                    {
                        using (new EditorGUI.IndentLevelScope())
                        {
                            foreach (var drawer in Drawers)
                            {
                                drawer.OnGUI(mask);
                            }
                        }
                    }
                }
            }

            public override int Extract()
            {
                int mask = 0;
                foreach (var drawer in Drawers)
                {
                    mask |= drawer.Extract();
                }
                return mask;
            }
        }

        public class ShaderFieldDrawer : Drawer
        {
            public ShaderFieldBase ShaderField { get; }

            public ShaderFieldDrawer(SerializedProperty serializedProperty, ShaderFieldBase shaderField) : base(serializedProperty)
            {
                ShaderField = shaderField;
            }

            public override void OnGUI(int mask)
            {
                if ((mask & ShaderField.Mask) != 0)
                {
                    base.OnGUI(mask);
                }
            }
        }

        public class KeywordDrawer : Drawer
        {
            public ShaderKeyword ShaderKeyword { get; }

            public KeywordDrawer(SerializedProperty serializedProperty, ShaderKeyword shaderKeyword) : base(serializedProperty)
            {
                ShaderKeyword = shaderKeyword;
            }

            public override int Extract()
            {
                return SerializedProperty.boolValue ? ShaderKeyword.Mask : 0;
            }
        }

        public class EnumKeywordDrawer : Drawer
        {
            public ShaderEnumKeyword ShaderEnumKeyword { get; }

            public EnumKeywordDrawer(SerializedProperty serializedProperty, ShaderEnumKeyword field) : base(serializedProperty)
            {
                ShaderEnumKeyword = field;
            }

            public override int Extract()
            {
                return ShaderEnumKeyword.Mask;
            }
        }
    }
}
